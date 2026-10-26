using Orbita.Domain.Fichas;

namespace Orbita.Application.Fichas;

public interface IFichaRepositorio
{
    Task<IReadOnlyList<Ficha>> ListarAsync(CancellationToken cancellationToken);

    Task<Ficha?> ObtenerAsync(Guid id, CancellationToken cancellationToken);

    Task<Ficha?> ObtenerParaModificarAsync(Guid id, CancellationToken cancellationToken);

    void Agregar(Ficha ficha);

    void Eliminar(Ficha ficha);

    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
