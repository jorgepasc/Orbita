using System.Text.Json;
using System.Text.Json.Serialization;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Fichas;

public sealed record SnapshotDto(
    [property: JsonPropertyName("fuente")]
    [property: JsonConverter(typeof(FuenteSnapshotJsonConverter))]
    FuenteSnapshot Fuente,
    [property: JsonPropertyName("idExterno")] string IdExterno,
    [property: JsonPropertyName("portada")] string? Portada,
    [property: JsonPropertyName("anio")] int? Anio,
    [property: JsonPropertyName("autoria")] string? Autoria,
    [property: JsonPropertyName("notaExterna")] decimal? NotaExterna,
    [property: JsonPropertyName("generos")] string[] Generos,
    [property: JsonPropertyName("detalles")] DetallesDto? Detalles = null)
{
    public static SnapshotDto Desde(Snapshot snapshot) => new(
        snapshot.Fuente,
        snapshot.IdExterno,
        snapshot.Portada,
        snapshot.Anio,
        snapshot.Autoria,
        snapshot.NotaExterna,
        snapshot.Generos,
        DetallesDto.Desde(snapshot.Detalles));

    public Snapshot ADominio() => new(
        Fuente,
        IdExterno,
        Portada,
        Anio,
        Autoria,
        NotaExterna,
        Generos,
        Detalles?.ADominio());
}

// El discriminador "tipo" identifica la forma de los detalles; añadir un Tipo = añadir un derivado.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "tipo")]
[JsonDerivedType(typeof(DetallesPeliculaDto), "pelicula")]
public abstract record DetallesDto
{
    public static DetallesDto? Desde(DetallesSnapshot? detalles) => detalles switch
    {
        null => null,
        DetallesPelicula pelicula => new DetallesPeliculaDto(
            pelicula.Actores,
            pelicula.Paises,
            pelicula.DuracionMinutos),
        _ => throw new NotSupportedException($"Detalles sin DTO: {detalles.GetType().Name}")
    };

    public abstract DetallesSnapshot ADominio();
}

public sealed record DetallesPeliculaDto(
    [property: JsonPropertyName("actores")] string[]? Actores,
    [property: JsonPropertyName("paises")] string[]? Paises,
    [property: JsonPropertyName("duracionMinutos")] int? DuracionMinutos) : DetallesDto
{
    public override DetallesSnapshot ADominio() => new DetallesPelicula(Actores ?? [], Paises ?? [], DuracionMinutos);
}

public sealed class FuenteSnapshotJsonConverter : JsonConverter<FuenteSnapshot>
{
    public override FuenteSnapshot Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        FuentesSnapshot.TryParse(reader.GetString(), out var fuente)
            ? fuente
            : throw new JsonException($"Fuente del Snapshot desconocida: {reader.GetString()}");

    public override void Write(
        Utf8JsonWriter writer,
        FuenteSnapshot value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(FuentesSnapshot.Nombre(value));
}
