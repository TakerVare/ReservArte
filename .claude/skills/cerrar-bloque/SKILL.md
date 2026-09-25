---
name: cerrar-bloque
description: "Cierra un bloque (épica de ClickUp) de ReservArte. Comprueba su definición de hecho, prepara un único prompt para la IA de documentación con todo el bloque, recalcula métricas, previsión y avance, actualiza plan.md, estado.md e historial.md y propone cómo seguir. Úsala cuando se cierre la última subtarea de un bloque, al terminar una fase del plan, o cuando Guillermo pida revisar o cerrar un bloque o preparar la documentación de lo hecho."
---

# /cerrar-bloque

## 1. Definición de hecho

Criterios en `.claude/contexto/gestion.md` §5. Lista lo que no los cumpla. Si falta algo, no
cierres: propón cómo resolverlo (terminarlo, o trasladarlo a su épica natural con el patrón de
traslado, que requiere el OK de Guillermo).

## 2. Prompt para la IA de documentación

- Plantilla A de `.claude/contexto/plantillas/prompt-ia-documentacion.md`.
- Incluye todas las tareas del bloque, las decisiones nuevas (para sus ADR) y lo acumulado en
  `estado.md` → «Documentación acumulada».
- Entrégalo en el chat y, si es largo, también como fichero. Vacía la sección de acumulados.
- Cuando Guillermo lo aplique, repasa con él las advertencias de esa IA con criterio: contrasta cada
  una con el código, porque hay falsos positivos.

## 3. Métricas, previsión y avance

- Throughput y tiempo de ciclo del bloque, con fechas de ClickUp (`gestion.md` §6).
- Horas pendientes del plan y nuevo factor de realismo → nuevo rango de fechas del hito.
- Avance del MVP y del proyecto con el modelo de `gestion.md` §7.
- Anótalo arriba en `plan.md` → «Previsión» y resúmelo en `estado.md`.

## 4. Cierre

- ClickUp: el padre del bloque a `shipped` (Infra: `done`).
- `historial.md`: entrada del bloque (qué se entregó, decisiones y métricas).
- Commit `chore(contexto): cierra bloque <id>` y push.
- Si el bloque deja algo visible para el centro, propón una demo a More Than Brows.
- Termina con `/siguiente`.
