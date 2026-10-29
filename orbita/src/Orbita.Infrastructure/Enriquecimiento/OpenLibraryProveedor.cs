using System.Text.Json;
using System.Text.RegularExpressions;
using Orbita.Application.Enriquecimiento;
using Orbita.Domain.Fichas;

namespace Orbita.Infrastructure.Enriquecimiento;

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
        var obra = await GetJsonAsync<DetalleObraOpenLibrary>(
            UrlDeClave(idExterno, "/works/"),
            cancellationToken);
        var claveAutor = obra.Authors?.FirstOrDefault()?.Author.Key;
        string? autoria = null;
        if (claveAutor is not null)
        {
            var autor = await GetJsonAsync<DetalleAutorOpenLibrary>(
                UrlDeClave(claveAutor, "/authors/"),
                cancellationToken);
            autoria = autor.Name;
        }

        // El detalle de la obra casi nunca trae first_publish_date ni las páginas; la búsqueda sí expone
        // first_publish_year y number_of_pages_median. Si ya hay año, un fallo de la búsqueda solo cuesta las páginas.
        var anio = Anio(obra.FirstPublishDate);
        DocumentoOpenLibrary? datosBusqueda = null;
        try
        {
            datosBusqueda = await BuscarObraAsync(idExterno, cancellationToken);
        }
        catch (ApiExternaException) when (anio is not null)
        {
        }

        return new Snapshot(
            Fuente,
            string.IsNullOrWhiteSpace(obra.Key) ? idExterno : obra.Key,
            obra.Covers?.FirstOrDefault(portada => portada > 0) is { } portada
                ? $"https://covers.openlibrary.org/b/id/{portada}-L.jpg"
                : null,
            anio ?? datosBusqueda?.FirstPublishYear,
            autoria,
            null,
            [.. (obra.Subjects ?? []).Take(5)],
            datosBusqueda?.NumberOfPagesMedian is > 0 and var paginas ? new DetallesLibro(paginas) : null);
    }

    private async Task<DocumentoOpenLibrary?> BuscarObraAsync(string clave, CancellationToken cancellationToken)
    {
        UrlDeClave(clave, "/works/");

        var busqueda = await GetJsonAsync<BusquedaOpenLibrary>(
            $"{BaseUrl}/search.json?q={Uri.EscapeDataString($"key:{clave}")}" +
            "&fields=key,title,first_publish_year,number_of_pages_median&limit=1",
            cancellationToken);

        return busqueda.Docs.FirstOrDefault();
    }

    private static string UrlDeClave(string clave, string prefijo)
    {
        if (!clave.StartsWith(prefijo, StringComparison.Ordinal))
        {
            throw new ApiExternaException("Open Library devolvió una clave de recurso no válida.");
        }

        var identificador = clave[prefijo.Length..];
        if (identificador.Length == 0 || !identificador.All(char.IsAsciiLetterOrDigit))
        {
            throw new ApiExternaException("Open Library devolvió una clave de recurso no válida.");
        }

        return $"{BaseUrl}{prefijo}{Uri.EscapeDataString(identificador)}.json";
    }

    private static int? Anio(string? fechaPublicacion)
    {
        var coincidencia = Regex.Match(fechaPublicacion ?? string.Empty, @"\b\d{4}\b");
        return coincidencia.Success && int.TryParse(coincidencia.Value, out var anio) ? anio : null;
    }

    private Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, url);
        solicitud.Headers.UserAgent.ParseAdd("Orbita/1.0 (uso personal)");

        return ClienteJsonExterno.EnviarAsync<T>(http, solicitud, "Open Library", Json, cancellationToken);
    }

    private sealed record BusquedaOpenLibrary(List<DocumentoOpenLibrary> Docs);

    private sealed record DocumentoOpenLibrary(
        string Key,
        string Title,
        int? CoverI,
        int? FirstPublishYear,
        int? NumberOfPagesMedian,
        string[]? AuthorName,
        string[]? Subject);

    private sealed record DetalleObraOpenLibrary(
        string Key,
        string? FirstPublishDate,
        List<int>? Covers,
        List<AutorOpenLibrary>? Authors,
        List<string>? Subjects);

    private sealed record AutorOpenLibrary(ClaveAutorOpenLibrary Author);

    private sealed record ClaveAutorOpenLibrary(string Key);

    private sealed record DetalleAutorOpenLibrary(string? Name);
}
