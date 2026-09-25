---
paths:
  - "data/**"
  - "ReservArte-Infrastructure/Persistence/**"
---

# Datos: base de desarrollo, scripts de `data/` y migraciones

## Base de datos (dev)

Docker: contenedor `reservarte-sql`, base `ReservArteDB`, `localhost,1433`.
sqlcmd desde Git Bash:
`MSYS_NO_PATHCONV=1 docker exec -it reservarte-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<pwd-dev>' -C -d ReservArteDB -Q "..."`
**Escrituras (UPDATE/DELETE/INSERT) vía sqlcmd requieren `SET QUOTED_IDENTIFIER ON;` al inicio** (SELECT no).
Organización seed (determinista): `AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE` (More Than Brows).
Usuarios seed: `guille@svalero.com` (admin), empleadas en `@reservarte.com` y clientas en `@example.com`.

## Scripts SQL de `data/`

**Scripts SQL de `data/` — mantener SIEMPRE alineados con la base de datos** (decisión del usuario,
RA-869f17mzg). Dos tipos separados:
- `data/schema/`, **creación** (DDL, sin datos): `create_ReservArteDB.sql` **generado** desde las
  migraciones EF, **nunca editado a mano**, más `drop_ReservArteDB.sql`.
- `data/demo/`, **datos demo de desarrollo** (DML): `seed_demo_ReservArteDB.sql`, alineado con
  `DevSeeder` (mismas cuentas y contraseñas) más horarios demo con `0 = lunes`.

**Regla en cada cambio de base de datos:** en el MISMO PR que la migración, ejecutar
`bash data/schema/regenerate-create.sh`; si la migración toca una tabla que siembra el demo, o cambia
`DevSeeder`, actualizar `seed_demo`. Verificar creando una base de prueba con los scripts (nunca
sobre `ReservArteDB`) y arrancando la API contra ella. Detalle y orden (drop → create → demo):
`data/README.md`.

Demo de clientes: `carmen.lopez@example.com` y `sofia.ruiz@example.com` (`Cliente123!`) en
`DevSeeder` y `seed_demo`. Las citas y la lista de espera nacen vacías: nadie las siembra.

## Cuidado con `regenerate-create.sh`

- Usa `--no-build`: compila antes, o generará un `create` sin la migración nueva **y aun así
  informará de éxito**. Revisa el diff del `create` generado.
- En Windows fallaba entero (`DirectoryNotFoundException` de `dotnet ef`) porque usaba una variable
  `TMP`, que ahí ya es variable de entorno; se renombró a `SCRIPT_TMP`. Misma precaución con `TEMP`
  en cualquier script nuevo.

## Shells

- **zsh (Mac):** los comandos de `data/README.md` del tipo `SQLCMD="docker exec …"` +
  `$SQLCMD < fichero.sql` fallan, porque zsh no parte la variable en palabras, y `${PIPESTATUS[0]}`
  no existe (es `${pipestatus[1]}`; leerlo mal da un éxito falso). Esos bloques van en un `.sh` con
  `#!/usr/bin/env bash` y `set -euo pipefail`, ejecutado con `bash`.
- **Git Bash (Windows):** `MSYS_NO_PATHCONV=1` delante de `docker exec … sqlcmd`.

## Migraciones

- Antes del PR: `dotnet ef migrations has-pending-model-changes` limpio y el `create` regenerado.
- Migraciones pendientes en un equipo: `dotnet ef migrations list` (marca las `(Pending)`) o
  `SELECT MigrationId FROM __EFMigrationsHistory` frente a
  `ReservArte-Infrastructure/Persistence/Migrations/`.
- Convenciones de esquema (tablas en plural, `CatalogCheck`, únicos filtrados, `Restrict` en el
  histórico): en la regla de backend.
- Verificación siempre sobre una base desechable creada con los scripts (drop → create → demo) y con
  la API arrancada contra ella; nunca sobre `ReservArteDB`.
