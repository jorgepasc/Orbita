using Orbita.Api.Fichas;

namespace Orbita.Api.Enriquecimiento;

public static class EnriquecimientoEndpoints
{
    public static IEndpointRouteBuilder MapEnriquecimientoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/enriquecimiento").RequireAuthorization();

        group.MapGet("/resultados", BuscarAsync);

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
        catch (ApiExternaException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "No se pudo consultar la API externa.",
                detail: exception.Message);
        }
    }
}
