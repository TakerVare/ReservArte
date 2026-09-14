#!/usr/bin/env bash
# =============================================================================
# Regenera data/schema/create_ReservArteDB.sql a partir de las migraciones de
# EF Core, que son la fuente de verdad del esquema.
#
# Cuándo: después de CADA migración nueva (y en el mismo PR que la migración).
# Requisito: solución compilada (`dotnet build`) y herramienta `dotnet-ef` 8.0.x.
# Uso (desde cualquier carpeta; en Windows, desde Git Bash):
#   bash data/schema/regenerate-create.sh
# =============================================================================
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
OUT="$ROOT/data/schema/create_ReservArteDB.sql"
TMP="$(mktemp)"
trap 'rm -f "$TMP"' EXIT

cd "$ROOT"

# Script IDEMPOTENTE: cada migración va protegida por su fila en
# __EFMigrationsHistory, así que se puede ejecutar varias veces y la API
# reconoce después la base de datos como migrada (no reaplica nada).
dotnet ef migrations script --idempotent --no-build \
  --project ReservArte-Infrastructure --startup-project ReservArte-API \
  -o "$TMP" > /dev/null

LAST_MIGRATION=$(dotnet ef migrations list --no-build \
  --project ReservArte-Infrastructure --startup-project ReservArte-API 2>/dev/null \
  | grep -E '^[0-9]{14}_' | tail -1 | sed 's/ (Pending)//')

{
  cat <<EOF
-- =============================================================================
-- ReservArte · CREACIÓN de la base de datos (solo esquema: DDL, sin datos)
-- =============================================================================
-- FICHERO GENERADO desde las migraciones de EF Core. NO EDITAR A MANO.
-- Un cambio de base de datos se hace con una migración y después se regenera:
--   bash data/schema/regenerate-create.sh
--
-- Última migración incluida: ${LAST_MIGRATION}
-- Idempotente: se puede ejecutar varias veces; las migraciones ya aplicadas
-- se saltan gracias a __EFMigrationsHistory.
-- Orden de uso: 1) drop_ReservArteDB.sql (opcional, DESTRUYE)
--               2) este fichero
--               3) data/demo/seed_demo_ReservArteDB.sql (solo desarrollo)
-- Detalle: data/README.md
-- =============================================================================

USE master;
GO

IF DB_ID(N'ReservArteDB') IS NULL
BEGIN
    CREATE DATABASE ReservArteDB;
END
GO

USE ReservArteDB;
GO

-- Opciones de sesión obligatorias para los índices filtrados de Identity
-- (EmailIndex, UserNameIndex). sqlcmd arranca con QUOTED_IDENTIFIER OFF, y sin
-- esto el script falla al crearlos (error 1934). Afectan a toda la sesión.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

EOF
  # El script de EF empieza con un BOM UTF-8: se quita para que la cabecera
  # quede al principio del fichero.
  LC_ALL=C sed $'1s/^\xef\xbb\xbf//' "$TMP"
} > "$OUT"

echo "Generado data/schema/create_ReservArteDB.sql (última migración: ${LAST_MIGRATION})"
