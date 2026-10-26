using Microsoft.EntityFrameworkCore;
using Orbita.Domain.Fichas;

namespace Orbita.Api.Fichas;

public sealed class FichaDbContext(DbContextOptions<FichaDbContext> options) : DbContext(options)
{
    public DbSet<Ficha> Fichas => Set<Ficha>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ficha = modelBuilder.Entity<Ficha>();
        ficha.ToTable("Ficha", table =>
            table.HasCheckConstraint(
                "CK_Ficha_Valoracion",
                "\"Valoracion\" IS NULL OR (\"Valoracion\" >= 0 AND \"Valoracion\" <= 10)"));
        ficha.HasKey(value => value.Id);
        ficha.Property(value => value.Titulo).HasMaxLength(LimitesFicha.Titulo).IsRequired();
        ficha.Property(value => value.Tipo)
            .HasConversion(
                tipo => TiposFicha.Nombre(tipo),
                nombre => TiposFicha.Parse(nombre))
            .HasMaxLength(20)
            .IsRequired();
        ficha.Property(value => value.Comentarios).HasMaxLength(LimitesFicha.Comentarios);
        ficha.Property(value => value.Compania).HasMaxLength(LimitesFicha.Texto);
        ficha.Property(value => value.Plataforma).HasMaxLength(LimitesFicha.Texto);
        ficha.Property(value => value.Idioma).HasMaxLength(LimitesFicha.Texto);
        ficha.Property(value => value.Autor).HasMaxLength(LimitesFicha.Texto);
        ficha.Property(value => value.Snapshot)
            .HasConversion(
                snapshot => SnapshotDocumento.Serializar(snapshot!),
                json => SnapshotDocumento.Deserializar(json))
            .HasColumnType("jsonb");
    }
}
