namespace Orbita.Domain.Fichas;

public sealed class ErroresValidacion
{
    private readonly Dictionary<string, string[]> errores = [];

    public bool Hay => errores.Count > 0;

    public IReadOnlyDictionary<string, string[]> Detalle => errores;

    public void Agregar(string campo, string mensaje) => errores[campo] = [mensaje];
}
