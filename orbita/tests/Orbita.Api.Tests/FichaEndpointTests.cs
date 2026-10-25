using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Orbita.Api.Tests;

public sealed class FichaEndpointTests : IClassFixture<OrbitaApiFactory>
{
    private readonly HttpClient client;

    public FichaEndpointTests(OrbitaApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Every_ficha_endpoint_requires_authentication()
    {
        var id = Guid.NewGuid();
        var input = new
        {
            titulo = "Ficha protegida",
            tipo = "Película",
            fecha = "2025-01-01",
            terminado = true,
            valoracion = 8.5m,
            comentarios = (string?)null,
            compania = (string?)null
        };

        using var listResponse = await client.GetAsync("/api/fichas");
        using var getResponse = await client.GetAsync($"/api/fichas/{id}");
        using var createResponse = await client.PostAsJsonAsync("/api/fichas", input);
        using var updateResponse = await client.PutAsJsonAsync($"/api/fichas/{id}", input);
        using var deleteResponse = await client.DeleteAsync($"/api/fichas/{id}");

        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_can_list_fichas()
    {
        await AuthenticateAsync();
        using var response = await client.GetAsync("/api/fichas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var fichas = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(fichas);
        Assert.Equal(JsonValueKind.Array, fichas.RootElement.ValueKind);
    }

    [Fact]
    public async Task Create_keeps_valuation_independent_from_completion()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Arrival",
            tipo = "Película",
            fecha = "2025-01-10",
            terminado = false,
            valoracion = 8.75m,
            comentarios = "Una película para volver a ver.",
            compania = "Solo"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.NotNull(ficha);
        Assert.Equal("Arrival", ficha.Titulo);
        Assert.Equal("Película", ficha.Tipo);
        Assert.Equal("2025-01-10", ficha.Fecha?.ToString("yyyy-MM-dd"));
        Assert.False(ficha.Terminado);
        Assert.Equal(8.75m, ficha.Valoracion);
        Assert.Equal("Una película para volver a ver.", ficha.Comentarios);
        Assert.Equal("Solo", ficha.Compania);
    }

    [Fact]
    public async Task Create_with_snapshot_persists_the_frozen_snapshot()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Dune",
            tipo = "Película",
            fecha = "2025-01-11",
            terminado = false,
            snapshot = new
            {
                fuente = "tmdb",
                idExterno = "438631",
                portada = "https://image.tmdb.org/t/p/w500/dune.jpg",
                anio = 2021,
                autoria = "Denis Villeneuve",
                notaExterna = 7.8m,
                generos = new[] { "Ciencia ficción", "Aventura" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.NotNull(ficha?.Snapshot);
        Assert.Equal("tmdb", ficha.Snapshot.Fuente);
        Assert.Equal("438631", ficha.Snapshot.IdExterno);
        Assert.Equal("https://image.tmdb.org/t/p/w500/dune.jpg", ficha.Snapshot.Portada);
        Assert.Equal(2021, ficha.Snapshot.Anio);
        Assert.Equal("Denis Villeneuve", ficha.Snapshot.Autoria);
        Assert.Equal(7.8m, ficha.Snapshot.NotaExterna);
        Assert.Equal(["Ciencia ficción", "Aventura"], ficha.Snapshot.Generos);
    }

    [Fact]
    public async Task Update_does_not_modify_the_frozen_snapshot()
    {
        await AuthenticateAsync();

        using var createResponse = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Dune",
            tipo = "Película",
            fecha = "2025-02-01",
            terminado = true,
            snapshot = new
            {
                fuente = "tmdb",
                idExterno = "438631",
                portada = (string?)null,
                anio = 2021,
                autoria = "Denis Villeneuve",
                notaExterna = (decimal?)null,
                generos = Array.Empty<string>()
            }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.NotNull(created?.Snapshot);

        using var updateResponse = await client.PutAsJsonAsync($"/api/fichas/{created.Id}", new
        {
            titulo = "Dune (reedición extendida)",
            tipo = "Película",
            fecha = "2025-02-02",
            terminado = true,
            valoracion = 9.0m,
            snapshot = new
            {
                fuente = "rawg",
                idExterno = "otro",
                portada = (string?)null,
                anio = 1999,
                autoria = (string?)null,
                notaExterna = (decimal?)null,
                generos = Array.Empty<string>()
            }
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("Dune (reedición extendida)", updated?.Titulo);
        Assert.NotNull(updated?.Snapshot);
        Assert.Equal("tmdb", updated.Snapshot.Fuente);
        Assert.Equal("438631", updated.Snapshot.IdExterno);

        using var getResponse = await client.GetAsync($"/api/fichas/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("tmdb", fetched?.Snapshot?.Fuente);
    }

    [Fact]
    public async Task Update_rejects_a_tipo_incompatible_with_the_immutable_snapshot()
    {
        await AuthenticateAsync();

        using var createResponse = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Dune",
            tipo = "Película",
            fecha = "2025-02-03",
            terminado = false,
            snapshot = new
            {
                fuente = "tmdb",
                idExterno = "438631",
                portada = (string?)null,
                anio = 2021,
                autoria = "Denis Villeneuve",
                notaExterna = (decimal?)null,
                generos = Array.Empty<string>()
            }
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.NotNull(created);

        using var updateResponse = await client.PutAsJsonAsync($"/api/fichas/{created.Id}", new
        {
            titulo = "Dune",
            tipo = "Libro",
            fecha = "2025-02-03",
            terminado = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        using var getResponse = await client.GetAsync($"/api/fichas/{created.Id}");
        var unchanged = await getResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("Película", unchanged?.Tipo);
        Assert.Equal("tmdb", unchanged?.Snapshot?.Fuente);
    }

    [Fact]
    public async Task Create_rejects_a_snapshot_from_a_source_incompatible_with_the_tipo()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Ficha con fuente cruzada",
            tipo = "Libro",
            fecha = "2025-02-04",
            terminado = false,
            snapshot = new
            {
                fuente = "tmdb",
                idExterno = "438631",
                portada = (string?)null,
                anio = 2021,
                autoria = (string?)null,
                notaExterna = (decimal?)null,
                generos = Array.Empty<string>()
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal("snapshot.fuente", problem!.RootElement.GetProperty("errors").EnumerateObject().First().Name);
    }

    [Fact]
    public async Task Create_without_snapshot_keeps_the_ficha_manual()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Ficha manual",
            tipo = "Cómic",
            fecha = "2025-02-05",
            terminado = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Null(ficha?.Snapshot);
    }

    [Theory]
    [InlineData("Película")]
    [InlineData("Serie")]
    [InlineData("Libro")]
    [InlineData("Juego")]
    [InlineData("Cómic")]
    public async Task Create_accepts_each_supported_tipo(string tipo)
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = $"Ficha de {tipo}",
            tipo,
            fecha = "2025-02-06",
            terminado = false
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal(tipo, ficha?.Tipo);
    }

    [Fact]
    public async Task User_can_read_update_and_delete_a_ficha()
    {
        await AuthenticateAsync();
        using var createResponse = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Arrival",
            tipo = "Película",
            fecha = "2025-03-14",
            terminado = true,
            valoracion = 9.25m,
            comentarios = (string?)null,
            compania = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.NotNull(created);

        using var listResponse = await client.GetAsync("/api/fichas");
        var listed = await listResponse.Content.ReadFromJsonAsync<FichaResponse[]>();
        Assert.Contains(listed ?? [], ficha => ficha.Id == created.Id);

        using var getResponse = await client.GetAsync($"/api/fichas/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("2025-03-14", fetched?.Fecha?.ToString("yyyy-MM-dd"));

        using var updateResponse = await client.PutAsJsonAsync($"/api/fichas/{created.Id}", new
        {
            titulo = "Arrival",
            tipo = "Película",
            fecha = "2025-03-15",
            terminado = true,
            valoracion = 9.5m,
            comentarios = "Relectura pendiente.",
            compania = "Georgia"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal(9.5m, updated?.Valoracion);
        Assert.Equal("Relectura pendiente.", updated?.Comentarios);
        Assert.Equal("Georgia", updated?.Compania);

        using var deleteResponse = await client.DeleteAsync($"/api/fichas/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        using var missingResponse = await client.GetAsync($"/api/fichas/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public async Task Create_rejects_valuation_outside_the_allowed_range(double valoracion)
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Ficha inválida",
            tipo = "Juego",
            fecha = "2025-02-07",
            terminado = false,
            valoracion
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_tipo_outside_the_glossary()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Ficha inválida",
            tipo = "Podcast",
            fecha = "2025-02-08",
            terminado = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_rejects_a_ficha_without_a_fecha(bool terminado)
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Ficha sin fecha",
            tipo = "Película",
            terminado
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_plataforma_outside_juego()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Película con plataforma",
            tipo = "Película",
            fecha = "2025-03-01",
            terminado = true,
            plataforma = "PC"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(
            "Plataforma",
            problem!.RootElement.GetProperty("errors").EnumerateObject().First().Name);
    }

    [Fact]
    public async Task Create_round_trips_autor_for_libro()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Dune",
            tipo = "Libro",
            fecha = "2025-03-04",
            terminado = true,
            autor = "Frank Herbert"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("Frank Herbert", ficha?.Autor);
    }

    [Fact]
    public async Task Create_rejects_autor_outside_libro()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Arrival",
            tipo = "Película",
            fecha = "2025-03-05",
            terminado = true,
            autor = "Alguien"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(
            "Autor",
            problem!.RootElement.GetProperty("errors").EnumerateObject().First().Name);
    }

    [Fact]
    public async Task Create_rejects_compania_for_libro()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Dune",
            tipo = "Libro",
            fecha = "2025-03-06",
            terminado = true,
            compania = "Solo"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(
            "Compania",
            problem!.RootElement.GetProperty("errors").EnumerateObject().First().Name);
    }

    [Fact]
    public async Task Create_rejects_idioma_outside_libro_and_comic()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Juego con idioma",
            tipo = "Juego",
            fecha = "2025-03-02",
            terminado = true,
            idioma = "es"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(
            "Idioma",
            problem!.RootElement.GetProperty("errors").EnumerateObject().First().Name);
    }

    [Fact]
    public async Task Create_and_update_round_trip_plataforma_for_juego()
    {
        await AuthenticateAsync();

        using var createResponse = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "God of War",
            tipo = "Juego",
            fecha = "2025-03-03",
            terminado = true,
            plataforma = "PS5"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("PS5", created?.Plataforma);

        using var updateResponse = await client.PutAsJsonAsync($"/api/fichas/{created!.Id}", new
        {
            titulo = "God of War",
            tipo = "Juego",
            fecha = "2025-03-04",
            terminado = true,
            plataforma = "PC"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("PC", updated?.Plataforma);

        using var getResponse = await client.GetAsync($"/api/fichas/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("PC", fetched?.Plataforma);
    }

    [Theory]
    [InlineData("Libro")]
    [InlineData("Cómic")]
    public async Task Create_round_trips_idioma_for_libro_and_comic(string tipo)
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = $"Lectura en español ({tipo})",
            tipo,
            fecha = "2025-03-05",
            terminado = true,
            idioma = "es"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("es", ficha?.Idioma);
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-user", "test-password"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
}

internal sealed record FichaResponse(
    Guid Id,
    string Titulo,
    string Tipo,
    DateOnly? Fecha,
    bool Terminado,
    decimal? Valoracion,
    string? Comentarios,
    string? Compania,
    string? Plataforma,
    string? Idioma,
    string? Autor,
    SnapshotResponse? Snapshot);

internal sealed record SnapshotResponse(
    string Fuente,
    string IdExterno,
    string? Portada,
    int? Anio,
    string? Autoria,
    decimal? NotaExterna,
    string[] Generos);

internal sealed record LoginRequest(string Username, string Password);

internal sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
