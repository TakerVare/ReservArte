# ADR-009: Migración a .NET 10 LTS

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El backend estaba en .NET 8. Microsoft deja de publicar parches de seguridad de .NET 8 el 10 de noviembre de 2026. .NET 10 es LTS, con soporte hasta noviembre de 2028. Seguir la regla «8.0.x» habría lanzado el producto sobre un runtime sin parches. .NET 9 es STS y llega al mismo fin de soporte.

## Decisión

El backend migra a .NET 10 LTS antes del 10 de noviembre de 2026. `global.json` fija la banda del SDK 10.0. Los paquetes de ASP.NET Core, EF Core e Identity siguen la versión del target.

## Alternativas descartadas

- Quedarse en .NET 8, sin parches desde el 10 de noviembre de 2026.
- Pasar a .NET 9, que no es LTS y termina el soporte en la misma ventana.

## Consecuencias

Los dos equipos y el CI necesitan un SDK 10.0.x. Desde EF Core 10, el script idempotente mete cada migración en un solo lote: el SQL a mano que use una columna creada en la misma migración va dentro de `EXEC`. Las versiones concretas están en el volumen 1 §4.1.

## Tareas relacionadas

D-09. Tarea `869f6r5ca`.
