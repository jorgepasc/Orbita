var builder = DistributedApplication.CreateBuilder(args);

var database = builder
    .AddPostgres("postgres")
    .AddDatabase("orbita");

var api = builder
    .AddProject<Projects.Orbita_Api>("api")
    .WithReference(database)
    .WaitFor(database);

foreach (var (configurationKey, environmentKey) in new[]
{
    ("Authentication:Username", "Authentication__Username"),
    ("Authentication:Password", "Authentication__Password"),
    ("Authentication:SigningKey", "Authentication__SigningKey"),
    ("Authentication:Issuer", "Authentication__Issuer"),
    ("Authentication:Audience", "Authentication__Audience"),
    ("Authentication:TokenLifetimeMinutes", "Authentication__TokenLifetimeMinutes")
})
{
    if (builder.Configuration[configurationKey] is { } value)
    {
        api = api.WithEnvironment(environmentKey, value);
    }
}

builder
    .AddJavaScriptApp("web", "../../Orbita.Web", "start")
    .WithNpm()
    .WithHttpEndpoint(targetPort: 4200, port: 4200, name: "http", isProxied: false)
    .WaitFor(api);

builder.Build().Run();
