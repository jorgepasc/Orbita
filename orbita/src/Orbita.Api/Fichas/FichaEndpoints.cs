using Orbita.Application.Comun;
using Orbita.Application.Fichas;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Fichas;

public static class FichaEndpoints
{
    public static IEndpointRouteBuilder MapFichaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fichas").RequireAuthorization();

        group.MapGet("", async (ListarFichas listarFichas, CancellationToken cancellationToken) =>
        {
            var fichas = await listarFichas.EjecutarAsync(cancellationToken);

            return Results.Ok(fichas.Select(FichaResponse.From).ToArray());
        });

        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ObtenerFicha obtenerFicha,
        CancellationToken cancellationToken)
    {
        var ficha = await obtenerFicha.EjecutarAsync(id, cancellationToken);
        return ficha is null ? Results.NotFound() : Results.Ok(FichaResponse.From(ficha));
    }

    private static async Task<IResult> CreateAsync(
        FichaInput input,
        CrearFicha crearFicha,
        CancellationToken cancellationToken)
    {
        var resultado = await crearFicha.EjecutarAsync(ADatos(input), input.Snapshot?.ADominio(), cancellationToken);

        return resultado.Estado == EstadoOperacion.Exito
            ? Results.Created($"/api/fichas/{resultado.Valor!.Id}", FichaResponse.From(resultado.Valor))
            : Invalido(resultado.Errores!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        FichaInput input,
        ActualizarFicha actualizarFicha,
        CancellationToken cancellationToken)
    {
        var resultado = await actualizarFicha.EjecutarAsync(id, ADatos(input), cancellationToken);

        return resultado.Estado switch
        {
            EstadoOperacion.NoEncontrado => Results.NotFound(),
            EstadoOperacion.Invalido => Invalido(resultado.Errores!),
            _ => Results.Ok(FichaResponse.From(resultado.Valor!))
        };
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        EliminarFicha eliminarFicha,
        CancellationToken cancellationToken) =>
        await eliminarFicha.EjecutarAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound();

    internal static DatosFicha ADatos(FichaInput input) => new(
        input.Titulo,
        input.Tipo,
        input.Fecha,
        input.Terminado,
        input.Valoracion,
        input.Comentarios,
        input.Compania,
        input.Plataforma,
        input.Idioma,
        input.Autor);

    private static IResult Invalido(ErroresValidacion errores) =>
        Results.ValidationProblem(new Dictionary<string, string[]>(errores.Detalle));
}

public sealed record FichaInput(
    string? Titulo,
    string? Tipo,
    DateOnly? Fecha,
    bool Terminado,
    decimal? Valoracion,
    string? Comentarios,
    string? Compania,
    string? Plataforma,
    string? Idioma,
    string? Autor,
    SnapshotDto? Snapshot);

public sealed record FichaResponse(
    Guid Id,
    string Titulo,
    string Tipo,
    DateOnly? Fecha,
    bool Terminado,
    decimal? Valoracion,
    string? Comentarios,
    string? Compania,
    string? Plataforma,
    string? Idioma,
    string? Autor,
    SnapshotDto? Snapshot)
{
    public static FichaResponse From(Ficha ficha) => new(
        ficha.Id,
        ficha.Titulo,
        TiposFicha.Nombre(ficha.Tipo),
        ficha.Fecha,
        ficha.Terminado,
        ficha.Valoracion,
        ficha.Comentarios,
        ficha.Compania,
        ficha.Plataforma,
        ficha.Idioma,
        ficha.Autor,
        ficha.Snapshot is null ? null : SnapshotDto.Desde(ficha.Snapshot));
}
