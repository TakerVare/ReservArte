# ADR-012: Gráficas con una librería de Vue

**Fecha:** 2026-09-24

**Estado:** aceptada

## Contexto

El stack preveía recharts para el dashboard. recharts es una librería de React. La SPA es Vue, y los colores de la interfaz salen de tokens CSS, no de literales.

## Decisión

Las gráficas se hacen con una librería de Vue y los colores salen de los tokens. La librería concreta queda por confirmar al llegar al dashboard (DP-02). La propuesta es vue-chartjs; la otra opción abierta es vue-echarts.

## Alternativas descartadas

- recharts, por ser una librería de React.

## Consecuencias

No se añade recharts. Elegir entre vue-chartjs y vue-echarts no reabre este ADR: se cierra en DP-02. Hasta entonces, el volumen 1 §4.1 no nombra una dependencia de gráficas como si ya estuviera instalada.

## Tareas relacionadas

D-12. Tarea `869f6r6nx`. Decisión pendiente DP-02.
