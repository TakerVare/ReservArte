# ADR-029: Aserciones con AwesomeAssertions

**Fecha:** 2026-09-28

**Estado:** aceptada

## Contexto

D-10 saca FluentAssertions 8 por su licencia comercial y deja abierto el sustituto. Se comparó AwesomeAssertions 9.6.0 (Apache-2.0, la API de FluentAssertions 8) con FluentAssertions 7.2.2 (la rama que sigue en licencia permisiva y ya no se actualiza). Las dos compilaban sin errores y pasaban la misma batería (522/522).

## Decisión

Las aserciones se escriben con AwesomeAssertions 9.6.0. Resuelve la parte de aserciones de D-10 (DP-03).

## Alternativas descartadas

- FluentAssertions 7.2.2, rama congelada: misma batería, y no se puede actualizar sin entrar en la licencia de pago.
- FluentAssertions 8, de pago para uso comercial.

## Consecuencias

El espacio de nombres es `AwesomeAssertions`. No se reintroduce FluentAssertions. Este ADR no sustituye al ADR-010: aquel decide salir de la licencia; este dice con qué librería.

## Tareas relacionadas

H-35. Tarea `869f6r7yh`. Relacionado con ADR-010; no lo sustituye.
