# ADR-014: Query filters cerrados por defecto

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

Sin tenant resuelto, el filtro global `CurrentOrganizationId == null` deja ver todas las organizaciones. Los repositorios lo compensan con un `Where` que no devuelve nada. Los jobs de Hangfire (recordatorios) correrán sin petición HTTP y, por tanto, sin tenant: cualquier consulta que no pase por un repositorio vería todos los centros.

## Decisión

Los query filters quedan cerrados por defecto, con un ámbito de sistema explícito para los procesos que deban ver más de una organización. Se hace antes de Hangfire.

## Alternativas descartadas

- Dejar los filtros abiertos cuando no hay tenant y compensarlo solo en los repositorios.

## Consecuencias

Un job o una consulta nueva no hereda el hueco. El ámbito de sistema tiene que ser explícito: no basta con «no hay tenant en el contexto».

## Tareas relacionadas

D-14. Tarea `869f6r5vy`.
