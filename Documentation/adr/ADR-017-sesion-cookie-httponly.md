# ADR-017: Sesión con refresh en cookie httpOnly

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

La sesión de la SPA no se renueva: el refresh no se persiste y, al caducar el access token, la persona vuelve al login. Tras recargar, el store recupera el token y deja `user` a null, así que los guards por rol no pueden rehidratar. Guardar tokens en `localStorage`, o devolverlos en el fragmento de la URL al volver del proveedor OAuth, los expone a script y a historial.

## Decisión

El refresh token va en una cookie httpOnly. El access token se queda en memoria. El retorno OAuth no lleva tokens en la URL. Al arrancar, la SPA rehidrata la sesión.

## Alternativas descartadas

- Tokens en `localStorage`.
- Tokens en el fragmento de la URL al volver del proveedor.

## Consecuencias

Hace falta un endpoint de refresco que lea la cookie, y la SPA no puede leer el refresh desde script. La protección CSRF de esa cookie sigue abierta (DP-04) y no la cierra este ADR.

## Tareas relacionadas

D-17. Tareas `869f6r61z`, `869f6r6hc`.
