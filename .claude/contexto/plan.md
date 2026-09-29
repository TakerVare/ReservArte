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
| 2.1d | `869f8pmpn` PostgreSQL en el Windows (Guillermo) | 1 | espera a 2.1c |
| 2.1e | `869f8pmq4` Documentación del bloque (ADR-031) | 2 | espera a 2.1c; `/cerrar-bloque` |
| 2.2 | `869f6r5ng` Infraestructura de tests de integración (Testcontainers.PostgreSql + WebApplicationFactory) | 8 | espera a 2.1c |
| 2.3 | `869f2gh37` Contratos HTTP de Empleados y Clientes | 6 | espera a 2.2 |
| 2.4 | `869f6r81n` Mapa central de códigos de error y respuesta común | 6 | espera a 2.2; absorbe `869f17y6k` |
| 2.5 | `869f74u70` Manejador global de excepciones | 3 | espera a 2.4 |
| 2.6 | `869f1k17q` 400 de model binding con envelope | 2 | espera a 2.4 |
| 2.7 | `869f6r4ww` Decisión de plataforma de producción (ADR) | 4 | sesión con Guillermo; decide DP-01 |

### Fase 3 — Primer vertical: la agenda (≈ 100 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 3.1 | `869d7f519` Endpoints de citas (decide `created_by`) | 12 | espera a `869f6r5r2` |
| 3.2 | `869d7f53r` Tests de cancelación y aislamiento (sin penalización) | 6 | |
| 3.3 | `869f2gn91` Historial de citas del cliente (`/history`) | 3 | |
| 3.4 | `869f6r69b` URL relativa y proxy de Vite | 3 | |
| 3.5 | `869eqxm8z` Vitest | 5 | |
| 3.6 | `869f6r6dk` vue-i18n 11 | 3 | |
| 3.7 | `869ep9p36` Reconciliación de layouts (solo BottomNav) | 5 | antes de cualquier pantalla de gestión |
| 3.8 | `869d7fbuf` Componentes base (Reka UI) | 10 | |
| 3.9 | `869d7fbxn` DataTable | 6 | |
| 3.10 | `869d7fc8y` CalendarPage | 12 | |
| 3.11 | `869d7fcbu` AppointmentCard | 4 | |
| 3.12 | `869d7fcd0` + `869d7fcen` Wizard de citas | 16 | |
| 3.13 | `869d7fch0` RescheduleModal | 5 | |
| 3.14 | `869d7fcfy` CancelModal (sin penalización en el piloto) | 4 | |
| 3.15 | `869d7fca1` Colores por estado y drag & drop | 6 | |

Cierre de bloque: demo de la agenda a More Than Brows y prompt de documentación.

### Fase 4 — Segundo vertical: gestión (≈ 42 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 4.1 | `869d7fbyt` + `869d7fc0h` Empleados: lista, detalle, formulario y horario | 16 | |
| 4.2 | `869d7fc34` + `869d7fc51` Clientes (sin tarjeta guardada en el piloto) | 16 | |
| 4.3 | `869d7fc6b` Servicios | 10 | |
| opc. | `869epnt88` LoginForm a VeeValidate + Zod | 2 | |

### Fase 5 — Tercer vertical: configuración mínima y recordatorios (≈ 46 h)

| Paso | Tarea | h | Notas |
|---|---|---|---|
| 5.1 | `869f6r5vy` Filtros de tenant cerrados y ámbito de sistema | 6 | bloquea `869d7edh9` |
| 5.2 | `869f74u7y` OrganizationSettings mínimo (épica `869f6r5y8`) | 8 | |
| 5.3 | `869f6r71x` Configuración mínima en la SPA | 6 | espera a 5.2 |
| 5.4 | `869d7f5wx` Recordatorios: entidades y migración | 6 | |
| 5.5 | `869d7f5zq` Programación con Hangfire | 6 | |
| 5.6 | `869d7f61y` Envío por canal (email en el piloto) | 4 | |
| 5.7 | `869d7f65a` Servicio de email SES y plantilla | 6 | producción necesita `869d7exmk` |
| 5.8 | `869d7f6aa` Endpoints de configuración y dashboard de Hangfire protegido | 4 | |
| opc. | `869f7axeg` No-shows | 6 | espera a 5.2 |

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
| 6.10 | Infra según el ADR de 2.7: `869d7ewec`, `869d7exff`, `869d7exag`, `869d7excz`, `869d7exmk`, `869d7exqg`, `869d7ewnz`, `869d7exj4` | ~30 | se ajustan al ADR |
| opc. | `869f6r6nx` + `869f7axcv` + `869d7fc7e` Dashboard | 17 | decide DP-02 |
| 6.11 | `869f6r4zt` Hito: MVP piloto en producción | — | |

### Fase 7 — Tras el piloto (orden provisional)

1. Redsys: `869d7eden` y sus subtareas, `869f2gnbm` (tarjetas guardadas), `869f7axdq`
   (penalización al cancelar) y el frontend de `869d7edya` (`869d7fcjv`, `869d7fcmz`, `869d7fcpa`,
   `869d7fcr5`, `869d7fcu1`, `869d7fcv7`).
2. Lista de espera (`869f7axfq` + `869d7fchj`), promoción de categoría (`869f7axh9`) y AuditLog
   (`869f2gtz8`).
3. Configuración completa (`869d7fcww`), identidad de marca (`869f74u8c`) y theming en runtime
   (`869f6r6xv`); deudas de accesibilidad `869f0v6vm`, `869f0w7r2` y `869f0w75h`.
4. App móvil como PWA (`869f6r74n`).
5. Fase 2 funcional: reserva pública y «Mi cuenta» (`869d7ee36`), fotos (`869d7ee5t`); fidelización
   y cupones aún sin tareas.
6. Fase 3 SaaS: onboarding y subdominios (`869d7ee9p`), suscripciones (`869d7eebv`); versiones
   legales por organización.
7. Sin fase asignada: `869d7ewzg` (husky + commitlint) y `869f2g60e` (árbol de estructura en la
   documentación).

## Previsión

Registro de cada recálculo (lo añade `/cerrar-bloque`; el más reciente arriba).

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
