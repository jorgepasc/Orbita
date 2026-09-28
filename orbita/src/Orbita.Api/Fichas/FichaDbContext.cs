using Microsoft.EntityFrameworkCore;

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
        ficha.Property(value => value.Titulo).HasMaxLength(300).IsRequired();
        ficha.Property(value => value.Tipo)
            .HasConversion(
                tipo => TiposFicha.Nombre(tipo),
                nombre => TiposFicha.Parse(nombre))
            .HasMaxLength(20)
            .IsRequired();
        ficha.Property(value => value.Comentarios).HasMaxLength(4000);
        ficha.Property(value => value.Compania).HasMaxLength(200);
        ficha.Property(value => value.Snapshot)
            .HasConversion(
                snapshot => snapshot!.RootElement.GetRawText(),
                json => System.Text.Json.JsonDocument.Parse(json))
            .HasColumnType("jsonb");
    }
}
