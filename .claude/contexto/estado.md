# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-28 · Mac (cierre de `869f6r4ec`).

## Dónde estamos

- `develop` tras el PR #77 (`869f6r4ba`, estructura de contexto de Claude Code). Último cierre
  funcional: PR #76 (`869d7f4xf`, máquina de estados de citas). Sin ramas de trabajo abiertas.
- Batería: unit **506/506**; E2E **57/57** (sin reejecutar desde el PR #60: la SPA no ha cambiado).
- Bloque abierto: **Sistema de Citas** `869d7edau` (5/8 tras la limpieza). **CRUD Servicios**
  `869d7ed7v` cerrado el 2026-09-25 (5/5; el dashboard pasó a `869f7axcv`). Su documentación ya se
  entregó tarea a tarea con el régimen anterior: no necesita prompt de bloque.
- Avance estimado (auditoría del 2026-09-23): MVP ≈ 39 % (backend ≈ 56 %, frontend ≈ 21 %);
  proyecto completo (fases 1-3) ≈ 20 %.
- Guillermo aprobó el 2026-09-24 todas las recomendaciones de la auditoría. El 2026-09-25 se crearon
  45 tareas y subtareas en ClickUp con 21 dependencias, y el orden propuesto está en `plan.md`
  (se confirma en la re-planificación, `869f6r4ec`).

## Tarea en curso

Ninguna.

## Qué toca (oleada hasta el 6-nov, fechas en ClickUp)

- Fase 0: ~~`869f6r4ba`~~, ~~`869f74uca`~~ y ~~`869f6r4ec`~~ cerradas. Quedan `869f6r52d` (nuevo
  régimen de documentación, lo aplica Guillermo) y `869f6r4hm` (plantilla de PR). **← siguiente**
- Fase 1 (hasta el 9-oct): CI → .NET 10 (objetivo 9-oct, tope 6-nov) → dependencias y licencias.
- Previsión del MVP piloto: probable mediados de febrero de 2027 (rango enero-marzo); detalle en
  `plan.md` → «Previsión».

## Espera a Guillermo

- Activar la ClickApp «Total time in Status» (Settings → ClickApps) para medir el tiempo de ciclo
  también en ClickUp.
- Aplicar la documentación pendiente de `869d7f4rd` (PR #75) y `869d7f4xf` (PR #76): los prompts
  ya están entregados.
- Pegar en la IA de documentación las instrucciones nuevas
  (`plantillas/instrucciones-ia-documentacion.md`), dentro de `869f6r52d`.
- Guardar las instrucciones nuevas del proyecto de claude.ai (texto entregado el 2026-09-25).
- Trámites externos (`869f6r4nz`): primero dominio (`869f6r785`), RGPD y EIPD (`869f6r7b3`) y
  textos legales (`869f6r7e7`).

## Pendiente menor

- Por PR (la próxima tarea con rama): `.claude/rules/backend.md` cita `869f2g02q`; el ID vigente es
  `869f7axh9`.
- Al cerrar `869f6r81n`: cancelar `869f17y6k` (absorbida) con comentario.

## Decisiones pendientes (plantéalas cuando salte su disparador)

- DP-01 Plataforma de producción (base de datos y hosting) → `869f6r4ww`, paso 2.7 del plan.
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el prompt del bloque de Citas

- (vacía: `869d7f4rd` y `869d7f4xf` ya tienen su prompt entregado con el régimen anterior)

## Equipos

- **Mac:** base recreada el 2026-09-23 con los scripts de `data/` (12 migraciones, 24 tablas);
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** 31 ramas locales fusionadas. Al volver a él, comprobar migraciones pendientes.
