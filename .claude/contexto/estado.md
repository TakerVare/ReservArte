# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-29 · Windows (cierra `869f8pmpn`; PR #98 mergeado).

## Dónde estamos

- `develop` tras el PR #97 (`869f1k17q`, envelope en model binding, 404 y 405). Manejador global de
  excepciones desde el PR #96, mapa único de errores desde el PR #95; contratos HTTP desde el PR #94. Tests de integración
  con PostgreSQL real desde el PR #93; base de datos en PostgreSQL 18 desde el PR #92. Sin ramas de
  trabajo abiertas.
- Batería: unit **566/566**; integración **81/81** (Testcontainers, en el CI desde el PR #93; necesitan
  Docker en marcha); E2E **57/57**. Reejecutada íntegra en **los dos equipos** contra PostgreSQL el
  2026-09-29. Último PR mergeado: **#98** (fix del `.gitignore` de Playwright).
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

Ninguna.

## Los dos equipos en PostgreSQL (2026-09-29)

El traspaso al Windows de la opción C está **cumplido**: `869f8pmpn` cerrada y los dos equipos corren
PostgreSQL 18. Detalle y evidencia en `historial.md`. Lo que quedaba de esa lista y sigue pendiente:

1. **Pegar en Cursor el prompt de cimientos de la API** (`prompts/2026-09-29-cimientos-api.md`),
   ANTES que el de PostgreSQL: crea los ADR con los siguientes números libres (previsiblemente 031,
   tests con PostgreSQL que sustituye a ADR-016, y 032, plataforma D-29). Después, revisar sus
   advertencias con criterio y enlazar los ADR nuevos desde `decisiones.md` (H-39 y D-29).
2. **`869f8pmq4` — documentación de la migración** (lista Docs; `draft` → `in review` al entregar):
   `/cerrar-bloque` de la épica `869f8pm99` (`869f8pmnm`, `869f8pmpa`, `869f8pmpn`). Fuentes: los
   acumulados de abajo, `analisis-postgresql.md` y D-28/H-37. Ojo: la descripción de ClickUp dice
   «ADR-031» y «plataforma pendiente solo en el hosting»; ambas cosas han cambiado (ADR con el
   siguiente número libre, previsiblemente 033; la plataforma ya es D-29, que desarrolla ADR-021).
   No toca ADR-016: lo sustituye el prompt de cimientos.
3. Después, `/siguiente`: la Fase 3 empieza con `869d7f519` (endpoints de citas).

Notas de entorno del Windows, por si hacen falta: en Git Bash, `MSYS_NO_PATHCONV=1` delante de
`docker exec` con rutas del contenedor; no usar `TMP` ni `TEMP` como nombres de variable; si WAHA
ocupa el 3000, `docker stop waha-waha-1` antes de la SPA. Los E2E arrancan la SPA por su cuenta
(`webServer` de Playwright) pero **no** la API: hay que tenerla en marcha en 5555.

## Qué toca (oleada hasta el 6-nov, fechas en ClickUp)

- Fases 0 y 1 **cerradas** el 28-sep: CI con checks obligatorios en `main`, .NET 10 LTS y sin
  dependencias de pago (MediatR fuera, Mapperly, AwesomeAssertions).
- Documentación de la Fase 1 **aplicada** (commit `b9ec48d`): ADR-001 a ADR-030 en
  `Documentation/adr/`, enlazados desde `decisiones.md`; advertencias de la IA revisadas.
- Fase 2 (cimientos de la API): ~~`869f6r5jf`~~ (PR #90) → **migración a PostgreSQL** (D-28, épica
  `869f8pm99`, análisis en `analisis-postgresql.md`): ~~`869f8pmnm` fechas en UTC~~ (PR #91) →
  ~~`869f8pmpa` cambio del motor~~ (PR #92) → ~~`869f6r5ng` Testcontainers sobre PostgreSQL~~ (PR #93)
  → ~~`869f8pmpn` Windows~~ (cerrada, fix en PR #98) → `869f8pmq4` documentación → ~~`869f2gh37`~~ (PR #94) → ~~`869f6r81n` mapa de errores~~ (PR #95) → ~~`869f74u70` manejador global~~ (PR #96) → ~~`869f1k17q` 400 de model binding~~ (PR #97) → `869f6r4ww` sesión de plataforma de producción (DP-01).
- Bloque `869f6r5r2` (cimientos de la API) **cerrado** el 29-sep. De la Fase 2 solo queda
  `869f8pmq4` (documentación de PostgreSQL). Plataforma decidida el 29-sep (`869f6r4ww`,
  D-29): AWS simplificado en `eu-south-2`, ≈ 35 €/mes; Fargate + ALB como vía de escalado.
- Previsión del MVP piloto (29-sep): optimista principios de enero de 2027; **probable, hacia el 22 de
  enero**; pesimista, finales de febrero. Detalle en `plan.md` → «Previsión».

## Espera a Guillermo

- **Reestructurar Infra por D-29 (necesita tu OK):** sacar de la Fase 6 `869d7ew72` (ALB + CloudFront, vía
  de escalado) y renombrar las que dicen `eu-west-1` o ECS Fargate (`869d7evyq`, `869d7echh`,
  `869d7exag`). Ya tienen comentario con el ajuste.
- **Pegar en Cursor (modo Agent, chat nuevo) el prompt del bloque de cimientos de la API:**
  `.claude/contexto/prompts/2026-09-29-cimientos-api.md`: incluye la plataforma (D-29) y pide dos ADR
  nuevos (tests con PostgreSQL, que sustituye a ADR-016; plataforma del piloto); el contrato de
  errores se queda en ADR-013. Después, repasar juntos sus advertencias y enlazar los ADR desde
  `decisiones.md`.

- **Secreto antiguo de Google:** los dos equipos ya usan el nuevo (Windows puesto el 29-sep). Si en la
  consola de Google sigue existiendo el antiguo, se puede borrar.

- Guardar las instrucciones nuevas del proyecto de claude.ai (texto entregado el 2026-09-25).
- Trámites externos (`869f6r4nz`): primero dominio (`869f6r785`), RGPD y EIPD (`869f6r7b3`) y
  textos legales (`869f6r7e7`).

## Pendiente menor

- **ClickUp, cuota agotada el 2026-09-29** (100/100; se repone ~15 h después). Pendiente de aplicar en
  la próxima sesión, en cualquiera de los dos equipos: **`869f8pmpn` a `shipped`** (no pasó por
  `in development`) y un comentario en ella con el PR #98 y la evidencia.
- Anotar en el backlog la deuda de NU1901 (`AWSSDK.Core` 4.0.0.32, GHSA-9cvc-h2w8-phrp).

## Decisiones pendientes (plantéalas cuando salte su disparador)

- DP-01 resuelta el 2026-09-29 (D-29): AWS simplificado (EC2 + RDS PostgreSQL 18) en `eu-south-2`;
  la arquitectura del vol. 1 (Fargate + ALB) queda como vía de escalado.
- DP-06 resuelta el 2026-09-29 (H-37): mayúsculas en la aplicación, PascalCase, PostgreSQL 18,
  Hangfire en la Fase 5. Tareas de ClickUp que nombraban SQL Server, ya ajustadas.
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- (DP-03 resuelta el 2026-09-28: AwesomeAssertions, H-35.)
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- `869f8pmpn` (sin PR propio) + fix del PR #98, para `869f8pmq4`: entorno de desarrollo del **equipo
  Windows** en PostgreSQL 18 (contenedor `reservarte-pg`, volumen `reservarte_pgdata`, SDK .NET 10 y
  `dotnet-ef` 10.0.12; SQL Server retirado de los dos equipos). Para el vol. 1 §12.2 (checklist de
  arranque) y la guía de user secrets: los E2E arrancan la SPA por su cuenta pero **no** la API, que
  debe estar en marcha en 5555; contraseña del seed de desarrollo en `DevSeeder`.
- `869f8pmpa` (PR #92), épica `869f8pm99` → ADR del motor (D-28, H-37; siguiente número libre) en `869f8pmq4`: motor PostgreSQL 18
  con Npgsql; historial de migraciones reiniciado en `InitialCreate`; emails en minúsculas
  (`EmailNormalizer` + CHECK) y búsquedas sin distinguir mayúsculas; scripts de `data/` para psql;
  entorno de desarrollo (`reservarte-pg`, vol. 1 §12.2 checklist de arranque y guía de user secrets).
- Advertencia de la IA en la Fase 1 (revisada el 2026-09-28; la de testing §3.1 va en el prompt del
  2026-09-29):
  - Vol. 3, meses 6-7 y cuadro de costes: siguen con React Native («Mobile Developer (React Native)»,
    480 h y 19.200 € dentro de los 211.140 €), contra el ADR-020 (PWA). Hace falta que Guillermo
    estime la PWA; se resuelve al planificar `869f6r74n`, y entonces se recalcula el presupuesto.
- Auditoría mensual de octubre (primera sesión del mes): registros de estado que quedan en los
  volúmenes 1-3 y en el checklist del vol. 3.
- `869f8pmnm` (PR #91), para el vol. 1 §5.1 (contrato): fechas con hora de entrada en ISO 8601 con
  zona (sin zona → 400 `GEN_VALIDATION_FAILED`, código `MissingTimeZone`); de salida, siempre UTC con
  `Z`. Corrige que las fechas con desplazamiento se guardaran en hora local del servidor.

## Equipos

- **Mac:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, base `reservarte`, `InitialCreate`);
  `reservarte-sql`, su volumen y el secreto `SqlServerLegacy`, retirados el 2026-09-29;
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, PostgreSQL 18.6, base `reservarte`,
  `InitialCreate`); SDK .NET 10 (`10.0.401`) y `dotnet-ef` 10.0.12; `reservarte-sql` y su volumen,
  retirados. 31 ramas locales fusionadas, borrables con `git branch -d`.
