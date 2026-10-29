using Microsoft.EntityFrameworkCore;
using Orbita.Domain.Fichas;

namespace Orbita.Infrastructure.Persistencia;

// Fichas de ejemplo para la base de datos local de desarrollo: dos por Estantería, con Snapshot.
// IDs externos y portadas son reales; notas, géneros y detalles son una copia aproximada congelada a mano.
public static class SemillaDesarrollo
{
    public static async Task<int> SembrarAsync(FichaDbContext contexto, CancellationToken cancellationToken)
    {
        if (await contexto.Fichas.AnyAsync(cancellationToken))
        {
            return 0;
        }

        var fichas = Fichas();
        contexto.Fichas.AddRange(fichas);
        await contexto.SaveChangesAsync(cancellationToken);
        return fichas.Count;
    }

    public static IReadOnlyList<Ficha> Fichas() =>
    [
        Crear(
            new DatosFicha("Dune", "Película", new DateOnly(2026, 9, 12), true, 8.5m,
                "La ambientación y el sonido, de cine grande.", "Georgia", null, null, null),
            new Snapshot(
                FuenteSnapshot.Tmdb,
                "438631",
                "https://image.tmdb.org/t/p/w500/d5NXSklXo0qyIYkgV94XAgMIckC.jpg",
                2021,
                "Denis Villeneuve",
                7.8m,
                ["Ciencia ficción", "Aventura"],
                new DetallesPelicula(
                    ["Timothée Chalamet", "Rebecca Ferguson", "Oscar Isaac", "Josh Brolin"],
                    ["US"],
                    155))),
        Crear(
            new DatosFicha("El viaje de Chihiro", "Película", new DateOnly(2026, 10, 3), true, 9.5m,
                null, "Family", null, null, null),
            new Snapshot(
                FuenteSnapshot.Tmdb,
                "129",
                "https://image.tmdb.org/t/p/w500/39wmItIWsg5sZMyRUHLkWBcuVCM.jpg",
                2001,
                "Hayao Miyazaki",
                8.5m,
                ["Animación", "Familia", "Fantasía"],
                new DetallesPelicula(
                    ["Rumi Hiiragi", "Miyu Irino", "Mari Natsuki", "Takashi Naito"],
                    ["JP"],
                    125))),
        Crear(
            new DatosFicha("Dune", "Libro", new DateOnly(2026, 8, 20), true, 9m,
                null, null, null, "es", "Frank Herbert"),
            new Snapshot(
                FuenteSnapshot.OpenLibrary,
                "/works/OL893414W",
                "https://covers.openlibrary.org/b/id/11481354-L.jpg",
                1965,
                "Frank Herbert",
                null,
                ["Dune (Imaginary place)", "Fiction", "Fiction, science fiction, general"],
                new DetallesLibro(608))),
        Crear(
            new DatosFicha("El nombre del viento", "Libro", new DateOnly(2026, 10, 15), false, null,
                "Lo dejé a mitad; quizá lo retome.", null, null, "es", "Patrick Rothfuss"),
            new Snapshot(
                FuenteSnapshot.OpenLibrary,
                "/works/OL8479867W",
                "https://covers.openlibrary.org/b/id/11480483-L.jpg",
                2007,
                "Patrick Rothfuss",
                null,
                ["American fantasy fiction", "Epic fiction"],
                new DetallesLibro(736))),
        Crear(
            new DatosFicha("The Witcher 3: Wild Hunt", "Juego", new DateOnly(2026, 7, 30), true, 9.5m,
                null, "Solo", "PC", null, null),
            new Snapshot(
                FuenteSnapshot.Rawg,
                "3328",
                "https://media.rawg.io/media/games/618/618c2031a07bbff6b4f611f10b6bcdbc.jpg",
                2015,
                "CD PROJEKT RED",
                92,
                ["Action", "Adventure", "RPG"],
                new DetallesJuego(["PC", "PlayStation 4", "Xbox One", "Nintendo Switch"], 46))),
        Crear(
            new DatosFicha("Portal 2", "Juego", new DateOnly(2026, 9, 28), true, 8m,
                "El cooperativo es lo mejor.", "Georgia", "PC", null, null),
            new Snapshot(
                FuenteSnapshot.Rawg,
                "4200",
                "https://media.rawg.io/media/games/2ba/2bac0e87cf45e5b508f227d281c9252a.jpg",
                2011,
                "Valve Software",
                95,
                ["Shooter", "Puzzle"],
                new DetallesJuego(["PC", "PlayStation 3", "Xbox 360"], 11)))
    ];

    private static Ficha Crear(DatosFicha datos, Snapshot snapshot) =>
        Ficha.TryCrear(datos, snapshot, out var ficha, out var errores)
            ? ficha
            : throw new InvalidOperationException(
                $"La semilla de desarrollo '{datos.Titulo}' no es válida: " +
                string.Join("; ", errores.Detalle.Select(error => $"{error.Key}: {error.Value[0]}")));
}
