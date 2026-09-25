---
status: accepted
---

# Infraestructura de coste cero: todo en Azure + Neon

Decidimos concentrar el hosting de aplicación en Azure — Static Web Apps Free para el frontend Angular (PWA) y App Service F1 Free para el backend .NET — y usar Neon Free como única pieza externa para Postgres, con el repo en GitHub y CI/CD completo en GitHub Actions (monorepo, un workflow por app con path filters, tests obligatorios antes de deploy).

**Por qué**: la regla innegociable es coste cero permanente, y ningún vendor grande ofrece las tres piezas gratis para siempre a la vez. Azure sí cubre frontend y backend gratis permanentes; para Postgres, las opciones gratuitas de Azure/AWS/GCP solo duran 12 meses, así que Neon (gratis permanente, auto-resume tras suspensión) es la única opción válida — y es vendor-neutral: Postgres puro, migrable con un pg_dump.

## Considered Options

- Todo Azure, incluida base de datos — descartada: Azure Database for PostgreSQL solo es gratis 12 meses, viola el coste cero a largo plazo
- Mix Vercel + Azure + Neon — descartada a petición del usuario: tres vendors para el mismo resultado que dos, sin ganancia real
- Firebase — descartado: NoSQL y su ventaja (frontend sin backend) se anula con un backend .NET innegociable
- Render Free para el backend — funciona, pero fragmenta vendors; Azure unifica portal y CI/CD

## Consequences

- App Service F1 no soporta "Always On": hay cold start tras inactividad y un límite de 60 min CPU/día — aceptable para uso personal.
- Neon se auto-suspende por inactividad y se reactiva sola en segundos al recibir conexiones.
