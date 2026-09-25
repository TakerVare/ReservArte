# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-25 · Mac (`869f74uca` a medias por la cuota de ClickUp).

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

`869f74uca` — limpieza y reorganización de ClickUp (Infra, `in progress`). Sin rama: solo toca
ClickUp y `.claude/contexto/`. Objetivo: aplicar los nueve puntos de su descripción tras enseñar a
Guillermo la lista concreta de cambios, y dejar el resultado como comentario en la tarea.

Guillermo aprobó la lista concreta el 2026-09-25 (incluidas las cuatro recomendaciones: archivar
también «Backlog» de Frontend, quitar las fechas futuras de mayo, `869d7ecpg` a `done` y publicar la
guía de user secrets; `869d7fd4d` sigue en `draft`). Él renombró el espacio Mobile y archivará las
listas vacías («List» ×5, «Bugs», «Architecture Decisions» y «Backlog» de Frontend).

**Hecho:** listas renombradas a «Backend» y «Frontend»; traslados con la original cancelada y
comentada: dashboard `869d7f4b4` → `869f7axcv` (independiente), penalización `869f6ae9h` →
`869f7axdq` (bajo Redsys), no-shows `869f2gtyv` → `869f7axeg` (bajo `869f6r5y8`), lista de espera
`869f2yh9b` → `869f7axfq` y promoción `869f2g02q` → `869f7axh9` (independientes); `waiting_on` a
`869f74u7y` en `869f7axdq` y `869f7axeg`, y `869d7fc7e` espera a `869f7axcv`; Servicios `869d7ed7v`
en `shipped` con título ajustado y sin fecha; React Native `869d7ee7c` cancelada con enlace a la PWA.

**Pendiente (cuota agotada; se renueva hacia las 7:00 del 2026-09-26), ~30 llamadas:**
1. Títulos y descripción:
   - `869d7fc7e` → «DashboardPage.vue: MetricCard + RevenueChart + AppointmentsList consumiendo
     GET /api/v1/dashboard»; descripción: librería de Vue con tokens (D-12, DP-02 en `869f6r6nx`),
     consume `869f7axcv`, opcional (DP-05).
   - `869d7f53r` → «Tests: cancelación sin penalización y aislamiento multi-tenant (Org A ≠ Org
     B)»; descripción sin los dos tests de penalización (pasan a `869f7axdq`).
   - `869d7fcfy` → «CancelModal.vue: selector de motivo (sin penalización en el piloto)».
   - `869d7fc51` → «CustomerForm.vue con consentimientos RGPD (tarjeta guardada fuera del
     piloto)»; `AddPaymentMethodModal` llega con `869f2gnbm`.
   - `869d7ex8r` → «Pipeline frontend: npm ci + lint + build en PR a develop (Vitest cuando exista)».
   - `869d7ex56` → «Pipeline backend: dotnet build + test + format --verify-no-changes en PR a develop».
2. Comentarios aclaratorios: `869d7ewu5` (no había CI; `main` con PR obligatorio y `develop` sin él,
   H-32) y `869d7fbpp` (Playwright + axe, H-29).
3. Docs a `publish`: `869f18nq5`, `869d7ee0w`, `869d7fcyw`, `869d7fd29` y `869d7fd0k`.
4. `869d7ecpg` → `done` (husky `869d7ewzg` sigue abierta como subtarea).
5. Quitar fechas: `869d7eden`, `869d7edh9`, `869d7edt7`, `869d7edvq`, `869d7edya`, `869d7ee36`,
   `869d7ee5t`, `869d7ee9p`, `869d7eebv`, `869d7echh`, `869d7eckn`, `869d7ecpg`, `869d7ecqz`,
   `869d7ectu`, `869d7ee0w` y `869d7edau`. Se mantiene el 6-nov de `869f6r5ca`.
6. Comentario de resultado en `869f74uca` y cierre de la tarea (`done`).
7. Por PR (siguiente tarea con rama): `.claude/rules/backend.md` cita `869f2g02q` → `869f7axh9`.

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
