# Orbita

Contexto de una app personal para registrar y valorar el consumo de medios (películas, series, libros, videojuegos y cómics), accesible desde móvil y escritorio, con enriquecimiento automático desde APIs públicas. Sustituye a un Excel manual.

## Language

**Ficha**:
La unidad de registro de algo consumido, sea completado o abandonado. Contiene los datos personales del usuario y el snapshot de enriquecimiento.
_Avoid_: entrada, registro, item, elemento

**Tipo**:
La clase de medio de una Ficha: Película, Serie, Libro, Juego o Cómic. No define entidades separadas; solo filtra y habilita campos específicos.
_Avoid_: categoría, colección

**Estantería**:
Sección de navegación de la librería que agrupa uno o dos Tipos: Pantalla (Película y Serie), Lecturas (Libro y Cómic) y Juegos (Juego). Existe solo en navegación y filtrado; no es un atributo de la Ficha.
_Avoid_: categoría, sección, vista, tipo

**Terminado**:
Booleano que indica si la Ficha se completó. Es independiente de la Valoración: una Ficha puede estar sin terminar y tener nota.
_Avoid_: estado, status

**Fecha**:
El día en que se terminó la Ficha o en que se decidió no continuarla. No hay máquina de estados.
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

**Plataforma**:
Dato manual opcional del Tipo Juego: dónde se jugó (PC, Switch, PS5…). No viene del Snapshot.
_Avoid_: consola, dispositivo, sistema

**Idioma**:
Dato manual opcional de los Tipos Libro y Cómic: en qué idioma se leyó. No viene del Snapshot.
_Avoid_: lengua, traducción

**Autor**:
Dato manual opcional del Tipo Libro: quién lo escribió. Al elegir un Resultado se autorrellena con la autoría del Snapshot si está vacío, y después es editable.
_Avoid_: escritor, autoría (la autoría es el campo del Snapshot)

**Snapshot**:
Copia congelada de los datos de las APIs externas capturada al crear la Ficha: portada (URL de CDN), año, autoría (director o creador en Película/Serie, estudio en Juego, autor en Libro/Cómic), Nota Externa, géneros e IDs externos. Navegar la app nunca llama a las APIs externas.
_Avoid_: caché, metadatos

**Enriquecimiento**:
Proceso opcional al crear una Ficha: buscar en las APIs externas (TMDB para Película y Serie, RAWG para Juego, Open Library para Libro y Cómic), elegir el resultado y congelarlo en el Snapshot. Una Ficha sin Enriquecimiento es manual.
_Avoid_: importación, scraping

**Resultado**:
Cada candidatura devuelta por una API externa durante el Enriquecimiento, aún sin congelar. Elegir un Resultado dispara su construcción como Snapshot; el Tipo lo declara el usuario, no la API (Libro y Cómic consultan la misma API).
_Avoid_: hit, match, sugerencia

**Copia de seguridad**:
Fichero JSON versionado con todas las Fichas y sus Snapshots, descargable desde la app y reimportable. Restaurar es un upsert por Id (crea las que faltan, actualiza las existentes sin tocar su Snapshot) y es todo o nada. No es Enriquecimiento ni una importación desde Excel.
_Avoid_: export, dump, migración

## Decisiones

Ver `docs/adr/` para las decisiones arquitectónicas: Ficha única con Tipo (0001), Snapshot al crear (0002) e infraestructura de coste cero todo-Azure + Neon (0003).
