using System.Text.Json;
using System.Text.Json.Serialization;
using Orbita.Domain.Fichas;

namespace Orbita.Infrastructure.Persistencia;

// Formato del Snapshot en la columna jsonb. No cambiar nombres ni orden: hay datos persistidos.
// "detalles" se omite si es nulo, así los snapshots sin detalles conservan su documento original.
internal sealed record SnapshotDocumento(
    [property: JsonPropertyName("fuente")] string Fuente,
    [property: JsonPropertyName("idExterno")] string IdExterno,
    [property: JsonPropertyName("portada")] string? Portada,
    [property: JsonPropertyName("anio")] int? Anio,
    [property: JsonPropertyName("autoria")] string? Autoria,
    [property: JsonPropertyName("notaExterna")] decimal? NotaExterna,
    [property: JsonPropertyName("generos")] string[] Generos,
    [property: JsonPropertyName("detalles")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DetallesDocumento? Detalles = null)
{
    public static string Serializar(Snapshot snapshot) => JsonSerializer.Serialize(
        new SnapshotDocumento(
            FuentesSnapshot.Nombre(snapshot.Fuente),
            snapshot.IdExterno,
            snapshot.Portada,
            snapshot.Anio,
            snapshot.Autoria,
            snapshot.NotaExterna,
            snapshot.Generos,
            DetallesDocumento.Desde(snapshot.Detalles)),
        (JsonSerializerOptions?)null);

    public static Snapshot? Deserializar(string json)
    {
        var documento = JsonSerializer.Deserialize<SnapshotDocumento>(json, (JsonSerializerOptions?)null);
        if (documento is null)
        {
            return null;
        }

        return FuentesSnapshot.TryParse(documento.Fuente, out var fuente)
            ? new Snapshot(
                fuente,
                documento.IdExterno,
                documento.Portada,
                documento.Anio,
                documento.Autoria,
                documento.NotaExterna,
                documento.Generos,
                documento.Detalles?.ADominio())
            : throw new JsonException($"Fuente del Snapshot desconocida: {documento.Fuente}");
    }
}

// El discriminador "tipo" identifica la forma de los detalles; añadir un Tipo = añadir un derivado.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "tipo")]
[JsonDerivedType(typeof(DetallesPeliculaDocumento), "pelicula")]
[JsonDerivedType(typeof(DetallesLibroDocumento), "libro")]
internal abstract record DetallesDocumento
{
    public static DetallesDocumento? Desde(DetallesSnapshot? detalles) => detalles switch
    {
        null => null,
        DetallesPelicula pelicula => new DetallesPeliculaDocumento(
            pelicula.Actores,
            pelicula.Paises,
            pelicula.DuracionMinutos),
        DetallesLibro libro => new DetallesLibroDocumento(libro.Paginas),
        _ => throw new NotSupportedException($"Detalles sin documento: {detalles.GetType().Name}")
    };

    public abstract DetallesSnapshot ADominio();
}

internal sealed record DetallesPeliculaDocumento(
    [property: JsonPropertyName("actores")] string[]? Actores,
    [property: JsonPropertyName("paises")] string[]? Paises,
    [property: JsonPropertyName("duracionMinutos")] int? DuracionMinutos) : DetallesDocumento
{
    public override DetallesSnapshot ADominio() => new DetallesPelicula(Actores ?? [], Paises ?? [], DuracionMinutos);
}

internal sealed record DetallesLibroDocumento(
    [property: JsonPropertyName("paginas")] int? Paginas) : DetallesDocumento
{
    public override DetallesSnapshot ADominio() => new DetallesLibro(Paginas);
}
