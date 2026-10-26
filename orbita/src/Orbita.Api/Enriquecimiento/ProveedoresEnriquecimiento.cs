using Orbita.Domain.Fichas;

namespace Orbita.Api.Enriquecimiento;

public sealed class ProveedoresEnriquecimiento(
    TmdbProveedor tmdb,
    RawgProveedor rawg,
    OpenLibraryProveedor openLibrary)
{
    public IProveedorEnriquecimiento Para(TipoFicha tipo) => FuentesSnapshot.PorTipo(tipo) switch
    {
        FuenteSnapshot.Tmdb => tmdb,
        FuenteSnapshot.Rawg => rawg,
        FuenteSnapshot.OpenLibrary => openLibrary,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo sin proveedor de enriquecimiento.")
    };
}
