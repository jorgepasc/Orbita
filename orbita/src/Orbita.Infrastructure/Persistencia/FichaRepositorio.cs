using Microsoft.EntityFrameworkCore;
using Orbita.Application.Fichas;
using Orbita.Domain.Fichas;

namespace Orbita.Infrastructure.Persistencia;

public sealed class FichaRepositorio(FichaDbContext database) : IFichaRepositorio
{
    public async Task<IReadOnlyList<Ficha>> ListarAsync(CancellationToken cancellationToken) =>
        await database.Fichas
            .AsNoTracking()
            .OrderBy(ficha => ficha.Titulo)
            .ThenBy(ficha => ficha.Id)
            .ToListAsync(cancellationToken);

    public Task<Ficha?> ObtenerAsync(Guid id, CancellationToken cancellationToken) =>
        database.Fichas
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

    public Task<Ficha?> ObtenerParaModificarAsync(Guid id, CancellationToken cancellationToken) =>
        database.Fichas.FirstOrDefaultAsync(value => value.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ficha>> ObtenerVariasParaModificarAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await database.Fichas
            .Where(ficha => ids.Contains(ficha.Id))
            .ToListAsync(cancellationToken);

    public void Agregar(Ficha ficha) => database.Fichas.Add(ficha);

    public void Eliminar(Ficha ficha) => database.Fichas.Remove(ficha);

    public Task GuardarCambiosAsync(CancellationToken cancellationToken) =>
        database.SaveChangesAsync(cancellationToken);
}
