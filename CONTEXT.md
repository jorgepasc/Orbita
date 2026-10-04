# Orbita

Contexto de una app personal para registrar y valorar el consumo de medios (películas, series, libros, videojuegos y cómics), accesible desde móvil y escritorio, con enriquecimiento automático desde APIs públicas. Sustituye a un Excel manual.

## Language

**Ficha**:
La unidad de registro de algo consumido o pendiente de consumir. Contiene los datos personales del usuario y el snapshot de enriquecimiento.
_Avoid_: entrada, registro, item, elemento

**Tipo**:
La clase de medio de una Ficha: Película, Serie, Libro, Juego o Cómic. No define entidades separadas; solo filtra y habilita campos específicos.
_Avoid_: categoría, colección

**Terminado**:
Booleano que indica si la Ficha se completó. Es independiente de la Valoración: una Ficha puede estar sin terminar y tener nota.
_Avoid_: estado, status

**Fecha**:
El día en que se terminó la Ficha o en que se decidió no continuarla. Una Ficha sin Fecha y sin Terminar es un pendiente (backlog). No hay máquina de estados.
_Avoid_: fecha de finalización (el término sugiere que solo aplica a lo terminado), fecha de inicio

**Valoración**:
Nota personal de 0 a 10 con decimales, independiente de Terminado y de la Nota Externa.
_Avoid_: puntuación, rating

**Nota Externa**:
Valoración agregada de una fuente pública (nota de TMDB, Metacritic), capturada en el Snapshot. Nunca se compara con la Valoración en el modelo; son campos distintos. Libro y Cómic no tienen Nota Externa: ninguna API gratuita la ofrece.
_Avoid_: nota IMDB, nota media

**Compañía**:
Persona(s) con las que se disfrutó la Ficha (ej. Georgia, Family, Solo). Dato manual, opcional en todos los Tipos.
_Avoid_: ver con, acompañante, estudio (el estudio/developer es otro concepto y viene de la API)

**Snapshot**:
Copia congelada de los datos de las APIs externas capturada al crear la Ficha: portada (URL de CDN), año, autoría (director o creador en Película/Serie, estudio en Juego, autor en Libro/Cómic), Nota Externa, géneros e IDs externos. Navegar la app nunca llama a las APIs externas.
_Avoid_: caché, metadatos

**Enriquecimiento**:
Proceso opcional al crear una Ficha: buscar en las APIs externas (TMDB para Película y Serie, RAWG para Juego, Open Library para Libro y Cómic), elegir el resultado y congelarlo en el Snapshot. Una Ficha sin Enriquecimiento es manual.
_Avoid_: importación, scraping

**Resultado**:
Cada candidatura devuelta por una API externa durante el Enriquecimiento, aún sin congelar. Elegir un Resultado dispara su construcción como Snapshot; el Tipo lo declara el usuario, no la API (Libro y Cómic consultan la misma API).
_Avoid_: hit, match, sugerencia

## Decisiones

Ver `docs/adr/` para las decisiones arquitectónicas: Ficha única con Tipo (0001), Snapshot al crear (0002) e infraestructura de coste cero todo-Azure + Neon (0003).
