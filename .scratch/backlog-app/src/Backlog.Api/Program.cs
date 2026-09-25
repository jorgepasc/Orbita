var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var databaseConnectionString = builder.Configuration.GetConnectionString("backlog");
builder.Services.AddSingleton(new BacklogDatabaseOptions(databaseConnectionString));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/api/health", (BacklogDatabaseOptions database) =>
    Results.Ok(new HealthResponse(
        Status: "ok",
        DatabaseConfigured: !string.IsNullOrWhiteSpace(database.ConnectionString))))
    .WithName("GetHealth");

app.Run();

public partial class Program;

public sealed record BacklogDatabaseOptions(string? ConnectionString);

public sealed record HealthResponse(string Status, bool DatabaseConfigured);
