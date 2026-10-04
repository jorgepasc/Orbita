using Orbita.Api.Fichas;

namespace Orbita.Api.Enriquecimiento;

public sealed class ProveedoresEnriquecimiento(
    TmdbProveedor tmdb,
    RawgProveedor rawg,
    OpenLibraryProveedor openLibrary)
{
    public IProveedorEnriquecimiento Para(TipoFicha tipo) => tipo switch
    {
        TipoFicha.Pelicula or TipoFicha.Serie => tmdb,
        TipoFicha.Juego => rawg,
        TipoFicha.Libro or TipoFicha.Comic => openLibrary,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo sin proveedor de enriquecimiento.")
    };
}
