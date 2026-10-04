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

        return [.. busqueda.Docs.Take(20).Select(documento => new Resultado(
            documento.Key,
            documento.Title,
            documento.FirstPublishYear,
            documento.CoverI is { } portada
                ? $"https://covers.openlibrary.org/b/id/{portada}-M.jpg"
                : null,
            null))];
    }

    public async Task<Snapshot> ConstruirSnapshotAsync(
        TipoFicha tipo,
        string idExterno,
        CancellationToken cancellationToken)
    {
        var busqueda = await GetJsonAsync<BusquedaOpenLibrary>(
            $"{BaseUrl}/search.json?q=key:{Uri.EscapeDataString(idExterno)}&limit=1",
            cancellationToken);
        var documento = busqueda.Docs.FirstOrDefault()
            ?? throw new ApiExternaException($"Open Library no encontró la obra {idExterno}.");

        return new Snapshot(
            Fuente,
            documento.Key,
            documento.CoverI is { } portada
                ? $"https://covers.openlibrary.org/b/id/{portada}-L.jpg"
                : null,
            documento.FirstPublishYear,
            documento.AuthorName?.FirstOrDefault(),
            null,
            [.. (documento.Subject ?? []).Take(5)]);
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
        int? FirstPublishYear,
        string[]? AuthorName,
        string[]? Subject);
}
