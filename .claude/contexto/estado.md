# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-28 · Mac (empieza `869f6r5jf`, sin ClickUp).

## Dónde estamos

- `develop` tras el PR #77 (`869f6r4ba`, estructura de contexto de Claude Code). Último cierre
  funcional: PR #76 (`869d7f4xf`, máquina de estados de citas). Sin ramas de trabajo abiertas.
- Batería: unit **522/522**; E2E **57/57** (sin reejecutar desde el PR #60: la SPA no ha cambiado).
- **Backend en .NET 10 LTS** desde el PR #86 (`869f6r5ca`). Último PR: #89 (`869f6r7yh`, AwesomeAssertions). **Sin dependencias de pago.** **Hay CI:** «Backend CI / build-test-format» y
  «Frontend CI / lint-build» en cada PR a `develop`/`main` y en cada push a `develop`.
  En `main` los dos son obligatorios, también para admins (`869f6r4t8`); `develop`, sin protección.
- Bloque abierto: **Sistema de Citas** `869d7edau` (5/8 tras la limpieza). **CRUD Servicios**
  `869d7ed7v` cerrado el 2026-09-25 (5/5; el dashboard pasó a `869f7axcv`). Su documentación ya se
  entregó tarea a tarea con el régimen anterior: no necesita prompt de bloque.
- Avance estimado (auditoría del 2026-09-23): MVP ≈ 39 % (backend ≈ 56 %, frontend ≈ 21 %);
  proyecto completo (fases 1-3) ≈ 20 %.
- Guillermo aprobó el 2026-09-24 todas las recomendaciones de la auditoría. El 2026-09-25 se crearon
  45 tareas y subtareas en ClickUp con 21 dependencias, y el orden propuesto está en `plan.md`
  (se confirma en la re-planificación, `869f6r4ec`).

## Tarea en curso

`869f6r5jf` — correcciones menores de la auditoría (Backend). Rama `feature/869f6r5jf-audit-fixes`.
Empezada con la cuota de ClickUp agotada: **alcance provisional** sacado del repo (auditoría y D-19),
que hay que contrastar con la descripción de ClickUp antes de abrir el PR:
1. Comentario obsoleto de `EmployeeRepository` (dice que `Employees` no tiene query filter).
2. Roles en minúsculas en datos de test (`AppDbContextTenantResolutionTests`, `session-ending.spec.ts`).
3. Clave muerta `IpRateLimiting` en `appsettings.json`.
4. `Email:Provider`: el proveedor se elige con `IsDevelopment()`; pasa a elegirse por configuración.
5. Validación de `MultiTenantOptions` al arrancar (D-19).
6. El 400 `ORG_TENANT_NOT_RESOLVED` sin motivo interno ni estrategia activa (D-19).

## Qué toca (oleada hasta el 6-nov, fechas en ClickUp)

- Fases 0 y 1 **cerradas** el 28-sep: CI con checks obligatorios en `main`, .NET 10 LTS y sin
  dependencias de pago (MediatR fuera, Mapperly, AwesomeAssertions).
- Documentación de la Fase 1 **aplicada** (commit `b9ec48d`): ADR-001 a ADR-030 en
  `Documentation/adr/`, enlazados desde `decisiones.md`; advertencias de la IA revisadas.
- Siguiente: Fase 2 (cimientos de la API), empezando por `869f6r5jf` correcciones menores.
- Previsión del MVP piloto: probable finales de enero de 2027 (rango primera quincena de enero -
  principios de marzo); detalle en `plan.md` → «Previsión».

## Pendiente en ClickUp (cuota agotada el 2026-09-28; se renueva hacia las 7:00 del 29-sep)

Aplicar en este orden en cuanto haya cuota (unas 5 llamadas):
1. `869f6r54r` → `publish` (ADR iniciales aplicados en `b9ec48d`).
2. `869f6r58r` → `publish` (incoherencias aplicadas en `b9ec48d`).
3. `869f6r5jf` → `in development` (empezada el 28-sep sin cuota).
4. Leer la descripción de `869f6r5jf` y contrastarla con el alcance provisional de «Tarea en curso»;
   si difiere, avisar a Guillermo antes de abrir el PR.
5. Cuando se abra el PR de `869f6r5jf`: → `in review`.

## Espera a Guillermo


- **Windows, antes de compilar `develop`:** instalar el SDK de .NET 10 (x64, 10.0.4xx o posterior)
  junto al 8; `dotnet tool update -g dotnet-ef --version 10.0.12`; revisar las credenciales de Google
  en user-secrets (en el Mac eran marcadores de posición hasta el 2026-09-28). Si allí se usa el
  secreto de Google antiguo, borrarlo en la consola cuando los dos equipos usen el nuevo.

- Guardar las instrucciones nuevas del proyecto de claude.ai (texto entregado el 2026-09-25).
- Trámites externos (`869f6r4nz`): primero dominio (`869f6r785`), RGPD y EIPD (`869f6r7b3`) y
  textos legales (`869f6r7e7`).

## Pendiente menor

- Al cerrar `869f6r81n`: cancelar `869f17y6k` (absorbida) con comentario.

## Decisiones pendientes (plantéalas cuando salte su disparador)

- DP-01 Plataforma de producción (base de datos y hosting) → `869f6r4ww`, paso 2.7 del plan.
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- (DP-03 resuelta el 2026-09-28: AwesomeAssertions, H-35.)
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- Advertencias de la IA en la Fase 1 (revisadas el 2026-09-28):
  - Estrategia de testing §3.1: el bloque histórico de suites (recuentos, PR, AutoMapper y
    `*ProfileTests`) debe depurarse; lo vigente ya está en el párrafo de herramientas.
  - Vol. 3, meses 6-7 y cuadro de costes: siguen con React Native («Mobile Developer (React Native)»,
    480 h y 19.200 € dentro de los 211.140 €), contra el ADR-020 (PWA). Hace falta que Guillermo
    estime la PWA; se resuelve al planificar `869f6r74n`, y entonces se recalcula el presupuesto.
- Auditoría mensual de octubre (primera sesión del mes): registros de estado que quedan en los
  volúmenes 1-3 y en el checklist del vol. 3.

## Equipos

- **Mac:** base recreada el 2026-09-23 con los scripts de `data/` (12 migraciones, 24 tablas);
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** 31 ramas locales fusionadas. Al volver a él, comprobar migraciones pendientes.
