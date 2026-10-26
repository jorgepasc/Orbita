using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orbita.Api.Autenticacion;
using Orbita.Api.Enriquecimiento;
using Orbita.Api.Fichas;
using Orbita.Application;
using Orbita.Application.Autenticacion;
using Orbita.Application.Enriquecimiento;
using Orbita.Application.Fichas;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddOptions<AuthenticationSettings>()
    .BindConfiguration(AuthenticationSettings.SectionName)
    .Validate(settings => settings.HasValidValues,
        "Authentication username and password are required, the signing key must be at least 32 UTF-8 bytes, and token lifetime must be positive.")
    .ValidateOnStart();
builder.Services.AddSingleton(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<AuthenticationSettings>>().Value);
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
            IssuerSigningKey = ServicioAutenticacionJwt.CrearClaveDeFirma(settings.SigningKey),
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
builder.Services.AddDbContext<FichaDbContext>(options => options.UseNpgsql(databaseConnectionString));

builder.Services.AddApplication();
builder.Services.AddScoped<IFichaRepositorio, FichaRepositorio>();
builder.Services.AddSingleton<IServicioAutenticacion, ServicioAutenticacionJwt>();

builder.Services.AddOptions<TmdbOptions>().BindConfiguration(TmdbOptions.SectionName);
builder.Services.AddOptions<RawgOptions>().BindConfiguration(RawgOptions.SectionName);
builder.Services.AddHttpClient<TmdbProveedor>();
builder.Services.AddHttpClient<RawgProveedor>();
builder.Services.AddHttpClient<OpenLibraryProveedor>();
builder.Services.AddTransient<IProveedorEnriquecimiento>(sp => sp.GetRequiredService<TmdbProveedor>());
builder.Services.AddTransient<IProveedorEnriquecimiento>(sp => sp.GetRequiredService<RawgProveedor>());
builder.Services.AddTransient<IProveedorEnriquecimiento>(sp => sp.GetRequiredService<OpenLibraryProveedor>());

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(databaseConnectionString))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<FichaDbContext>().Database.MigrateAsync();
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
