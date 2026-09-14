# ReservArte

Repositorio para la solución completa de gestión multi-tenant para centros de diseño de cejas. La **documentación técnica** vive en `Documentation/` (tres volúmenes: análisis, implementación y planificación). La **gestión del proyecto** se hace en **ClickUp** (workspace *ReservArte*: Spaces Backend, Frontend, Mobile, **Infrastructure** y **Documentation** — detalle en el volumen de planificación). **Git:** *Git Flow*, [Conventional Commits](https://www.conventionalcommits.org/) y plantilla de PR en [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md) (volumen 3 §10.1.2). Stack: **ASP.NET Core 8**, **Vue 3 + Vite** (web), **React Native** (móvil), **SQL Server en Docker**, **AWS**, **Cloudinary** (imágenes), **Redsys**. Autenticación: **Identity** (local + **Google, Apple, Instagram/Meta**) y **JWT**; **2FA opcional** (TOTP) por usuario.

## Base de datos (desarrollo)

SQL Server corre en Docker (contenedor `reservarte-sql`, base `ReservArteDB`, `localhost,1433`). Hay dos formas de tenerla lista:

- **Arrancando la API en `Development`**: aplica las migraciones de EF Core y siembra los datos mínimos (`DevSeeder`) automáticamente.
- **Con los scripts SQL de [`data/`](data/README.md)**, sin arrancar la API. Orden: `schema/drop_ReservArteDB.sql` (opcional, **destruye** la base) → `schema/create_ReservArteDB.sql` (esquema, generado desde las migraciones) → `demo/seed_demo_ReservArteDB.sql` (datos demo, **solo desarrollo**).

Los scripts de creación se regeneran tras cada migración (`bash data/schema/regenerate-create.sh`) y los de datos demo se mantienen alineados con el esquema. Comandos, cuentas demo y reglas de mantenimiento: [`data/README.md`](data/README.md).
