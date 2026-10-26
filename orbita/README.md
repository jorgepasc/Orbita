# Orbita

Aplicación personal para registrar y valorar el consumo de películas, series, libros, juegos y cómics.

## Requisitos

- .NET SDK 10
- .NET Aspire 13
- Node.js 24 y npm 11
- Docker Desktop en ejecución para PostgreSQL

## Desarrollo local

Desde el directorio `orbita`, configura las credenciales y una clave de firma en los user-secrets
del AppHost. La clave debe tener al menos 32 bytes UTF-8; genera una aleatoria así (compatible con
Windows PowerShell 5.1):

```powershell
dotnet user-secrets set "Authentication:Username" "tu-usuario" --project src/Orbita.AppHost/Orbita.AppHost.AppHost
dotnet user-secrets set "Authentication:Password" "tu-contraseña" --project src/Orbita.AppHost/Orbita.AppHost.AppHost
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 48
$rng.GetBytes($bytes)
$signingKey = [Convert]::ToBase64String($bytes)
$rng.Dispose()
dotnet user-secrets set "Authentication:SigningKey" $signingKey --project src/Orbita.AppHost/Orbita.AppHost.AppHost
```

Para el Enriquecimiento, regístrate gratis en [themoviedb.org](https://www.themoviedb.org/) y
[rawg.io](https://rawg.io/) y guarda sus claves en los mismos user-secrets (Open Library no
requiere clave):

```powershell
dotnet user-secrets set "Tmdb:BearerToken" "tu-token-de-tmdb" --project src/Orbita.AppHost/Orbita.AppHost.AppHost
dotnet user-secrets set "Rawg:ApiKey" "tu-api-key-de-rawg" --project src/Orbita.AppHost/Orbita.AppHost.AppHost
```

Issuer y Audience tienen valores locales predeterminados (`Orbita` y `Orbita.Web`); también se
pueden fijar como `Authentication:Issuer` y `Authentication:Audience`. La vida del token es de
60 minutos por defecto y se configura con `Authentication:TokenLifetimeMinutes`.

Desde el directorio del proyecto (`orbita`), ejecuta:

```powershell
dotnet run --project src/Orbita.AppHost/Orbita.AppHost.AppHost
```

Aspire levanta el backend, la aplicación Angular y PostgreSQL. Abre el dashboard de Aspire para acceder a los recursos. La UI también está disponible en `https://localhost:4200`.

En Azure configura las mismas claves como valores de configuración de la API con los nombres de
entorno `Authentication__Username`, `Authentication__Password` y `Authentication__SigningKey`
(y opcionalmente Issuer, Audience y TokenLifetimeMinutes). Las claves de enriquecimiento viajan
como `Tmdb__BearerToken` y `Rawg__ApiKey`.

La librería se navega por Estanterías (`/pantalla`, `/lecturas`, `/juegos`) y el detalle de cada Ficha vive en `/fichas/:id`. El backend recibe la conexión PostgreSQL en `ConnectionStrings__orbita`, gestionada por Aspire; el endpoint `GET /api/health` sigue disponible para Aspire y Azure aunque la UI ya no lo muestre.

## Arquitectura del backend

El backend sigue Clean Architecture con la regla de dependencias hacia dentro
(`Api → Infrastructure → Application → Domain`); `tests/Orbita.Api.Tests/ArquitecturaTests.cs` la vigila.

| Proyecto | Responsabilidad |
|----------|-----------------|
| `Orbita.Domain` | Agregado `Ficha` con sus invariantes (`TryCrear`/`Actualizar`), `TipoFicha`, `Snapshot` y `FuenteSnapshot`. Sin dependencias. |
| `Orbita.Application` | Casos de uso (`CrearFicha`, `BuscarResultados`…) y puertos: `IFichaRepositorio`, `IProveedorEnriquecimiento`, `IServicioAutenticacion`. |
| `Orbita.Infrastructure` | EF Core/Postgres y migraciones, clientes de TMDB, RAWG y Open Library, emisión de JWT. Se registra con `AddInfrastructure()`. |
| `Orbita.Api` | Endpoints HTTP, contratos (`FichaInput`, `SnapshotDto`…) y composición en `Program.cs`. |

Las migraciones viven en `Orbita.Infrastructure` y se generan con la API como proyecto de arranque:

```powershell
dotnet ef migrations add NombreMigracion --project src/Orbita.Infrastructure --startup-project src/Orbita.Api --output-dir Persistencia/Migrations
```

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

## Despliegue

Producción vive en Azure App Service F1 (Linux, .NET 10) con Postgres en Neon; ver el ADR
[0003](../docs/adr/0003-infraestructura-coste-cero-azure-neon.md). La API sirve también el build de
Angular desde `wwwroot`, así que hay un solo origen y un solo despliegue.

- Recursos: grupo `rg-orbita`, plan `plan-orbita` (F1, France Central) y web app `orbita-jorgepasc`.
- App settings de la web app: `ConnectionStrings__orbita` (formato Npgsql, conexión directa de Neon
  sin pooler), `Authentication__Username`, `Authentication__Password`, `Authentication__SigningKey`,
  `Tmdb__BearerToken` y `Rawg__ApiKey`.
- Cada push a `master` que toque `orbita/` ejecuta `.github/workflows/deploy.yml`: tests, lint y
  formato de backend y frontend, y después build de Angular, publish de la API y deploy.
- El workflow necesita el secreto de GitHub `AZURE_WEBAPP_PUBLISH_PROFILE`, con el publish profile de
  la web app (`az webapp deployment list-publishing-profiles -g rg-orbita -n orbita-jorgepasc --xml`).
- La API aplica las migraciones al arrancar. Tras un periodo de inactividad la primera petición tarda
  más (cold start del F1 y reactivación de Neon).

Para usarla en el móvil, abre la URL de la web app y elige «Añadir a pantalla de inicio» para instalar
la PWA.

### HTTPS en local

La API (`https://localhost:7001`) y la web (`https://localhost:4200`) usan el certificado de desarrollo de .NET.
Confíalo y expórtalo para Angular una sola vez:

```powershell
dotnet dev-certs https --trust
dotnet dev-certs https --export-path src/Orbita.Web/.certs/localhost.pem --format Pem --no-password
```
