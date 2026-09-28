# ADR-003: Entrega en vertical hacia un MVP piloto

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La auditoría describió una construcción en horizontal: API de empleados, clientes, catálogo y paquetes que ninguna pantalla consumía. El cliente piloto no podía usar ni opinar sobre nada salvo el login, y los contratos se habían diseñado sin su consumidor. El mayor riesgo de producto seguía intacto.

## Decisión

Se entrega en vertical (API, pantalla y E2E), empezando por la agenda, hacia un MVP piloto acotado. El orden de construcción prioriza algo que el centro pueda usar.

## Alternativas descartadas

- Seguir completando el backend por módulos, sin UI que lo consuma.

## Consecuencias

Habrá retoques en contratos que se cerraron sin pantalla. Redsys, con preautorización y penalizaciones, queda detrás de una agenda que alguien use. El alcance del piloto vive en el plan de trabajo, no en este registro.

## Tareas relacionadas

D-03. Tarea `869f6r4zt`.
