using System.Text.Json;
using Orbita.Api.Fichas;

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

        try
        {
            var resultados = await proveedores.Para(tipoFicha).BuscarAsync(tipoFicha, q!.Trim(), cancellationToken);
            return Results.Ok(resultados);
        }
        catch (Exception exception) when (EsFalloApiExterna(exception))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "No se pudo consultar la API externa.");
        }
    }

    private static async Task<IResult> SnapshotAsync(
        string? tipo,
        string? idExterno,
        ProveedoresEnriquecimiento proveedores,
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

        try
        {
            var snapshot = await proveedores
                .Para(tipoFicha)
                .ConstruirSnapshotAsync(tipoFicha, idExterno!.Trim(), cancellationToken);
            return Results.Ok(snapshot);
        }
        catch (Exception exception) when (EsFalloApiExterna(exception))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "No se pudo consultar la API externa.");
        }
    }

    private static bool EsFalloApiExterna(Exception exception) =>
        exception is ApiExternaException or HttpRequestException or JsonException;
}
