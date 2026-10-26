using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orbita.Api.Enriquecimiento;
using Orbita.Api.Fichas;
using Orbita.Application;
using Orbita.Application.Autenticacion;
using Orbita.Infrastructure;
using Orbita.Infrastructure.Autenticacion;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AuthenticationSettings>>((options, settingsOptions) =>
    {
        var settings = settingsOptions.Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = settings.ClaveDeFirma(),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var databaseConnectionString = builder.Configuration.GetConnectionString("orbita");
builder.Services.AddSingleton(new OrbitaDatabaseOptions(databaseConnectionString));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(databaseConnectionString))
{
    await app.Services.AplicarMigracionesAsync();
}

app.MapDefaultEndpoints();

app.MapGet("/api/health", (OrbitaDatabaseOptions database) =>
    Results.Ok(new HealthResponse(
        Status: "ok",
        DatabaseConfigured: !string.IsNullOrWhiteSpace(database.ConnectionString))))
    .WithName("GetHealth")
    .AllowAnonymous();

app.MapPost("/api/auth/login", (LoginRequest request, IServicioAutenticacion autenticacion) =>
{
    var sesion = autenticacion.IniciarSesion(request.Username, request.Password);

    return sesion is null
        ? Results.Unauthorized()
        : Results.Ok(new LoginResponse(sesion.AccessToken, sesion.ExpiresAtUtc));
})
    .AllowAnonymous();

app.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
    Results.Ok(new CurrentUserResponse(principal.Identity?.Name ?? string.Empty)))
    .RequireAuthorization();

app.MapFichaEndpoints();
app.MapEnriquecimientoEndpoints();

// En producción la API sirve también el build de Angular desde wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallback("/api/{**rest}", () => Results.NotFound()).AllowAnonymous();
app.MapFallbackToFile("index.html").AllowAnonymous();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

public partial class Program;

public sealed record OrbitaDatabaseOptions(string? ConnectionString);

public sealed record HealthResponse(string Status, bool DatabaseConfigured);
