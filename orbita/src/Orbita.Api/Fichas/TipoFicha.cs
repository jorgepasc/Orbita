namespace Orbita.Api.Fichas;

public enum TipoFicha
{
    Pelicula,
    Serie,
    Libro,
    Juego,
    Comic
}

public static class TiposFicha
{
    private static readonly IReadOnlyDictionary<string, TipoFicha> PorNombre =
        new Dictionary<string, TipoFicha>(StringComparer.Ordinal)
    {
        ["Película"] = TipoFicha.Pelicula,
        ["Serie"] = TipoFicha.Serie,
        ["Libro"] = TipoFicha.Libro,
        ["Juego"] = TipoFicha.Juego,
        ["Cómic"] = TipoFicha.Comic
    };

    public static bool TryParse(string? nombre, out TipoFicha tipo)
    {
        if (nombre is not null && PorNombre.TryGetValue(nombre, out tipo))
        {
            return true;
        }

        tipo = default;
        return false;
    }

    public static string Nombre(TipoFicha tipo) =>
        PorNombre.First(entry => entry.Value == tipo).Key;

    public static TipoFicha Parse(string nombre) => TryParse(nombre, out var tipo)
        ? tipo
        : throw new InvalidOperationException($"Tipo de Ficha desconocido: {nombre}");
}
