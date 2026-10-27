using Orbita.Application.Comun;
using Orbita.Application.Fichas;
using Orbita.Domain.Fichas;

namespace Orbita.Application.CopiaSeguridad;

public sealed record FichaImportada(Guid Id, DatosFicha Datos, Snapshot? Snapshot);

public sealed record ResumenImportacion(int Creadas, int Actualizadas);

public sealed class ImportarFichas(IFichaRepositorio fichas)
{
    public async Task<ResultadoOperacion<ResumenImportacion>> EjecutarAsync(
        IReadOnlyList<FichaImportada> importadas,
        CancellationToken cancellationToken)
    {
        var errores = new ErroresValidacion();

        var ids = new HashSet<Guid>();
        for (var i = 0; i < importadas.Count; i++)
        {
            if (!ids.Add(importadas[i].Id))
            {
                errores.Agregar($"fichas[{i}].Id", "El Id está repetido en el fichero.");
            }
        }

        if (errores.Hay)
        {
            return ResultadoOperacion<ResumenImportacion>.Invalido(errores);
        }

        var existentes = (await fichas.ObtenerVariasParaModificarAsync(ids, cancellationToken))
            .ToDictionary(ficha => ficha.Id);

        var creadas = 0;
        var actualizadas = 0;

        for (var i = 0; i < importadas.Count; i++)
        {
            var importada = importadas[i];

            if (existentes.TryGetValue(importada.Id, out var existente))
            {
                // El Snapshot de la BD se conserva: es inmutable tras crear (ADR 0002).
                var erroresFicha = existente.Actualizar(importada.Datos);
                Reindexar(errores, i, erroresFicha);
                actualizadas++;
            }
            else if (Ficha.TryRestaurar(
                importada.Id,
                importada.Datos,
                importada.Snapshot,
                out var nueva,
                out var erroresFicha))
            {
                fichas.Agregar(nueva);
                creadas++;
            }
            else
            {
                Reindexar(errores, i, erroresFicha);
            }
        }

        if (errores.Hay)
        {
            // Todo o nada: sin GuardarCambiosAsync no se persiste ninguna Ficha.
            return ResultadoOperacion<ResumenImportacion>.Invalido(errores);
        }

        await fichas.GuardarCambiosAsync(cancellationToken);

        return ResultadoOperacion<ResumenImportacion>.Exito(new ResumenImportacion(creadas, actualizadas));
    }

    private static void Reindexar(ErroresValidacion destino, int indice, ErroresValidacion origen)
    {
        foreach (var (campo, mensajes) in origen.Detalle)
        {
            destino.Agregar($"fichas[{indice}].{campo}", mensajes[0]);
        }
    }
}
