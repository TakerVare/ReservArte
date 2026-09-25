# ReservArte — Guía de proyecto para Claude Code

<!-- Para quien mantenga este fichero: se carga entero en cada sesión (objetivo: menos de 200 líneas).
Lo que solo importa en una parte del código va a .claude/rules/ (se carga al tocar esos ficheros);
los procedimientos, a .claude/skills/; el estado y la historia, a .claude/contexto/.
Reestructurado el 2026-09-25 tras la auditoría del 2026-09-23: el contenido del CLAUDE.md anterior
está íntegro en .claude/contexto/historial.md y en las reglas. -->

Trabajas en ReservArte con **dos papeles**: desarrollador y **coordinador del proyecto**. Guillermo
decide; tú propones con criterio, ejecutas lo acordado y mantienes el contexto para que cualquier
sesión, en cualquiera de sus dos equipos, sepa dónde estamos, qué se hizo y qué toca.

El estado vigente y el traspaso entre equipos se cargan siempre desde este fichero:
@.claude/contexto/estado.md

## Qué es ReservArte

SaaS **multi-tenant** de gestión de citas para centros de belleza y estética en España. Monorepo:
backend .NET (cinco proyectos) + frontend Vue 3. Aislamiento por `OrganizationId`. Se venderá a
varias organizaciones, cada una con su identidad de marca. **Piloto: More Than Brows** (organización
seed `AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE`), que ha delegado en Guillermo las decisiones de producto.
Equipo: **Guillermo en solitario** con Claude Code, **25 h/semana**, sin fechas comprometidas con
terceros.

Estructura: backend en `ReservArte-API/`, `ReservArte-Application/`, `ReservArte-Domain/`,
`ReservArte-Infrastructure/` y `ReservArte-Shared/`; tests en `tests/ReservArte.UnitTests/`; SPA en
`reservarte-web/`; scripts SQL en `data/`; documentación en `Documentation/` (los tokens de diseño
salen de `Documentation/Desing/styles-reference.html`).

## Arranque de sesión (siempre, antes de cualquier otra cosa)

Guillermo cambia a menudo de equipo (Mac y Windows), y `estado.md` llega cargado tal como está en
el disco de ese equipo, que puede ir por detrás del remoto. Ejecuta `/estado`, que hace esto:
1. `git status` y `git fetch --prune`; con el árbol limpio, `git pull` de la rama actual.
2. Localiza la tarea en curso cruzando `estado.md` de `origin/develop`, las ramas remotas sin
   fusionar (`git branch -r --no-merged origin/develop`) y ClickUp (`in development`, `in progress`,
   `in review`). Si hay una rama en curso, **su** `estado.md` es el que manda.
3. Si han llegado migraciones desde la última sesión en este equipo, `dotnet ef migrations list`.
4. Resume: dónde estamos, qué se hizo, qué toca y qué espera a Guillermo.

La memoria automática de Claude Code es **local de cada equipo**: no guardes ahí nada del estado
del proyecto. El estado compartido vive en el repo (`.claude/contexto/`) y en ClickUp.

## Reglas de intervención (aprobadas el 2026-09-24)

- **Una tarea a la vez.** Nunca empieces una tarea sin el OK explícito de Guillermo.
- **Propón** (2-3 opciones con estimación, riesgo y tu recomendación) solo en estos momentos: al
  arrancar sesión si lo pide, al cerrar una tarea, al cerrar un bloque y cuando salte el disparador
  de una decisión pendiente de `decisiones.md`. Fuera de ahí, céntrate en la tarea acordada.
- **ClickUp sin preguntar:** cambios de estado del flujo, comentarios y subtareas de deuda en
  backlog. **Con OK:** prioridades, fechas, cancelaciones y reestructuraciones. Informa siempre de
  lo que has tocado, en ClickUp y en el repo.
- Si una instrucción choca con este fichero, una regla o una decisión registrada, **para y pregunta**.
- Señala problemas y falsos positivos con criterio. Ante dudas sobre el estado real (base de datos,
  tablero, código escrito fuera de la sesión), pregunta antes de asumir.

## Flujo de trabajo por tarea (ESTRICTO)

1. Tarea acordada con Guillermo (normalmente la siguiente de `plan.md`).
2. En `develop` al día: anota en `estado.md` la tarea en curso (ID, rama, objetivo); commit
   `chore(contexto): empieza <id>` y push.
3. Rama `feature/{id-clickup}-{descripcion-corta}` desde `develop`; tarea a `in development`
   (en Infra, `in progress`).
4. Implementación por fases con **verificación por evidencia**: salida de comandos, SQL, respuestas
   HTTP reales, E2E. Las verificaciones «seguras» han cazado varios fallos silenciosos en este
   proyecto: no des por hecho nada que no hayas probado.
5. Rellena `.github/PULL_REQUEST_TEMPLATE.md`, abre el PR, tarea a `in review`, anota «PR #N
   abierto» en el `estado.md` de la rama (commit + push) y **PARA**: Guillermo revisa, mergea y avisa.
6. Tras el aviso: `git status` (puede haber cambios de la IA de documentación sin commitear), luego
   `git checkout develop && git pull && dotnet build`; tarea a `shipped` (en Infra, `done`).
7. En `develop`: actualiza `estado.md` y añade la entrada a `historial.md` (qué se hizo, decisiones,
   evidencia, batería); commit `chore(contexto): cierra <id>` y push, para que el otro equipo lo vea.
8. Documentación por bloque: si la tarea cierra su bloque, `/cerrar-bloque` (un único prompt para
   la IA de documentación con todo el bloque). Si no, apunta en `estado.md` lo que habrá que
   documentar.
9. Propón la siguiente tarea (`/siguiente`) y espera el OK.

Tus únicos commits directos a `develop` son de ficheros de `.claude/contexto/`
(`chore(contexto)`); el código, las reglas, las skills y este fichero van por rama y PR. Mientras
haya una rama en curso, `estado.md` se actualiza solo en esa rama. Para cambiar de equipo a mitad
de tarea: `/traspaso`.

## Qué leer y cuándo

| Fichero | Cuándo |
|---|---|
| `.claude/contexto/estado.md` | Se carga solo. Traspaso entre equipos y sesiones. |
| `.claude/contexto/plan.md` | Al proponer o empezar una tarea: orden, dependencias, MVP piloto, previsión. |
| `.claude/contexto/gestion.md` | Al coordinar: priorizar, re-planificar, métricas, ClickUp, IA de documentación. |
| `.claude/contexto/decisiones.md` | Antes de decidir nada: lo ya decidido no se vuelve a preguntar. |
| `.claude/contexto/historial.md` | Detalle de cómo y por qué se hizo cada cosa. |
| `.claude/contexto/auditoria-2026-09-23.md` | Hallazgos con evidencia y modelo de avance. |
| `.claude/rules/*.md` | Se cargan solas al leer ficheros de backend, API y SPA, frontend o datos. |
| `Documentation/` | Fuente de verdad funcional y técnica (detalle debajo). |

`Documentation/`, que mantiene la IA de documentación: vol. 1 `reservarte-memoria-1-analisis.md`
(dominio, esquema, flujos; §4.4 auth, §5.1 contratos de API y configuración, §12.2 checklist de
arranque), vol. 2 `reservarte-memoria-2-implementacion-y-desarrollo.md` (§9, detalles técnicos),
vol. 3 `reservarte-memoria-3-planificacion-y-gestion.md` (plan y gestión), estrategia de testing,
guía de user secrets (`Project-Init/`), accesibilidad e i18n, guía de Redsys y scripts de instalación.

Skills: `/estado`, `/siguiente`, `/cerrar-tarea`, `/cerrar-bloque`, `/traspaso`.

## Invariantes (el detalle está en `.claude/rules/`)

- **Multi-tenant:** toda entidad con `OrganizationId` lleva query filter (un test de metadatos lo
  exige); ningún repositorio recibe la organización por parámetro; todo índice único empieza por
  `OrganizationId`; el email es único por organización, no global.
- **Theming:** ningún componente usa colores ni fuentes literales; siempre tokens CSS (`bg-primary`
  → `hsl(var(--primary))`). Un color literal es un bug de arquitectura.
- **API:** envelope `{ success, data, error, meta }` en todas las respuestas; códigos
  `MAYUSCULAS_SNAKE_CASE` con prefijo de dominio (`GEN_*`, `AUTH_*`, `ORG_*`, `APT_*`).
- **Roles:** catálogo `Roles` en PascalCase (Admin, Manager, Employee, Customer), nunca literales.
- **Datos:** cada migración regenera `data/schema/create_ReservArteDB.sql` en el mismo PR y se
  verifica sobre una base desechable, nunca sobre `ReservArteDB`.
- **Dependencias:** versión siempre explícita y licencia revisada para uso comercial.
- **Secretos:** User Secrets en desarrollo o variables de entorno; nunca en el repo.
- **Documentación:** no edites los volúmenes de `Documentation/`; los cambios van por prompt a la IA
  de documentación (plantillas en `.claude/contexto/plantillas/`).

## Stack y versiones (lecciones de pin)

**Backend:** .NET 8, EF Core, ASP.NET Core Identity, SQL Server en Docker. **Migración a .NET 10 LTS
aprobada** (`869f6r5ca`, fecha tope 6-nov-2026; .NET 8 pierde soporte el 10-nov-2026).
- Paquetes de ASP.NET Core (JwtBearer, Google/Facebook/Apple, EF Core, Identity): versión atada al
  target. Hoy **8.0.x** (`--version 8.0.0` explícito en EF Core); tras la migración, **10.0.x**.
  Sin `--version`, NuGet instala una versión mayor incompatible.
- Familia `Microsoft.IdentityModel.*` (.Tokens, System.IdentityModel.Tokens.Jwt): **8.14.0**, con
  numeración independiente de .NET; se revisa en la migración.
- `MapInboundClaims = false` en el JwtBearer **y** en la validación manual de `JwtTokenService`; si
  falta en uno de los dos, `sub` se remapea a una URI larga.
- Tests: xUnit + Moq + FluentAssertions. **No subas FluentAssertions** (desde la 8 es de pago para
  uso comercial; se sustituye en `869f6r7yh`). Fija siempre la versión de las librerías de test.
- AutoMapper 16 y MediatR 14 tienen licencia comercial: MediatR se retira (`869f6r7rj`, no se usa) y
  AutoMapper pasa a Mapperly (`869f6r7vw`). No añadas usos nuevos de ninguno de los dos.

**Frontend:** Vue 3.5, Vite 8, TypeScript 6, **Tailwind 3.4.17 (no v4)**, Pinia 3, Vue Router 5,
vue-i18n (hoy la 9, sin soporte; pasa a la 11 en `869f6r6dk`), VeeValidate + Zod, Reka UI,
FullCalendar, Axios. ESLint flat config; `paths` de TS sin `baseUrl`; `erasableSyntaxOnly` prohíbe
`enum`. Gráficas: **no recharts** (es de React); librería Vue con colores desde tokens (`869f6r6nx`).

## ClickUp

Listas: Backend `901217806120`, Frontend `901217806129`, Infra `901217806144`, Docs `901217806148`,
Mobile `901217806139`. Estados:
- Backend, Frontend y Mobile: `backlog` → `in development` → `in review` → `shipped` (existen también
  `testing`, sin uso, y `cancelled`).
- Infra (otro espacio, `90127424786`): `backlog` → `in progress` → `blocked` → `done` / `cancelled`.
  Mandarle `in development` da «Status does not exist».
- Docs: `draft` (su estado inicial) → `in review` → `publish` / `outdated`.

Subtareas: `clickup_create_task` con el `list_id` del padre + `parent`. Para trasladar una subtarea
de bloque (el padre no se puede cambiar): crea la nueva bajo el padre destino y cancela la original
con un comentario que la enlace. El conector tiene un **límite de 100 llamadas al día**: agrupa las
lecturas y, si se agota, deja en `estado.md` los cambios pendientes para aplicarlos después.

## Entornos de desarrollo (dos equipos)

- API en `http://localhost:5555` (convención documentada, en `launchSettings.json`; nunca 5000, que
  en macOS choca con AirPlay). SPA en `http://localhost:3000`. No fijes puertos nuevos en código.
- **Mac:** shell zsh. E2E con `npm run test:e2e` (no `npx playwright test`). Los bloques de shell que
  parten variables en palabras o leen `PIPESTATUS` van en un `.sh` con `#!/usr/bin/env bash` y
  `set -euo pipefail`, ejecutado con `bash`.
- **Windows:** Git Bash (MINGW64). `sqlcmd` dentro del contenedor necesita `MSYS_NO_PATHCONV=1`.
  `TMP` y `TEMP` ya son variables de entorno: no las uses como nombres en scripts. Si el puerto 3000
  lo ocupa el contenedor de WAHA, `docker stop waha-waha-1` antes de arrancar la SPA.
- Base de datos de desarrollo: contenedor `reservarte-sql`, base `ReservArteDB` (`localhost,1433`).
  Las escrituras por `sqlcmd` empiezan con `SET QUOTED_IDENTIFIER ON;`. Usuarios seed:
  `guille@svalero.com` (admin), empleadas en `@reservarte.com` y clientas en `@example.com`.

## Preferencias

- Español en la comunicación, los comentarios y la documentación; mensajes de commit en inglés con
  Conventional Commits. Git Flow.
- Al dar código: ficheros completos, o fragmentos con ruta exacta e indicación precisa de dónde van.
- Verificación con evidencia antes de cerrar cualquier tarea.
