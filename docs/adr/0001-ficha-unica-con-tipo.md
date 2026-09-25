# Ficha única con campo Tipo

Una Ficha puede ser Película, Serie, Libro, Juego o Cómic. Decidimos modelar todo como una única entidad con un campo Tipo discriminador más campos específicos opcionales por tipo (Autor/Idioma en libros, etc.), en lugar de cuatro o cinco entidades separadas con sus propias tablas y CRUDs.

**Por qué**: la app es personal y de un solo usuario; las Fichas comparten los mismos campos núcleo (Título, Fecha, Terminado, Valoración, Comentarios, Compañía) y el producto se hojea como una sola colección con filtros, no como colecciones aisladas. Cuatro entidades multiplicarían el código, la UI y la migración del Excel sin beneficio real. Quien quiera granularidad (p. ej. "Breaking Bad T2") la consigue escribiendo el título que quiera; el patrón por defecto es una Ficha por título.

## Considered Options

- Una entidad Ficha con Tipo (elegida)
- Una entidad por tipo de medio con tablas separadas
