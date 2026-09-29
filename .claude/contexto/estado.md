# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-29 · Mac (empieza `869f6r81n`).

## Dónde estamos

- `develop` tras el PR #94 (`869f2gh37`, contratos HTTP de Empleados y Clientes). Tests de integración
  con PostgreSQL real desde el PR #93; base de datos en PostgreSQL 18 desde el PR #92. Sin ramas de
  trabajo abiertas.
- Batería: unit **544/544**; integración **57/57** (Testcontainers, en el CI desde el PR #93; necesitan
  Docker en marcha); E2E **57/57** (reejecutados contra PostgreSQL el 2026-09-29).
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

`869f6r81n` — mapa central de códigos de error a HTTP y respuesta común de controladores (Backend,
`in development`). Rama `feature/869f6r81n-error-map`. Objetivo: un único mapa `ErrorCode → status` con
test que exige status para todo el catálogo; base o extensión para `Result<T> → IActionResult`,
validación FluentValidation y camelCase de campos; fuera las copias de Empleados, Clientes,
Servicios, Paquetes y Disponibilidad; valorar unificar `AuthResult<T>` (absorbe `869f17y6k`).

## Qué toca (oleada hasta el 6-nov, fechas en ClickUp)

- Fases 0 y 1 **cerradas** el 28-sep: CI con checks obligatorios en `main`, .NET 10 LTS y sin
  dependencias de pago (MediatR fuera, Mapperly, AwesomeAssertions).
- Documentación de la Fase 1 **aplicada** (commit `b9ec48d`): ADR-001 a ADR-030 en
  `Documentation/adr/`, enlazados desde `decisiones.md`; advertencias de la IA revisadas.
- Fase 2 (cimientos de la API): ~~`869f6r5jf`~~ (PR #90) → **migración a PostgreSQL** (D-28, épica
  `869f8pm99`, análisis en `analisis-postgresql.md`): ~~`869f8pmnm` fechas en UTC~~ (PR #91) →
  ~~`869f8pmpa` cambio del motor~~ (PR #92) → ~~`869f6r5ng` Testcontainers sobre PostgreSQL~~ (PR #93)
  → `869f8pmpn` Windows (en el Windows) → `869f8pmq4` documentación → ~~`869f2gh37`~~ (PR #94) → `869f6r81n` mapa de errores **← en curso**.
- Previsión del MVP piloto: probable finales de enero de 2027 antes de la migración a PostgreSQL;
  con sus ≈ 20 h, principios de febrero. Se recalcula al cerrar el bloque.

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
- DP-06 resuelta el 2026-09-29 (H-37): mayúsculas en la aplicación, PascalCase, PostgreSQL 18,
  Hangfire en la Fase 5. Tareas de ClickUp que nombraban SQL Server, ya ajustadas.
- DP-01 queda solo para el hosting (el motor ya es PostgreSQL, D-28).
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- (DP-03 resuelta el 2026-09-28: AwesomeAssertions, H-35.)
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- `869f8pmpa` (PR #92), épica `869f8pm99` → ADR-031 (D-28, H-37) en `869f8pmq4`: motor PostgreSQL 18
  con Npgsql; historial de migraciones reiniciado en `InitialCreate`; emails en minúsculas
  (`EmailNormalizer` + CHECK) y búsquedas sin distinguir mayúsculas; scripts de `data/` para psql;
  entorno de desarrollo (`reservarte-pg`, vol. 1 §12.2 checklist de arranque y guía de user secrets).
- `869f6r5ng` (PR #93), para la estrategia de testing y el vol. 2: proyecto
  `tests/ReservArte.IntegrationTests` (WebApplicationFactory + Testcontainers.PostgreSql, fixture con dos
  centros, qué se prueba ahí y qué en unitarios, Docker como requisito, CI en dos pasos); arreglo de
  disponibilidad: las ausencias (UTC) se pasan a la hora de `Europe/Madrid` antes de recortarlas.
- `869f2gh37` (PR #94), para la estrategia de testing: contratos HTTP de Empleados y Clientes por rol
  (tokens emitidos con `IJwtTokenService`, envelope comprobado también en 401/403). Límite conocido: el
  token de una cuenta dada de baja vale hasta que caduca (60 min); la baja bloquea login y refresco.
- Advertencias de la IA en la Fase 1 (revisadas el 2026-09-28):
  - Estrategia de testing §3.1: el bloque histórico de suites (recuentos, PR, AutoMapper y
    `*ProfileTests`) debe depurarse; lo vigente ya está en el párrafo de herramientas.
  - Vol. 3, meses 6-7 y cuadro de costes: siguen con React Native («Mobile Developer (React Native)»,
    480 h y 19.200 € dentro de los 211.140 €), contra el ADR-020 (PWA). Hace falta que Guillermo
    estime la PWA; se resuelve al planificar `869f6r74n`, y entonces se recalcula el presupuesto.
- Auditoría mensual de octubre (primera sesión del mes): registros de estado que quedan en los
  volúmenes 1-3 y en el checklist del vol. 3.
- `869f6r5jf` (PR #90), para el vol. 1 §5.1.3 (contrato de configuración) y el vol. 2:
  `Email:Provider` (`File` | `Ses`; sin proveedor válido la API no arranca); `MultiTenant` validada
  al arrancar (estrategia Header o Subdomain, `BaseDomain` obligatorio con Subdomain,
  `DefaultOrganizationId` vacío o GUID; Header y `DefaultOrganizationId` solo en Development); el
  400 `ORG_TENANT_NOT_RESOLVED` ya no da motivo ni estrategia (solo en el log); fuera
  `IpRateLimiting`; la API sale con código 1 si el host falla.
- `869f8pmnm` (PR #91), para el vol. 1 §5.1 (contrato): fechas con hora de entrada en ISO 8601 con
  zona (sin zona → 400 `GEN_VALIDATION_FAILED`, código `MissingTimeZone`); de salida, siempre UTC con
  `Z`. Corrige que las fechas con desplazamiento se guardaran en hora local del servidor.

## Equipos

- **Mac:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, base `reservarte`, `InitialCreate`);
  `reservarte-sql`, su volumen y el secreto `SqlServerLegacy`, retirados el 2026-09-29;
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** 31 ramas locales fusionadas. Al volver a él: `869f8pmpn` (contenedor `reservarte-pg`,
  User Secret y `dotnet ef database update`), además del SDK 10 y `dotnet-ef` 10.
