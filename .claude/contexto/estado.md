# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-28 · Mac (`869f6r7rj`, PR #87 abierto).

## Dónde estamos

- `develop` tras el PR #77 (`869f6r4ba`, estructura de contexto de Claude Code). Último cierre
  funcional: PR #76 (`869d7f4xf`, máquina de estados de citas). Sin ramas de trabajo abiertas.
- Batería: unit **506/506**; E2E **57/57** (sin reejecutar desde el PR #60: la SPA no ha cambiado).
- **Backend en .NET 10 LTS** desde el PR #86 (`869f6r5ca`). Último PR: #86. **Hay CI:** «Backend CI / build-test-format» y
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

`869f6r7rj` — retirar MediatR, instalado y sin uso (Backend, `in development`). Rama
`feature/869f6r7rj-remove-mediatr`. Objetivo: quitar el paquete y su mención en las reglas, con
build, tests, format y arranque de la API como evidencia.

**PR #87 abierto, esperando revisión de Guillermo.**

## Qué toca (oleada hasta el 6-nov, fechas en ClickUp)

- Fase 0 **cerrada** el 2026-09-28 (`869f6r4ba`, `869f74uca`, `869f6r4ec`, `869f6r4hm` y
  `869f6r52d`). Sus decisiones se documentan en los ADR iniciales (`869f6r54r`, paso 1.8).
- Fase 1 (hasta el 9-oct): ~~`869d7ex56` CI backend~~ → ~~`869d7ex8r` CI frontend~~ →
  ~~`869f6r4t8` checks~~ → ~~.NET 10~~ (28-sep) → dependencias y licencias:
  `869f6r7rj` retirar MediatR **← siguiente** → `869f6r7vw` Mapperly → `869f6r7yh` FluentAssertions.
- Previsión del MVP piloto: probable mediados de febrero de 2027 (rango enero-marzo); detalle en
  `plan.md` → «Previsión».

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
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- Para `869f6r58r` (incoherencias, paso 1.9) — advertencias de la IA al crear `Documentation/adr/`,
  contrastadas el 2026-09-28: el vol. 3 describe la estructura de ClickUp anterior a la limpieza
  (listas «Active Sprint», «Bugs» y «Architecture Decisions», espacio «Mobile (React Native)»;
  líneas ~56-81 y ~2201). La lista de ADR de ClickUp ya no existe: los ADR viven solo en
  `Documentation/adr/`. Enlazar esa carpeta desde el vol. 3 y desde el vol. 1 §4 (Arquitectura).
- Para el prompt de la Fase 1 (`869f6r5ca`, PR #86): el stack pasa a .NET 10 LTS (vol. 1 §4.1 es la
  fuente única de stack y versiones): SDK por `global.json` (banda 10.0.x), paquetes de Microsoft
  10.0.12, `Microsoft.IdentityModel.*` 8.19.2, Serilog.AspNetCore 10.0.0, Swashbuckle 10.2.3,
  Hangfire 1.8.25, Apple OAuth 10.0.0; `dotnet-ef` 10.0.x en la guía de instalación. El script
  `create` de EF 10 va en un lote por migración (`EXEC` en el SQL a mano que use columnas nuevas).
  El CI de backend instala el SDK de `global.json`.

## Equipos

- **Mac:** base recreada el 2026-09-23 con los scripts de `data/` (12 migraciones, 24 tablas);
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** 31 ramas locales fusionadas. Al volver a él, comprobar migraciones pendientes.
