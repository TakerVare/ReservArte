# Plan de implementación

> Orden de trabajo propuesto el 2026-09-25 sobre las recomendaciones que Guillermo aprobó el
> 2026-09-24. Se confirma o ajusta en la re-planificación (`869f6r4ec`) y se revisa en cada cierre
> de bloque. El estado de cada tarea vive en ClickUp; aquí solo el orden, las dependencias y las
> estimaciones. Las dependencias duras están también en ClickUp como `waiting_on`.

## Principios de orden

1. Fechas tope externas primero: .NET 8 pierde soporte el 10-nov-2026.
2. Red de seguridad antes de acelerar: CI y tests de integración protegen todo lo que viene.
3. Cimientos justo antes de quien los necesita: el mapa de errores antes de los endpoints de citas;
   los filtros cerrados antes de Hangfire.
4. Entrega en vertical (API + pantalla + E2E por módulo), empezando por la agenda.
5. Lo que tiene plazo externo arranca ya y en paralelo (trámites), sin ocupar el hueco de desarrollo.
6. Una tarea de desarrollo en curso a la vez.

## MVP piloto (More Than Brows)

**Dentro:** agenda y citas (calendario, alta con wizard, reagendar y cancelar sin penalización);
clientes (ficha, consentimientos, alergias, notas e historial de citas); servicios (catálogo);
empleados (ficha, horario y ausencias); recordatorio de cita por email; configuración mínima del
centro (zona horaria, umbral de cancelación y recordatorios); login local y con Google; sesión que se
renueva sola; guards por rol; cobro en el centro.

**Fuera (post-piloto):** Redsys y penalizaciones, lista de espera, promoción de categoría, AuditLog,
configuración completa y theming en runtime, app móvil, reserva pública, fotos, fidelización y todo
lo de SaaS. Apple e Instagram se activan cuando terminen sus trámites. Dashboard y no-shows son
opcionales, al final, si el calendario lo permite.

El alcance lo decide Guillermo, en quien More Than Brows ha delegado las decisiones de producto.

## Hitos

- **.NET 10 en `develop`**: objetivo 9-oct-2026; fecha tope 6-nov-2026 (`869f6r5ca`).
- **Primera demo de la agenda** al centro: al cerrar la Fase 3, hacia finales de noviembre o la
  primera semana de diciembre de 2026.
- **MVP piloto en producción** (`869f6r4zt`): rango de fechas en «Previsión».

## Fases y pasos

Estimaciones en horas de trabajo con Claude Code, revisadas en `869f6r4ec` (2026-09-28). Las de la
oleada en curso están también en ClickUp (`time_estimate`); el resto se vuelca al entrar en su oleada.
El orden se confirmó tal cual el 2026-09-28: Guillermo descartó adelantar la agenda moviendo 2.3,
2.7 y 3.9 (ganaba unos 10 días).

### Fase 0 — Arranque del nuevo modelo de trabajo (≈ 9 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 0.1 | `869f6r4ba` Incorporar la estructura de contexto de Claude Code | 2 | |
| 0.2 | `869f74uca` Limpieza y reorganización de ClickUp | 2 | |
| 0.3 | `869f6r4ec` Re-planificación con la capacidad real | 3 | espera a 0.2 |
| 0.4 | `869f6r52d` Nuevo régimen de documentación | 1 | Guillermo + IA de documentación |
| 0.5 | `869f6r4hm` Plantilla de PR | 1 | |

En paralelo, Guillermo: trámites `869f6r4nz` → dominio `869f6r785`, RGPD y EIPD `869f6r7b3`,
textos legales `869f6r7e7`; después, cuenta Redsys `869f6r7hd`; opcionales, Apple `869f6r7kt` y
Meta `869f6r7p9`.

### Fase 1 — Red de seguridad y plataforma (≈ 34 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 1.1 | `869d7ex56` CI backend: build + test + `dotnet format --verify-no-changes` | 4 | |
| 1.2 | `869d7ex8r` CI frontend: `npm ci` + lint + build (Vitest cuando exista) | 3 | |
| 1.3 | `869f6r4t8` Checks obligatorios en `main` | 1 | espera a 1.1 y 1.2 |
| 1.4 | `869f6r5ca` Migración a .NET 10 LTS | 10 | tope 6-nov; espera a 1.1 |
| 1.5 | `869f6r7rj` Retirar MediatR | 1 | |
| 1.6 | `869f6r7vw` AutoMapper → Mapperly | 6 | |
| 1.7 | `869f6r7yh` Sustituir FluentAssertions 8 y fijar versiones de test | 4 | decide DP-03 |
| 1.8 | `869f6r54r` ADR iniciales (prompt a la IA de documentación) | 2 | espera a 0.4 |
| 1.9 | `869f6r58r` Incoherencias de documentación y retirada de Gabriel | 3 | tras 1.4, para incluir .NET 10 |

Cierre de bloque: un prompt de documentación con 1.1-1.7, junto con los de 1.8 y 1.9.

### Fase 2 — Cimientos de la API (≈ 33 h + 20 h de la migración a PostgreSQL)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 2.1 | `869f6r5jf` Correcciones menores de la auditoría | 4 | |
| 2.1b | `869f8pmnm` Fechas en UTC en la frontera de la API | 3 | épica `869f8pm99` (D-28) |
| 2.1c | `869f8pmpa` Cambio del motor a PostgreSQL (un PR) | 14 | espera a 2.1b; decisiones en H-37 |
| 2.1d | `869f8pmpn` PostgreSQL en el Windows (Guillermo) | 1 | hecha el 29-sep (fix en PR #98) |
| 2.1e | `869f8pmq4` Documentación del bloque (ADR del motor) | 2 | prompt entregado el 29-sep (ADR-033) |
| 2.2 | `869f6r5ng` Infraestructura de tests de integración (Testcontainers.PostgreSql + WebApplicationFactory) | 8 | espera a 2.1c |
| 2.3 | `869f2gh37` Contratos HTTP de Empleados y Clientes | 6 | espera a 2.2 |
| 2.4 | `869f6r81n` Mapa central de códigos de error y respuesta común | 6 | espera a 2.2; absorbe `869f17y6k` |
| 2.5 | `869f74u70` Manejador global de excepciones | 3 | espera a 2.4 |
| 2.6 | `869f1k17q` 400 de model binding con envelope | 2 | espera a 2.4 |
| 2.7 | `869f6r4ww` Decisión de plataforma de producción (ADR) | 4 | decidida el 29-sep: D-29, AWS simplificado |

### Fase 3 — Primer vertical: la agenda (≈ 100 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 3.1 | `869d7f519` Endpoints de citas (decide `created_by`) | 12 | hecha el 30-sep (PR #101, H-40) |
| 3.2 | `869d7f53r` Tests de cancelación y aislamiento (sin penalización) | 6 | hecha el 30-sep (PR #102) |
| 3.3 | `869f2gn91` Historial de citas del cliente (`/history`) | 3 | hecha el 30-sep (PR #103) |
| 3.3b | `869f9cu2x` Prueba de alergia previa (deuda, aviso sin bloqueo) | 2 | hecha el 30-sep (PR #104, H-41); cierra el bloque `869d7edau` |
| 3.4 | `869f6r69b` URL relativa y proxy de Vite | 3 | |
| 3.5 | `869eqxm8z` Vitest | 5 | |
| 3.6 | `869f6r6dk` vue-i18n 11 | 3 | |
| 3.7 | `869ep9p36` Reconciliación de layouts (solo BottomNav) | 5 | hecha el 1-oct (PR #108) |
| 3.8 | `869d7fbuf` Componentes base (Reka UI) | 10 | hecha el 1-oct (PR #111) |
| 3.9 | `869d7fbxn` DataTable | 6 | hecha el 1-oct (PR #113): `DataList` + `useDataList` |
| 3.10 | `869fagpx9` API de reserva (huecos por servicio, días con hueco, ventana, reserva por la clienta) | 10 | hecha el 1-oct (PR #114); sustituye a `869d7fc8y` (agenda, cancelada) |
| 3.11 | `869d7fcbu` AppointmentCard | 4 | cancelada el 1-oct: la cubre el listado de citas (`869fajn7g`, PR #117) |
| 3.12 | `869fagpyg` Pantalla de reserva y modificación (Figma `387:56629`) | 12 | hecha el 1-oct (PR #115); sustituye al wizard (`869d7fcd0` + `869d7fcen`, cancelados) |
| 3.13 | `869d7fch0` RescheduleModal | 5 | cancelada el 1-oct: la cubre la pantalla de reserva |
| 3.14 | `869d7fcfy` CancelModal (sin penalización en el piloto) | 4 | hecha el 1-oct (PR #118) |
| 3.15 | `869d7fca1` Colores por estado y drag & drop | 6 | cancelada el 1-oct: colores hechos en `869fajn7g`; arrastre descartado |

Cierre de bloque: demo de la agenda a More Than Brows y prompt de documentación.

### Fase 4 — Segundo vertical: gestión (≈ 62 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 4.1 | ~~`869d7fbyt` + `869d7fc0h` Empleados: lista, detalle, formulario y horario~~ | 16 | PR #119, 2-oct |
| 4.1b | ~~`869faz10y` Servicios que presta cada empleado: API `GET/PUT /employees/{id}/services` y pestaña «Servicios» en la ficha, solo casillas (nivel 1 por defecto)~~ | 8 | PR #120, 2-oct |
| 4.2 | ~~`869d7fc34` + `869d7fc51` Clientes (sin tarjeta guardada en el piloto)~~ | 16 | PR #121, 2-oct |
| 4.2b | ~~`869fazwwe` Ficha de clienta completa: consentimientos (dar y retirar; retirar el de datos da de baja, H-47), alergias y bloqueo, con su API; ficha de empleado de `guille@svalero.com` en el seeder~~ | 12 | PR #122, 2-oct |
| 4.3 | ~~`869d7fc6b` Servicios~~ | 10 | PR #123, 2-oct |
| opc. | `869epnt88` LoginForm a VeeValidate + Zod | 2 | |

### Fase 5 — Tercer vertical: configuración mínima y recordatorios (≈ 46 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 5.1 | ~~`869f6r5vy` Filtros de tenant cerrados y ámbito de sistema~~ | 6 | PR #124, 2-oct |
| 5.2 | ~~`869f74u7y` OrganizationSettings mínimo (épica `869f6r5y8`)~~ | 8 | PR #125, 5-oct |
| 5.3 | ~~`869f6r71x` Configuración mínima en la SPA~~ | 6 | PR #126, 5-oct; el recordatorio por email queda en `869fc1a48`, tras 5.8 |
| 5.4 | ~~`869d7f5wx` Recordatorios: entidades y migración~~ | 6 | PR #127, 5-oct |
| 5.5 | ~~`869d7f5zq` Programación con Hangfire~~ | 6 | PR #128, 6-oct |
| 5.6 | `869d7f61y` Envío por canal (email en el piloto) | 4 | decidir si el token de confirmación se guarda en claro o como hash |
| 5.7 | `869d7f65a` Servicio de email SES y plantilla | 6 | producción necesita `869d7exmk` |
| 5.8 | `869d7f6aa` Endpoints de configuración y dashboard de Hangfire protegido | 4 | |
| opc. | `869f7axeg` No-shows | 6 | desbloqueada (5.2 hecha) |

### Fase 6 — Salida a producción del piloto (≈ 94 h + opcional)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 6.1 | `869f6r61z` Sesión (backend): refresh en cookie httpOnly | 8 | decide DP-04 |
| 6.2 | `869f6r6hc` Sesión (SPA): token en memoria y rehidratación | 8 | espera a 6.1 y 3.4 |
| 6.3 | `869f1auqv` Guards por rol | 4 | espera a 6.2 |
| 6.4 | `869f151x1` 2FA también en el login social | 4 | |
| 6.5 | `869epkndg` Consentimiento RGPD en el alta social | 4 | |
| 6.6 | `869f1812p`, `869f1mqah`, `869en8a17` Pendientes de autenticación | 9 | |
| 6.7 | `869f74u8w`, `869f74u98`, `869f74u9m` Endurecimiento (limitador e IP real, caché de tenant, cabeceras) | 9 | `869f74u8w` espera a 2.7 |
| 6.8 | `869f74ua4` Cifrado de campos sensibles | 6 | si la EIPD (`869f6r7b3`) lo exige |
| 6.9 | `869f6r6uu` + `869eqxm7w` + `869f18uta` E2E de producto y E2E en CI | 12 | |
| 6.10 | Infra según D-29 (AWS simplificado): `869d7ewec`, `869d7exff`, `869d7exag`, `869d7excz`, `869d7exmk`, `869d7exqg`, `869d7ewnz`, `869d7exj4` | ~20 | ajustadas el 29-sep y renombradas el 30-sep; ALB + CloudFront (`869d7ew72`) fuera del piloto: vía de escalado |
| opc. | `869f6r6nx` + `869f7axcv` + `869fb3r11` Dashboard | 17 | decide DP-02; la pantalla se trasladó desde `869d7fc7e` el 2-oct |
| 6.11 | `869f6r4zt` Hito: MVP piloto en producción | — | |

### Fase 7 — Tras el piloto (orden provisional)

1. Redsys: `869d7eden` y sus subtareas, `869f2gnbm` (tarjetas guardadas), `869f7axdq`
   (penalización al cancelar) y el frontend de `869d7edya` (`869d7fcjv`, `869d7fcmz`, `869d7fcpa`,
   `869d7fcr5`, `869d7fcu1`, `869d7fcv7`).
2. Lista de espera (`869f7axfq` + `869d7fchj`), promoción de categoría (`869f7axh9`) y AuditLog
   (`869f2gtz8`).
3. Configuración completa (`869d7fcww`), identidad de marca (`869f74u8c`) y theming en runtime
   (`869f6r6xv`); deudas de accesibilidad `869f0v6vm`, `869f0w7r2` y `869f0w75h`.
4. App móvil nativa en **React Native**, después del piloto: una app por centro (marca blanca), para clientas y personal (H-46, sustituye a D-20; `869f6r74n`). Estimación antigua ≈ 480 h, por revisar al planificarla; no cambia la fecha del MVP.
5. Fase 2 funcional: reserva pública y «Mi cuenta» (`869d7ee36`), fotos (`869d7ee5t`); fidelización
   y cupones aún sin tareas.
6. Fase 3 SaaS: onboarding y subdominios (`869d7ee9p`), suscripciones (`869d7eebv`); versiones
   legales por organización.
7. Sin fase asignada: `869d7ewzg` (husky + commitlint) y `869f2g60e` (árbol de estructura en la
   documentación).

## Previsión

Registro de cada recálculo (lo añade `/cerrar-bloque`; el más reciente arriba).

**2026-10-02 (cierre de la Fase 4, bloque `869d7edt7`, `/cerrar-bloque`):**
- Fase 4 cerrada en un día: 4.1 empleados (PR #119), 4.1b servicios del empleado (PR #120), 4.2
  clientes (PR #121), 4.2b ficha de clienta completa (PR #122) y 4.3 servicios (PR #123). 4.1b y 4.2b
  las pidió Guillermo al revisar (+20 h al plan); los pasos 3.8 y 3.9 del bloque ya estaban hechos.
  La pantalla del dashboard (`869d7fc7e`) se trasladó a `869fb3r11`, opcional en la Fase 6.
- Tiempo de ciclo (commits `empieza` → merge): 4.1 ≈ 29 min, 4.1b ≈ 1 h 19 min (con la espera del
  cupo de ClickUp), 4.2 ≈ 22 min, 4.2b ≈ 36 min, 4.3 ≈ 1 h 51 min (con una pausa por el límite de
  uso). ≈ 62 h de plan en ≈ 4 h 40 min de reloj, revisión incluida: ≈ 13 veces.
- Throughput: 5 PRs el 2-oct (todos de este bloque); 42 PRs del 28-sep al 2-oct.
- Pendiente: ≈ 138 h (180 h − 42 h de la Fase 4; 4.1b y 4.2b entraron y salieron el mismo día). Son
  las Fases 5 y 6 (≈ 130 h) más los restos sueltos. **Factor 0,8 sin cambios**: lo que queda pesa en
  infraestructura en AWS, trámites (dominio, RGPD y EIPD, textos legales) y validación con el centro,
  que la IA no acelera.
- MVP piloto en producción: **optimista, finales de octubre de 2026 (factor 1,6); probable, hacia el
  20 de noviembre de 2026 (factor 0,8); pesimista, hacia el 4 de diciembre de 2026 (factor 0,6)**
  (antes: principios de noviembre, 4 de diciembre y 8 de enero). Las fechas dependen de los trámites
  externos, que no han avanzado.
- Avance (modelo de `gestion.md` §7, ±5 puntos): frontend del MVP de ≈ 55 % a ≈ 75 % (gestión
  completa de empleados, clientes y servicios; faltan configuración, sesión renovable y guards por
  rol); backend de ≈ 63 % a ≈ 66 % (servicios del empleado, ficha de clienta) → **MVP ≈ 70 %**,
  **proyecto ≈ 35 %**.

**2026-10-01 (cierre de la Fase 3, bloque `869d7edvq`, `/cerrar-bloque`):**
- Fase 3 cerrada: pasos 3.4-3.9 y 3.14 hechos; 3.10 y 3.12 sustituidos por la API y la pantalla de
  reserva (H-44, H-45); 3.11, 3.13 y 3.15 cancelados por cubiertos o descartados. Además, fuera del
  plan: Mis citas y Contacto (H-42), Contacto por anchos, admin de Google, bug de Mis citas y listado
  de citas del personal. 14 tareas, PRs #105-#118, el 30-sep y el 1-oct.
- Tiempo de ciclo (commits `empieza`/`cierra`, con revisión y merge): de 7 min a 1 h por tarea
  (3.7 cruzó la noche). Los pasos del plan sumaban ≈ 79 h; el trabajo de código asistido va de 10 a
  30 veces más rápido que lo estimado. El cuello de botella es la disponibilidad de Guillermo para
  diseño, decisiones y revisión, no la implementación.
- Throughput: 18 PRs fusionados el 30-sep y el 1-oct (14 de este bloque).
- Pendiente: ≈ 180 h (255 h − 79 h de la Fase 3 + ≈ 4 h de `869fabu4m`, datos de contacto desde la
  API). **Factor 0,8 sin cambios** a propósito: lo que queda pesa más en lo que no acelera la IA
  (trámites, AWS en producción, textos legales, validación con el centro).
- MVP piloto en producción: **optimista, principios de noviembre de 2026 (factor 1,6); probable,
  hacia el 4 de diciembre de 2026 (factor 0,8); pesimista, hacia el 8 de enero de 2027 (factor 0,6 y
  dos semanas de Navidad)** (antes: finales de diciembre, 11 de enero y 10 de febrero). Dependencias
  externas que pueden mover la fecha: dominio, RGPD y EIPD, textos legales.
- Avance (modelo de `gestion.md` §7, ±5 puntos): Citas en el backend ≈ 95 % (reserva por la clienta
  hecha); frontend del MVP de ≈ 21 % a ≈ 55 % (agenda completa: reserva, Mis citas, listado,
  cancelación; faltan las pantallas de gestión de la Fase 4 y la configuración) → backend ≈ 63 %,
  **MVP ≈ 59 %**, **proyecto ≈ 30 %**.

**2026-09-30 (cierre del bloque «Sistema de Citas», `869d7edau`, `/cerrar-bloque`):**
- Bloque abierto el 16-sep (PR #69) y cerrado el 30-sep: 9 subtareas en `shipped` y 5 canceladas o
  trasladadas (lista de espera, no-shows, penalización y promoción de categoría, fuera del bloque).
  Esta tanda: `869d7f519`, `869d7f53r`, `869f2gn91` y la deuda `869f9cu2x` (PRs #101-#104).
- Tiempo de ciclo (commits `empieza`/`cierra`, reloj de pared con revisión y merge): `869d7f519`
  ≈ 6 h (estimada en 12 h), `869f2gn91` ≈ 30 min (3 h), `869f9cu2x` ≈ 20 min hasta el merge (sin
  estimar). `869d7f53r` marca 6 h 30 min, pero incluye la noche. Primer trabajo de **producto**
  medido: el sesgo baja a unas 2-6 veces, frente a unas 9 en infraestructura. **Sigue sin aplicarse
  al plan**: lo que queda de la Fase 3 es frontend, sin historial.
- Throughput: 23 PRs fusionados del 28 al 30-sep.
- Pendiente: ≈ 255 h (276 h − 21 h de los pasos 3.1-3.3; la deuda de alergia no estaba en el
  plan). Factor 0,8 sin cambios.
- MVP piloto en producción, con dos semanas de Navidad: **optimista, finales de diciembre de 2026;
  probable, hacia el 11 de enero de 2027; pesimista, hacia el 10 de febrero de 2027** (antes: 18 de
  enero y 19 de febrero).
- Avance (modelo de `gestion.md` §7): Citas en el backend pasa de ≈ 50 % a ≈ 85 % (faltan la
  reserva pública y la restricción de exclusión; lista de espera, no-shows y penalización salieron
  del bloque) → backend del MVP ≈ 61 %, **MVP ≈ 41 %**, **proyecto ≈ 21 %** (±5 puntos).

**2026-09-29 (cierre del bloque «Migración a PostgreSQL», `869f8pm99`, `/cerrar-bloque`):**
- Bloque: `869f8pmnm` (PR #91), `869f8pmpa` (PR #92), `869f8pmpn` (Windows, fix en PR #98) y el
  prompt de `869f8pmq4`. Estimado ≈ 20 h; reloj de commits `empieza`/`cierra`: 14 min, 29 min y
  30 min (el de `869f8pmpa` no recoge el trabajo previo al commit de inicio). Mismo sesgo que en los
  bloques anteriores; sigue sin aplicarse al plan.
- **Fase 2 terminada** el 29-sep (pasos 2.1-2.7). D-29 baja el paso 6.10 de ≈ 30 h a ≈ 20 h.
- Pendiente: ≈ 276 h (289 h − 3 h de 2.1d y 2.1e − 10 h del paso 6.10). Factor 0,8 sin cambios.
- MVP piloto en producción, con dos semanas de Navidad: **optimista, finales de diciembre de 2026;
  probable, hacia el 18 de enero de 2027; pesimista, hacia el 19 de febrero de 2027**.
- Avance sin cambios (MVP ≈ 39 %, proyecto ≈ 20 %): la migración de motor no mueve filas de producto.

**2026-09-29 (cierre del bloque «Cimientos de la API», `869f6r5r2`, `/cerrar-bloque`):**
- Fase 2 casi completa en un día: `869f6r5jf`, `869f8pmnm`, `869f8pmpa`, `869f6r5ng`, `869f2gh37`,
  `869f6r81n`, `869f74u70` y `869f1k17q` (PRs #90-#97, 8 tareas). Quedan 2.1d (Windows, 1 h),
  2.1e (documentación de PostgreSQL, 2 h) y 2.7 (plataforma, 4 h).
- Tiempo de ciclo (commits `empieza`/`cierra`, reloj de pared con revisión y merge): las cinco
  tareas de esta tanda, estimadas en 25 h, tardaron unas 2 h 50 min (`869f6r5ng` 1 h 35 min; el
  resto, entre 14 y 31 min). Sesgo ≈ 9 veces, igual que en la Fase 1 y otra vez en trabajo de
  backend con red de tests. **Sigue sin aplicarse al plan**: el frontend (80 % de lo pendiente)
  aún no tiene historial.
- Pendiente: ≈ 289 h (315 h del 28-sep + 20 h de la migración a PostgreSQL − 46 h hechas en la
  Fase 2). Factor de realismo sin cambios (0,8 → 20 h/semana).
- MVP piloto en producción, con dos semanas de Navidad: **optimista, principios de enero de 2027;
  probable, hacia el 22 de enero de 2027; pesimista, finales de febrero de 2027** (antes: primera
  quincena de enero, finales de enero y principios de marzo).
- Avance (modelo de `gestion.md` §7): MVP ≈ 39 %, proyecto ≈ 20 %, sin cambios. Los cimientos, los
  tests y el cambio de motor no mueven filas de producto; bajan el riesgo de la Fase 3 (endpoints
  de Citas y pantallas).

**2026-09-28 (cierre de la Fase 1, `/cerrar-bloque`):**
- Fases 0 y 1 terminadas el 28-sep (plan: .NET 10 hacia mediados de octubre; objetivo 9-oct).
  Throughput: 12 tareas del 25 al 28-sep, 10 de ellas el 28.
- Tiempo de ciclo medido con los commits `empieza`/`cierra`: las 7 tareas de código de la Fase 1,
  estimadas en 29 h, se cerraron en unas 3 h de reloj (14:55-18:13, con revisiones y merges
  incluidos). Sesgo de estimación observado: unas 9 veces a la baja en trabajo mecánico de
  infraestructura y dependencias con buena red de tests. **No se aplica al resto del plan** hasta
  medir tareas de producto (endpoints de citas y primeras pantallas): el frontend no tiene historial.
- Pendiente: ≈ 315 h (fases 2-6 sin opcionales; los pasos 1.8 y 1.9 van en el prompt entregado).
  Factor de realismo sin cambios (0,8 → 20 h/semana).
- MVP piloto en producción, con dos semanas de Navidad: **optimista, primera quincena de enero de
  2027; probable, finales de enero de 2027; pesimista, principios de marzo de 2027** (antes:
  mediados de febrero como probable). Se recalcula al cerrar la Fase 2 y, sobre todo, con las
  primeras pantallas de la Fase 3.
- Avance (modelo de `gestion.md` §7, sin infraestructura ni dependencias): MVP ≈ 39 %, proyecto
  ≈ 20 %, sin cambios; la Fase 1 no mueve funcionalidad, reduce riesgo.

**2026-09-28 (re-planificación, `869f6r4ec`):**
- Línea base medida con los PRs fusionados en `develop` (17-ago a 27-sep): 57 PRs, 41 de ellos
  features; unas 7 features por semana de media, muy irregular (0 a 22 por semana) y casi todo
  backend con patrones asentados. El frontend, el 80 % de lo pendiente, no tiene historial.
- Tiempo de ciclo: sin datos (ramas de un solo commit; ClickUp sin la ClickApp «Total time in
  Status»). Se mide desde ahora con los commits `empieza`/`cierra` de cada tarea (`gestion.md` §6).
- Pendiente: ≈ 352 h (fases 0-6 sin opcionales). Orden sin cambios.
- Factor de realismo: 0,8 (≈ 20 h/semana efectivas), sin datos para ajustarlo aún; se recalcula al
  cerrar la Fase 1.
- MVP piloto en producción, con dos semanas de Navidad: **optimista (25 h/semana), mediados de enero
  de 2027; probable (20 h/semana), mediados de febrero de 2027; pesimista (15 h/semana), finales de
  marzo de 2027.** Contraste por tareas (≈ 70 a 7/semana): diciembre; se descarta por optimista.
- Oleada con fecha en ClickUp (viernes de cada semana, a 20 h/semana desde el 28-sep):

| Semana | Pasos |
|---|---|
| 28-sep → 2-oct | 0.4, 0.5, 1.1, 1.2, 1.3 |
| 5-oct → 9-oct | 1.4 (objetivo), 1.5, 1.6, 1.7, 1.8, 1.9 |
| 12-oct → 16-oct | 2.1, 2.2, 2.3 |
| 19-oct → 23-oct | 2.4, 2.5, 2.6, 2.7 |
| 26-oct → 30-oct | 3.1, 3.2, 3.3, 3.4, 3.5 |
| 2-nov → 6-nov | 3.6, 3.7, 3.8 |

**2026-09-25 (preliminar, antes de medir):**
- Fases 0-6 sin opcionales: ≈ 358 h.
- Capacidad: 25 h/semana con factor de realismo 0,8 → ≈ 20 h/semana efectivas. Inicio: semana del
  28-sep-2026.
- .NET 10 en `develop`: hacia mediados de octubre, con unas tres semanas de holgura sobre el tope.
- MVP piloto en producción: **optimista, finales de enero de 2027; probable, mediados o finales de
  febrero de 2027** (incluye dos semanas de Navidad).
- La re-planificación (`869f6r4ec`) sustituye estas cifras por las medidas (throughput y tiempo de
  ciclo) y pone fechas solo a las próximas 4-6 semanas.
