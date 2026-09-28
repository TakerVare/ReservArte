# ADR-007: Documentación por bloque, auditoría mensual y ADR

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

Cada tarea disparaba un ciclo de documentación antes de la siguiente. La auditoría de la IA de documentación era incremental: miraba lo que cambiaba cada prompt y no veía las secciones que nadie tocaba (stack, metodología, cifras). Las decisiones estaban fechadas y enterradas en párrafos.

## Decisión

La documentación se actualiza por bloque, no por tarea, con una auditoría completa mensual. Las decisiones de arquitectura se registran como ADR en `Documentation/adr/`.

## Alternativas descartadas

- Un prompt de documentación por tarea.

## Consecuencias

Hay menos pasadas de documentación y cada una cubre un bloque. Un ADR aceptado no se reescribe. Los volúmenes pueden enlazar un ADR; no copian la decisión.

## Tareas relacionadas

D-07. Tareas `869f6r52d`, `869f6r54r`.
