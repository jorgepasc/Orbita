# Orbita

Aplicación personal para registrar y valorar el consumo de películas, series, libros, juegos y cómics.

## Requisitos

- .NET SDK 10
- .NET Aspire 13
- Node.js 24 y npm 11
- Docker Desktop en ejecución para PostgreSQL

## Desarrollo local

Desde el directorio del proyecto (`orbita`), ejecuta un único comando:

```powershell
dotnet run --project src/Orbita.AppHost/Orbita.AppHost.AppHost
```

Aspire levanta el backend, la aplicación Angular y PostgreSQL. Abre el dashboard de Aspire para acceder a los recursos. La UI también está disponible en `http://localhost:4200`.

La aplicación Angular consulta `GET /api/health` a través de su proxy local. El backend recibe la conexión PostgreSQL en `ConnectionStrings__orbita`, gestionada por Aspire.

## Verificación

```powershell
dotnet test Orbita.slnx
dotnet build Orbita.slnx --no-restore
dotnet format Orbita.slnx --verify-no-changes
Set-Location src/Orbita.Web
npm ci
npm test
npm run lint
npm run format:check
npm run build
```

El build de producción de Angular incluye el manifest y el service worker de la PWA.
