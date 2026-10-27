using System.Reflection;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Tests;

// Regla de dependencias de Clean Architecture: Api -> Infrastructure -> Application -> Domain.
public sealed class ArquitecturaTests
{
    private static readonly Assembly Domain = typeof(Ficha).Assembly;
    private static readonly Assembly Application = typeof(Orbita.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Orbita.Infrastructure.DependencyInjection).Assembly;

    private static readonly string[] FrameworksDeInfraestructura =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore",
        "Microsoft.IdentityModel",
        "System.IdentityModel"
    ];

    [Fact]
    public void Domain_depends_on_nothing_but_the_base_class_library()
    {
        var referencias = Referencias(Domain);

        Assert.DoesNotContain(referencias, nombre => nombre.StartsWith("Orbita.", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias, nombre => nombre.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_depends_only_on_the_domain_and_not_on_frameworks_of_infrastructure()
    {
        var referencias = Referencias(Application);

        Assert.DoesNotContain("Orbita.Infrastructure", referencias);
        Assert.DoesNotContain("Orbita.Api", referencias);
        Assert.DoesNotContain(
            referencias,
            nombre => FrameworksDeInfraestructura.Any(prefijo => nombre.StartsWith(prefijo, StringComparison.Ordinal)));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_the_api()
    {
        Assert.DoesNotContain("Orbita.Api", Referencias(Infrastructure));
    }

    private static string[] Referencias(Assembly ensamblado) =>
        [.. ensamblado.GetReferencedAssemblies().Select(referencia => referencia.Name!)];
}
