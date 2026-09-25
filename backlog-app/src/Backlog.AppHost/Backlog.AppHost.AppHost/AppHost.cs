var builder = DistributedApplication.CreateBuilder(args);

var database = builder
    .AddPostgres("postgres")
    .AddDatabase("backlog");

var api = builder
    .AddProject<Projects.Backlog_Api>("api")
    .WithReference(database)
    .WaitFor(database);

builder
    .AddJavaScriptApp("web", "../../Backlog.Web", "start")
    .WithNpm()
    .WithHttpEndpoint(targetPort: 4200, port: 4200, name: "http", isProxied: false)
    .WaitFor(api);

builder.Build().Run();
