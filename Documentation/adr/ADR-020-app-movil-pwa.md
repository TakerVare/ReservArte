# ADR-020: Aplicación móvil como PWA

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El plan preveía una app en React Native, con un segundo stack de UI y unas 480 h. La web ya es mobile-first, con navegación inferior. Una persona no mantiene dos interfaces.

## Decisión

La aplicación móvil es una PWA sobre la SPA Vue. Si hace falta publicarla en las tiendas, se empaqueta con Capacitor.

## Alternativas descartadas

- React Native como segundo cliente, con su propio stack de UI.

## Consecuencias

No hay proyecto `reservarte-mobile`. El espacio de ClickUp ya no se llama «React Native». El presupuesto que aún carga 480 h de un desarrollador móvil no está recalculado: esa cifra queda como advertencia, no como plan vigente.

## Tareas relacionadas

D-20. Tarea `869f6r74n`.
