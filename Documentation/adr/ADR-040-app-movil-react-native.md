# ADR-040: App móvil nativa en React Native

**Fecha:** 2026-10-01

**Estado:** aceptada

## Contexto

Hace falta presencia en App Store y Google Play, y una imagen de marca en las tiendas. Sustituye a [ADR-020](ADR-020-app-movil-pwa.md).

## Decisión

La app móvil será nativa en React Native (H-46). Se publica en App Store y Google Play después del piloto: el piloto arranca con la web. Habrá una app por centro (marca blanca) y servirá a clientas y personal. La estimación antigua, unas 480 h, se revisa al planificarla.

## Alternativas descartadas

- PWA sobre la SPA Vue ([ADR-020](ADR-020-app-movil-pwa.md)).
- PWA empaquetada con Capacitor.

## Consecuencias

Hay un segundo frontend en React y doble mantenimiento. Cada centro supone una publicación y una revisión de Apple, además de las cuentas de desarrollador. El piloto no espera a la app.

## Tareas relacionadas

H-46. Tarea `869f6r74n`.
