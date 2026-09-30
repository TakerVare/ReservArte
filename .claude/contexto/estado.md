# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-30 · Mac (PR #103 de `869f2gn91`).

## Dónde estamos

- `develop` tras el PR #102 (`869d7f53r`, tests de cancelación y aislamiento de citas). API de citas
  desde el PR #101 (`869d7f519`). Build sin avisos desde el PR #100. Último de producto: PR #97
  (`869f1k17q`, envelope en model binding, 404 y 405). Manejador global de
  excepciones desde el PR #96, mapa único de errores desde el PR #95; contratos HTTP desde el PR #94. Tests de integración
  con PostgreSQL real desde el PR #93; base de datos en PostgreSQL 18 desde el PR #92. Sin ramas de
  trabajo abiertas.
- Batería: unit **568/568**; integración **116/116** (Testcontainers, en el CI desde el PR #93; necesitan
  Docker en marcha); E2E **57/57**. Reejecutada íntegra en **los dos equipos** contra PostgreSQL el
  2026-09-29. Último PR mergeado: **#98** (fix del `.gitignore` de Playwright).
- **Backend en .NET 10 LTS** desde el PR #86 (`869f6r5ca`). Último PR: #89 (`869f6r7yh`, AwesomeAssertions). **Sin dependencias de pago.** **Hay CI:** «Backend CI / build-test-format» y
  «Frontend CI / lint-build» en cada PR a `develop`/`main` y en cada push a `develop`.
  En `main` los dos son obligatorios, también para admins (`869f6r4t8`); `develop`, sin protección.
- Bloque abierto: **Sistema de Citas** `869d7edau` (7/8 con `869d7f519` y `869d7f53r`; recuento a confirmar en ClickUp). **CRUD Servicios**
  `869d7ed7v` cerrado el 2026-09-25 (5/5; el dashboard pasó a `869f7axcv`). Su documentación ya se
  entregó tarea a tarea con el régimen anterior: no necesita prompt de bloque.
- Avance estimado (auditoría del 2026-09-23): MVP ≈ 39 % (backend ≈ 56 %, frontend ≈ 21 %);
  proyecto completo (fases 1-3) ≈ 20 %.
- Guillermo aprobó el 2026-09-24 todas las recomendaciones de la auditoría. El 2026-09-25 se crearon
  45 tareas y subtareas en ClickUp con 21 dependencias, y el orden propuesto está en `plan.md`
  (se confirma en la re-planificación, `869f6r4ec`).

## Tarea en curso

`869f2gn91` — historial de citas de la clienta, `GET /api/v1/customers/{id}/history` (Backend; ClickUp
sin cuota: alcance del vol. 1 §5.1). **PR #103 abierto, esperando revisión.** Rama
`feature/869f2gn91-customer-history`. Objetivo: solo personal
(como el resto de `/customers`); citas activas de la clienta en todos sus estados, de la más reciente
a la más antigua, paginadas y con sus líneas; clienta inexistente o de otro centro → 404. Hecho: 5 tests
(integración 121/121), 3 mutaciones cazadas.

## Los dos equipos en PostgreSQL (2026-09-29)

El traspaso al Windows de la opción C está **cumplido**: `869f8pmpn` cerrada y los dos equipos corren
PostgreSQL 18. Detalle y evidencia en `historial.md`. Lo que quedaba de esa lista y sigue pendiente:

1. ~~Prompt de cimientos de la API~~: **aplicado** el 29-sep (ADR-031, tests con PostgreSQL, que
   sustituye a ADR-016; ADR-032, plataforma D-29; vol. 1, 2 y 3 en 1.2; estrategia de testing 1.1).
   ADR enlazados desde `decisiones.md`; advertencias revisadas y llevadas al acumulado de abajo.
   **Commit de la documentación: lo hace Guillermo** (como en la Fase 1).
2. ~~`869f8pmq4`~~: prompt **aplicado** el 29-sep (ADR-033 del motor; vol. 1, 2 y 3 en 1.3). ADR-033
   enlazado desde D-28 y H-37; advertencias revisadas. **Commit de la documentación: Guillermo.**
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
  → ~~`869f8pmpn` Windows~~ (cerrada, fix en PR #98) → `869f8pmq4` documentación → ~~`869f2gh37`~~ (PR #94) → ~~`869f6r81n` mapa de errores~~ (PR #95) → ~~`869f74u70` manejador global~~ (PR #96) → ~~`869f1k17q` 400 de model binding~~ (PR #97) → ~~`869f6r4ww` plataforma de producción~~ (D-29).
- Bloques `869f6r5r2` (cimientos de la API) y `869f8pm99` (migración a PostgreSQL) **cerrados** el
  29-sep: **Fase 2 terminada** (falta aplicar el prompt de PostgreSQL). Plataforma decidida el 29-sep (`869f6r4ww`,
  D-29): AWS simplificado en `eu-south-2`, ≈ 35 €/mes; Fargate + ALB como vía de escalado.
- Previsión del MVP piloto (29-sep, tras D-29): optimista finales de diciembre de 2026; **probable,
  hacia el 18 de enero de 2027**; pesimista, hacia el 19 de febrero. Detalle en `plan.md` → «Previsión».

## Espera a Guillermo

- **Hacer el commit de la documentación** del prompt de PostgreSQL (8 ficheros modificados y ADR-033 nuevo
  en `Documentation/`).
- **Reestructurar Infra por D-29 (OK de Guillermo el 29-sep; aplicar al reponerse la cuota):** sacar de la Fase 6 `869d7ew72` (ALB + CloudFront, vía
  de escalado) y renombrar las que dicen `eu-west-1` o ECS Fargate (`869d7evyq`, `869d7echh`,
  `869d7exag`). Ya tienen comentario con el ajuste.

- **Secreto antiguo de Google:** los dos equipos ya usan el nuevo (Windows puesto el 29-sep). Si en la
  consola de Google sigue existiendo el antiguo, se puede borrar.

- Guardar las instrucciones nuevas del proyecto de claude.ai (texto entregado el 2026-09-25).
- Trámites externos (`869f6r4nz`): primero dominio (`869f6r785`), RGPD y EIPD (`869f6r7b3`) y
  textos legales (`869f6r7e7`).

## Pendiente menor

- **ClickUp, cuota agotada** (el 30-sep por la mañana seguía agotada: se repone hacia las 16:00). Pendiente de aplicar en la próxima sesión, en cualquiera de los dos equipos:
  - **`869f8pmpn` a `shipped`** (no pasó por `in development`) con un comentario: PR #98 y la
    evidencia de `historial.md`.
  - **`869f8pmq4`** (Docs) a `publish` con un comentario: prompt `prompts/2026-09-29-postgresql.md`
    aplicado el 29-sep (ADR-033), en cuanto Guillermo haya hecho el commit de la documentación.
  - **`869f8pm99`** (épica de la migración) a `shipped`: bloque cerrado el 29-sep.
  - **`869f8ewx5`** (artefactos de Playwright) a `done` con un comentario: PR #98 (reglas del
    `.gitignore` y `playwright-report/`) y PR #99 (`test-results/.last-run.json` fuera del índice).
  - **`869f8hpfj`** (AWSSDK.Core) a `done` con un comentario: PR #100, `AWSSDK.Core` 4.0.102.7 fijada
    en la API (AWS.Logger.SeriLog 4.0.2 ya era la última); 0 avisos y ningún paquete vulnerable.
  - **`869d7f519`** a `shipped` con un comentario: PR #101 y las decisiones H-40.
  - **`869d7f53r`** a `shipped` con un comentario: PR #102 (alcance deducido sin leer la descripción:
    confirmar que no pedía nada más).
  - **`869f2gn91`** a `in review` (o `shipped` si ya está mergeado) con un comentario: PR #103.
  - **Crear subtarea de deuda** en el bloque de Citas (`869d7edau`), en backlog: «La reserva no comprueba
    la prueba de alergia previa (`Service.RequiresAllergyTest`, `AllergyTestHoursBefore`)», con
    enlace al PR #101.

## Decisiones pendientes (plantéalas cuando salte su disparador)

- DP-01 resuelta el 2026-09-29 (D-29): AWS simplificado (EC2 + RDS PostgreSQL 18) en `eu-south-2`;
  la arquitectura del vol. 1 (Fargate + ALB) queda como vía de escalado.
- DP-06 resuelta el 2026-09-29 (H-37): mayúsculas en la aplicación, PascalCase, PostgreSQL 18,
  Hangfire en la Fase 5. Tareas de ClickUp que nombraban SQL Server, ya ajustadas.
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- (DP-03 resuelta el 2026-09-28: AwesomeAssertions, H-35.)
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- `869f2gn91` (PR #103), para el vol. 1 §5.1 (contrato de `GET /customers/{id}/history`: solo personal,
  citas activas en cualquier estado, orden, paginación, `AppointmentDetailDto`, 404) y la entrada de
  §5.1 que hoy lo marca como pendiente.
- `869d7f53r` (PR #102), para la estrategia de testing: cancelación y aislamiento de citas por HTTP;
  el aislamiento descansa en dos capas (filtro global y repositorio).
- `869d7f519` (PR #101), bloque de Citas `869d7edau`, para el vol. 1 §3.1.5 y §5.1 (contrato de `/appointments`:
  rutas, roles, cálculo de fin, precio y duración, errores) y §5.2 (`created_by` pasa a `CreatedById`,
  escalar sin FK; decisión H-40), y el vol. 2 §9.9 (`AppointmentBookingService` separado de la máquina
  de estados; carrera conocida y restricción de exclusión pendiente).
- (Lo de la migración a PostgreSQL y las advertencias del prompt de cimientos van en
  `prompts/2026-09-29-postgresql.md`, entregado el 29-sep.)
- Advertencia de la IA en la Fase 1 (revisada el 2026-09-28):
  - Vol. 3, meses 6-7 y cuadro de costes: siguen con React Native («Mobile Developer (React Native)»,
    480 h y 19.200 € dentro de los 211.140 €), contra el ADR-020 (PWA). Hace falta que Guillermo
    estime la PWA; se resuelve al planificar `869f6r74n`, y entonces se recalcula el presupuesto.
- Auditoría mensual de octubre (primera sesión del mes): registros de estado que quedan en los
  volúmenes 1-3 y en el checklist del vol. 3, incluidas las notas históricas «Runtime (PR #nn): SQL
  Server…» que el prompt de PostgreSQL deja sin tocar a propósito.
- `appsettings.Production.json` fija `Serilog:Region` en `eu-west-1`; con D-29 es `eu-south-2`. Se
  corrige al montar la infraestructura (Fase 6), junto a la reestructuración de Infra.
- Advertencias del prompt de PostgreSQL (29-sep), revisadas, para el próximo prompt o la auditoría:
  - Vol. 3 §12.1 (línea «Configurar VPC en región eu-west-1»): la región es `eu-south-2` (D-29).
    Encaja con la reestructuración de Infra que espera el OK de Guillermo.
  - Vol. 3 §11.2, §11.6 y §11.7: filas de RDS de 5 y 50 centros, sus totales, el break-even y el ROI
    quedan «por recalcular». Se recalculan cuando haga falta el plan de negocio (no bloquea el piloto).
  - `Análisis de pantallas y estructura.md`: la cabecera sigue en versión 1.0 y octubre de 2025.
  - Falsos positivos o por diseño: `AspNet.Security.OAuth.Apple` 10.0.0 es un paquete de la comunidad
    con numeración propia (su 10.0.0 es la de .NET 10); ADR-032 cita los 133 € como presupuesto
    antiguo; ADR-021 sigue «pendiente» porque no se reescribe.

## Equipos

- **Mac:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, base `reservarte`, `InitialCreate`);
  `reservarte-sql`, su volumen y el secreto `SqlServerLegacy`, retirados el 2026-09-29;
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, PostgreSQL 18.6, base `reservarte`,
  `InitialCreate`); SDK .NET 10 (`10.0.401`) y `dotnet-ef` 10.0.12; `reservarte-sql` y su volumen,
  retirados. 31 ramas locales fusionadas, borrables con `git branch -d`.
