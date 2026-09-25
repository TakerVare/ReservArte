---
name: siguiente
description: "Propone a Guillermo la siguiente tarea de ReservArte con dos o tres opciones razonadas (estimación, riesgo, dependencias y recomendación) a partir de plan.md, ClickUp y las decisiones pendientes. Úsala al cerrar una tarea o un bloque, cuando Guillermo pregunte qué hacemos ahora, qué toca o qué sigue, y cuando haga falta reordenar el trabajo. Solo propone: nunca empieza la tarea sin su OK."
---

# /siguiente — proponer la siguiente tarea

## 1. Reúne el contexto

- `.claude/contexto/plan.md`: el siguiente paso pendiente en orden, sus dependencias y su estimación.
- ClickUp: el estado real de ese paso y de sus dependencias (`waiting_on`). No propongas algo
  bloqueado como opción principal.
- `.claude/contexto/decisiones.md` → «Pendientes»: ¿salta algún disparador con este paso?
- `.claude/contexto/estado.md`: qué espera a Guillermo y qué trámites externos van tarde.
- Mientras `869f6r5ca` siga abierta, la fecha tope de .NET 10 (6-nov-2026) pesa en cualquier orden.

## 2. Elige las opciones

- **A:** el siguiente paso del plan, salvo que esté bloqueado.
- **B y C:** alternativas reales, no de relleno: adelantar algo que desbloquea más, partir una tarea
  grande, atender un riesgo que haya aparecido o cerrar el bloque. Si no hay alternativa real,
  presenta solo A.
- Criterios de prioridad: `.claude/contexto/gestion.md` §3.

## 3. Presenta

Con el formato de `gestion.md` §4: como mucho tres opciones, cada una con estimación, riesgo y lo
que desbloquea; tu recomendación con el porqué; y la pregunta final. Si la opción recomendada cambia
el orden de `plan.md`, di qué pasos se mueven.

## 4. Con el OK

- Si cambia el orden, actualiza `plan.md` en `develop` en el mismo commit de contexto del paso
  siguiente.
- Sigue el paso 2 del flujo de `CLAUDE.md`: anota la tarea en `estado.md` → «Tarea en curso» (ID,
  rama y objetivo), commit `chore(contexto): empieza <id>` y push; después, la rama y la tarea a
  `in development` (en Infra, `in progress`).
