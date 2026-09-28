# ADR-008: CI obligatorio en cada pull request

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

Sin CI, el formato, los tests y el lint dependían de que alguien los ejecutara. La tarea del pipeline llevaba en el backlog desde mayo. Con el volumen de código que genera la IA, la auditoría lo señaló como la inversión con más retorno.

## Decisión

El CI es obligatorio: build, test, format y lint en cada pull request, con checks requeridos en `main`.

## Alternativas descartadas

- Dejar las puertas de calidad como casillas manuales de la plantilla de pull request.

## Consecuencias

Un pull request no se sostiene solo con casillas marcadas. El detalle de los workflows está en la estrategia de testing; qué checks exige `main` está en el ADR-028. Este ADR no los sustituye: fija que el CI existe y es obligatorio.

## Tareas relacionadas

D-08. Tareas `869d7ex56`, `869d7ex8r`, `869f6r4t8`. Véase también ADR-028.
