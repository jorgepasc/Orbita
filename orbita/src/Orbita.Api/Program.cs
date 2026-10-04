using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orbita.Api.Enriquecimiento;
using Orbita.Api.Fichas;

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
            IssuerSigningKey = CreateSigningKey(settings.SigningKey),
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

builder.Services.AddOptions<TmdbOptions>().BindConfiguration(TmdbOptions.SectionName);
builder.Services.AddOptions<RawgOptions>().BindConfiguration(RawgOptions.SectionName);
builder.Services.AddHttpClient<TmdbProveedor>();
builder.Services.AddHttpClient<RawgProveedor>();
builder.Services.AddHttpClient<OpenLibraryProveedor>();
builder.Services.AddTransient<ProveedoresEnriquecimiento>();

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

app.MapPost("/api/auth/login", (LoginRequest request, AuthenticationSettings settings) =>
{
    if (!CredentialsMatch(request.Username, settings.Username)
        || !CredentialsMatch(request.Password, settings.Password))
    {
        return Results.Unauthorized();
    }

    var now = DateTimeOffset.UtcNow;
    var expiresAt = now.AddMinutes(settings.TokenLifetimeMinutes);
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: settings.Issuer,
        audience: settings.Audience,
        claims: [new Claim(ClaimTypes.Name, settings.Username)],
        notBefore: now.UtcDateTime,
        expires: expiresAt.UtcDateTime,
        signingCredentials: new SigningCredentials(
            CreateSigningKey(settings.SigningKey),
            SecurityAlgorithms.HmacSha256));

    return Results.Ok(new LoginResponse(
        AccessToken: new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token),
        ExpiresAtUtc: expiresAt));
})
    .AllowAnonymous();

app.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
    Results.Ok(new CurrentUserResponse(principal.Identity?.Name ?? string.Empty)))
    .RequireAuthorization();

app.MapFichaEndpoints();
app.MapEnriquecimientoEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

static bool CredentialsMatch(string? actual, string expected)
{
    if (actual is null)
    {
        return false;
    }

    var actualBytes = Encoding.UTF8.GetBytes(actual);
    var expectedBytes = Encoding.UTF8.GetBytes(expected);

    return actualBytes.Length == expectedBytes.Length
        && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
}

static SymmetricSecurityKey CreateSigningKey(string signingKey) =>
    new(Encoding.UTF8.GetBytes(signingKey));

public partial class Program;

public sealed record OrbitaDatabaseOptions(string? ConnectionString);

public sealed record HealthResponse(string Status, bool DatabaseConfigured);
