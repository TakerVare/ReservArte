# ADR-023: Limpieza de ClickUp y definición de hecho por bloque

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

Las listas no coincidían con el trabajo: «Active Sprint» era el backlog, y «Bugs» y «Architecture Decisions» estaban vacías. Los bloques no podían cerrarse porque lo que se descubría se colgaba del bloque activo, y no había una definición de hecho por bloque. El espacio de documentación tenía tareas en borrador con la guía ya escrita.

## Decisión

ClickUp se limpia: listas renombradas, listas vacías archivadas, subtareas en su épica natural y una definición de hecho por bloque.

## Alternativas descartadas

No quedó registrada una alternativa descartada.

## Consecuencias

Un bug no tiene lista propia: va a la lista de su área, con «Bug:» en el título. Los ADR no tienen lista. La estructura vigente está en el volumen 3 §10.1.1.

## Tareas relacionadas

D-23. Tarea `869f74uca`.
