# Orbita Web

Aplicación Angular del monorepo Orbita. El comando `start` usa la configuración de producción para servir también el service worker de la PWA.

## Desarrollo local

Desde `src/Orbita.Web`:

```powershell
npm ci
npm start
```

Para que la pantalla muestre el backend cuando se arranca Angular por separado, ejecuta la API en `http://localhost:5001`.

## Verificación

```powershell
npm test
npm run lint
npm run format:check
npm run build
```
