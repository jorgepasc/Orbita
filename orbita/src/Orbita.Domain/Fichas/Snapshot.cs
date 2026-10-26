using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orbita.Domain.Fichas;

[JsonConverter(typeof(FuenteSnapshotJsonConverter))]
public enum FuenteSnapshot
{
    Tmdb,
    Rawg,
    OpenLibrary
}

public sealed class FuenteSnapshotJsonConverter : JsonConverter<FuenteSnapshot>
{
    public override FuenteSnapshot Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.GetString() switch
        {
            "tmdb" => FuenteSnapshot.Tmdb,
            "rawg" => FuenteSnapshot.Rawg,
            "openlibrary" => FuenteSnapshot.OpenLibrary,
            _ => throw new JsonException($"Fuente del Snapshot desconocida: {reader.GetString()}")
        };

    public override void Write(
        Utf8JsonWriter writer,
        FuenteSnapshot value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(FuentesSnapshot.Nombre(value));
}

public static class FuentesSnapshot
{
    public static string Nombre(FuenteSnapshot fuente) => fuente switch
    {
        FuenteSnapshot.Tmdb => "tmdb",
        FuenteSnapshot.Rawg => "rawg",
        _ => "openlibrary"
    };

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
    [property: JsonPropertyName("fuente")] FuenteSnapshot Fuente,
    [property: JsonPropertyName("idExterno")] string IdExterno,
    [property: JsonPropertyName("portada")] string? Portada,
    [property: JsonPropertyName("anio")] int? Anio,
    [property: JsonPropertyName("autoria")] string? Autoria,
    [property: JsonPropertyName("notaExterna")] decimal? NotaExterna,
    [property: JsonPropertyName("generos")] string[] Generos);
