using Orbita.Api.Fichas;

namespace Orbita.Api.Enriquecimiento;

public sealed record Resultado(
    string IdExterno,
    string Titulo,
    int? Anio,
    string? Portada,
    decimal? NotaExterna);

public interface IProveedorEnriquecimiento
{
    FuenteSnapshot Fuente { get; }

    Task<IReadOnlyList<Resultado>> BuscarAsync(TipoFicha tipo, string texto, CancellationToken cancellationToken);

    Task<Snapshot> ConstruirSnapshotAsync(TipoFicha tipo, string idExterno, CancellationToken cancellationToken);
}

public sealed class ApiExternaException(string mensaje) : Exception(mensaje);

internal static class Anio
{
    public static int? DesdeFecha(string? fecha) =>
        fecha is { Length: >= 4 } && int.TryParse(fecha.AsSpan(0, 4), out var anio) ? anio : null;
}
