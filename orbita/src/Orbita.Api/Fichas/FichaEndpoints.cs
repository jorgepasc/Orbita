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
        var errors = Validate(input, input.Snapshot, out var tipo);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var ficha = new Ficha();
        Apply(input, tipo, ficha);
        ficha.Snapshot = input.Snapshot;
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

        var errors = Validate(input, ficha.Snapshot, out var tipo);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        Apply(input, tipo, ficha);
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

    private static Dictionary<string, string[]> Validate(
        FichaInput input,
        Snapshot? snapshot,
        out TipoFicha tipo)
    {
        var errors = new Dictionary<string, string[]>();
        tipo = default;

        if (string.IsNullOrWhiteSpace(input.Titulo))
        {
            errors[nameof(input.Titulo)] = ["El título es obligatorio."];
        }
        else if (input.Titulo.Trim().Length > 300)
        {
            errors[nameof(input.Titulo)] = ["El título no puede superar los 300 caracteres."];
        }

        if (!TiposFicha.TryParse(input.Tipo, out tipo))
        {
            errors[nameof(input.Tipo)] = ["El Tipo debe ser Película, Serie, Libro, Juego o Cómic."];
        }
        else
        {
            if (snapshot is not null)
            {
                if (string.IsNullOrWhiteSpace(snapshot.IdExterno))
                {
                    errors["snapshot.idExterno"] = ["El Snapshot necesita un idExterno."];
                }
                else if (!FuentesSnapshot.EsCompatible(snapshot.Fuente, tipo))
                {
                    errors["snapshot.fuente"] =
                        [$"La fuente '{FuentesSnapshot.Nombre(snapshot.Fuente)}' no es válida para el Tipo {TiposFicha.Nombre(tipo)}."];
                }
            }

            if (!string.IsNullOrWhiteSpace(input.Plataforma) && tipo != TipoFicha.Juego)
            {
                errors[nameof(input.Plataforma)] =
                    ["La Plataforma solo está disponible para el Tipo Juego."];
            }

            if (!string.IsNullOrWhiteSpace(input.Compania) && tipo == TipoFicha.Libro)
            {
                errors[nameof(input.Compania)] =
                    ["La Compañía no está disponible para el Tipo Libro."];
            }

            if (!string.IsNullOrWhiteSpace(input.Autor) && tipo != TipoFicha.Libro)
            {
                errors[nameof(input.Autor)] =
                    ["El Autor solo está disponible para el Tipo Libro."];
            }

            if (!string.IsNullOrWhiteSpace(input.Idioma) && tipo is not (TipoFicha.Libro or TipoFicha.Comic))
            {
                errors[nameof(input.Idioma)] =
                    ["El Idioma solo está disponible para los Tipos Libro y Cómic."];
            }
        }

        if (input.Fecha is null)
        {
            errors[nameof(input.Fecha)] = ["La Fecha es obligatoria."];
        }

        if (input.Valoracion is < 0 or > 10)
        {
            errors[nameof(input.Valoracion)] = ["La Valoración debe estar entre 0 y 10."];
        }

        if (input.Comentarios?.Length > 4000)
        {
            errors[nameof(input.Comentarios)] = ["Los Comentarios no pueden superar los 4000 caracteres."];
        }

        if (input.Compania?.Length > 200)
        {
            errors[nameof(input.Compania)] = ["La Compañía no puede superar los 200 caracteres."];
        }

        if (input.Plataforma?.Length > 200)
        {
            errors[nameof(input.Plataforma)] = ["La Plataforma no puede superar los 200 caracteres."];
        }

        if (input.Autor?.Length > 200)
        {
            errors[nameof(input.Autor)] = ["El Autor no puede superar los 200 caracteres."];
        }

        if (input.Idioma?.Length > 200)
        {
            errors[nameof(input.Idioma)] = ["El Idioma no puede superar los 200 caracteres."];
        }

        return errors;
    }

    private static void Apply(FichaInput input, TipoFicha tipo, Ficha ficha)
    {
        ficha.Titulo = input.Titulo!.Trim();
        ficha.Tipo = tipo;
        ficha.Fecha = input.Fecha;
        ficha.Terminado = input.Terminado;
        ficha.Valoracion = input.Valoracion;
        ficha.Comentarios = input.Comentarios;
        ficha.Compania = input.Compania;
        ficha.Plataforma = input.Plataforma;
        ficha.Idioma = input.Idioma;
        ficha.Autor = input.Autor;
    }
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
    Snapshot? Snapshot);

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
    Snapshot? Snapshot)
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
        ficha.Snapshot);
}
