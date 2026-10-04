using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Orbita.Api.Tests;

public sealed class EnriquecimientoEndpointTests : IClassFixture<OrbitaApiFactory>
{
    private readonly HttpClient client;
    private readonly StubApisExternas apisExternas;

    public EnriquecimientoEndpointTests(OrbitaApiFactory factory)
    {
        apisExternas = factory.ApisExternas;
        apisExternas.Reiniciar();
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_for_pelicula_queries_tmdb_and_returns_resultados()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.themoviedb.org/3/search/movie",
            """
            {
              "results": [
                {
                  "id": 438631,
                  "title": "Dune",
                  "release_date": "2021-09-15",
                  "poster_path": "/dune.jpg",
                  "vote_average": 7.8
                }
              ]
            }
            """);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<ResultadoResponse>>();
        Assert.NotNull(resultados);
        var resultado = Assert.Single(resultados);
        Assert.Equal("438631", resultado.IdExterno);
        Assert.Equal("Dune", resultado.Titulo);
        Assert.Equal(2021, resultado.Anio);
        Assert.Equal("https://image.tmdb.org/t/p/w154/dune.jpg", resultado.Portada);
        Assert.Equal(7.8m, resultado.NotaExterna);

        var solicitud = Assert.Single(apisExternas.SolicitudesQueContienen("/3/search/movie"));
        Assert.Contains("query=Dune", solicitud);
        Assert.Contains("language=es-ES", solicitud);
    }

    [Fact]
    public async Task Search_for_serie_queries_the_tv_endpoint_and_maps_the_name()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.themoviedb.org/3/search/tv",
            """
            {
              "results": [
                {
                  "id": 94997,
                  "name": "La casa del dragón",
                  "first_air_date": "2022-08-21",
                  "poster_path": "/dragon.jpg",
                  "vote_average": 8.5
                }
              ]
            }
            """);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/resultados?q=casa+drag%C3%B3n&tipo=Serie");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<ResultadoResponse>>();
        Assert.NotNull(resultados);
        var resultado = Assert.Single(resultados);
        Assert.Equal("94997", resultado.IdExterno);
        Assert.Equal("La casa del dragón", resultado.Titulo);
        Assert.Equal(2022, resultado.Anio);
        Assert.Equal(8.5m, resultado.NotaExterna);

        Assert.Single(apisExternas.SolicitudesQueContienen("/3/search/tv"));
    }

    [Fact]
    public async Task Search_for_juego_queries_rawg_with_the_api_key()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.rawg.io/api/games",
            """
            {
              "results": [
                {
                  "id": 58175,
                  "name": "God of War",
                  "background_image": "https://media.rawg.io/kratos.jpg",
                  "released": "2018-04-20",
                  "metacritic": 94
                }
              ]
            }
            """);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/resultados?q=God+of+War&tipo=Juego");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<ResultadoResponse>>();
        Assert.NotNull(resultados);
        var resultado = Assert.Single(resultados);
        Assert.Equal("58175", resultado.IdExterno);
        Assert.Equal("God of War", resultado.Titulo);
        Assert.Equal(2018, resultado.Anio);
        Assert.Equal("https://media.rawg.io/kratos.jpg", resultado.Portada);
        Assert.Equal(94m, resultado.NotaExterna);

        var solicitud = Assert.Single(apisExternas.SolicitudesQueContienen("api.rawg.io/api/games"));
        Assert.Contains("search=God%20of%20War", solicitud);
        Assert.Contains("key=test-rawg-key", solicitud);
        Assert.Contains("page_size=20", solicitud);
    }

    [Theory]
    [InlineData("Libro")]
    [InlineData("Cómic")]
    public async Task Search_for_lecturas_queries_the_same_open_library_api(string tipo)
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "openlibrary.org/search.json",
            """
            {
              "docs": [
                {
                  "key": "/works/OL45804W",
                  "title": "El nombre del viento",
                  "cover_i": 8225417,
                  "first_publish_year": 2007
                }
              ]
            }
            """);

        using var response = await client.GetAsync(
            $"/api/enriquecimiento/resultados?q=nombre+viento&tipo={Uri.EscapeDataString(tipo)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<ResultadoResponse>>();
        Assert.NotNull(resultados);
        var resultado = Assert.Single(resultados);
        Assert.Equal("/works/OL45804W", resultado.IdExterno);
        Assert.Equal("El nombre del viento", resultado.Titulo);
        Assert.Equal(2007, resultado.Anio);
        Assert.Equal("https://covers.openlibrary.org/b/id/8225417-M.jpg", resultado.Portada);
        Assert.Null(resultado.NotaExterna);
    }

    [Fact]
    public async Task Search_requires_the_query_text()
    {
        await AuthenticateAsync();

        using var response = await client.GetAsync("/api/enriquecimiento/resultados?tipo=Pel%C3%ADcula");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_rejects_a_tipo_outside_the_glossary()
    {
        await AuthenticateAsync();

        using var response = await client.GetAsync("/api/enriquecimiento/resultados?q=dune&tipo=Podcast");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_returns_bad_gateway_when_the_external_api_is_down()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.themoviedb.org/3/search/movie",
            """{ "results": [] }""",
            HttpStatusCode.InternalServerError);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Search_requires_authentication()
    {
        using var response = await client.GetAsync(
            "/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task AuthenticateAsync()
    {
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("test-user", "test-password"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
}

internal sealed record ResultadoResponse(
    string IdExterno,
    string Titulo,
    int? Anio,
    string? Portada,
    decimal? NotaExterna);
