using Orbita.Application.Comun;
using Orbita.Domain.Fichas;

namespace Orbita.Application.Fichas;

public sealed class ListarFichas(IFichaRepositorio fichas)
{
    public Task<IReadOnlyList<Ficha>> EjecutarAsync(CancellationToken cancellationToken) =>
        fichas.ListarAsync(cancellationToken);
}

public sealed class ObtenerFicha(IFichaRepositorio fichas)
{
    public Task<Ficha?> EjecutarAsync(Guid id, CancellationToken cancellationToken) =>
        fichas.ObtenerAsync(id, cancellationToken);
}

public sealed class CrearFicha(IFichaRepositorio fichas)
{
    public async Task<ResultadoOperacion<Ficha>> EjecutarAsync(
        DatosFicha datos,
        Snapshot? snapshot,
        CancellationToken cancellationToken)
    {
        if (!Ficha.TryCrear(datos, snapshot, out var ficha, out var errores))
        {
            return ResultadoOperacion<Ficha>.Invalido(errores);
        }

        fichas.Agregar(ficha);
        await fichas.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<Ficha>.Exito(ficha);
    }
}

public sealed class ActualizarFicha(IFichaRepositorio fichas)
{
    public async Task<ResultadoOperacion<Ficha>> EjecutarAsync(
        Guid id,
        DatosFicha datos,
        CancellationToken cancellationToken)
    {
        var ficha = await fichas.ObtenerParaModificarAsync(id, cancellationToken);
        if (ficha is null)
        {
            return ResultadoOperacion<Ficha>.NoEncontrado();
        }

        var errores = ficha.Actualizar(datos);
        if (errores.Hay)
        {
            return ResultadoOperacion<Ficha>.Invalido(errores);
        }

        await fichas.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<Ficha>.Exito(ficha);
    }
}

public sealed class EliminarFicha(IFichaRepositorio fichas)
{
    public async Task<bool> EjecutarAsync(Guid id, CancellationToken cancellationToken)
    {
        var ficha = await fichas.ObtenerParaModificarAsync(id, cancellationToken);
        if (ficha is null)
        {
            return false;
        }

        fichas.Eliminar(ficha);
        await fichas.GuardarCambiosAsync(cancellationToken);
        return true;
    }
}
