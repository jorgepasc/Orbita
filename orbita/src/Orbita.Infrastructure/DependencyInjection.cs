using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orbita.Application.Autenticacion;
using Orbita.Application.Enriquecimiento;
using Orbita.Application.Fichas;
using Orbita.Infrastructure.Autenticacion;
using Orbita.Infrastructure.Enriquecimiento;
using Orbita.Infrastructure.Persistencia;

namespace Orbita.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AuthenticationSettings>()
            .BindConfiguration(AuthenticationSettings.SectionName)
            .Validate(settings => settings.HasValidValues,
                "Authentication username and password are required, the signing key must be at least 32 UTF-8 bytes, and token lifetime must be positive.")
            .ValidateOnStart();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<AuthenticationSettings>>().Value);
        services.AddSingleton<IServicioAutenticacion, ServicioAutenticacionJwt>();

        var databaseConnectionString = configuration.GetConnectionString("orbita");
        services.AddDbContext<FichaDbContext>(options => options.UseNpgsql(databaseConnectionString));
        services.AddScoped<IFichaRepositorio, FichaRepositorio>();

        services.AddOptions<TmdbOptions>().BindConfiguration(TmdbOptions.SectionName);
        services.AddOptions<RawgOptions>().BindConfiguration(RawgOptions.SectionName);
        services.AddHttpClient<TmdbProveedor>();
        services.AddHttpClient<RawgProveedor>();
        services.AddHttpClient<OpenLibraryProveedor>();
        services.AddTransient<IProveedorEnriquecimiento>(sp => sp.GetRequiredService<TmdbProveedor>());
        services.AddTransient<IProveedorEnriquecimiento>(sp => sp.GetRequiredService<RawgProveedor>());
        services.AddTransient<IProveedorEnriquecimiento>(sp => sp.GetRequiredService<OpenLibraryProveedor>());

        return services;
    }

    public static async Task AplicarMigracionesAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<FichaDbContext>().Database.MigrateAsync();
    }

    // Solo para la base de datos local: no hace nada si ya hay Fichas.
    public static async Task<int> SembrarDatosDesarrolloAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await SemillaDesarrollo.SembrarAsync(
            scope.ServiceProvider.GetRequiredService<FichaDbContext>(),
            CancellationToken.None);
    }
}
