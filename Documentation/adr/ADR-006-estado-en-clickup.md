# ADR-006: El estado vive en ClickUp y en el traspaso del repo

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El estado estaba triplicado: ClickUp, el volumen 3 y `CLAUDE.md` ya divergían. Los volúmenes hacían a la vez de especificación, registro de cambios y cuadro de estado, y por eso envejecían y se volvían difíciles de leer.

## Decisión

El estado vive en ClickUp. El traspaso entre equipos vive en `.claude/contexto/estado.md`. Los volúmenes no registran estado, pull requests ni recuentos.

## Alternativas descartadas

- Seguir manteniendo el estado en ClickUp, en los volúmenes y en `CLAUDE.md` a la vez.

## Consecuencias

Al tocar una sección, el registro de estado que hubiera dentro se depura, sin reescribir el volumen entero de una vez. El detalle de cada tarea queda en su pull request.

## Tareas relacionadas

D-06. Tarea `869f6r52d`.
