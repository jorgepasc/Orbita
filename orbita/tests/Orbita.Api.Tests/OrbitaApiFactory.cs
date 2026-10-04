using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Orbita.Api.Enriquecimiento;
using Orbita.Api.Fichas;

namespace Orbita.Api.Tests;

public sealed class OrbitaApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"OrbitaTests-{Guid.NewGuid()}";

    public StubApisExternas ApisExternas { get; } = new();

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
                ["Authentication:TokenLifetimeMinutes"] = "60",
                ["Tmdb:BearerToken"] = "test-tmdb-token",
                ["Rawg:ApiKey"] = "test-rawg-key"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FichaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FichaDbContext>>();
            services.RemoveAll<FichaDbContext>();
            services.AddDbContext<FichaDbContext>(options => options.UseInMemoryDatabase(databaseName));

            services.AddHttpClient<TmdbProveedor>().ConfigurePrimaryHttpMessageHandler(() => ApisExternas);
            services.AddHttpClient<RawgProveedor>().ConfigurePrimaryHttpMessageHandler(() => ApisExternas);
            services.AddHttpClient<OpenLibraryProveedor>()
                .ConfigurePrimaryHttpMessageHandler(() => ApisExternas);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<FichaDbContext>().Database.EnsureCreated();
        return host;
    }
}
