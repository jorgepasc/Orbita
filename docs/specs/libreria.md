# Librería y navegación — Spec de iteración

Estado: diseño cerrado en sesión de grilling (2026-10-04). Términos según `CONTEXT.md`; estructura de Ficha única con campos por Tipo según ADR 0001. Esta spec prevalece sobre la descripción de tarjeta de `docs/specs/enriquecimiento.md` donde se solapen (p. ej. chips de géneros → un solo chip).

## Objetivo

Sustituir el dashboard plano por una librería organizada en tres Estanterías con tarjetas de póster y valoraciones, con detalle por Ficha y creación contextual. La app registra solo lo consumido: el backlog desaparece del modelo.

## Decisiones

1. **Estanterías**: tres secciones de navegación, no se persisten ni son atributo de la Ficha:
   - **Pantalla** — Película y Serie
   - **Lecturas** — Libro y Cómic
   - **Juegos** — Juego
2. **Aterrizaje**: tras login, `''` redirige a `/pantalla`. Sin home global ni listado mixto de Tipos.
3. **Solo lo consumido**: toda Ficha es consumida, completada o abandonada. No hay pendientes ni backlog en esta iteración.
4. **Fecha obligatoria**: al crear y al editar, siempre. El estado "sin Fecha y sin Terminar" deja de ser válido. `Terminado` distingue completada de no continuada.
5. **Campos por Tipo nuevos**, opcionales y manuales: **Plataforma** (Juego: PC, Switch, PS5…) e **Idioma** (Libro y Cómic: es, en…). Pueden crecer divergentes en el futuro: mantenerlos opcionales y discriminados por Tipo.
6. **Tarjeta de la Estantería**:
   - Portada 2:3 del Snapshot; placeholder si Ficha manual.
   - Un chip de género (el primero del Snapshot) en la esquina del póster; nada si no hay Snapshot.
   - Título.
   - Valoración y Nota Externa si existen, en crudo; la Nota Externa de Metacritic (0–100) lleva sufijo "MC".
   - Marca "No continuada" si `Terminado = false`.
   - Sin fecha, sin año, sin chip de Tipo.
7. **Detalle de Ficha**: ruta plana `/fichas/:id` (no anidada por Estantería). Cabecera con el Snapshot: portada, título, autoría con etiqueta por Tipo (Director en Película, Creador en Serie, Estudio en Juego, Autor en Libro y Cómic), año, Nota Externa y todos los géneros. Cuerpo personal: Fecha, Completada/No continuada, Valoración, Compañía, Comentarios y Plataforma o Idioma según Tipo. Libro y Cómic no pintan Nota Externa. Ficha manual: cabecera solo con título. Editar reutiliza el formulario actual en panel superpuesto; Borrar pide confirmación y vuelve a la Estantería.
8. **Creación contextual**: cada Estantería tiene su "+"; el Tipo queda preseleccionado por la Estantería (en las dobles, elección entre sus dos Tipos; en Juegos, directo). El Enriquecimiento hereda ese Tipo.
9. **Listado** — todo en cliente con el `GET /api/fichas` actual:
   - Filtro de Tipo en estanterías dobles: "Todo | Película | Serie" y "Todo | Libro | Cómic".
   - Búsqueda por título dentro de la propia Estantería.
   - Orden por defecto por Fecha descendente, agrupado por meses con cabecera `MMMM YYYY` ("marzo 2026"), meses descendentes y dentro de cada mes Fecha descendente.
   - Selector de orden "Fecha | Valoración": Valoración pinta una lista plana sin meses, de mayor a menor nota, con las Fichas sin Valoración al final (desempate por Fecha descendente y título). Sin filtros extra.
10. **Cabecera ilustrada**: cada Estantería pinta junto a su título una ilustración de línea propia (claqueta, libro con bocadillo, mando) con una animación sutil que respeta `prefers-reduced-motion`.
11. **Navegación responsive**: móvil (PWA) con tab bar inferior fija de las 3 Estanterías; escritorio con barra superior horizontal y contenido de ancho máximo. Grid de 2–3 columnas en móvil, 5–6 en escritorio.
12. **El dashboard muere**: la vista actual de `''` desaparece con su widget de estado del backend/postgres (scaffolding de desarrollo, no producto). Cada Estantería tiene su empty state.

## Cambios en el modelo y la API

- `Ficha` + migración: columnas nuevas `Plataforma` (string, nullable) e `Idioma` (string, nullable). `FichaInput`/`FichaResponse` las añaden.
- Validación (agregado `Ficha`, `Ficha.TryCrear`/`Actualizar` en `Orbita.Domain`): Fecha obligatoria siempre (antes solo si `Terminado`); `Plataforma` solo aceptada con Tipo Juego; `Idioma` solo con Tipo Libro o Cómic (error 400 en caso contrario, mismo criterio que la `fuente` del Snapshot).
- `GET /api/fichas` no cambia de forma: devuelve todas las Fichas; Estanterías, filtros, orden y búsqueda se resuelven en cliente.
- Rutas Angular: `/pantalla`, `/lecturas`, `/juegos`, `/fichas/:id`; `''` → redirect a `/pantalla`.

## Formulario de creación/edición

- Fecha siempre obligatoria (fuera el condicional `terminado` actual).
- Campo Plataforma visible solo con Tipo Juego; campo Idioma solo con Libro o Cómic.
- Tarjeta: fuera el estado "Pendiente"; quedan "Terminado" / "No continuada".

## Pruebas

- Backend: POST/PUT rechazan Fecha ausente; Plataforma rechazada fuera de Juego; Idioma fuera de Libro/Cómic; round-trip de los campos nuevos.
- Frontend: cada Estantería filtra sus Tipos; el segmented de las dobles filtra por Tipo; agrupación mensual `MMMM YYYY` descendente; búsqueda por título; tarjeta con/sin Snapshot y con/sin Valoración; detalle con etiquetas de autoría por Tipo; "+" contextual preselecciona el Tipo.

## Fuera de alcance de esta iteración

- Backlog/pendientes (reversible a futuro: basta con relajar la Fecha).
- Filtros extra (completadas vs. abandonadas).
- Vista tabla tipo Excel con celdas editables (sin portada), alternativa a las tarjetas.
- Búsqueda global multi-Tipo.
- Campos por Tipo adicionales (formato de lectura, tomos, temporadas) — pendiente revisar el Excel de origen.
- Importación del Excel histórico.

## Pendientes

- [ ] Revisar el Excel de origen por campos por Tipo que valgan la pena (más allá de Plataforma e Idioma).
- [ ] Verificar que no existen Fichas sin Fecha en la base de datos antes de desplegar la validación estricta; si las hay, darles Fecha o borrarlas.
