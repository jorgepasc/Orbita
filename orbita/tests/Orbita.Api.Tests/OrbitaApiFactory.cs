using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Orbita.Api.Tests;

public sealed class OrbitaApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Username"] = "test-user",
                ["Authentication:Password"] = "test-password",
                ["Authentication:SigningKey"] = "test-signing-key-with-at-least-32-bytes",
                ["Authentication:Issuer"] = "orbita-tests",
                ["Authentication:Audience"] = "orbita-tests-client",
                ["Authentication:TokenLifetimeMinutes"] = "60"
            });
        });
    }
}
