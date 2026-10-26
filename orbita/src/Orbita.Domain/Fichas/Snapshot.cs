namespace Orbita.Domain.Fichas;

public enum FuenteSnapshot
{
    Tmdb,
    Rawg,
    OpenLibrary
}

public static class FuentesSnapshot
{
    public static string Nombre(FuenteSnapshot fuente) => fuente switch
    {
        FuenteSnapshot.Tmdb => "tmdb",
        FuenteSnapshot.Rawg => "rawg",
        _ => "openlibrary"
    };

    public static bool TryParse(string? nombre, out FuenteSnapshot fuente)
    {
        switch (nombre)
        {
            case "tmdb":
                fuente = FuenteSnapshot.Tmdb;
                return true;
            case "rawg":
                fuente = FuenteSnapshot.Rawg;
                return true;
            case "openlibrary":
                fuente = FuenteSnapshot.OpenLibrary;
                return true;
            default:
                fuente = default;
                return false;
        }
    }

    public static FuenteSnapshot? PorTipo(TipoFicha tipo) => tipo switch
    {
        TipoFicha.Pelicula or TipoFicha.Serie => FuenteSnapshot.Tmdb,
        TipoFicha.Juego => FuenteSnapshot.Rawg,
        TipoFicha.Libro or TipoFicha.Comic => FuenteSnapshot.OpenLibrary,
        _ => null
    };

    public static bool EsCompatible(FuenteSnapshot fuente, TipoFicha tipo) => PorTipo(tipo) == fuente;
}

public sealed record Snapshot(
    FuenteSnapshot Fuente,
    string IdExterno,
    string? Portada,
    int? Anio,
    string? Autoria,
    decimal? NotaExterna,
    string[] Generos);
