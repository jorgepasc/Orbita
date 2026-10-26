namespace Orbita.Api.Salud;

public sealed record OrbitaDatabaseOptions(string? ConnectionString);

public sealed record HealthResponse(string Status, bool DatabaseConfigured);

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", (OrbitaDatabaseOptions database) =>
            Results.Ok(new HealthResponse(
                Status: "ok",
                DatabaseConfigured: !string.IsNullOrWhiteSpace(database.ConnectionString))))
            .WithName("GetHealth")
            .AllowAnonymous();

        return endpoints;
    }
}
