using System.Net.Http.Json;
using System.Text.Json;
using Orbita.Api.Fichas;

namespace Orbita.Api.Enriquecimiento;

public sealed class OpenLibraryProveedor(HttpClient http) : IProveedorEnriquecimiento
{
    private const string BaseUrl = "https://openlibrary.org";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public FuenteSnapshot Fuente => FuenteSnapshot.OpenLibrary;

    public async Task<IReadOnlyList<Resultado>> BuscarAsync(
        TipoFicha tipo,
        string texto,
        CancellationToken cancellationToken)
    {
        var busqueda = await GetJsonAsync<BusquedaOpenLibrary>(
            $"{BaseUrl}/search.json?q={Uri.EscapeDataString(texto)}&limit=20",
            cancellationToken);

        return [.. busqueda.Docs.Select(documento => new Resultado(
            documento.Key,
            documento.Title,
            documento.FirstPublishYear,
            documento.CoverI is { } portada
                ? $"https://covers.openlibrary.org/b/id/{portada}-M.jpg"
                : null,
            null))];
    }

    private async Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiExternaException($"Open Library respondió {(int)response.StatusCode} al consultar {url}.");
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
            ?? throw new ApiExternaException($"Open Library devolvió una respuesta vacía al consultar {url}.");
    }

    private sealed record BusquedaOpenLibrary(List<DocumentoOpenLibrary> Docs);

    private sealed record DocumentoOpenLibrary(
        string Key,
        string Title,
        int? CoverI,
        int? FirstPublishYear);
}
