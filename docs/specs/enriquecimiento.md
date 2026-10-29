# Enriquecimiento — Spec de iteración

Estado: diseño cerrado en sesión de grilling (2026-10-04). Términos según `CONTEXT.md`; decisiones de fondo en ADR 0001 (Ficha única con Tipo) y 0002 (Snapshot al crear).

## Objetivo

Al crear una Ficha, poder buscar en APIs externas gratuitas, elegir un Resultado y congelarlo como Snapshot. Los cinco Tipos son enriquecibles desde el primer corte.

| Tipo | API | Nota Externa |
|------|-----|--------------|
| Película | TMDB (`/search/movie`) | vote_average de TMDB (0–10) |
| Serie | TMDB (`/search/tv`) | vote_average de TMDB (0–10) |
| Juego | RAWG (`/api/games`) | nota Metacritic vía RAWG (0–100, a veces nula) |
| Libro | Open Library (`/search.json`) | sin nota |
| Cómic | Open Library (misma API que Libro) | sin nota |

Libro y Cómic consultan la misma API: el Tipo lo declara el usuario al crear, no lo deduce la API.

## Decisiones

1. **Backend proxy**: todas las llamadas a TMDB/RAWG/Open Library salen del backend .NET. Las keys viven en user-secrets (local) y App Settings (Azure), nunca en el frontend.
2. **Flujo integrado con escape manual**: el formulario de creación muestra la búsqueda para cualquier Tipo; el enlace "añadir manualmente" la salta y crea la Ficha sin Snapshot (como hoy).
3. **Snapshot al elegir, no al guardar**: elegir un Resultado dispara un endpoint de detalle que construye el Snapshot y lo devuelve al formulario (preview). El POST de creación viaja con el Snapshot ya construido: guardar nunca llama a terceros ni puede fallar por su culpa.
4. **Inmutable tras crear**: editar una Ficha no toca el Snapshot. Re-enriquecer (elegir la "Dune" equivocada → re-buscar) queda para una iteración futura; workaround actual: borrar y recrear.
5. **Idioma**: TMDB con `language=es-ES` (géneros en español, título localizado). RAWG y Open Library no localizan: llegan como estén.
6. **Sin normalizar la Nota Externa**: se guarda el valor crudo de la fuente (7,8 de TMDB, 84 de Metacritic). Nunca se compara con la Valoración.

## Snapshot

Núcleo común para todos los Tipos más un `detalles` opcional propio de cada Tipo (ADR 0004). Columna `Ficha.Snapshot`, jsonb, ya existe en el modelo y la migración:

```json
{
  "fuente": "tmdb | rawg | openlibrary",
  "idExterno": "438631",
  "portada": "https://image.tmdb.org/t/p/w500/….jpg",
  "anio": 2021,
  "autoria": "Denis Villeneuve",
  "notaExterna": 7.8,
  "generos": ["Ciencia ficción", "Aventura"],
  "detalles": {
    "tipo": "pelicula",
    "actores": ["Timothée Chalamet", "Zendaya"],
    "paises": ["US", "CA"],
    "duracionMinutos": 155
  }
}
```

- `detalles`: se omite si el proveedor no aporta nada para ese Tipo. Hoy existen `pelicula` (actores principales, máximo 4; países en ISO 3166-1; duración) y `libro` (páginas, compartido por Libro y Cómic; Open Library solo las expone como `number_of_pages_median` en la búsqueda, así que es un dato aproximado y se omite si no hay). Juego (plataformas, horas medias) y Serie (actores, países, temporadas, episodios) llegarán como derivados nuevos. El dominio rechaza unos `detalles` que no correspondan al Tipo.

- `autoria`: director (Película), creador (Serie, `created_by[0]`), estudio (Juego, `developers[0]`), autor (Libro/Cómic). El significado lo da el Tipo de la Ficha.
- `notaExterna`: nula para Libro/Cómic y para juegos sin nota Metacritic.
- `generos`: en Open Library son `subjects` (texto libre y ruidoso); se aceptan tal cual, recortando a un máximo razonable (p. ej. 5).
- `portada`: URL del CDN hotlinkeada (ADR 0002); se guarda a tamaño tarjeta (~w500 en TMDB). Portada ausente → campo nulo, la UI pinta placeholder.

## Contrato de API

Autenticadas con el mismo JWT que `/api/fichas`.

- `GET /api/enriquecimiento/resultados?q={texto}&tipo={Tipo}` → lista de Resultados: `{ idExterno, titulo, anio, portada (thumb), notaExterna }`. Máximo 20, sin paginación. El `tipo` usa los nombres del glosario (`Película`, `Serie`, `Juego`, `Libro`, `Cómic`).
- `GET /api/enriquecimiento/snapshot?tipo={Tipo}&idExterno={id}` → el Snapshot (shape de arriba), construido desde el detalle de la API correspondiente (TMDB con `append_to_response=credits` para el director).
- `POST /api/fichas` / `PUT`: `FichaInput`/`FichaResponse` añaden `snapshot` (objeto opcional). El backend valida que la `fuente` del Snapshot sea válida para el `Tipo` de la Ficha (tmdb → Película/Serie, rawg → Juego, openlibrary → Libro/Cómic). En `PUT`, el Snapshot se ignora (inmutable).

## Frontend (Angular)

- El panel de creación gana, encima del formulario actual, un paso de búsqueda visible para todos los Tipos: input + botón buscar + lista de Resultados (tarjeta con portada, título, año, nota si existe).
- Elegir un Resultado → `snapshot` del backend → preview en el formulario (portada, autoría, géneros, Nota Externa) + `titulo` prerrelleno con el título del Resultado (localizado, editable). Guardar manda FichaInput + snapshot.
- "Añadir manualmente" (siempre visible) oculta la búsqueda y muestra el formulario actual tal cual.
- Sin resultados / API caída: mensaje + el escape manual sigue ahí.
- Tarjeta de Ficha en el dashboard: portada, badge de Nota Externa y chips de géneros cuando hay Snapshot; sin Snapshot, la tarjeta actual.

## Pruebas

- Tests de integración con el HTTP externo stubbeado (HttpMessageHandler falso por cliente TMDB/RAWG/OL): nunca se llaman APIs reales desde CI, ni se necesitan keys para testear.
- Casos: búsqueda por Tipo, construcción de Snapshot por fuente, `fuente` incompatible con el Tipo rechazada, POST con Snapshot se persiste, PUT no modifica el Snapshot, Ficha manual sigue funcionando.

## Configuración

- `Tmdb:BearerToken` (cuenta gratuita en themoviedb.org), `Rawg:ApiKey` (cuenta gratuita en rawg.io). Open Library no requiere key.
- Local: user-secrets. Azure: App Settings del App Service.

## Fuera de alcance de esta iteración

- Re-enriquecer / reemplazar Snapshot al editar.
- Búsqueda mixta (una query contra varias APIs a la vez).
- Paginación de Resultados.
- Importación del Excel histórico (si existió, no está en este repo).

## Pendientes

- [ ] Darse de alta en TMDB y RAWG para obtener las keys.
- [ ] Confirmar que el tamaño de portada elegido (w500) rinde bien en tarjeta y móvil.
