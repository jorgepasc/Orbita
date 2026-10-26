using System.Text.Json;
using Orbita.Api.Fichas;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Enriquecimiento;

public static class EnriquecimientoEndpoints
{
    public static IEndpointRouteBuilder MapEnriquecimientoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/enriquecimiento").RequireAuthorization();

        group.MapGet("/resultados", BuscarAsync);
        group.MapGet("/snapshot", SnapshotAsync);

        return endpoints;
    }

    private static async Task<IResult> BuscarAsync(
        string? q,
        string? tipo,
        ProveedoresEnriquecimiento proveedores,
        ILogger<ProveedoresEnriquecimiento> logger,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(q))
        {
            errors["q"] = ["El texto de búsqueda es obligatorio."];
        }

        if (!TiposFicha.TryParse(tipo, out var tipoFicha))
        {
            errors["tipo"] = ["El Tipo debe ser Película, Serie, Libro, Juego o Cómic."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return await ConApiExterna(logger, async () =>
        {
            var resultados = await proveedores.Para(tipoFicha).BuscarAsync(tipoFicha, q!.Trim(), cancellationToken);
            return Results.Ok(resultados);
        });
    }

    private static async Task<IResult> SnapshotAsync(
        string? tipo,
        string? idExterno,
        ProveedoresEnriquecimiento proveedores,
        ILogger<ProveedoresEnriquecimiento> logger,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!TiposFicha.TryParse(tipo, out var tipoFicha))
        {
            errors["tipo"] = ["El Tipo debe ser Película, Serie, Libro, Juego o Cómic."];
        }

        if (string.IsNullOrWhiteSpace(idExterno))
        {
            errors["idExterno"] = ["El idExterno es obligatorio."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        return await ConApiExterna(logger, async () =>
        {
            var snapshot = await proveedores
                .Para(tipoFicha)
                .ConstruirSnapshotAsync(tipoFicha, idExterno!.Trim(), cancellationToken);
            return Results.Ok(SnapshotDto.Desde(snapshot));
        });
    }

    private static async Task<IResult> ConApiExterna(ILogger logger, Func<Task<IResult>> ejecutar)
    {
        try
        {
            return await ejecutar();
        }
        catch (Exception exception) when (EsFalloApiExterna(exception))
        {
            logger.LogWarning(exception, "Fallo al consultar una API externa de enriquecimiento.");
            return ProblemaApiExterna();
        }
    }

    private static IResult ProblemaApiExterna() => Results.Problem(
        statusCode: StatusCodes.Status502BadGateway,
        title: "No se pudo consultar la API externa.");

    private static bool EsFalloApiExterna(Exception exception) =>
        exception is ApiExternaException or HttpRequestException or JsonException;
}
