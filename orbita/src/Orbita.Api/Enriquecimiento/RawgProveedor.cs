using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Orbita.Api.Fichas;

namespace Orbita.Api.Enriquecimiento;

public sealed class RawgOptions
{
    public const string SectionName = "Rawg";

    public string? ApiKey { get; set; }
}

public sealed class RawgProveedor(HttpClient http, IOptions<RawgOptions> options) : IProveedorEnriquecimiento
{
    private const string BaseUrl = "https://api.rawg.io/api";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public FuenteSnapshot Fuente => FuenteSnapshot.Rawg;

    public async Task<IReadOnlyList<Resultado>> BuscarAsync(
        TipoFicha tipo,
        string texto,
        CancellationToken cancellationToken)
    {
        var clave = Uri.EscapeDataString(options.Value.ApiKey ?? string.Empty);
        var busqueda = await GetJsonAsync<BusquedaRawg>(
            $"{BaseUrl}/games?search={Uri.EscapeDataString(texto)}&page_size=20&key={clave}",
            cancellationToken);

        return [.. busqueda.Results.Select(juego => new Resultado(
            juego.Id.ToString(),
            juego.Name,
            Anio.DesdeFecha(juego.Released),
            juego.BackgroundImage,
            juego.Metacritic))];
    }

    private async Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiExternaException($"RAWG respondió {(int)response.StatusCode} al consultar {url}.");
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
            ?? throw new ApiExternaException($"RAWG devolvió una respuesta vacía al consultar {url}.");
    }

    private sealed record BusquedaRawg(List<ResultadoRawg> Results);

    private sealed record ResultadoRawg(
        int Id,
        string Name,
        string? BackgroundImage,
        string? Released,
        int? Metacritic);
}
