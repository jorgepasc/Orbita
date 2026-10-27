using Microsoft.Extensions.DependencyInjection;
using Orbita.Application.CopiaSeguridad;
using Orbita.Application.Enriquecimiento;
using Orbita.Application.Fichas;

namespace Orbita.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListarFichas>();
        services.AddScoped<ObtenerFicha>();
        services.AddScoped<CrearFicha>();
        services.AddScoped<ActualizarFicha>();
        services.AddScoped<EliminarFicha>();
        services.AddScoped<ImportarFichas>();

        services.AddTransient<SelectorProveedores>();
        services.AddTransient<BuscarResultados>();
        services.AddTransient<ConstruirSnapshot>();

        return services;
    }
}
