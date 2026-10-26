using Microsoft.EntityFrameworkCore;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Fichas;

public static class FichaEndpoints
{
    public static IEndpointRouteBuilder MapFichaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fichas").RequireAuthorization();

        group.MapGet("", async (FichaDbContext database, CancellationToken cancellationToken) =>
        {
            var fichas = await database.Fichas
                .AsNoTracking()
                .OrderBy(ficha => ficha.Titulo)
                .ThenBy(ficha => ficha.Id)
                .ToListAsync(cancellationToken);

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
        FichaDbContext database,
        CancellationToken cancellationToken)
    {
        var ficha = await database.Fichas
            .AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        return ficha is null ? Results.NotFound() : Results.Ok(FichaResponse.From(ficha));
    }

    private static async Task<IResult> CreateAsync(
        FichaInput input,
        FichaDbContext database,
        CancellationToken cancellationToken)
    {
        if (!Ficha.TryCrear(ADatos(input), input.Snapshot?.ADominio(), out var ficha, out var errores))
        {
            return Invalido(errores);
        }

        database.Fichas.Add(ficha);
        await database.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/fichas/{ficha.Id}", FichaResponse.From(ficha));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        FichaInput input,
        FichaDbContext database,
        CancellationToken cancellationToken)
    {
        var ficha = await database.Fichas.FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (ficha is null)
        {
            return Results.NotFound();
        }

        var errores = ficha.Actualizar(ADatos(input));
        if (errores.Hay)
        {
            return Invalido(errores);
        }

        await database.SaveChangesAsync(cancellationToken);

        return Results.Ok(FichaResponse.From(ficha));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        FichaDbContext database,
        CancellationToken cancellationToken)
    {
        var ficha = await database.Fichas.FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (ficha is null)
        {
            return Results.NotFound();
        }

        database.Fichas.Remove(ficha);
        await database.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static DatosFicha ADatos(FichaInput input) => new(
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
