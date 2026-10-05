using Orbita.Api.Enriquecimiento;

namespace Orbita.Api.Fichas;

public sealed class Ficha
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Titulo { get; set; } = string.Empty;

    public TipoFicha Tipo { get; set; }

    public DateOnly? Fecha { get; set; }

    public bool Terminado { get; set; }

    public decimal? Valoracion { get; set; }

    public string? Comentarios { get; set; }

    public string? Compania { get; set; }

    public string? Plataforma { get; set; }

    public string? Idioma { get; set; }

    public Snapshot? Snapshot { get; set; }
}
