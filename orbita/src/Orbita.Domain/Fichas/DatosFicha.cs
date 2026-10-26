namespace Orbita.Domain.Fichas;

public sealed record DatosFicha(
    string? Titulo,
    string? Tipo,
    DateOnly? Fecha,
    bool Terminado,
    decimal? Valoracion,
    string? Comentarios,
    string? Compania,
    string? Plataforma,
    string? Idioma,
    string? Autor);
