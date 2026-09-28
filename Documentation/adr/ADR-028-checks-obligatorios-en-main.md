# ADR-028: Checks obligatorios en main

**Fecha:** 2026-09-28

**Estado:** aceptada

## Contexto

D-08 exige CI en cada pull request y checks requeridos en `main`. Había que concretar qué checks y si un administrador puede saltárselos. `develop` recibe commits directos de contexto (ADR-027): exigirle los mismos checks impediría ese traspaso.

## Decisión

`main` exige los checks `build-test-format` (workflow Backend CI) y `lint-build` (workflow Frontend CI), también a los administradores. No exige que la rama esté al día. `develop` no tiene protección de checks.

## Alternativas descartadas

- Dejar los checks en opcional.
- Proteger también `develop`, lo que bloquearía los commits de contexto.

## Consecuencias

Un pull request a `main` no se fusiona sin los dos checks, ni aunque lo abra un administrador. Este ADR concreta el de D-08; no lo sustituye.

## Tareas relacionadas

H-34. Tarea `869f6r4t8`. Relacionado con ADR-008 y ADR-027.
