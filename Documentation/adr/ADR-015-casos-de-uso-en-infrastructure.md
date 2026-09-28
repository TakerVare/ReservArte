# ADR-015: Casos de uso en Infrastructure

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La Clean Architecture del repositorio es nominal. Los casos de uso viven en Infrastructure. Application tiene contratos, DTOs y validadores. No es un fallo de compilación, y explica que los servicios dependan de `UserManager` y que un servicio acumule muchas dependencias. La auditoría pedía que, si se queda así, conste como decisión.

## Decisión

Los casos de uso siguen en Infrastructure. Application tiene contratos, DTOs, validadores y mappers. Es una decisión consciente: no se mueven.

## Alternativas descartadas

- Mover los casos de uso a Application para acercarse a una Clean Architecture en la que Application no depende de Infrastructure.

## Consecuencias

Los documentos no describen el backend como Clean Architecture «pura». Un servicio nuevo sigue el sitio real, no el diagrama de capas del plan antiguo.

## Tareas relacionadas

D-15. Tarea `869f6r54r`.
