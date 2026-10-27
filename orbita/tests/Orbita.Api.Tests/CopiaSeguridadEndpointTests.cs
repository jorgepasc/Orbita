using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Orbita.Api.Tests;

public sealed class CopiaSeguridadEndpointTests : IClassFixture<OrbitaApiFactory>
{
    private readonly HttpClient client;

    public CopiaSeguridadEndpointTests(OrbitaApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Both_endpoints_require_authentication()
    {
        using var exportar = await client.GetAsync("/api/copia-seguridad");
        using var importar = await client.PostAsJsonAsync(
            "/api/copia-seguridad",
            new { version = 1, fichas = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Unauthorized, exportar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, importar.StatusCode);
    }

    [Fact]
    public async Task Export_returns_the_versioned_document_with_snapshots_as_a_download()
    {
        await AuthenticateAsync();
        var id = await CrearConSnapshotAsync("Export Dune");

        using var response = await client.GetAsync("/api/copia-seguridad");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("attachment; filename=orbita-", response.Content.Headers.ContentDisposition?.ToString());
        using var documento = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(documento);
        Assert.Equal(1, documento.RootElement.GetProperty("version").GetInt32());
        var ficha = documento.RootElement.GetProperty("fichas").EnumerateArray()
            .Single(value => value.GetProperty("id").GetGuid() == id);
        Assert.Equal("tmdb", ficha.GetProperty("snapshot").GetProperty("fuente").GetString());
    }

    [Fact]
    public async Task Restoring_an_export_recreates_deleted_fichas_with_the_same_id_and_snapshot()
    {
        await AuthenticateAsync();
        var id = await CrearConSnapshotAsync("Roundtrip Dune");
        var copia = await client.GetStringAsync("/api/copia-seguridad");
        (await client.DeleteAsync($"/api/fichas/{id}")).Dispose();

        using var response = await Importar(copia);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var restaurada = await client.GetAsync($"/api/fichas/{id}");
        Assert.Equal(HttpStatusCode.OK, restaurada.StatusCode);
        var ficha = await restaurada.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.Equal("Roundtrip Dune", ficha?.Titulo);
        Assert.Equal("438631", ficha?.Snapshot?.IdExterno);
    }

    [Fact]
    public async Task Restoring_the_same_file_twice_does_not_duplicate_fichas()
    {
        await AuthenticateAsync();
        await CrearConSnapshotAsync("Idempotente");
        var copia = await client.GetStringAsync("/api/copia-seguridad");
        var total = JsonDocument.Parse(copia).RootElement.GetProperty("fichas").GetArrayLength();

        using var response = await Importar(copia);

        var resumen = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, resumen.GetProperty("creadas").GetInt32());
        Assert.Equal(total, resumen.GetProperty("actualizadas").GetInt32());
        var despues = await client.GetFromJsonAsync<JsonElement[]>("/api/fichas");
        Assert.Equal(total, despues?.Length);
    }

    [Fact]
    public async Task Restoring_over_an_existing_ficha_keeps_its_original_snapshot()
    {
        await AuthenticateAsync();
        var id = await CrearConSnapshotAsync("Snapshot original");

        using var response = await Importar(Copia(new
        {
            id,
            titulo = "Snapshot original editada",
            tipo = "Película",
            fecha = "2025-03-01",
            terminado = true,
            snapshot = new
            {
                fuente = "tmdb",
                idExterno = "otro",
                generos = Array.Empty<string>()
            }
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ficha = await client.GetFromJsonAsync<FichaResponse>($"/api/fichas/{id}");
        Assert.Equal("Snapshot original editada", ficha?.Titulo);
        Assert.Equal("438631", ficha?.Snapshot?.IdExterno);
    }

    [Fact]
    public async Task An_invalid_ficha_rejects_the_whole_file_and_persists_nothing()
    {
        await AuthenticateAsync();
        var valida = Guid.NewGuid();

        using var response = await Importar(Copia(
            new { id = valida, titulo = "No debe importarse", tipo = "Libro", fecha = "2025-01-01", terminado = true },
            new { id = Guid.NewGuid(), titulo = "", tipo = "Libro", fecha = "2025-01-01", terminado = true }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problema = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problema.GetProperty("errors").TryGetProperty("fichas[1].Titulo", out _));
        using var buscada = await client.GetAsync($"/api/fichas/{valida}");
        Assert.Equal(HttpStatusCode.NotFound, buscada.StatusCode);
    }

    [Fact]
    public async Task Duplicated_ids_and_unknown_versions_are_rejected()
    {
        await AuthenticateAsync();
        var id = Guid.NewGuid();
        var ficha = new { id, titulo = "Repetida", tipo = "Serie", fecha = "2025-01-01", terminado = false };

        using var duplicados = await Importar(Copia(ficha, ficha));
        using var version = await Importar(JsonSerializer.Serialize(new { version = 2, fichas = new[] { ficha } }));

        Assert.Equal(HttpStatusCode.BadRequest, duplicados.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, version.StatusCode);
    }

    private static string Copia(params object[] fichas) =>
        JsonSerializer.Serialize(new { version = 1, fichas });

    private async Task<HttpResponseMessage> Importar(string json) =>
        await client.PostAsync("/api/copia-seguridad", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    private async Task<Guid> CrearConSnapshotAsync(string titulo)
    {
        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo,
            tipo = "Película",
            fecha = "2025-01-11",
            terminado = true,
            valoracion = 8.5m,
            snapshot = new
            {
                fuente = "tmdb",
                idExterno = "438631",
                portada = "https://image.tmdb.org/t/p/w500/dune.jpg",
                anio = 2021,
                autoria = "Denis Villeneuve",
                notaExterna = 7.8m,
                generos = new[] { "Ciencia ficción" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ficha = await response.Content.ReadFromJsonAsync<FichaResponse>();
        Assert.NotNull(ficha);
        return ficha.Id;
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-user", "test-password"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
}
