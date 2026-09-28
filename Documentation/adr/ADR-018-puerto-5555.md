# ADR-018: Puerto 5555 como convención, sin fallback a localhost

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La regla «no fijar puertos» se incumplía en todo el repositorio. Convivían una `baseURL` absoluta y el proxy `/api`, así que el proxy no se usaba. Un build sin `VITE_API_BASE_URL` llamaría a localhost en producción. El puerto 5000 choca con AirPlay en macOS.

## Decisión

El puerto 5555 queda como convención documentada de la API en desarrollo. El código no lleva fallbacks a localhost. La SPA usa rutas relativas y el proxy de Vite.

## Alternativas descartadas

- Una `baseURL` absoluta con fallback a localhost.

## Consecuencias

El puerto de desarrollo se documenta; no se dispersa en clientes HTTP. Producción no hereda una URL de desarrollo por omisión de variable.

## Tareas relacionadas

D-18. Tarea `869f6r69b`.
