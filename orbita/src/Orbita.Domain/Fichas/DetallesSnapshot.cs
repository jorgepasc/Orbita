namespace Orbita.Domain.Fichas;

// Datos de enriquecimiento propios de cada Tipo; el Snapshot lleva como mucho uno (ADR 0004).
public abstract record DetallesSnapshot;

public sealed record DetallesPelicula(
    string[] Actores,
    string[] Paises,
    int? DuracionMinutos) : DetallesSnapshot;

public static class DetallesSnapshots
{
    public static bool EsCompatible(DetallesSnapshot detalles, TipoFicha tipo) => detalles switch
    {
        DetallesPelicula => tipo == TipoFicha.Pelicula,
        _ => false
    };

    public static string Nombre(DetallesSnapshot detalles) => detalles switch
    {
        DetallesPelicula => "de película",
        _ => detalles.GetType().Name
    };
}
