#!/usr/bin/env bash
# =============================================================================
# Regenera data/schema/create_ReservArteDB.sql a partir de las migraciones de
# EF Core, que son la fuente de verdad del esquema.
#
# Cuándo: después de CADA migración nueva (y en el mismo PR que la migración).
# Requisito: solución compilada (`dotnet build`) y herramienta `dotnet-ef` 10.0.x.
# Motor: PostgreSQL (D-28). El script resultante se ejecuta con psql (ver su cabecera).
# Uso (desde cualquier carpeta; en Windows, desde Git Bash):
#   bash data/schema/regenerate-create.sh
# =============================================================================
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="$ROOT/data/schema/create_ReservArteDB.sql"
# OJO con el nombre: en Windows, TMP y TEMP son variables de entorno YA
# exportadas, así que asignarlas aquí se las pasa a `dotnet ef`, que resuelve
# Path.GetTempFileName() contra un fichero en vez de un directorio y falla con
# DirectoryNotFoundException. En macOS la variable equivalente es TMPDIR y el
# choque no se da.
SCRIPT_TMP="$(mktemp)"
trap 'rm -f "$SCRIPT_TMP"' EXIT

cd "$ROOT"

# Script IDEMPOTENTE: cada migración va protegida por su fila en
# __EFMigrationsHistory, así que se puede ejecutar varias veces y la API
# reconoce después la base de datos como migrada (no reaplica nada).
dotnet ef migrations script --idempotent --no-build \
  --project ReservArte-Infrastructure --startup-project ReservArte-API \
  -o "$SCRIPT_TMP" > /dev/null

LAST_MIGRATION=$(dotnet ef migrations list --no-build \
  --project ReservArte-Infrastructure --startup-project ReservArte-API 2>/dev/null \
  | grep -E '^[0-9]{14}_' | tail -1 | sed 's/ (Pending)//')

{
  cat <<EOF
-- =============================================================================
-- ReservArte · CREACIÓN de la base de datos PostgreSQL (solo esquema, sin datos)
-- =============================================================================
-- FICHERO GENERADO desde las migraciones de EF Core. NO EDITAR A MANO.
-- Un cambio de base de datos se hace con una migración y después se regenera:
--   bash data/schema/regenerate-create.sh
--
-- Última migración incluida: ${LAST_MIGRATION}
-- Idempotente: se puede ejecutar varias veces; crea la base solo si no existe y
-- salta las migraciones ya aplicadas gracias a __EFMigrationsHistory.
-- Uso (conectado a la base de mantenimiento «postgres»):
--   docker exec -i reservarte-pg psql -U reservarte -d postgres -v ON_ERROR_STOP=1 < data/schema/create_ReservArteDB.sql
-- Otra base (p. ej. una desechable): añadir -v db=nombre. Por defecto, reservarte.
-- Orden: 1) drop_ReservArteDB.sql (opcional, DESTRUYE)  2) este fichero
--        3) data/demo/seed_demo_ReservArteDB.sql (solo desarrollo). Detalle: data/README.md
-- =============================================================================

\if :{?db}
\else
\set db reservarte
\endif

SELECT format('CREATE DATABASE %I', :'db')
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = :'db') \gexec

\connect :db

EOF
  # El script de EF puede empezar con un BOM UTF-8: se quita para que la
  # cabecera quede al principio del fichero.
  LC_ALL=C sed $'1s/^\xef\xbb\xbf//' "$SCRIPT_TMP"
} > "$OUT"

echo "Generado data/schema/create_ReservArteDB.sql (última migración: ${LAST_MIGRATION})"
