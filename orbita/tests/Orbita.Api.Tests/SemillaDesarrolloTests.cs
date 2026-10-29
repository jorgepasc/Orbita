using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orbita.Domain.Fichas;
using Orbita.Infrastructure.Persistencia;

namespace Orbita.Api.Tests;

public sealed class SemillaDesarrolloTests
{
    [Fact]
    public void Seed_has_two_fichas_per_tipo_and_every_one_carries_a_snapshot_with_detalles()
    {
        var fichas = SemillaDesarrollo.Fichas();

        Assert.Equal(2, fichas.Count(ficha => ficha.Tipo == TipoFicha.Pelicula));
        Assert.Equal(2, fichas.Count(ficha => ficha.Tipo == TipoFicha.Libro));
        Assert.Equal(2, fichas.Count(ficha => ficha.Tipo == TipoFicha.Juego));
        Assert.All(fichas, ficha =>
        {
            Assert.NotNull(ficha.Snapshot);
            Assert.NotNull(ficha.Snapshot.Portada);
            Assert.NotNull(ficha.Snapshot.Detalles);
        });
    }

    [Fact]
    public async Task Seeding_fills_an_empty_database_once_and_never_touches_existing_fichas()
    {
        var options = new DbContextOptionsBuilder<FichaDbContext>()
            .UseInMemoryDatabase($"Semilla-{Guid.NewGuid()}")
            .Options;
        await using var contexto = new FichaDbContext(options);

        Assert.Equal(6, await SemillaDesarrollo.SembrarAsync(contexto, CancellationToken.None));
        Assert.Equal(0, await SemillaDesarrollo.SembrarAsync(contexto, CancellationToken.None));
        Assert.Equal(6, await contexto.Fichas.CountAsync());
    }

    [Fact]
    public async Task Api_without_the_seed_flag_starts_with_no_fichas()
    {
        await using var factory = new OrbitaApiFactory();
        using var scope = factory.Services.CreateScope();

        var contexto = scope.ServiceProvider.GetRequiredService<FichaDbContext>();

        Assert.False(await contexto.Fichas.AnyAsync());
    }
}
