using Orbita.Domain.Fichas;

namespace Orbita.Application.Comun;

public enum EstadoOperacion
{
    Exito,
    NoEncontrado,
    Invalido
}

public sealed class ResultadoOperacion<T>
{
    private ResultadoOperacion(EstadoOperacion estado, T? valor, ErroresValidacion? errores)
    {
        Estado = estado;
        Valor = valor;
        Errores = errores;
    }

    public EstadoOperacion Estado { get; }

    public T? Valor { get; }

    public ErroresValidacion? Errores { get; }

    public static ResultadoOperacion<T> Exito(T valor) => new(EstadoOperacion.Exito, valor, null);

    public static ResultadoOperacion<T> NoEncontrado() => new(EstadoOperacion.NoEncontrado, default, null);

    public static ResultadoOperacion<T> Invalido(ErroresValidacion errores) =>
        new(EstadoOperacion.Invalido, default, errores);
}
