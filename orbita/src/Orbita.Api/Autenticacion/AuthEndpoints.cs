using System.Security.Claims;
using Orbita.Application.Autenticacion;

namespace Orbita.Api.Autenticacion;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", (LoginRequest request, IServicioAutenticacion autenticacion) =>
        {
            var sesion = autenticacion.IniciarSesion(request.Username, request.Password);

            return sesion is null
                ? Results.Unauthorized()
                : Results.Ok(new LoginResponse(sesion.AccessToken, sesion.ExpiresAtUtc));
        })
            .AllowAnonymous();

        endpoints.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
            Results.Ok(new CurrentUserResponse(principal.Identity?.Name ?? string.Empty)))
            .RequireAuthorization();

        return endpoints;
    }
}
