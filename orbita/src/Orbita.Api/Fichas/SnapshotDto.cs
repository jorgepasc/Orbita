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
    [property: JsonPropertyName("generos")] string[] Generos)
{
    public static SnapshotDto Desde(Snapshot snapshot) => new(
        snapshot.Fuente,
        snapshot.IdExterno,
        snapshot.Portada,
        snapshot.Anio,
        snapshot.Autoria,
        snapshot.NotaExterna,
        snapshot.Generos);

    public Snapshot ADominio() => new(Fuente, IdExterno, Portada, Anio, Autoria, NotaExterna, Generos);
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
