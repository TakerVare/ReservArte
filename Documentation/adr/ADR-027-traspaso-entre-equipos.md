# ADR-027: Traspaso entre equipos a través del repo

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

Hay dos equipos (Mac y Windows) y la memoria automática del asistente es local de cada uno. El estado que solo vive en un disco llega viejo al otro. Los commits directos a `develop` de código se mezclarían con el traspaso.

## Decisión

El traspaso entre equipos pasa por el repositorio. `.claude/contexto/estado.md` se actualiza en cada punto de control. Los ficheros de `.claude/contexto/` son los únicos con commits directos a `develop` (`chore(contexto)`).

## Alternativas descartadas

- Fiar el traspaso a la memoria local de cada equipo.

## Consecuencias

`develop` no exige pull request precisamente para estos commits. Proteger `develop` con los mismos checks que `main` los bloquearía (ADR-028). El resto del código entra por rama y pull request.

## Tareas relacionadas

D-27. Tarea `869f6r4ba`.
