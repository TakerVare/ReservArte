# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-09-30 · Mac (cierre de `869eqxm8z`).

## Dónde estamos

- `develop` tras el PR #106 (`869eqxm8z`, Vitest). Rutas relativas y proxy de Vite desde el PR #105;
  prueba de alergia desde el PR #104; historial de la clienta desde el PR #103; API de citas desde el PR #101; tests de cancelación y aislamiento desde el #102. Build sin avisos desde el PR #100. Envelope en model binding, 404 y 405
  desde el PR #97; manejador global de excepciones desde el PR #96, mapa único de errores desde el PR #95; contratos HTTP desde el PR #94. Tests de integración
  con PostgreSQL real desde el PR #93; base de datos en PostgreSQL 18 desde el PR #92. Sin ramas de
  trabajo abiertas.
- Batería: unit backend **568/568**; unit frontend **59/59** (Vitest, en el CI); integración **130/130** (Testcontainers, en el CI desde el PR #93; necesitan
  Docker en marcha); E2E **63/63** (30-sep). Reejecutada íntegra en **los dos equipos** contra PostgreSQL el
  2026-09-29; unit e integración, de nuevo en el Mac el 30-sep (E2E sin reejecutar: la SPA no ha
  cambiado).
- **Backend en .NET 10 LTS** desde el PR #86 (`869f6r5ca`); AwesomeAssertions desde el #89. **Sin dependencias de pago.** **Hay CI:** «Backend CI / build-test-format» y
  «Frontend CI / lint-build» en cada PR a `develop`/`main` y en cada push a `develop`.
  En `main` los dos son obligatorios, también para admins (`869f6r4t8`); `develop`, sin protección.
- Bloque **Sistema de Citas** `869d7edau` **cerrado el 30-sep** (9 subtareas en `shipped`; prompt
  `prompts/2026-09-30-citas.md` entregado). **CRUD Servicios** `869d7ed7v`, cerrado el 25-sep.
- Avance estimado (recalculado el 30-sep, `plan.md` → «Previsión»): MVP ≈ 41 % (backend ≈ 61 %,
  frontend ≈ 21 %); proyecto completo (fases 1-3) ≈ 21 %.
- Guillermo aprobó el 2026-09-24 todas las recomendaciones de la auditoría. El 2026-09-25 se crearon
  45 tareas y subtareas en ClickUp con 21 dependencias, y el orden propuesto está en `plan.md`
  (se confirma en la re-planificación, `869f6r4ec`).

## Tarea en curso

Ninguna. Último cierre: `869eqxm8z` (PR #106). Siguiente del plan: paso 3.6, `869f6r6dk`
(vue-i18n 11); espera el OK de Guillermo.

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
- Fase 3: backend de la agenda terminado (pasos 3.1-3.3 y la deuda de alergia); sigue el frontend
  (3.4-3.15). Demo de la agenda a More Than Brows al cerrar la Fase 3.
- Previsión del MVP piloto (30-sep, cierre de Citas): optimista finales de diciembre de 2026;
  **probable, hacia el 11 de enero de 2027**; pesimista, hacia el 10 de febrero. Detalle en `plan.md` → «Previsión».

## Espera a Guillermo


- **Secreto antiguo de Google:** los dos equipos ya usan el nuevo (Windows puesto el 29-sep). Si en la
  consola de Google sigue existiendo el antiguo, se puede borrar.

- Guardar las instrucciones nuevas del proyecto de claude.ai (texto entregado el 2026-09-25).
- Trámites externos (`869f6r4nz`): primero dominio (`869f6r785`), RGPD y EIPD (`869f6r7b3`) y
  textos legales (`869f6r7e7`).

## Pendiente menor

- ClickUp al día el 30-sep: cierres de la migración (`869f8pmpn`, `869f8pmq4` publish, épica
  `869f8pm99`), de la opción B (`869f8ewx5`, `869f8hpfj`) y de citas (`869d7f519`, `869d7f53r`,
  `869f2gn91`), con comentarios; deuda `869f9cu2x` (prueba de alergia) creada; Infra renombrada por
  D-29 (`869d7evyq`, `869d7echh`, `869d7exag`).
- `869d7f519`: DELETE = baja lógica y `/cancel` sin penalización, **confirmado por Guillermo el 30-sep**
  (la descripción de ClickUp decía DELETE = cancelar).
- **Node en los dos equipos:** dejarlo como el CI (Node 24, la última 24.x), no el 26 (rama Current).
  Mac: nvm tiene 20.20.2, 24.11.1 y 26.10.0, y el alias `default` sigue en 24.11.1 →
  `nvm install 24 && nvm alias default 24`, abrir terminal nueva y comprobar que `npm -v` ≥ 11.20.
  Windows: igual antes de tocar dependencias, y `npm ci` al volver (el lockfile cambió en el PR #106).
- **Windows:** comentar o borrar `VITE_API_BASE_URL` y `VITE_APP_URL` del `.env` local (en el Mac, ya
  hecho el 30-sep). Ya no se usan.
- `ReservArte-Domain/Entities/Customer.cs`, comentario final: dice que `Appointments` y `WaitingLists`
  no están en el DbContext, y sí lo están (lo que no existe es la navegación desde `Customer`).
  Corregirlo en el próximo PR de backend (advertencia de la IA, 30-sep, verificada).

## Decisiones pendientes (plantéalas cuando salte su disparador)

- DP-01 resuelta el 2026-09-29 (D-29): AWS simplificado (EC2 + RDS PostgreSQL 18) en `eu-south-2`;
  la arquitectura del vol. 1 (Fargate + ALB) queda como vía de escalado.
- DP-06 resuelta el 2026-09-29 (H-37): mayúsculas en la aplicación, PascalCase, PostgreSQL 18,
  Hangfire en la Fase 5. Tareas de ClickUp que nombraban SQL Server, ya ajustadas.
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- (DP-03 resuelta el 2026-09-28: AwesomeAssertions, H-35.)
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- `869eqxm8z` (PR #106): estrategia de testing (capa unitaria y de componente del frontend: Vitest +
  `@vue/test-utils` en happy-dom, convención `__tests__/`, en el CI) y vol. 3 §12.2 (Vitest deja de
  estar pendiente); vol. 1 o guía de instalación: Node 24 con npm ≥ 11.20 para generar el lockfile.
- `869f6r69b` (PR #105): configuración del frontend (vol. 1 §5.1.3 o donde se describa):
  `API_PROXY_TARGET` para el proxy de Vite; se retiran `VITE_API_BASE_URL` y `VITE_APP_URL`; la SPA
  llama a `/api` en su mismo origen. `Scripts de instalación.md` y el vol. 3 aún citan
  `VITE_API_BASE_URL`. Para la Fase 6 (despliegue): en producción, `/api` se sirve en el mismo origen
  que la SPA (proxy inverso delante de la API).
- Prompt del bloque de Citas (`prompts/2026-09-30-citas.md`) **aplicado** el 30-sep (commit `5d9bf11`,
  ADR-034 y ADR-035, enlazados desde H-40 y H-41). Advertencias revisadas; van a la auditoría de octubre:
  - Vol. 3: el bloque de Citas del MVP aún incluye la penalización, la lista de espera y el contador
    de no-shows, dice «5/12» y que faltan los endpoints. Esas subtareas se cancelaron y trasladaron
    (la penalización cita `869f6ae9h`, la original cancelada; la vigente es `869f7axdq`, en Redsys).
  - Vol. 2 §9.9: sigue siendo en gran parte un registro de tareas (PRs, recuentos, «siguiente»).
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

- **Mac:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, base `reservarte`); la base de
  desarrollo tiene pendientes `AddAppointmentCreatedBy` y `AddCustomerLastAllergyTest` (se aplican al
  arrancar la API en Development);
  `reservarte-sql`, su volumen y el secreto `SqlServerLegacy`, retirados el 2026-09-29;
  `guille@svalero.com` ya no tiene 2FA. 25 ramas locales fusionadas, borrables con `git branch -d`.
- **Windows:** PostgreSQL desde el 2026-09-29 (`reservarte-pg`, PostgreSQL 18.6, base `reservarte`,
  `InitialCreate`); SDK .NET 10 (`10.0.401`) y `dotnet-ef` 10.0.12; `reservarte-sql` y su volumen,
  retirados. 31 ramas locales fusionadas, borrables con `git branch -d`. Al volver: `dotnet ef
  migrations list` (han llegado `AddAppointmentCreatedBy` y `AddCustomerLastAllergyTest`).
