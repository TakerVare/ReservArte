# ADR-013: Mapa central de errores HTTP

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El mapa de código de error a status HTTP estaba copiado en cada controlador y ya divergía: el mismo código acababa en 409 en un endpoint y en 500 en otro. No había un manejador global de excepciones: un error no controlado salía como 500 sin el envelope. El 400 de model binding devolvía `ProblemDetails`. Los endpoints de citas iban a traer `APT_INVALID_STATE`, y el mismo error no puede significar dos status.

## Decisión

Antes de los endpoints de citas: un mapa central de código de aplicación a HTTP, un manejador global de excepciones y el envelope también en los 400 de model binding.

## Alternativas descartadas

- Seguir copiando el mapa en cada controlador.

## Consecuencias

Un código de error tiene un status. El controlador no traduce por su cuenta. El hueco del 400 sin envelope se cierra en el pipeline, no caso a caso.

## Tareas relacionadas

D-13. Tareas `869f6r5r2`, `869f1k17q`.
