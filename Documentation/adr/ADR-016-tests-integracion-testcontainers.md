# ADR-016: Integración con SQL Server real

**Fecha:** 2026-09-24

**Estado:** sustituida por ADR-031

## Contexto

Los repositorios se prueban contra SQLite, que no se comporta como SQL Server en colaciones, `LIKE` o `DateOnly`/`TimeOnly`. No hay tests de integración HTTP ni Testcontainers, aunque la estrategia de testing los daba por base. No existe el proyecto `tests/ReservArte.IntegrationTests`.

## Decisión

Los tests de integración usan SQL Server real (Testcontainers) y `WebApplicationFactory`.

## Alternativas descartadas

- Conformarse con SQLite.
- Usar el proveedor InMemory de EF Core.

## Consecuencias

Hasta que ese proyecto exista, la integración de repositorio sigue siendo SQLite dentro de `tests/ReservArte.UnitTests`. La estrategia de testing describe el diseño aprobado y lo distingue de lo que ya corre.

## Tareas relacionadas

D-16. Tareas `869f6r5ng`, `869f2gh37`.
