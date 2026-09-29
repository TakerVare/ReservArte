-- =============================================================================
-- ReservArte: eliminar la base de datos (PostgreSQL)
-- DESTRUYE la base y todos sus datos. WITH (FORCE) cierra antes las sesiones
-- abiertas (PostgreSQL 13+).
--
-- Uso (conectado a la base de mantenimiento «postgres», no a la que se borra):
--   docker exec -i reservarte-pg psql -U reservarte -d postgres -v ON_ERROR_STOP=1 < data/schema/drop_ReservArteDB.sql
-- Otra base (p. ej. una desechable): añadir -v db=nombre. Por defecto, reservarte.
-- =============================================================================

\if :{?db}
\else
\set db reservarte
\endif

SELECT format('DROP DATABASE IF EXISTS %I WITH (FORCE)', :'db') \gexec
\echo Base de datos :db eliminada (si existía).
