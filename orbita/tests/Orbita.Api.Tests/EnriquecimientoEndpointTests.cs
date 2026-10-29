using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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
    public async Task Search_returns_bad_gateway_when_the_external_connection_fails()
    {
        await AuthenticateAsync();
        apisExternas.Fallar(
            "api.themoviedb.org/3/search/movie",
            new HttpRequestException("No se pudo establecer la conexión."));

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

    [Fact]
    public async Task Snapshot_for_pelicula_freezes_tmdb_detail_with_the_director()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.themoviedb.org/3/movie/438631",
            """
            {
              "id": 438631,
              "title": "Dune",
              "release_date": "2021-09-15",
              "poster_path": "/dune.jpg",
              "vote_average": 7.8,
              "genres": [
                { "id": 878, "name": "Ciencia ficción" },
                { "id": 12, "name": "Aventura" }
              ],
              "runtime": 155,
              "production_countries": [
                { "iso_3166_1": "US", "name": "United States of America" },
                { "iso_3166_1": "CA", "name": "Canada" }
              ],
              "credits": {
                "cast": [
                  { "name": "Timothée Chalamet" },
                  { "name": "Rebecca Ferguson" },
                  { "name": "Zendaya" },
                  { "name": "Oscar Isaac" },
                  { "name": "Josh Brolin" }
                ],
                "crew": [
                  { "name": "Denis Villeneuve", "job": "Director" },
                  { "name": "Jon Spaihts", "job": "Screenplay" }
                ]
              }
            }
            """);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Pel%C3%ADcula&idExterno=438631");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal("tmdb", snapshot.Fuente);
        Assert.Equal("438631", snapshot.IdExterno);
        Assert.Equal("https://image.tmdb.org/t/p/w500/dune.jpg", snapshot.Portada);
        Assert.Equal(2021, snapshot.Anio);
        Assert.Equal("Denis Villeneuve", snapshot.Autoria);
        Assert.Equal(7.8m, snapshot.NotaExterna);
        Assert.Equal(["Ciencia ficción", "Aventura"], snapshot.Generos);

        var detalles = snapshot.Detalles!.Value;
        Assert.Equal("pelicula", detalles.GetProperty("tipo").GetString());
        Assert.Equal(
            ["Timothée Chalamet", "Rebecca Ferguson", "Zendaya", "Oscar Isaac"],
            detalles.GetProperty("actores").Deserialize<string[]>()!);
        Assert.Equal(["US", "CA"], detalles.GetProperty("paises").Deserialize<string[]>()!);
        Assert.Equal(155, detalles.GetProperty("duracionMinutos").GetInt32());

        var solicitud = Assert.Single(apisExternas.SolicitudesQueContienen("/3/movie/438631"));
        Assert.Contains("language=es-ES", solicitud);
        Assert.Contains("append_to_response=credits", solicitud);
    }

    [Fact]
    public async Task Snapshot_for_serie_uses_the_creator_from_the_tv_detail()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.themoviedb.org/3/tv/94997",
            """
            {
              "id": 94997,
              "name": "La casa del dragón",
              "first_air_date": "2022-08-21",
              "poster_path": null,
              "vote_average": 8.5,
              "genres": [
                { "id": 10765, "name": "Sci-Fi & Fantasy" }
              ],
              "created_by": [
                { "id": 19303, "name": "Ryan Condal" }
              ],
              "number_of_seasons": 2,
              "number_of_episodes": 18,
              "production_countries": [
                { "iso_3166_1": "US", "name": "United States of America" }
              ],
              "credits": {
                "cast": [
                  { "name": "Emma D'Arcy" },
                  { "name": "Matt Smith" }
                ]
              }
            }
            """);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Serie&idExterno=94997");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal("tmdb", snapshot.Fuente);
        Assert.Equal("94997", snapshot.IdExterno);
        Assert.Null(snapshot.Portada);
        Assert.Equal(2022, snapshot.Anio);
        Assert.Equal("Ryan Condal", snapshot.Autoria);
        Assert.Equal(["Sci-Fi & Fantasy"], snapshot.Generos);

        var detalles = snapshot.Detalles!.Value;
        Assert.Equal("serie", detalles.GetProperty("tipo").GetString());
        Assert.Equal(["Emma D'Arcy", "Matt Smith"], detalles.GetProperty("actores").Deserialize<string[]>()!);
        Assert.Equal(["US"], detalles.GetProperty("paises").Deserialize<string[]>()!);
        Assert.Equal(2, detalles.GetProperty("temporadas").GetInt32());
        Assert.Equal(18, detalles.GetProperty("episodios").GetInt32());

        var solicitud = Assert.Single(apisExternas.SolicitudesQueContienen("/3/tv/94997"));
        Assert.Contains("append_to_response=credits", solicitud);
    }

    [Fact]
    public async Task Snapshot_for_serie_without_extra_data_has_no_detalles()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.themoviedb.org/3/tv/1",
            """{ "id": 1, "name": "Obscura", "number_of_seasons": 0, "number_of_episodes": 0 }""");

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Serie&idExterno=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Null(snapshot.Detalles);
    }

    [Fact]
    public async Task Snapshot_for_juego_uses_the_rawg_detail_with_metacritic()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.rawg.io/api/games/58175",
            """
            {
              "id": 58175,
              "name": "God of War",
              "background_image": "https://media.rawg.io/kratos.jpg",
              "released": "2018-04-20",
              "metacritic": 94,
              "developers": [
                { "id": 159, "name": "Santa Monica Studio" }
              ],
              "genres": [
                { "id": 4, "name": "Action" }
              ],
              "playtime": 14,
              "platforms": [
                { "platform": { "id": 187, "name": "PlayStation 5" } },
                { "platform": { "id": 4, "name": "PC" } }
              ]
            }
            """);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Juego&idExterno=58175");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal("rawg", snapshot.Fuente);
        Assert.Equal("58175", snapshot.IdExterno);
        Assert.Equal("https://media.rawg.io/kratos.jpg", snapshot.Portada);
        Assert.Equal(2018, snapshot.Anio);
        Assert.Equal("Santa Monica Studio", snapshot.Autoria);
        Assert.Equal(94m, snapshot.NotaExterna);
        Assert.Equal(["Action"], snapshot.Generos);

        var detalles = snapshot.Detalles!.Value;
        Assert.Equal("juego", detalles.GetProperty("tipo").GetString());
        Assert.Equal(["PlayStation 5", "PC"], detalles.GetProperty("plataformas").Deserialize<string[]>()!);
        Assert.Equal(14, detalles.GetProperty("horasMedias").GetInt32());

        var solicitud = Assert.Single(apisExternas.SolicitudesQueContienen("api.rawg.io/api/games/58175"));
        Assert.Contains("key=test-rawg-key", solicitud);
    }

    [Fact]
    public async Task Snapshot_for_juego_without_platforms_or_playtime_has_no_detalles()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "api.rawg.io/api/games/1",
            """{ "id": 1, "name": "Obscuro", "playtime": 0, "platforms": [] }""");

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Juego&idExterno=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Null(snapshot.Detalles);
    }

    [Fact]
    public async Task Snapshot_for_libro_uses_open_library_and_caps_the_subjects()
    {
        await AuthenticateAsync();
        apisExternas.Responder(
            "openlibrary.org/works/OL45804W.json",
            """
            {
              "key": "/works/OL45804W",
              "first_publish_date": "2007",
              "covers": [8225417],
              "authors": [
                { "author": { "key": "/authors/OL23919A" } }
              ],
              "subjects": ["Fantasy", "Fiction", "Magic", "Kvothe", "Epic", "Extra"]
            }
            """);
        apisExternas.Responder(
            "openlibrary.org/authors/OL23919A.json",
            """{ "name": "Patrick Rothfuss" }""");
        apisExternas.Responder(
            "openlibrary.org/search.json",
            """{ "docs": [ { "key": "/works/OL45804W", "title": "El nombre del viento", "number_of_pages_median": 662 } ] }""");

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Libro&idExterno=%2Fworks%2FOL45804W");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal("openlibrary", snapshot.Fuente);
        Assert.Equal("/works/OL45804W", snapshot.IdExterno);
        Assert.Equal("https://covers.openlibrary.org/b/id/8225417-L.jpg", snapshot.Portada);
        Assert.Equal(2007, snapshot.Anio);
        Assert.Equal("Patrick Rothfuss", snapshot.Autoria);
        Assert.Null(snapshot.NotaExterna);
        Assert.Equal(["Fantasy", "Fiction", "Magic", "Kvothe", "Epic"], snapshot.Generos);

        Assert.Single(apisExternas.SolicitudesQueContienen("openlibrary.org/works/OL45804W.json"));
        Assert.Single(apisExternas.SolicitudesQueContienen("openlibrary.org/authors/OL23919A.json"));

        var detalles = snapshot.Detalles!.Value;
        Assert.Equal("libro", detalles.GetProperty("tipo").GetString());
        Assert.Equal(662, detalles.GetProperty("paginas").GetInt32());
        Assert.Contains(
            "number_of_pages_median",
            Assert.Single(apisExternas.SolicitudesQueContienen("openlibrary.org/search.json")));
    }

    [Fact]
    public async Task Snapshot_for_libro_without_pages_in_the_search_has_no_detalles()
    {
        await AuthenticateAsync();
        StubLibro(primeraPublicacion: "2007");
        apisExternas.Responder(
            "openlibrary.org/search.json",
            """{ "docs": [ { "key": "/works/OL45804W", "title": "El nombre del viento" } ] }""");

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=C%C3%B3mic&idExterno=%2Fworks%2FOL45804W");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal(2007, snapshot.Anio);
        Assert.Null(snapshot.Detalles);
    }

    [Fact]
    public async Task Snapshot_for_libro_survives_a_failed_search_when_the_year_is_known()
    {
        await AuthenticateAsync();
        StubLibro(primeraPublicacion: "2007");
        apisExternas.Responder("openlibrary.org/search.json", "{}", HttpStatusCode.BadGateway);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Libro&idExterno=%2Fworks%2FOL45804W");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var snapshot = await response.Content.ReadFromJsonAsync<SnapshotResponse>();
        Assert.NotNull(snapshot);
        Assert.Equal(2007, snapshot.Anio);
        Assert.Null(snapshot.Detalles);
    }

    [Fact]
    public async Task Snapshot_for_libro_fails_when_the_search_fails_and_the_year_is_unknown()
    {
        await AuthenticateAsync();
        StubLibro(primeraPublicacion: null);
        apisExternas.Responder("openlibrary.org/search.json", "{}", HttpStatusCode.BadGateway);

        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Libro&idExterno=%2Fworks%2FOL45804W");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private void StubLibro(string? primeraPublicacion)
    {
        var fecha = primeraPublicacion is null ? string.Empty : $"\"first_publish_date\": \"{primeraPublicacion}\",";
        apisExternas.Responder(
            "openlibrary.org/works/OL45804W.json",
            $$"""{ "key": "/works/OL45804W", {{fecha}} "subjects": [] }""");
    }

    [Fact]
    public async Task Snapshot_requires_authentication()
    {
        using var response = await client.GetAsync(
            "/api/enriquecimiento/snapshot?tipo=Pel%C3%ADcula&idExterno=438631");

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
