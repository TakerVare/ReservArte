# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-25 · Mac (cierre de `869f6r4ba`).

## Dónde estamos

- `develop` tras el PR #77 (`869f6r4ba`, estructura de contexto de Claude Code). Último cierre
  funcional: PR #76 (`869d7f4xf`, máquina de estados de citas). Sin ramas de trabajo abiertas.
- Batería: unit **506/506**; E2E **57/57** (sin reejecutar desde el PR #60: la SPA no ha cambiado).
- Bloques abiertos: **Sistema de Citas** `869d7edau` (5/12) y **CRUD Servicios** `869d7ed7v` (5/6,
  parado; se cierra al sacar el dashboard en la limpieza de ClickUp).
- Avance estimado (auditoría del 2026-09-23): MVP ≈ 39 % (backend ≈ 56 %, frontend ≈ 21 %);
  proyecto completo (fases 1-3) ≈ 20 %.
- Guillermo aprobó el 2026-09-24 todas las recomendaciones de la auditoría. El 2026-09-25 se crearon
  45 tareas y subtareas en ClickUp con 21 dependencias, y el orden propuesto está en `plan.md`
  (se confirma en la re-planificación, `869f6r4ec`).

## Tarea en curso

Ninguna.

## Qué toca (Fase 0 del plan)

1. ~~`869f6r4ba`~~ — estructura de contexto: cerrada (PR #77, `done`).
2. `869f74uca` — limpieza y reorganización de ClickUp. **← siguiente**
3. `869f6r4ec` — re-planificación con la capacidad real.
4. `869f6r52d` — nuevo régimen de documentación (lo aplica Guillermo en la IA de documentación).
5. `869f6r4hm` — plantilla de PR.

Después, Fase 1: CI → .NET 10 (fecha tope 6-nov-2026) → dependencias y licencias.

## Espera a Guillermo

- Aplicar la documentación pendiente de `869d7f4rd` (PR #75) y `869d7f4xf` (PR #76): los prompts
  ya están entregados.
- Pegar en la IA de documentación las instrucciones nuevas
  (`plantillas/instrucciones-ia-documentacion.md`), dentro de `869f6r52d`.
- Guardar las instrucciones nuevas del proyecto de claude.ai (texto entregado el 2026-09-25).
- Trámites externos (`869f6r4nz`): primero dominio (`869f6r785`), RGPD y EIPD (`869f6r7b3`) y
  textos legales (`869f6r7e7`).

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
