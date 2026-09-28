# ADR-021: Decidir la plataforma de producción antes de montarla

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El presupuesto de infraestructura, del orden de 55 € al mes, no incluye licencia de SQL Server. La edición Developer no se puede usar en producción. Express limita cada base de datos a 10 GB. Montar el mismo contenedor de desarrollo en producción dejaría el motor sin decidir.

## Decisión

La plataforma de producción (motor o edición de base de datos, y hosting) se decide conscientemente antes de montar infraestructura. El resultado de esa decisión sigue pendiente (DP-01).

## Alternativas descartadas

- SQL Server en Docker para producción, sin licencia presupuestada.

## Consecuencias

No se aprovisiona producción «mientras tanto» con la imagen de desarrollo. DP-01 elige motor y hosting; este ADR solo obliga a decidirlo antes de montar.

## Tareas relacionadas

D-21. Tarea `869f6r4ww`. Decisión pendiente DP-01.
