# Orbita

Aplicación personal para registrar y valorar el consumo de películas, series, libros, juegos y cómics.

## Requisitos

- .NET SDK 10
- .NET Aspire 13
- Node.js 24 y npm 11
- Docker Desktop en ejecución para PostgreSQL

## Desarrollo local

Configura las credenciales y una clave de firma en los user-secrets del AppHost. La clave debe
tener al menos 32 bytes UTF-8; este comando genera una aleatoria:

```powershell
dotnet user-secrets set "Authentication:Username" "tu-usuario" --project src/Orbita.AppHost/Orbita.AppHost.AppHost
dotnet user-secrets set "Authentication:Password" "tu-contraseña" --project src/Orbita.AppHost/Orbita.AppHost.AppHost
dotnet user-secrets set "Authentication:SigningKey" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))) --project src/Orbita.AppHost/Orbita.AppHost.AppHost
```

Issuer y Audience tienen valores locales predeterminados (`Orbita` y `Orbita.Web`); también se
pueden fijar como `Authentication:Issuer` y `Authentication:Audience`. La vida del token es de
60 minutos por defecto y se configura con `Authentication:TokenLifetimeMinutes`.

Desde el directorio del proyecto (`orbita`), ejecuta:

```powershell
dotnet run --project src/Orbita.AppHost/Orbita.AppHost.AppHost
```

Aspire levanta el backend, la aplicación Angular y PostgreSQL. Abre el dashboard de Aspire para acceder a los recursos. La UI también está disponible en `http://localhost:4200`.

En Azure configura las mismas claves como valores de configuración de la API con los nombres de
entorno `Authentication__Username`, `Authentication__Password` y
`Authentication__SigningKey` (y opcionalmente Issuer, Audience y TokenLifetimeMinutes).

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
