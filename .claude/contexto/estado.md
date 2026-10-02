# Estado y traspaso entre equipos

> Lo actualiza Claude Code en cada punto de control (al empezar y al cerrar una tarea, con
> `/traspaso` y al cerrar un bloque) y lo sube enseguida para que el otro equipo lo vea. Mientras
> haya una rama en curso, se actualiza solo en esa rama. Corto: la historia va a `historial.md` y el
> orden a `plan.md`.

**Última actualización:** 2026-10-02 · Mac (PR #119 de `869d7fbyt`).

## Dónde estamos

- `develop` tras el PR #119 (`869d7fbyt` + `869d7fc0h`, empleados). CancelModal desde el PR #118. Listado de citas del personal desde el PR #117. Mis citas filtrada por la cuenta desde el PR #116. Pantalla de reserva desde el PR #115. API de reserva desde el PR #114 (H-44 y H-45). Listado de gestión desde el PR #113. Admin de Google en desarrollo desde el PR #112. Componentes base desde el PR #111. Contacto por anchos desde el PR #110. Mis citas y Contacto desde el PR #109. Navegación plana desde el PR #108. vue-i18n 11 desde el PR #107; Vitest desde el PR #106; rutas relativas y
  proxy de Vite desde el PR #105;
  prueba de alergia desde el PR #104; historial de la clienta desde el PR #103; API de citas desde el PR #101; tests de cancelación y aislamiento desde el #102. Build sin avisos desde el PR #100. Envelope en model binding, 404 y 405
  desde el PR #97; manejador global de excepciones desde el PR #96, mapa único de errores desde el PR #95; contratos HTTP desde el PR #94. Tests de integración
  con PostgreSQL real desde el PR #93; base de datos en PostgreSQL 18 desde el PR #92. Sin ramas de
  trabajo abiertas.
- Batería: unit backend **568/568**; unit frontend **191/191** (Vitest, en el CI); integración **145/145** (Testcontainers, en el CI desde el PR #93; necesitan
  Docker en marcha); E2E **195/195** (2-oct, PR #119). Reejecutada íntegra en **los dos equipos** contra PostgreSQL el
  2026-09-29; unit e integración, de nuevo en el Mac el 30-sep (E2E sin reejecutar: la SPA no ha
  cambiado).
- **Backend en .NET 10 LTS** desde el PR #86 (`869f6r5ca`); AwesomeAssertions desde el #89. **Sin dependencias de pago.** **Hay CI:** «Backend CI / build-test-format» y
  «Frontend CI / lint-build» en cada PR a `develop`/`main` y en cada push a `develop`.
  En `main` los dos son obligatorios, también para admins (`869f6r4t8`); `develop`, sin protección.
- Bloque **Sistema de Citas** `869d7edau` **cerrado el 30-sep** (9 subtareas en `shipped`; prompt
  `prompts/2026-09-30-citas.md` entregado). **CRUD Servicios** `869d7ed7v`, cerrado el 25-sep.
- Avance estimado (recalculado el 1-oct, `plan.md` → «Previsión»): MVP ≈ 59 % (backend ≈ 63 %,
  frontend ≈ 55 %); proyecto completo (fases 1-3) ≈ 30 %. **Fase 3 cerrada** el 1-oct.
- Guillermo aprobó el 2026-09-24 todas las recomendaciones de la auditoría. El 2026-09-25 se crearon
  45 tareas y subtareas en ClickUp con 21 dependencias, y el orden propuesto está en `plan.md`
  (se confirma en la re-planificación, `869f6r4ec`).

## Tarea en curso

4.1b — servicios que presta cada empleado (OK de Guillermo el 2-oct: ahora, antes de 4.2; solo
casillas, sin nivel visible). **ID de ClickUp pendiente** (cupo agotado): rama local provisional
`feature/pendiente-servicios-empleado`, que se renombra con el ID antes del push. Objetivo:
`GET/PUT /api/v1/employees/{id}/services` (reemplaza el conjunto entero, como el horario; baja lógica
de lo que sale, nivel 1 en las altas) y pestaña «Servicios» en `/empleados/:id`.
**ClickUp pendiente** (cupo diario agotado el 2-oct; aplicar al recuperarlo):
- `869d7fbyt` y `869d7fc0h` a `shipped`, con comentario que enlace el PR #119.
- Crear la tarea de 4.1b en Backend (o Frontend) en `in development` y renombrar la rama con su ID.
- Deuda en backlog: foto del empleado en la ficha (subir, cambiar y quitar), ligada a `869d7ee5t`.
Para la documentación (bloque de la Fase 4): pantallas de empleados (vol. 1, análisis de pantallas),
`Avatar`, zona horaria del centro en el frontend.
Nota de entorno: la shell de Claude Code puede heredar un Node antiguo del arranque de la sesión;
si `node -v` no es la 24 más reciente, lanzar con `PATH=~/.nvm/versions/node/v24.21.0/bin:$PATH`.

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
- Fase 3 **cerrada** el 1-oct (backend y frontend de la agenda). Demo de la agenda a More Than Brows pendiente de fecha.
- Previsión del MVP piloto (1-oct, cierre de la Fase 3): optimista principios de noviembre de 2026;
  **probable, hacia el 4 de diciembre de 2026**; pesimista, hacia el 8 de enero de 2027. Detalle en `plan.md` → «Previsión».

## Espera a Guillermo

- **Probar el login con Google de `takervare@gmail.com`** (Admin desde el PR #112): debe ver el
  Área de administración en «Mi cuenta». En el Windows, antes, arrancar la API en Development.

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
- **Node en el Windows:** dejarlo como el CI (Node 24, la última 24.x; con 24.21.0, npm 11.19):
  `nvm install 24` y usarla por defecto, antes de tocar dependencias; después `npm ci` (el lockfile
  cambió en los PR #106 y #107). El Mac ya está (30-sep: `default` → 24 = 24.21.0).
- **Windows:** comentar o borrar `VITE_API_BASE_URL` y `VITE_APP_URL` del `.env` local (en el Mac, ya
  hecho el 30-sep). Ya no se usan.

## Decisiones pendientes (plantéalas cuando salte su disparador)

- DP-01 resuelta el 2026-09-29 (D-29): AWS simplificado (EC2 + RDS PostgreSQL 18) en `eu-south-2`;
  la arquitectura del vol. 1 (Fargate + ALB) queda como vía de escalado.
- DP-06 resuelta el 2026-09-29 (H-37): mayúsculas en la aplicación, PascalCase, PostgreSQL 18,
  Hangfire en la Fase 5. Tareas de ClickUp que nombraban SQL Server, ya ajustadas.
- DP-02 Librería de gráficas (propuesta: vue-chartjs) → al llegar al dashboard.
- (DP-03 resuelta el 2026-09-28: AwesomeAssertions, H-35.)
- Resto en `decisiones.md` → «Pendientes».

## Documentación acumulada para el próximo prompt

- Prompt de la Fase 3 **aplicado** el 1-oct (commit `f6e2481`, ADR-036 a ADR-039, enlazados desde H-42 a
  H-45). Advertencias revisadas: las válidas van al prompt de correcciones de la auditoría.
- Auditoría mensual de octubre **cerrada** el 1-oct: informe revisado con Guillermo y prompt de
  correcciones aplicado (commit `b71a6fc`, ADR-040 enlazado desde H-46). Advertencias revisadas; para el
  próximo prompt:
  - Vol. 1 §5.2: el DDL sigue en T-SQL (95 `NVARCHAR`, `UNIQUEIDENTIFIER`, `NEWID()`); sustituirlo por
    un enlace a `data/schema/create_ReservArteDB.sql` como fuente única (motor PostgreSQL 18).
  - Análisis de pantallas, árbol: quedan `PublicLayout.vue`, `Footer.vue`, `CalendarView.vue` y
    `AppointmentWizard.vue`, que no existen.
  - Vol. 3, mes 1: «Crear solución con Clean Architecture» (ver ADR-015).
  - Notas menores de SQL Server: paquete de Hangfire retirado (vol. 1) y PR #76 del vol. 3.
  - Meses 1-2 y checklist §12.2 del vol. 3 con PRs y recuentos: se limpian al tocarlos (no reescribir
    historia de golpe).
- `appsettings.Production.json` fija `Serilog:Region` en `eu-west-1`; con D-29 es `eu-south-2`. Se
  corrige al montar la infraestructura (Fase 6).
- Falsos positivos ya revisados (no reabrir): `AspNet.Security.OAuth.Apple` 10.0.0 tiene numeración
  propia; ADR-032 cita los 133 € como presupuesto antiguo; ADR-021 sigue «pendiente» porque no se
  reescribe.

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
