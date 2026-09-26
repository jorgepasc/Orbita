var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var databaseConnectionString = builder.Configuration.GetConnectionString("orbita");
builder.Services.AddSingleton(new OrbitaDatabaseOptions(databaseConnectionString));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/health", (OrbitaDatabaseOptions database) =>
    Results.Ok(new HealthResponse(
        Status: "ok",
        DatabaseConfigured: !string.IsNullOrWhiteSpace(database.ConnectionString))))
    .WithName("GetHealth");

app.Run();

public partial class Program;

public sealed record OrbitaDatabaseOptions(string? ConnectionString);

public sealed record HealthResponse(string Status, bool DatabaseConfigured);
