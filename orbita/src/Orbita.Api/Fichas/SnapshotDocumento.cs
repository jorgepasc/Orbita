using System.Text.Json;
using System.Text.Json.Serialization;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Fichas;

// Formato del Snapshot en la columna jsonb. No cambiar nombres ni orden: hay datos persistidos.
internal sealed record SnapshotDocumento(
    [property: JsonPropertyName("fuente")] string Fuente,
    [property: JsonPropertyName("idExterno")] string IdExterno,
    [property: JsonPropertyName("portada")] string? Portada,
    [property: JsonPropertyName("anio")] int? Anio,
    [property: JsonPropertyName("autoria")] string? Autoria,
    [property: JsonPropertyName("notaExterna")] decimal? NotaExterna,
    [property: JsonPropertyName("generos")] string[] Generos)
{
    public static string Serializar(Snapshot snapshot) => JsonSerializer.Serialize(
        new SnapshotDocumento(
            FuentesSnapshot.Nombre(snapshot.Fuente),
            snapshot.IdExterno,
            snapshot.Portada,
            snapshot.Anio,
            snapshot.Autoria,
            snapshot.NotaExterna,
            snapshot.Generos),
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
                documento.Generos)
            : throw new JsonException($"Fuente del Snapshot desconocida: {documento.Fuente}");
    }
}
