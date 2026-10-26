using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orbita.Domain.Fichas;
using Orbita.Infrastructure.Persistencia;

namespace Orbita.Api.Tests;

// Fijan comportamiento sutil que el refactor a capas no debe alterar.
public sealed class CaracterizacionTests : IClassFixture<OrbitaApiFactory>
{
    private readonly OrbitaApiFactory factory;
    private readonly HttpClient client;

    public CaracterizacionTests(OrbitaApiFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_reports_the_length_message_when_compania_exceeds_the_limit_for_libro()
    {
        await AuthenticateAsync();

        using var response = await client.PostAsJsonAsync("/api/fichas", new
        {
            titulo = "Dune",
            tipo = "Libro",
            fecha = "2025-03-06",
            terminado = true,
            compania = new string('a', 201)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = problem!.RootElement.GetProperty("errors");
        Assert.Equal(
            "La Compañía no puede superar los 200 caracteres.",
            errors.GetProperty("Compania")[0].GetString());
    }

    [Fact]
    public async Task Update_of_a_missing_ficha_returns_not_found_even_with_an_invalid_body()
    {
        await AuthenticateAsync();

        using var response = await client.PutAsJsonAsync($"/api/fichas/{Guid.NewGuid()}", new
        {
            titulo = "",
            tipo = "Podcast"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Snapshot_is_stored_as_the_exact_jsonb_document_and_reads_back()
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FichaDbContext>();
        var converter = database.Model
            .FindEntityType(typeof(Ficha))!
            .FindProperty(nameof(Ficha.Snapshot))!
            .GetValueConverter()!;

        var snapshot = new Snapshot(
            FuenteSnapshot.Tmdb,
            "438631",
            "https://image.tmdb.org/t/p/w500/dune.jpg",
            2021,
            "Denis Villeneuve",
            7.8m,
            ["Ciencia ficción", "Aventura"]);

        var json = (string)converter.ConvertToProvider(snapshot)!;

        Assert.Equal(
            "{\"fuente\":\"tmdb\",\"idExterno\":\"438631\",\"portada\":\"https://image.tmdb.org/t/p/w500/dune.jpg\"," +
            "\"anio\":2021,\"autoria\":\"Denis Villeneuve\",\"notaExterna\":7.8,\"generos\":[\"Ciencia ficci\\u00F3n\",\"Aventura\"]}",
            json);

        var leido = (Snapshot)converter.ConvertFromProvider(
            "{\"fuente\":\"openlibrary\",\"idExterno\":\"/works/OL1W\",\"portada\":null,\"anio\":null," +
            "\"autoria\":null,\"notaExterna\":null,\"generos\":[]}")!;

        Assert.Equal(FuenteSnapshot.OpenLibrary, leido.Fuente);
        Assert.Equal("/works/OL1W", leido.IdExterno);
        Assert.Empty(leido.Generos);
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
