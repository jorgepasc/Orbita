using System.Text.Json;
using Microsoft.Extensions.Options;
using Orbita.Application.Enriquecimiento;
using Orbita.Domain.Fichas;

namespace Orbita.Infrastructure.Enriquecimiento;

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

        return [.. busqueda.Results.Take(20).Select(juego => new Resultado(
            juego.Id.ToString(),
            juego.Name,
            Anio.DesdeFecha(juego.Released),
            juego.BackgroundImage,
            juego.Metacritic))];
    }

    public async Task<Snapshot> ConstruirSnapshotAsync(
        TipoFicha tipo,
        string idExterno,
        CancellationToken cancellationToken)
    {
        var clave = Uri.EscapeDataString(options.Value.ApiKey ?? string.Empty);
        var detalle = await GetJsonAsync<DetalleRawg>(
            $"{BaseUrl}/games/{Uri.EscapeDataString(idExterno)}?key={clave}",
            cancellationToken);

        return new Snapshot(
            Fuente,
            detalle.Id.ToString(),
            detalle.BackgroundImage,
            Anio.DesdeFecha(detalle.Released),
            detalle.Developers?.FirstOrDefault()?.Name,
            detalle.Metacritic,
            [.. (detalle.Genres ?? []).Select(genero => genero.Name)],
            DetallesDeJuego(detalle));
    }

    // RAWG devuelve playtime = 0 cuando no tiene dato; se trata como ausente.
    private static DetallesJuego? DetallesDeJuego(DetalleRawg detalle)
    {
        string[] plataformas = [.. (detalle.Platforms ?? []).Select(entrada => entrada.Platform.Name)];
        var horas = detalle.Playtime is > 0 ? detalle.Playtime : null;

        return plataformas.Length == 0 && horas is null ? null : new DetallesJuego(plataformas, horas);
    }

    private Task<T> GetJsonAsync<T>(string url, CancellationToken cancellationToken) =>
        ClienteJsonExterno.EnviarAsync<T>(
            http,
            new HttpRequestMessage(HttpMethod.Get, url),
            "RAWG",
            Json,
            cancellationToken);

    private sealed record BusquedaRawg(List<ResultadoRawg> Results);

    private sealed record ResultadoRawg(
        int Id,
        string Name,
        string? BackgroundImage,
        string? Released,
        int? Metacritic);

    private sealed record DetalleRawg(
        int Id,
        string Name,
        string? BackgroundImage,
        string? Released,
        int? Metacritic,
        List<DesarrolladorRawg>? Developers,
        List<GeneroRawg>? Genres,
        List<EntradaPlataformaRawg>? Platforms,
        int? Playtime);

    private sealed record EntradaPlataformaRawg(PlataformaRawg Platform);

    private sealed record PlataformaRawg(string Name);

    private sealed record DesarrolladorRawg(string Name);

    private sealed record GeneroRawg(string Name);
}
