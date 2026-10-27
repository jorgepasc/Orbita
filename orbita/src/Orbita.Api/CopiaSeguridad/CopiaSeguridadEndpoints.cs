using Orbita.Api.Fichas;
using Orbita.Application.Comun;
using Orbita.Application.CopiaSeguridad;
using Orbita.Application.Fichas;

namespace Orbita.Api.CopiaSeguridad;

public static class CopiaSeguridadEndpoints
{
    private const int VersionFormato = 1;

    public static IEndpointRouteBuilder MapCopiaSeguridadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/copia-seguridad").RequireAuthorization();

        group.MapGet("", ExportarAsync);
        group.MapPost("", ImportarAsync);

        return endpoints;
    }

    private static async Task<IResult> ExportarAsync(
        ListarFichas listarFichas,
        HttpContext contexto,
        CancellationToken cancellationToken)
    {
        var fichas = await listarFichas.EjecutarAsync(cancellationToken);
        var ahora = DateTimeOffset.UtcNow;
        var documento = new CopiaSeguridadDocumento(
            VersionFormato,
            ahora,
            [.. fichas.Select(FichaResponse.From)]);

        contexto.Response.Headers.ContentDisposition = $"attachment; filename=orbita-{ahora:yyyy-MM-dd}.json";

        return Results.Ok(documento);
    }

    private static async Task<IResult> ImportarAsync(
        CopiaSeguridadEntrada entrada,
        ImportarFichas importarFichas,
        CancellationToken cancellationToken)
    {
        if (entrada.Version != VersionFormato)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["version"] = [$"Versión de copia no soportada; se esperaba {VersionFormato}."]
            });
        }

        if (entrada.Fichas is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["fichas"] = ["El fichero no contiene la lista de fichas."]
            });
        }

        var importadas = entrada.Fichas
            .Select(ficha => new FichaImportada(
                ficha.Id,
                FichaEndpoints.ADatos(ficha.ComoInput()),
                ficha.Snapshot?.ADominio()))
            .ToArray();

        var resultado = await importarFichas.EjecutarAsync(importadas, cancellationToken);

        return resultado.Estado == EstadoOperacion.Exito
            ? Results.Ok(new ResumenImportacionResponse(resultado.Valor!.Creadas, resultado.Valor.Actualizadas))
            : Results.ValidationProblem(new Dictionary<string, string[]>(resultado.Errores!.Detalle));
    }
}

public sealed record CopiaSeguridadDocumento(
    int Version,
    DateTimeOffset ExportadaEn,
    FichaResponse[] Fichas);

public sealed record CopiaSeguridadEntrada(int? Version, FichaCopia[]? Fichas);

public sealed record FichaCopia(
    Guid Id,
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
    SnapshotDto? Snapshot)
{
    public FichaInput ComoInput() => new(
        Titulo,
        Tipo,
        Fecha,
        Terminado,
        Valoracion,
        Comentarios,
        Compania,
        Plataforma,
        Idioma,
        Autor,
        Snapshot);
}

public sealed record ResumenImportacionResponse(int Creadas, int Actualizadas);
