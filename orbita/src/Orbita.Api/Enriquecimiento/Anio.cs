namespace Orbita.Api.Enriquecimiento;

internal static class Anio
{
    public static int? DesdeFecha(string? fecha) =>
        fecha is { Length: >= 4 } && int.TryParse(fecha.AsSpan(0, 4), out var anio) ? anio : null;
}
