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
Incluye los 401/403 que emite ASP.NET Core sin pasar por controladores: `JwtBearerEvents.OnChallenge`
→ 401 `GEN_UNAUTHORIZED` y `OnForbidden` → 403 `GEN_FORBIDDEN` (RA-869f1anz3). `GEN_FORBIDDEN`
significa «sin permiso», **no** cierra la sesión en la SPA (nunca en `SESSION_ENDING_ERROR_CODES`).

**Códigos de error** (`ErrorCodes.cs`): prefijo por dominio, MAYUSCULAS_SNAKE_CASE
(`AUTH_INVALID_CREDENTIALS`, `AUTH_REFRESH_INVALID`, `AUTH_MFA_INVALID`, `GEN_VALIDATION_FAILED`,
`GEN_CONFLICT`, `GEN_RATE_LIMITED`, `ORG_TENANT_NOT_RESOLVED`, `ORG_TENANT_MISMATCH`, etc.).
Tenant: **400 `ORG_TENANT_NOT_RESOLVED`** (no se pudo resolver la organización → corregir contexto)
vs **403 `ORG_TENANT_MISMATCH`** (resuelta, pero no coincide con el claim del JWT → cerrar sesión;
la SPA lo cablea en el interceptor de `client.ts`).

**Multi-tenant:** `TenantMiddleware` resuelve la organización por cabecera `X-Organization-Id`
(dev, con fallback `DefaultOrganizationId`) o subdominio (prod). Valida coherencia con el claim
`organization_id` del JWT si la petición está autenticada (403 si discrepan).
**Query filters globales por `OrganizationId`** en `AppDbContext` para TODA entidad multi-tenant
mapeada (`Employee`, `User`, `UserLogin`, `RefreshToken` vía su usuario, `EmployeeAvailability`,
`EmployeeException`, `Customer`, `CustomerNote`, `CustomerAllergy`, `CustomerConsent`) — RA-869f17vet. Sin tenant resuelto (migraciones, seeders) no restringen.
Un test de metadatos falla si una entidad nueva con `OrganizationId` se mapea sin filtro: al
añadir módulos (Clientes, Servicios, Citas…), el filtro es obligatorio. Saltarse el filtro
(`IgnoreQueryFilters()`) solo con justificación; hoy no hay ningún uso en código de producción.

**Email único por organización, no global** (RA-869f1xc0u): la misma persona puede tener cuenta
en varios centros. Índices únicos `(OrganizationId, NormalizedEmail)` y `(OrganizationId,
NormalizedUserName)` en `AspNetUsers`, `(OrganizationId, Email)` en `Employees`, y clave
`(OrganizationId, LoginProvider, ProviderKey)` en `AspNetUserLogins` (entidad `UserLogin`; la
organización la rellena `OrganizationUserStore` al vincular). El `UserValidator` de Identity valida
por organización porque busca a través del filtro; no hay validador propio. Todo índice único de una
entidad multi-tenant nace con `OrganizationId` delante (Clientes incluido). **Sin tenant resuelto**
(seeders, futuros jobs) `FindByEmailAsync` falla si el email está en dos centros: ese camino debe
fijar antes la organización en `ICurrentOrganizationService`.

**Auth (completa y verificada):** JWT (claims `sub`/`email`/`organization_id`/`role` [corto,
no URI]/`jti`) con `MapInboundClaims = false` en emisión y validación. Refresh token opaco
con rotación. OAuth Google/Apple/Meta (Meta con esquema "Instagram"), tokens a la SPA por
**fragmento de URL**. 2FA TOTP con ticket intermedio (`mfa_pending`, 5 min, sin `role`) →
`POST /auth/mfa/verify` → JWT final. Códigos de recuperación de un solo uso. Rate limiting
nativo .NET 8 (10/h login, 20/h verify) → 429. CAPTCHA verificable (Turnstile, desactivado en dev).
La baja de un empleado **bloquea su cuenta** (lockout de Identity como interruptor, no como contador;
login/refresh/MFA/OAuth lo comprueban — RA-869f180e5). Hueco conocido: **el login social se salta el
2FA** (el callback externo emite tokens definitivos sin ticket `mfa_pending`) — RA-869f151x1.

**Escrituras que abarcan ficha y cuenta de Identity** (RA-869f1811u): siempre dentro de
`IUnitOfWork.ExecuteInTransactionAsync` (`EfUnitOfWork`). La transacción se abre DENTRO de la
estrategia de ejecución (`EnableRetryOnFailure` rechaza transacciones abiertas a mano); confirma si
el `Result` es éxito y, si no, deshace **y vacía el change tracker**. Comprobar SIEMPRE el
`IdentityResult`: el `UserManager` comparte el `AppDbContext`, y un cambio que Identity rechaza queda
en memoria y lo persistiría el siguiente `SaveChanges`. La operación puede reejecutarse ante un
fallo transitorio: construir entidades dentro y dejar los efectos externos (correos) para después
del commit.

**Empleada y clienta con la misma cuenta** (RA-869d7f369): un `User` puede tener ficha `Employee` y
ficha `Customer` con el mismo Id; `User.Rol` es el rol de personal. `CustomerService` distingue **cuenta
de personal** (`Rol != Customer`, falla cerrado) de cuenta solo de cliente. El alta de cliente con el
email de una cuenta del centro sin ficha **añade la ficha** sin tocar la cuenta ni invitar; con ficha ya
existente → 409. Una cuenta nueva nace `Customer` sin contraseña y recibe la invitación `set-password`
tras el commit. Editar la ficha de una cuenta de personal no toca la cuenta, y cambiar su email es
**403** (se cambia desde Empleados: evita que se secuestre el acceso del personal desde Clientes). En
cuenta solo de cliente, nombre/email/teléfono/imagen se sincronizan (SetEmail solo si cambia). La baja
de la ficha de cliente **no** hace lockout. Toda ficha nace categoría **`new`** (también registro y
alta social); la promoción a `regular` llega con Citas (`869f2g02q`, bloque de Citas `869d7edau`).

## Contrato de API para el frontend

- Base URL dev: `http://localhost:5555` (puerto real de `launchSettings.json`; NUNCA 5000 — colisiona con AirPlay en macOS). SPA en `http://localhost:3000`, proxy Vite `/api` → 5555.
- **Login** (`POST /api/v1/auth/login`): responde con tokens normales, O con
  `{ mfaRequired: true, mfaTicket }` (sin tokens) si el usuario tiene 2FA. El frontend debe
  contemplar ambos casos: si `mfaRequired`, redirigir a `/login/two-factor`.
- **OAuth**: `GET /api/v1/auth/external/{provider}/challenge?returnUrl=...` (302 al IdP) →
  aterriza en `{SPA}/auth/callback#access_token=...&refresh_token=...` (leer del **fragmento**).
- **MFA verify** (`POST /api/v1/auth/mfa/verify`): `{ mfaTicket, code }` (code = TOTP o recuperación) → tokens.
- Otros: `register`, `refresh-token`, `forgot-password`, `GET /api/v1/account/me` (`[Authorize]`),
  `POST /api/v1/account/mfa/enable|confirm|disable`.
- **Registro** (`POST /api/v1/auth/register`): además de términos y privacidad exige
  `acceptedDataProcessing: true` (checkbox propio; 400 `GEN_VALIDATION_FAILED` si falta). Crea cuenta,
  ficha `Customer` y consentimiento `data_processing` fechado en una transacción (RA-869f1xc2n). El alta
  social nueva crea cuenta, vínculo y ficha, **sin** consentimientos (no pasa por el formulario); vincular
  un proveedor a una cuenta existente no toca fichas.
- **`POST /api/v1/auth/set-password`** (`{ email, token, newPassword }`): canjea el token de la
  **invitación de alta** (proveedor `Invitation`, 7 días) por la contraseña. Es distinto de
  `reset-password` (proveedor de recuperación, 1 día) y solo vale para cuentas sin contraseña; su
  401 es de negocio, así que está exceptuado en el interceptor de `client.ts` (igual que el de
  `reset-password`, RA-869f1m12x: sin la excepción, un enlace caducado mandaba a `/login` sin mostrar el motivo).
- **Empleados** (`[Authorize(Roles = Admin,Manager)]`): `GET|POST /api/v1/employees`,
  `GET|PUT|DELETE /api/v1/employees/{id}` (DELETE = baja lógica + bloqueo de cuenta),
  `POST /api/v1/employees/{id}/reactivate`, `POST /api/v1/employees/{id}/invitation` (reenvía la
  invitación; 409 si ya tiene contraseña o está de baja). Lista: `data.items` + `meta.pagination`. Reglas por dato
  en `EmployeeService` (403 `GEN_FORBIDDEN`): solo un Admin asigna el rol Admin o gestiona a otro
  Admin; nadie cambia su propio rol ni se da de baja a sí mismo.
- **Disponibilidad**: `GET|PUT /api/v1/employees/{id}/availability`,
  `POST /api/v1/employees/{id}/exceptions`, `DELETE /api/v1/employees/{id}/exceptions/{exceptionId}`.
  GET devuelve `{ weeklySchedule, exceptions, exceptionsFrom, exceptionsTo }`; `from`/`to` (UTC)
  acotan las ausencias, por defecto desde hoy y 90 días. PUT **reemplaza la semana entera** (lista
  vacía = sin horario); valida día 0-6, fin > inicio y ausencia de solapes. Las ausencias son baja
  lógica. La **lectura** la permite a cualquiera del módulo; las **escrituras** aplican la regla de
  que un Manager no toca a un Admin.
- **Clientes** (RA-869d7f3bt): la clase admite **Admin, Manager y Employee** (lectura); POST/PUT/DELETE y
  reactivate exigen además **Admin o Manager**; Customer → 403. `GET /api/v1/customers?search&category&
  isBlocked&isActive&page&pageSize` (`data.items` + `meta.pagination`; sin `isActive` = solo activos),
  `GET /{id}` (perfil con consentimientos, alergias y notas vigentes), `POST` (201 + Location;
  `grantedConsents` con `data_processing` obligatorio → si no, 400 `field=grantedConsents`; 409 si el email
  ya tiene ficha), `PUT /{id}` (403 si cambia el email de una cuenta de personal), `DELETE /{id}` (baja
  lógica idempotente, **sin** lockout) y `POST /{id}/reactivate`. **Notas** (RA-869d7f3fw), todo el personal:
  `POST /{id}/notes` (`{ note }` ≤2000; 201; la firma la ficha `Employee` **activa** de quien llama, sin ella
  403: un admin sin ficha no escribe notas) y `DELETE /{id}/notes/{noteId}` (baja lógica idempotente; solo
  su autora, Admin o Manager, si no 403). Las notas vigentes se leen en `GET /{id}`.
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
5. Rellenar plantilla de PR (`.github/PULL_REQUEST_TEMPLATE.md`), abrir el PR y **PARAR**: el
   usuario lo aprueba y mergea, y avisa.
6. Tras su aviso: `git checkout develop && git pull && dotnet build` (antes del checkout,
   comprobar `git status` por si hay cambios de la IA de documentación sin commitear).
7. Entregar entonces el **prompt para la IA de documentación**: con auditoría de coherencia
   previa obligatoria (verificar que prompts anteriores están aplicados; reportar
   contradicciones sin corregir) y pidiéndole expresamente que **señale advertencias** donde
   lo encuentre oportuno.
8. El usuario aplica la documentación; cuando queda sin advertencias, avisa y se empieza la
   siguiente tarea.

**Una tarea a la vez, en orden. No adelantar tareas ni proponer siguientes pasos fuera de turno.**

## ClickUp

Listas: Backend `901217806120`, Frontend `901217806129`, Infra `901217806144`, Docs `901217806148`.
Estados: `backlog` → `in development` → `shipped`. Subtareas: `clickup_create_task` con `list_id`
(debe coincidir con la lista del padre) + `parent`. Último bloque cerrado: **CRUD Empleados**
(`869d7ed2j`, backend, 10/10).

## Base de datos (dev)

Docker: contenedor `reservarte-sql`, base `ReservArteDB`, `localhost,1433`.
sqlcmd desde Git Bash:
`MSYS_NO_PATHCONV=1 docker exec -it reservarte-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<pwd-dev>' -C -d ReservArteDB -Q "..."`
**Escrituras (UPDATE/DELETE/INSERT) vía sqlcmd requieren `SET QUOTED_IDENTIFIER ON;` al inicio** (SELECT no).
Organización seed (determinista): `AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE` (More Than Brows).
Usuarios seed: `guille@svalero.com` (admin), empleadas en `@reservarte.com` y clientas en `@example.com`.

**Scripts SQL de `data/` — mantener SIEMPRE alineados con la base de datos** (decisión del usuario,
RA-869f17mzg). Dos tipos separados:
- `data/schema/`, **creación** (DDL, sin datos): `create_ReservArteDB.sql` **generado** desde las
  migraciones EF, **nunca editado a mano**, más `drop_ReservArteDB.sql`.
- `data/demo/`, **datos demo de desarrollo** (DML): `seed_demo_ReservArteDB.sql`, alineado con
  `DevSeeder` (mismas cuentas y contraseñas) más horarios demo con `0 = lunes`.

**Regla en cada cambio de base de datos:** en el MISMO PR que la migración, ejecutar
`bash data/schema/regenerate-create.sh`; si la migración toca una tabla que siembra el demo, o cambia
`DevSeeder`, actualizar `seed_demo`. Verificar creando una base de prueba con los scripts (nunca
sobre `ReservArteDB`) y arrancando la API contra ella. Detalle y orden (drop → create → demo):
`data/README.md`.

## Preferencias de trabajo

- **Idioma: español** en todo (comunicación, comentarios, mensajes de commit en inglés convencional).
- Al dar código: **archivos completos** o fragmentos con ruta exacta e indicación precisa de dónde va.
- Verificación con evidencia antes de cerrar cualquier tarea.
- Conventional Commits + Git Flow.
- No hardcodear credenciales; secretos en User Secrets (dev) — ver guía en `/Documentation`.

## Estado actual (2026-09-15)

- ✅ Setup backend y frontend completos.
- ✅ Módulo de Auth backend completo (9/9) + reset de contraseña + consentimiento RGPD.
- ✅ Bloque de UI `869d7edpt` **completo (7/7)**: layouts, páginas de auth (login local, OAuth
  callback, 2FA, registro, forgot/reset) y tests E2E Playwright + axe (24/24).
- ✅ Backend **CRUD Empleados** (`869d7ed2j`) **completo (10/10)**. Hecho: entidades y
  navegaciones, repositorio + migración (`EmployeeAvailabilities`/`EmployeeExceptions` con
  `OrganizationId` y query filters), servicio + validadores + AutoMapper, baja que bloquea la
  cuenta, catálogo canónico de roles (`869f18116`, PascalCase: Admin/Manager/Employee/Customer),
  endpoints CRUD con reglas de rol (`869d7ezz4`) + envelope de los 401/403 (`869f1anz3`),
  disponibilidad y ausencias (`869d7f01b`), invitación por email al dar de alta con reenvío y
  página `/set-password` (`869f17y68`), atomicidad de ficha + cuenta con `IUnitOfWork`
  (`869f1811u`), batería de tests (unit 182/182, E2E 51/51).
- ✅ Query filters globales por tenant en todas las entidades multi-tenant mapeadas (`869f17vet`):
  cierra el canje de un refresh token de una organización en el contexto de otra (verificado en
  runtime antes/después). Batería actual: unit 195/195, E2E 51/51.
- ✅ Scripts SQL de `data/` alineados con las migraciones (`869f17mzg`): `schema/` (creación, generado
  desde EF) y `demo/` (datos demo de desarrollo). Verificado: esquema idéntico al de EF (140 elementos)
  y la API arranca contra una base creada por script sin migrar ni sembrar.
- ⏳ Bloque **CRUD Clientes** (`869d7ed68`) **6/7** (`869d7f3q4` cancelada: sus tests de consentimiento van
  en PR #60 y el de no-shows se entrega con `869d7f3ka`). Último: notas internas (`869d7f3fw`, alcance
  reducido a notas; `/history` → `869f2gn91` en Citas y `/payment-methods` + mapeo de
  `CustomerPaymentMethod` → `869f2gnbm` en Redsys). Antes: endpoints `/api/v1/customers` (`869d7f3bt`,
  PR #61) y `CustomerService` + validadores (`869d7f369`, PR #60); reglas de cuenta mixta en «Arquitectura
  clave»; categoría `new` por defecto. Batería: unit 293/293, E2E 57/57. **Pendiente: `869d7f3ka`**
  (no-shows; necesita decidir `OrganizationSettings`/`CancellationPolicy`, `AuditLog` y quién dispara el
  no-show, que llega con Citas).
  Hecho antes: dominio (`869d7f2z5`), esquema + repositorio (`869d7f32r`: query filters, CHECK de
  catálogos con `CatalogCheck`, email único `(OrganizationId, Email)`, un consentimiento vigente por
  finalidad) y alta pública con ficha (`869f1xc2n`: registro y alta social crean la ficha en la
  transacción de la cuenta; `BackfillCustomerProfiles`). `CustomerPaymentMethod` sigue en `Ignore` hasta
  `869d7f3fw`; el historial de citas llega con Citas. Demo: `carmen.lopez@example.com` y
  `sofia.ruiz@example.com` (`Cliente123!`) en `DevSeeder` y `seed_demo`.
- ✅ Email único por organización (`869f1xc0u`): índices por organización en `AspNetUsers` y
  `Employees`, clave de `AspNetUserLogins` con `OrganizationId`, sin validador global. Verificado en
  runtime sobre base creada por script (mismo email en dos centros: registro, login y alta de empleada
  OK; duplicado dentro del centro 409). Batería: unit 219/219, E2E 51/51.
- 📋 Backlog no bloqueante: `869en8a17` (rate limiting + `AUTH_MFA_INVALID`), `869f151x1`
  (2FA en OAuth), `869f1812p` (EmailConfirmed), `869f17y6k` (unificar Result/AuthResult),
  `869f1k17q` (400 de model binding sin envelope), `869f1mqah` (resultados de Identity ignorados en auth).