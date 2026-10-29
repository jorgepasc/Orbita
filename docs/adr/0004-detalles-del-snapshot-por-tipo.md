# Detalles del Snapshot por Tipo

El Snapshot (ADR 0002) conserva su núcleo común (`fuente`, `idExterno`, `portada`, `anio`, `autoria`, `notaExterna`, `generos`) y gana un campo opcional `detalles`: un objeto polimórfico con un discriminador `tipo` cuya forma depende del Tipo de la Ficha. Cada Tipo define sus campos de enriquecimiento propios (Película: actores, países, duración; Libro y Cómic: páginas; Juego: plataformas, horas medias; Serie: actores, países, temporadas, episodios) en lugar de añadirlos todos al núcleo como campos opcionales.

**Por qué**: el Snapshot plano habría acumulado campos que son siempre nulos fuera de su Tipo, que es justo el problema que se quería evitar al mirar la tabla `Ficha`. Un `jsonb` con documento polimórfico da la estructura de una tabla por Tipo sin joins ni migraciones de esquema, y mantiene ADR 0001 (una sola tabla `Ficha`). Los datos siguen sin consultarse por dentro (nada filtra ni ordena por ellos); si algún día hiciera falta, ese campo concreto se promociona a columna.

**Consecuencias**:

- Compatibilidad: `detalles` se omite si es nulo. Los Snapshots ya persistidos y las copias de seguridad anteriores siguen siendo válidos y se leen con `detalles` nulo; no hay migración de datos.
- Coherencia: el dominio rechaza (`snapshot.detalles`) unos detalles que no correspondan al Tipo de la Ficha, igual que ya hace con la `fuente`.
- Extensión: añadir los detalles de un Tipo es un derivado nuevo en dominio, documento `jsonb` y DTO, más su proveedor. Cada derivado es una pieza que el cliente puede enviar, así que la validación vive en el dominio.
- `autoria` sigue siendo la fuente única de director, creador, estudio o autor; no se duplica en `detalles`.
- El Snapshot sigue siendo inmutable tras crear (ADR 0002): las Fichas existentes no ganan `detalles` salvo que se re-enriquezcan en una iteración futura.
- Los países se guardan como código ISO 3166-1; la presentación (nombre o bandera) es cosa del frontend.

## Considered Options

- Núcleo común más `detalles` polimórfico (elegida)
- Snapshot plano con todos los campos opcionales
- Núcleo más diccionario libre (`extras`), descartado por no tener tipado ni validación
- Tablas de detalle por Tipo, descartado por coste frente a un beneficio que hoy no existe
