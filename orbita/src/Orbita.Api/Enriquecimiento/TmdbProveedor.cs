using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Orbita.Api.Fichas;

namespace Orbita.Api.Enriquecimiento;

public sealed class TmdbOptions
{
    public const string SectionName = "Tmdb";

    public string? BearerToken { get; set; }
}

public sealed class TmdbProveedor(HttpClient http, IOptions<TmdbOptions> options) : IProveedorEnriquecimiento
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private const string TamanoBusqueda = "w154";

    public FuenteSnapshot Fuente => FuenteSnapshot.Tmdb;

    public async Task<IReadOnlyList<Resultado>> BuscarAsync(
        TipoFicha tipo,
        string texto,
        CancellationToken cancellationToken)
    {
        var ruta = tipo == TipoFicha.Pelicula ? "movie" : "tv";
        var busqueda = await GetJsonAsync<BusquedaTmdb>(
            $"{BaseUrl}/search/{ruta}?query={Uri.EscapeDataString(texto)}&language=es-ES",
            cancellationToken);

        return [.. busqueda.Results.Select(resultado => new Resultado(
            resultado.Id.ToString(),
            resultado.Title ?? resultado.Name ?? string.Empty,
            Anio.DesdeFecha(resultado.ReleaseDate ?? resultado.FirstAirDate),
            resultado.PosterPath is null ? null : $"https://image.tmdb.org/t/p/{TamanoBusqueda}{resultado.PosterPath}",
            resultado.VoteAverage))];
    }

    private async Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.BearerToken);

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiExternaException($"TMDB respondió {(int)response.StatusCode} al consultar {url}.");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new ApiExternaException($"TMDB devolvió una respuesta vacía al consultar {url}.");
    }

    private sealed record BusquedaTmdb(
        [property: JsonPropertyName("results")] List<ResultadoTmdb>? Results);

    private sealed record ResultadoTmdb(
        int Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
        [property: JsonPropertyName("poster_path")] string? PosterPath,
        [property: JsonPropertyName("vote_average")] decimal? VoteAverage);
}
