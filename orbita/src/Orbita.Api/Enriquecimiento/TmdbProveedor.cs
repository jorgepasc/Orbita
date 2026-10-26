using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Orbita.Application.Enriquecimiento;
using Orbita.Domain.Fichas;

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
    private const string TamanoDetalle = "w500";

    public FuenteSnapshot Fuente => FuenteSnapshot.Tmdb;

    public async Task<IReadOnlyList<Resultado>> BuscarAsync(
        TipoFicha tipo,
        string texto,
        CancellationToken cancellationToken)
    {
        var configuracion = Configuracion(tipo);
        var busqueda = await GetJsonAsync<BusquedaTmdb>(
            $"{BaseUrl}/search/{configuracion.Ruta}?query={Uri.EscapeDataString(texto)}&language=es-ES",
            cancellationToken);

        return [.. (busqueda.Results ?? []).Take(20).Select(resultado => new Resultado(
            resultado.Id.ToString(),
            resultado.Title ?? resultado.Name ?? string.Empty,
            Anio.DesdeFecha(resultado.ReleaseDate ?? resultado.FirstAirDate),
            resultado.PosterPath is null ? null : $"https://image.tmdb.org/t/p/{TamanoBusqueda}{resultado.PosterPath}",
            resultado.VoteAverage))];
    }

    public async Task<Snapshot> ConstruirSnapshotAsync(
        TipoFicha tipo,
        string idExterno,
        CancellationToken cancellationToken)
    {
        var configuracion = Configuracion(tipo);
        var anexarCreditos = configuracion.EsPelicula ? "&append_to_response=credits" : string.Empty;
        var url = $"{BaseUrl}/{configuracion.Ruta}/{Uri.EscapeDataString(idExterno)}?language=es-ES{anexarCreditos}";
        var detalle = await GetJsonAsync<DetalleTmdb>(url, cancellationToken);

        return new Snapshot(
            Fuente,
            detalle.Id.ToString(),
            detalle.PosterPath is null ? null : $"https://image.tmdb.org/t/p/{TamanoDetalle}{detalle.PosterPath}",
            Anio.DesdeFecha(detalle.ReleaseDate ?? detalle.FirstAirDate),
            configuracion.EsPelicula
                ? detalle.Credits?.Crew?.FirstOrDefault(miembro => miembro.Job == "Director")?.Name
                : detalle.CreatedBy?.FirstOrDefault()?.Name,
            detalle.VoteAverage,
            [.. (detalle.Genres ?? []).Select(genero => genero.Name)]);
    }

    private Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.BearerToken))
        {
            throw new ApiExternaException("TMDB no está configurado: falta Tmdb:BearerToken.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.BearerToken);

        return ClienteJsonExterno.EnviarAsync<T>(http, request, "TMDB", null, cancellationToken);
    }

    private static ConfiguracionTipoTmdb Configuracion(TipoFicha tipo) => tipo switch
    {
        TipoFicha.Pelicula => new("movie", true),
        TipoFicha.Serie => new("tv", false),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo sin proveedor TMDB.")
    };

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

    private sealed record DetalleTmdb(
        int Id,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("release_date")] string? ReleaseDate,
        [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
        [property: JsonPropertyName("poster_path")] string? PosterPath,
        [property: JsonPropertyName("vote_average")] decimal? VoteAverage,
        List<GeneroTmdb>? Genres,
        [property: JsonPropertyName("created_by")] List<CreadorTmdb>? CreatedBy,
        CreditosTmdb? Credits);

    private sealed record GeneroTmdb(string Name);

    private sealed record CreadorTmdb(string Name);

    private sealed record CreditosTmdb(List<MiembroCrewTmdb>? Crew);

    private sealed record MiembroCrewTmdb(string Name, string Job);

    private sealed record ConfiguracionTipoTmdb(string Ruta, bool EsPelicula);
}
