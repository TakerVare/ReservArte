# ADR-001: Kanban con una tarea en curso

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La auditoría del 2026-09-23 constató que el volumen 3 describía Scrum (sprints de dos semanas, dailies, story points, velocity, Slack) y que el trabajo real era un flujo continuo: una persona con IA y una tarea cada vez. La lista «Active Sprint» contenía el backlog desde mayo. ClickUp ya registra throughput y tiempo de ciclo, y nadie los usaba para replanificar.

## Decisión

La metodología es Kanban: una tarea de desarrollo en curso y un desarrollador con IA. Las métricas son el throughput y el tiempo de ciclo. Se retira de la documentación el Scrum que no se practica.

## Alternativas descartadas

- Scrum documentado (sprints, dailies, story points, velocity) que no se practicaba. Formalizarlo habría descrito un equipo que no existe.

## Consecuencias

Los volúmenes dejan de organizar el trabajo por sprints. La replanificación, cuando haga falta, sale de las métricas de ClickUp y no de una velocity. No hay ceremonias que mantener.

## Tareas relacionadas

D-01. Tareas `869f6r4ec`, `869f6r58r`.
