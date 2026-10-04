# Snapshot de enriquecimiento al crear la Ficha

Al crear una Ficha, el usuario busca el título contra las APIs públicas (TMDB para pelis/series, RAWG para juegos con su nota de Metacritic, Open Library para libros/cómics), elige el resultado y el backend congela un Snapshot en la base de datos: URL de portada del CDN, año, autoría (director o creador en pelis/series, estudio en juegos, autor en libros/cómics), Nota Externa, géneros e IDs externos. El Enriquecimiento es opcional: una Ficha puede crearse manual sin Snapshot.

**Por qué**: navegar la app nunca debe llamar a las APIs externas — es más rápido, no dependiente de terceros, y no quema los rate limits de los tiers gratuitos con navegación ordinaria. Las portadas se hotlinkean del CDN de la API (URLs estables y permitidas) con lazy loading; no se descargan binarios a la base de datos.

**Consecuencia**: los datos externos pueden quedar desactualizados respecto a la fuente; se acepta porque es consumo personal, y el Snapshot puede re-consultarse manualmente en el futuro. El Snapshot se construye al elegir el Resultado y viaja con la petición de creación: guardar una Ficha nunca llama a las APIs externas, así que el POST no puede fallar por culpa de un tercio. El Snapshot es inmutable tras crear; re-enriquecer queda para una iteración futura.
