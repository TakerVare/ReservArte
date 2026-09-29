---
paths:
  - "data/**"
  - "ReservArte-Infrastructure/Persistence/**"
---

# Datos: base de desarrollo, scripts de `data/` y migraciones

## Base de datos (dev)

Motor: **PostgreSQL 18** (D-28). Docker: contenedor `reservarte-pg` (imagen `postgres:18`), usuario
`reservarte`, base `reservarte`, publicado solo en `127.0.0.1:5432`, volumen `reservarte_pgdata` y
zona horaria UTC. La cadena de conexión va en User Secrets (`ConnectionStrings:DefaultConnection`).
psql dentro del contenedor (no pide contraseña por el socket local):
`docker exec -i reservarte-pg psql -U reservarte -d reservarte -v ON_ERROR_STOP=1 -c "..."`
En Git Bash (Windows), `MSYS_NO_PATHCONV=1` delante si algún argumento empieza por `/`.
Organización seed (determinista): `AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE` (More Than Brows).
Usuarios seed: `guille@svalero.com` (admin), empleadas en `@reservarte.com` y clientas en `@example.com`.

## SQL a mano en PostgreSQL

- Tablas y columnas en PascalCase: **siempre entre comillas dobles** (`SELECT "Email" FROM "Customers"`).
  Sin comillas, PostgreSQL las pasa a minúsculas y no las encuentra.
- Booleanos con `TRUE`/`FALSE`, no `1`/`0`; fechas `timestamptz` en UTC (`now()`).
- Las comparaciones de texto distinguen mayúsculas: los emails se guardan en minúsculas (H-37, CHECK
  `CK_*_EmailLowercase`), así que se buscan en minúsculas.
- Tras insertar Ids explícitos en una columna de identidad, `setval` para avanzar la secuencia (el
  `seed_demo` ya lo hace), o el siguiente alta chocará con la clave primaria.

## Scripts SQL de `data/`

**Scripts SQL de `data/` — mantener SIEMPRE alineados con la base de datos** (decisión del usuario,
RA-869f17mzg). Dos tipos separados:
- `data/schema/`, **creación** (DDL, sin datos): `create_ReservArteDB.sql` **generado** desde las
  migraciones EF, **nunca editado a mano**, más `drop_ReservArteDB.sql`.
- `data/demo/`, **datos demo de desarrollo** (DML): `seed_demo_ReservArteDB.sql`, alineado con
  `DevSeeder` (mismas cuentas y contraseñas) más horarios demo con `0 = lunes`.

Los tres son scripts de **psql** (usan `\if`, `\gexec`, `\connect`) y aceptan `-v db=<nombre>` para
apuntar a otra base; por defecto, `reservarte`. `drop` y `create` se lanzan conectados a la base de
mantenimiento `postgres`.

**Regla en cada cambio de base de datos:** en el MISMO PR que la migración, ejecutar
`bash data/schema/regenerate-create.sh`; si la migración toca una tabla que siembra el demo, o cambia
`DevSeeder`, actualizar `seed_demo`. Verificar creando una base desechable con los scripts
(`-v db=ra_<algo>`, nunca la base `reservarte`) y arrancando la API contra ella. Detalle y orden
(drop → create → demo): `data/README.md`.

Demo de clientes: `carmen.lopez@example.com` y `sofia.ruiz@example.com` (`Cliente123!`) en
`DevSeeder` y `seed_demo`. Las citas y la lista de espera nacen vacías: nadie las siembra.

## Cuidado con `regenerate-create.sh`

- Usa `--no-build`: compila antes, o generará un `create` sin la migración nueva **y aun así
  informará de éxito**. Revisa el diff del `create` generado.
- En Windows fallaba entero (`DirectoryNotFoundException` de `dotnet ef`) porque usaba una variable
  `TMP`, que ahí ya es variable de entorno; se renombró a `SCRIPT_TMP`. Misma precaución con `TEMP`
  en cualquier script nuevo.

## Shells

- **zsh (Mac):** una variable con un comando entero (`PSQL="docker exec -i reservarte-pg psql …"` +
  `$PSQL < fichero.sql`) falla, porque zsh no parte la variable en palabras, y `${PIPESTATUS[0]}`
  no existe (es `${pipestatus[1]}`; leerlo mal da un éxito falso). Esos bloques van en un `.sh` con
  `#!/usr/bin/env bash` y `set -euo pipefail`, ejecutado con `bash`.
- **Git Bash (Windows):** `MSYS_NO_PATHCONV=1` delante de `docker exec` cuando un argumento sea una
  ruta absoluta del contenedor.

## Migraciones

- Antes del PR: `dotnet ef migrations has-pending-model-changes` limpio y el `create` regenerado.
- El historial arranca en `InitialCreate` (2026-09-29, `869f8pmpa`): las 12 migraciones de SQL
  Server se retiraron con el cambio de motor.
- En `migrationBuilder.Sql()`, en filtros de índices únicos y en CHECK, el SQL es de PostgreSQL:
  identificadores entre comillas dobles y booleanos `TRUE`/`FALSE`. Un `[Col]` o un `= 1` de SQL
  Server compila en C# y solo falla al aplicar la migración.
- El `create` regenerado se ejecuta siempre entero sobre la base desechable, dos veces (es
  idempotente y así se comprueba).
- Migraciones pendientes en un equipo: `dotnet ef migrations list` (marca las `(Pending)`) o
  `SELECT "MigrationId" FROM "__EFMigrationsHistory"` frente a
  `ReservArte-Infrastructure/Persistence/Migrations/`.
- Convenciones de esquema (tablas en plural, `CatalogCheck`, únicos filtrados, `Restrict` en el
  histórico): en la regla de backend.
- Verificación siempre sobre una base desechable creada con los scripts (drop → create → demo) y con
  la API arrancada contra ella; nunca sobre la base `reservarte`.
