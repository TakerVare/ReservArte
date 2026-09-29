# ADR-031: Tests de integración contra PostgreSQL real con Testcontainers

**Fecha:** 2026-09-29

**Estado:** aceptada

## Contexto

El motor pasó a PostgreSQL (D-28). SQLite no reproduce la comparación de texto, `timestamptz`, los `CHECK` ni los `Kind` de `DateTime` que exige Npgsql. Nada probaba el contrato HTTP. En su primera ejecución, estos tests destaparon un 500 en la consulta de huecos que no habían visto ni las pruebas manuales ni los E2E.

Este ADR sustituye al [ADR-016](ADR-016-tests-integracion-testcontainers.md), que fijaba SQL Server.

## Decisión

Los tests de integración usan `WebApplicationFactory<Program>` en Development contra PostgreSQL 18 real (Testcontainers, imagen `postgres:18`). Una colección comparte un contenedor por ejecución. La fixture siembra un centro B. Cada test crea sus datos y no depende de recuentos globales. La configuración de la fixture se impone a los User Secrets. Los tokens de rol se emiten con `IJwtTokenService`, porque el login admite 10 peticiones por hora. Las variantes usan `WithWebHostBuilder` para sustituir servicios o aislar el rate limiter. En el CI hay un paso por proyecto de tests.

## Alternativas descartadas

- SQLite o un proveedor en memoria para la integración: no reproduce el motor.
- Una base compartida fuera de Docker: deja estado entre ejecuciones y puede tocar la base de desarrollo.
- Un contenedor por clase: el arranque es más lento y no aporta, porque los datos ya se aíslan por test.

## Consecuencias

Docker es requisito para `dotnet test` de la solución, en los equipos y en el CI. Los tests comparten base y no pueden contar filas globales. El login está limitado, así que los tokens de rol se emiten sin pasar por él.

## Tareas relacionadas

H-39. Tareas `869f6r5ng`, `869f2gh37`. Sustituye al ADR-016.
