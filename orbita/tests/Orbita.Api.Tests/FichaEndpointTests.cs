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
    public async Task Fichas_require_authentication()
    {
        using var response = await client.GetAsync("/api/fichas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
    public async Task Create_ficha_keeps_valuation_independent_from_completion_and_date()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Arrival",
            tipo = "Película",
            fecha = (DateOnly?)null,
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
        Assert.Null(ficha.Fecha);
        Assert.False(ficha.Terminado);
        Assert.Equal(8.75m, ficha.Valoracion);
        Assert.Equal("Una película para volver a ver.", ficha.Comentarios);
        Assert.Equal("Solo", ficha.Compania);
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
            titulo = "El nombre del viento",
            tipo = "Libro",
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
            titulo = "El nombre del viento",
            tipo = "Libro",
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
            terminado = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_a_finished_ficha_without_a_fecha()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Ficha terminada sin fecha",
            tipo = "Película",
            terminado = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    string? Compania);

internal sealed record LoginRequest(string Username, string Password);

internal sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
