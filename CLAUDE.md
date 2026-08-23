# ReservArte — Guía de proyecto para Claude Code

> Este archivo es la memoria permanente del proyecto. Léelo al inicio de cada sesión.
> La documentación exhaustiva vive en `/Documentation`; aquí está el destilado operativo.

## Qué es ReservArte

SaaS **multi-tenant** de gestión de citas para centros de belleza/estética en España.
Monorepo. Backend .NET 8 (Clean Architecture) + frontend Vue 3. Aislamiento por
`OrganizationId`. El software se redistribuirá: cada organización es un tenant con su
propia identidad de marca.

## Estructura del repositorio

- `ReservArte-API/` — capa web/API (controllers, middleware, extensiones, Program.cs)
- `ReservArte-Application/` — DTOs, interfaces, validadores (FluentValidation)
- `ReservArte-Domain/` — entidades, interfaces de dominio
- `ReservArte-Infrastructure/` — EF Core, servicios, persistencia, seeders
- `ReservArte-Shared/` — envelope de API, códigos de error
- `reservarte-web/` — frontend Vue 3 + Vite + TypeScript
- `tests/ReservArte.UnitTests/` — tests unitarios (xUnit + Moq + FluentAssertions)
- `Documentation/` — documentación completa del proyecto (ver más abajo)
- `Documentation/Desing/styles-reference.html` — **hoja de estilos de referencia** (fuente de tokens de diseño)

## Documentación (fuente de verdad — consúltala)

Toda en `/Documentation`. Tres volúmenes principales:
- **Volumen 1 — Análisis** (`reservarte-memoria-1-analisis.md`): dominio, esquema BD, flujos, §4.4 auth, §5.1 contratos de API/config, §12.2 checklist de arranque.
- **Volumen 2 — Implementación** (`reservarte-memoria-2-implementacion-y-desarrollo.md`): §9 detalles técnicos (auth, rate limiting, etc.).
- **Volumen 3 — Planificación**: roadmap y seguimiento de sprints.
- Estrategia de testing, guía de user-secrets y scripts de instalación, también en `/Documentation`.

Los volúmenes los mantiene una **IA de documentación** separada. No los edites directamente:
los cambios de documentación se hacen mediante prompts a esa IA (ver flujo de trabajo).

## Stack y versiones (¡lecciones de pin importantes!)

**Backend:** .NET 8, EF Core 8.0.0, ASP.NET Core Identity, SQL Server en Docker.
- Paquetes de **ASP.NET Core** (JwtBearer, Google/Facebook/Apple auth, EF Core, Identity)
  → versión **8.0.x**, atada al target .NET 8. Pedirlos sin `--version` instala 9.x incompatible.
- Familia **`Microsoft.IdentityModel.*`** (.Tokens, System.IdentityModel.Tokens.Jwt)
  → versión **8.14.0**, numeración independiente de .NET.
- Moq / FluentAssertions → sin fijar versión (no atados a .NET 8).
- Al instalar EF Core: `--version 8.0.0` explícito siempre.

**Frontend:** Vue 3 + Vite + TypeScript, **Tailwind 3.4.17** (NO v4), Pinia, Vue Router,
vue-i18n v9 (locale `es`), VeeValidate + Zod, shadcn-vue / **Reka UI**, FullCalendar,
recharts. ESLint flat config. TS con `paths` (sin baseUrl). `erasableSyntaxOnly` prohíbe enums.

## Arquitectura clave

**Envelope de respuesta** (todas las respuestas API): `{ success, data, error, meta }`,
donde `meta` lleva `requestId`, `timestamp`, `version`, `pagination`. Definido en
`ReservArte-Shared/Api` (`ApiResponse`, `ApiError`, `ApiErrorDetail`, `ApiMeta`, `ErrorCodes`).

**Códigos de error** (`ErrorCodes.cs`): prefijo por dominio, MAYUSCULAS_SNAKE_CASE
(`AUTH_INVALID_CREDENTIALS`, `AUTH_REFRESH_INVALID`, `AUTH_MFA_INVALID`, `GEN_VALIDATION_FAILED`,
`GEN_CONFLICT`, `GEN_RATE_LIMITED`, `ORG_TENANT_NOT_RESOLVED`, etc.).

**Multi-tenant:** `TenantMiddleware` resuelve la organización por cabecera `X-Organization-Id`
(dev, con fallback `DefaultOrganizationId`) o subdominio (prod). Valida coherencia con el claim
`organization_id` del JWT si la petición está autenticada (403 si discrepan).

**Auth (completa y verificada):** JWT (claims `sub`/`email`/`organization_id`/`role` [corto,
no URI]/`jti`) con `MapInboundClaims = false` en emisión y validación. Refresh token opaco
con rotación. OAuth Google/Apple/Meta (Meta con esquema "Instagram"), tokens a la SPA por
**fragmento de URL**. 2FA TOTP con ticket intermedio (`mfa_pending`, 5 min, sin `role`) →
`POST /auth/mfa/verify` → JWT final. Códigos de recuperación de un solo uso. Rate limiting
nativo .NET 8 (10/h login, 20/h verify) → 429. CAPTCHA verificable (Turnstile, desactivado en dev).

## Contrato de API para el frontend

- Base URL dev: `http://localhost:5218` (NUNCA 5000 — colisiona con AirPlay en macOS). SPA en `http://localhost:3000`, proxy Vite `/api` → 5218.
- **Login** (`POST /api/v1/auth/login`): responde con tokens normales, O con
  `{ mfaRequired: true, mfaTicket }` (sin tokens) si el usuario tiene 2FA. El frontend debe
  contemplar ambos casos: si `mfaRequired`, redirigir a `/login/two-factor`.
- **OAuth**: `GET /api/v1/auth/external/{provider}/challenge?returnUrl=...` (302 al IdP) →
  aterriza en `{SPA}/auth/callback#access_token=...&refresh_token=...` (leer del **fragmento**).
- **MFA verify** (`POST /api/v1/auth/mfa/verify`): `{ mfaTicket, code }` (code = TOTP o recuperación) → tokens.
- Otros: `register`, `refresh-token`, `forgot-password`, `GET /api/v1/account/me` (`[Authorize]`),
  `POST /api/v1/account/mfa/enable|confirm|disable`.
- **Ya existe en frontend:** `authStore` (hidrata `localStorage['authToken']`), `uiStore`,
  router con 7 rutas y guards `requiresAuth`/`requiresMfa`, `client.ts` (Axios + Bearer + 401→login).
  Las páginas son **stubs** pendientes de implementar (este bloque de trabajo).

## Theming multi-tenant (CRÍTICO para todo el frontend)

Cada organización personaliza su **identidad de marca ligera: color + fuente + logo**.
Mecanismo: **tokens CSS (variables HSL) en `globals.css`**, inyectados en runtime según el tenant.

**REGLA INQUEBRANTABLE:** los componentes NUNCA usan colores/fuentes literales
(`bg-blue-600`, `font-['Inter']`). SIEMPRE vía variable CSS mapeada en Tailwind
(`bg-primary`, etc., resueltas a `hsl(var(--primary))`). Esto permite que al cargar un tenant
se sobreescriban las variables (`--primary`, `--font-sans`, logo) y toda la UI se repinte.
Un componente con un color hardcodeado es un bug de arquitectura.

La entidad de configuración de tema por organización y su pantalla de edición son trabajo
futuro (módulo Configuración), pero **todo componente se construye desde hoy con esta disciplina**.
Tokens fieles a `Documentation/Desing/styles-reference.html` y al Dev Mode de Figma.

## Flujo de trabajo por tarea (ESTRICTO)

1. Rama `feature/{clickup-id}-{descripcion-corta}` desde `develop`.
2. Mover la tarea de ClickUp a "in development".
3. Implementación por fases, con **verificación por evidencia** antes de cerrar (no dar por
   hecho lo que no se ha probado; en este proyecto las verificaciones "seguras" han cazado
   varios fallos silenciosos).
4. Marcar la tarea "shipped" solo tras verificar.
5. Rellenar plantilla de PR (`.github/PULL_REQUEST_TEMPLATE.md`) y redactar un **prompt para la
   IA de documentación** (con auditoría de coherencia previa obligatoria: verificar que prompts
   anteriores están aplicados antes de escribir; reportar contradicciones sin corregir).
6. Merge vía PR en GitHub → ritual: `git checkout develop && git pull && dotnet build`.

**Una tarea a la vez, en orden. No adelantar tareas ni proponer siguientes pasos fuera de turno.**

## ClickUp

Listas: Backend `901217806120`, Frontend `901217806129`, Infra `901217806144`, Docs `901217806148`.
Estados: `backlog` → `in development` → `shipped`. Subtareas: `clickup_create_task` con `list_id`
(debe coincidir con la lista del padre) + `parent`. Bloque de UI actual bajo el padre
`869d7edpt` (Layout + páginas Auth).

## Base de datos (dev)

Docker: contenedor `reservarte-sql`, base `ReservArteDB`, `localhost,1433`.
sqlcmd desde Git Bash:
`MSYS_NO_PATHCONV=1 docker exec -it reservarte-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<pwd-dev>' -C -d ReservArteDB -Q "..."`
**Escrituras (UPDATE/DELETE/INSERT) vía sqlcmd requieren `SET QUOTED_IDENTIFIER ON;` al inicio** (SELECT no).
Organización seed (determinista): `AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE` (More Than Brows).
Usuarios seed: `guille@svalero.com` (admin), y empleadas en `@reservarte.com`.

## Preferencias de trabajo

- **Idioma: español** en todo (comunicación, comentarios, mensajes de commit en inglés convencional).
- Al dar código: **archivos completos** o fragmentos con ruta exacta e indicación precisa de dónde va.
- Verificación con evidencia antes de cerrar cualquier tarea.
- Conventional Commits + Git Flow.
- No hardcodear credenciales; secretos en User Secrets (dev) — ver guía en `/Documentation`.

## Estado actual (al iniciar el bloque de UI)

- ✅ Setup backend y frontend completos.
- ✅ Módulo de Auth backend completo (9/9): Identity, JWT, endpoints, OAuth, 2FA, rate limiting, tests.
- ⏳ **Ahora:** bloque de UI (`869d7edpt`) — layouts, componentes base y páginas de auth.
  Decisión tomada: construir **componentes base primero** (fieles a Figma/hoja de estilos, con
  theming por variables), luego layouts, luego páginas. Login UI aún NO existe (solo stubs).
- 📋 Backlog no bloqueante: `869en8a17` (refinamientos rate limiting + `AUTH_MFA_INVALID`).