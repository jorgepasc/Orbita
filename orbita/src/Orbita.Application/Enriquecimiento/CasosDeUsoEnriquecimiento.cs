using Orbita.Domain.Fichas;

namespace Orbita.Application.Enriquecimiento;

public sealed class BuscarResultados(SelectorProveedores proveedores)
{
    public Task<IReadOnlyList<Resultado>> EjecutarAsync(
        TipoFicha tipo,
        string texto,
        CancellationToken cancellationToken) =>
        proveedores.Para(tipo).BuscarAsync(tipo, texto, cancellationToken);
}

public sealed class ConstruirSnapshot(SelectorProveedores proveedores)
{
    public Task<Snapshot> EjecutarAsync(
        TipoFicha tipo,
        string idExterno,
        CancellationToken cancellationToken) =>
        proveedores.Para(tipo).ConstruirSnapshotAsync(tipo, idExterno, cancellationToken);
}
