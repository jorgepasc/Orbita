using Orbita.Domain.Fichas;

namespace Orbita.Application.Enriquecimiento;

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
