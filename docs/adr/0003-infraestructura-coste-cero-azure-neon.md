---
status: accepted
---

# Infraestructura de coste cero: todo en Azure + Neon

Decidimos concentrar el hosting de aplicación en Azure — un único App Service F1 Free que ejecuta el backend .NET y sirve también el frontend Angular (PWA) compilado desde `wwwroot` — y usar Neon Free como única pieza externa para Postgres, con el repo en GitHub y CI/CD completo en GitHub Actions (monorepo, tests obligatorios antes de deploy).

**Por qué**: la regla innegociable es coste cero permanente, y ningún vendor grande ofrece hosting y Postgres gratis para siempre a la vez. Azure sí cubre el hosting gratis permanente; para Postgres, las opciones gratuitas de Azure/AWS/GCP solo duran 12 meses, así que Neon (gratis permanente, auto-resume tras suspensión) es la única opción válida — y es vendor-neutral: Postgres puro, migrable con un pg_dump.

**Frontend servido por la API**: el frontend llama a rutas relativas `/api/...`. Static Web Apps Free no puede enlazar un backend externo (solo el plan Standard) ni hacer de proxy, así que habría exigido CORS, una URL base configurable y dos despliegues, sin evitar el cold start de la API. Servir el build de Angular desde la propia API da un único origen y un único despliegue.

## Considered Options

- Static Web Apps Free para el frontend + App Service para la API — descartada: no permite proxy ni backend enlazado en el plan gratuito; exige CORS y una URL base de API
- Todo Azure, incluida base de datos — descartada: Azure Database for PostgreSQL solo es gratis 12 meses, viola el coste cero a largo plazo
- Mix Vercel + Azure + Neon — descartada a petición del usuario: tres vendors para el mismo resultado que dos, sin ganancia real
- Firebase — descartado: NoSQL y su ventaja (frontend sin backend) se anula con un backend .NET innegociable
- Render Free para el backend — funciona, pero fragmenta vendors; Azure unifica portal y CI/CD

## Consequences

- App Service F1 no soporta "Always On": hay cold start tras inactividad y un límite de 60 min CPU/día — aceptable para uso personal. La PWA guarda la app en caché y suaviza la primera carga.
- Neon se auto-suspende por inactividad y se reactiva sola en segundos al recibir conexiones.
- Frontend y API se despliegan siempre juntos: un cambio en cualquiera de los dos redespliega ambos.
