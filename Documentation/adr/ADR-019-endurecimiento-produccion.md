# ADR-019: Endurecimiento antes de producción

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La auditoría listó huecos que no bloquean el desarrollo local y sí un despliegue: el tenant se resuelve en base de datos en cada petición y va antes del rate limiter; el limitador parte por `RemoteIpAddress` y, tras un balanceador, todos compartirían IP; `Europe/Madrid` está fijado en código; `MultiTenantOptions` no se valida al arrancar; el 400 de tenant devuelve al cliente el motivo interno y la estrategia activa.

## Decisión

Antes de producción: el limitador va antes del tenant y usa la IP real tras el proxy; hay caché de tenant; cabeceras de seguridad; zona horaria por organización; validación de `MultiTenantOptions` al arrancar; y el 400 de tenant no incluye detalles internos.

## Alternativas descartadas

- Salir a producción con el comportamiento actual.

## Consecuencias

Ninguno de estos puntos se pospone al «ya desplegaremos». La zona horaria por organización sustituye el reloj fijo de Madrid, que hoy descarta huecos pasados con esa zona.

## Tareas relacionadas

D-19. Tareas `869f6r65a`, `869f74u7y`, `869f6r5jf`.
