using Orbita.Api.Autenticacion;
using Orbita.Api.Enriquecimiento;
using Orbita.Api.Fichas;
using Orbita.Api.Salud;
using Orbita.Application;
using Orbita.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAutenticacionJwt();

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

app.MapHealthEndpoints();
app.MapAuthEndpoints();
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
