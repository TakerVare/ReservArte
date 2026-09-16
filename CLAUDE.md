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
`EmployeeException`, `Customer`, `CustomerNote`, `CustomerAllergy`, `CustomerConsent`, el catálogo de
servicios completo, y `Appointment`, `AppointmentServiceItem` y `WaitingList`) — RA-869f17vet. Sin tenant resuelto (migraciones, seeders) no restringen.
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
- **Servicios** (RA-869d7f42u): **la lectura la permite cualquier rol autenticado, Customer incluido**
  (el catálogo no es dato personal y el cliente lo necesita para elegir servicio al reservar); es la
  diferencia deliberada con Empleados y Clientes. POST/PUT/DELETE y reactivate exigen **Admin o
  Manager**. `GET /api/v1/services?search&categoryId&isActive&page&pageSize` (`data.items` +
  `meta.pagination`; sin `isActive` = solo activos; `pageSize` acotado a 100), `GET /{id}` (detalle con
  variaciones y tarifas por nivel **vigentes**), `GET /api/v1/services/categories?isActive`
  (`data.items`; **sin `isActive` devuelve todas**, activas y retiradas: el formulario de edición
  necesita ver la categoría retirada de un servicio ya guardado), `POST` (201 + Location;
  categoría inexistente en el centro → 400
  `field=categoryId`, **no** 404: el recurso que se crea es el servicio), `PUT /{id}` (no toca la baja),
  `DELETE /{id}` (baja lógica idempotente; el servicio no desaparece porque las citas cerradas
  seguirán apuntando a él) y `POST /{id}/reactivate`. Validación: nombre obligatorio ≤200, duración > 0,
  precio ≥ 0, y antelación de prueba de alergia > 0 solo si `requiresAllergyTest`.
- **Catálogo — categorías, variaciones y tarifas** (RA-869f2wtrk). Todas estas escrituras exigen
  **Admin o Manager**; completan el catálogo, que antes solo se podía montar entero por SQL.
  **Categorías:** `POST /api/v1/services/categories` (201, `Location` a la lista, porque no hay
  endpoint de categoría por id), `PUT|DELETE /api/v1/services/categories/{categoryId}` y
  `POST /api/v1/services/categories/{categoryId}/reactivate`. La **baja de una categoría se permite
  aunque tenga servicios**: es lógica, así que conservan su `categoryId` y la categoría retirada sigue
  saliendo en `GET /categories` sin filtro.
  **Variaciones:** `POST /api/v1/services/{id}/variations` (201, `Location` al detalle del servicio) y
  `PUT|DELETE /api/v1/services/{id}/variations/{variationId}` (baja lógica idempotente). Un
  `durationModifier` que deje la duración resultante ≤ 0 → 400 `field=durationModifier` (lo valida el
  servicio, no FluentValidation: depende del servicio al que se añade); pedir una variación desde otro
  servicio → 404.
  **Tarifas:** `PUT /api/v1/services/{id}/pricings/{employeeLevel}` es un **upsert** — el nivel es la
  clave natural y el índice único solo admite una vigente por servicio y nivel, así que repetirlo
  actualiza en vez de dar 409. El nivel se normaliza a minúsculas; fuera de `EmployeeLevels` → 400
  `field=employeeLevel`. `DELETE /api/v1/services/{id}/pricings/{employeeLevel}` retira la vigente y
  **no es idempotente**: sin tarifa vigente → 404 (el recurso es la vigente).
- **Paquetes** (RA-869d7f45n), recurso propio en `/api/v1/service-packages` con
  `ServicePackagesController` y `IServicePackageRepository` / `IServicePackageService` separados del
  resto del catálogo. Misma autorización: **lectura para cualquier rol autenticado**, escrituras
  **Admin o Manager**. `GET /api/v1/service-packages?search&isActive&page&pageSize` y `GET /{id}`
  (cada paquete lleva sus líneas ordenadas por `order`, con `serviceName`, `basePrice` y
  `durationMinutes`), `POST` (201 + Location), `PUT /{id}`, `DELETE /{id}` (baja lógica idempotente)
  y `POST /{id}/reactivate`. **El `PUT` reemplaza la composición entera** (mismo criterio que
  `PUT …/availability` de Empleados), y el repositorio **borra físicamente** las líneas anteriores:
  no son histórico de negocio. El repositorio **impone** el paquete y el tenant a cada línea, así que
  una petición no puede colar líneas en otro paquete ni en otro centro. Un `serviceId` que no exista
  en el centro → 400 con el **índice de la línea** (`field=items[1].serviceId`), no 404.
  **Importes:** `totalPrice` es lo que se cobra y `discountPercentage` es informativo; la respuesta
  añade `itemsTotalPrice` (suma de los precios base), `savings` y `totalDurationMinutes`, calculados
  al leer y **no guardados**. `savings` puede salir negativo si el paquete es más caro que la suma:
  no se recorta a cero, para que la incoherencia se vea.
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
(debe coincidir con la lista del padre) + `parent`. Último bloque cerrado: **CRUD Clientes**
(`869d7ed68`, backend, 6/6). **Bloques en curso:** CRUD Servicios (`869d7ed7v`, backend, 5/6; parado
a la espera de Citas) y **Sistema de Citas** (`869d7edau`, backend, 1/11).
Para trasladar una subtarea a otro bloque (no se puede cambiar el padre):
crear la nueva bajo el padre destino y cancelar la original con comentario que la enlace.

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

## Estado actual (2026-09-16)

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
- ✅ Backend **CRUD Clientes** (`869d7ed68`) **completo (6/6)**, cerrado 2026-09-15 (`869d7f3q4` y
  `869d7f3ka` canceladas). Hecho: notas internas (`869d7f3fw`, PR #62), endpoints `/api/v1/customers`
  (`869d7f3bt`, PR #61), `CustomerService` + validadores (`869d7f369`, PR #60); reglas de cuenta mixta en
  «Arquitectura clave»; categoría `new` por defecto. Batería: unit 293/293, E2E 57/57.
  **Trasladado a otros bloques** (dependen de módulos que no existen): `/history` → `869f2gn91` (Citas),
  tarjetas + mapeo de `CustomerPaymentMethod` → `869f2gnbm` (Redsys), no-shows → `869f2gtyv` (Citas).
  Decisiones ya tomadas para no-shows: umbral en tabla `OrganizationSettings` (diseño vol. 1 §5.2,
  `OrganizationId` Guid; `Configuration`/`CancellationPolicy` antiguas se retiran con el módulo de
  Configuración), desbloqueo manual con motivo pone el contador a 0, sin `AuditLog` genérico (`869f2gtz8`).
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
- 🚧 Backend **CRUD Servicios** (`869d7ed7v`) **en curso (5/6)**, abierto 2026-09-16. Hecho:
  entidades del catálogo en Domain (`869d7f3wa`, PR #64), persistencia y servicio de aplicación
  (`869d7f3z0`, PR #65: migración `AddServiceCatalog`, las 7 entidades **salen de `Ignore`**,
  `IServiceRepository` + `ServiceCatalogService`), endpoints de servicios (`869d7f42u`, PR #66) y
  escrituras de categorías, variaciones y tarifas (`869f2wtrk`) y **paquetes** (`869d7f45n`:
  `IServicePackageRepository` / `IServicePackageService` propios, porque son un recurso HTTP
  distinto). El catálogo queda completo.
  **Se adelantó al bloque de Citas** (`869d7edau`) porque Citas depende de él:
  `AppointmentServiceItem` y `WaitingList` tienen FK a `Services`, y la duración y el importe de una
  cita salen de `Service.DurationMinutes`/`BasePrice`.
  Alcance: **7 entidades** (`Service`, `ServiceCategory`, `ServiceVariation`, `ServicePricing`,
  `ServicePackage`, `ServicePackageItem`, `EmployeeServiceAssignment`), todas con `OrganizationId`
  **`Guid`** y las hijas con tenant propio + navegación `Organization` (RA-869f17myx). Catálogo
  `EmployeeLevels` (`junior`/`senior`/`expert`) para `ServicePricing.EmployeeLevel`.
  Fuera de alcance: `ServiceProduct` (necesita `Product`), `ServicePhoto` (necesita `Appointment`),
  `ServicePromotion` (sin subtarea). Batería: unit 344/344, E2E 57/57 (no reejecutados).
  **Decisiones tomadas:** (a) las dos escalas de «nivel» se mantienen **independientes** —
  `ProficiencyLevel` (1-5) es *quién puede* prestar el servicio y `EmployeeLevel` *cuánto cuesta*—,
  sin regla que las ligue; (b) el catálogo es el **primer módulo cuya lectura permite el rol
  `Customer`** (las escrituras siguen siendo Admin|Manager), porque el cliente lo necesita para
  elegir servicio al reservar; revertirlo es una línea (`[Authorize]` → `[Authorize(Roles = …)]`).
  **Decisiones del catálogo** (`869f2wtrk`): dar de baja una categoría **se permite aunque tenga
  servicios** —la baja es lógica, ninguno queda sin clasificar y la categoría retirada sigue saliendo
  en `GET /categories` sin filtro—; y las tarifas se exponen como **upsert por nivel**
  (`PUT …/pricings/{level}`), porque el nivel es su clave natural y el índice único solo admite una
  vigente, así que repetir la llamada actualiza en vez de chocar.
  Pendiente del bloque: solo `869d7f4b4` (dashboard), que **necesita datos de citas** para ser útil,
  así que el bloque queda **parado** hasta que Citas dé de qué medir.
- 🚧 Backend **Sistema de Citas** (`869d7edau`) **en curso (2/11)**, abierto 2026-09-16. Es el núcleo
  del producto y lo desbloqueó el catálogo. Hecho: entidades en Domain (`869d7f4f1`): `Appointment`,
  `AppointmentServiceItem` y `WaitingList` con `OrganizationId` **`Guid`**, la línea de cita y la
  lista de espera con tenant propio + navegación `Organization` (RA-869f17myx). **Siguen en `Ignore`**
  hasta la migración de `869d7f4j8`. Retiradas de `Appointment` las navegaciones a módulos que no
  existen (`PaymentMethod` y `PaymentMethodId`, `Payments`, `Photos`, `ReminderLogs`,
  `ConfirmationTokens`); se conservan `RedsysOrderNumber` y `RedsysPreAuthToken`, que son escalares y
  llevarán índice único en `869d7f4j8`.
  **Decisión de estados (del usuario):** `AppointmentStatuses` tiene **8 valores**, fiel al CHECK de
  diseño de vol. 1 §5.2.2 — la cancelación se desdobla en `cancelled`, `cancelled_by_customer` y
  `cancelled_by_business` — **y se mantiene `CancelledByType`**. El mismo dato vive en dos columnas:
  **`Status` es la fuente de verdad** y la coherencia la debe imponer el servicio al cancelar
  (`869d7f4xf`). Para no repetir los tres literales hay `AppointmentStatuses.Cancellations`, y
  `Terminal` recoge los estados de los que no se sale.
  **Subtarea nueva `869f2yh9b`** (lista de espera: repositorio, servicio y endpoints): se creó al
  alinear `WaitingList`, porque ninguna de las 10 subtareas le daba capa de datos y habría repetido lo
  de los paquetes. El bloque pasa de 10 a **11** subtareas.
  Hecho también: **migración `AddAppointments`** (`869d7f4j8`), que mapea las **tres** entidades
  —`WaitingList` **entra en esta migración** (decisión del usuario; la propia descripción de ClickUp
  ya pedía su índice), así que `869f2yh9b` no necesitará migración propia—. `Appointments`:
  `idx_appointments_org_date`, `idx_appointments_redsys_order` **único y filtrado**
  (`WHERE [RedsysOrderNumber] IS NOT NULL`: en SQL Server un único sin filtro solo admite UN nulo, y
  la mayoría de citas no pasan por Redsys), CHECK de los 8 estados y de `CancelledByType` vía
  `CatalogCheck`, más `EndTime > StartTime` e importes ≥ 0. **FK a `Customers` y a `Employees` en
  `Restrict` las dos** (histórico de negocio + los dos caminos en cascada desde `AspNetUsers`);
  `AppointmentServiceItems` cuelga en `Cascade` de su cita y en `Restrict` de `Services`.
  `WaitingList` con `idx_waiting_lists_org_service_priority`, `Cascade` desde `Customers` y
  `Restrict` en el resto. Su tabla nació **en singular** (como el ERD de diseño) y se renombró a
  **`WaitingLists`** a petición del usuario al revisar el PR #70, ya mergeado: el renombrado va en su
  propia migración (`RenameWaitingListToWaitingLists`, PR #71), que arrastra PK, FK, los cuatro
  índices y el CHECK. **Ninguna tabla del esquema va en singular.**
  Batería: unit 410/410, E2E 57/57 (no reejecutados; la SPA no se toca).
- 📋 Backlog no bloqueante: `869en8a17` (rate limiting + `AUTH_MFA_INVALID`), `869f151x1`
  (2FA en OAuth), `869f1812p` (EmailConfirmed), `869f17y6k` (unificar Result/AuthResult),
  `869f1k17q` (400 de model binding sin envelope), `869f1mqah` (resultados de Identity ignorados en auth), `869f2gh37` (tests de integración HTTP con
  `WebApplicationFactory`), `869f2gtz8` (`AuditLog` transversal).

**`dotnet format` ya es puerta de calidad de verdad** (`869f2pjf8`, cerrada 2026-09-16, PR #72):
`dotnet format --verify-no-changes` sale **0 avisos y código 0** sobre `develop`. Se eligió el
camino de **alinear el espaciado** (los 113 avisos eran todos `WHITESPACE`, 15 ficheros; no hay
`.editorconfig`, así que manda la regla por defecto de C#). A partir de ahora, la casilla del DoD
«el linter no reporta errores nuevos» se marca de verdad, no «sin errores nuevos»: **cualquier
aviso que aparezca lo ha introducido el PR**. Al medirlo, NO encadenar con `| tail`: se leería el
código de salida de `tail` (0) y parecería que pasa.

## Dónde continuar (2026-09-16)

**Bloque Sistema de Citas (`869d7edau`) abierto, 2/11.** Es el núcleo del producto. El catálogo de
Servicios quedó **completo** (5/6) y **parado**: solo le falta el dashboard (`869d7f4b4`), que pide
«citas de hoy por estado», «ingresos del mes» y «próximas citas» y hoy no tendría nada que medir.
Se retomará cuando Citas dé datos.

**`869d7f4j8` cerrada y mergeada (PR #70 + PR #71**, el segundo solo con el renombrado de
`WaitingList` a `WaitingLists` que pidió el usuario al revisar el primero). **Documentación aplicada
y auditada, sin advertencias pendientes** (commits `38071a9` y `ff6d749`): vol. 1 (v9 del esquema,
ERD, `CREATE` reales de las tres tablas), vol. 2 **§9.9**, vol. 3 y estrategia de testing. La segunda
ronda corrigió tres contradicciones que detectó la propia IA de documentación: un texto roto en
vol. 3, la nota de `CustomerPaymentMethod.Appointments` (atribuía el `Ignore` a `Appointment`, que ya
está mapeada) y el motivo de `ServicePhoto` (sigue fuera **por alcance de módulo**, no porque le
falte tabla padre).

**Las dos decisiones que quedaban abiertas, ya resueltas (2026-09-16):**
1. El sketch de `appointments` (vol. 1 §5.2) conserva `redsys_auth_code`,
   `redsys_transaction_type` y `created_by`, que **no existen en la tabla**. Decisión del usuario:
   los dos de Redsys los decide **`869d7eden`** (su dueño natural) y `created_by` lo decide
   **`869d7f519`** al hacer los endpoints, que sabrá si hace falta registrar quién creó la cita; si
   no hacen falta, se **retiran del sketch**. Anotado como comentario en ambas tareas.
   (`payment_method_id` ya tenía dueño: `869f2gnbm`.)
2. ~~`dotnet format`: la línea base de `develop` pasa de 101 a 113 avisos.~~ **Resuelta**: el
   usuario pidió reducirlos y se alineó el espaciado entero en `869f2pjf8` (PR #72). Línea base
   **0**.

**Siguiente en orden: `869d7f4n4`** (3/11) — repositorio de citas. La capa de datos ya existe: las
tres tablas están mapeadas, con filtro por tenant y con la base de dev al día.

Después: `869d7f4rd` (disponibilidad), `869d7f4xf` (máquina de estados,
que además debe **imponer la coherencia entre `Status` y `CancelledByType`**), `869d7f519`
(endpoints), `869d7f53r` (tests), `869f2yh9b` (lista de espera), `869f2g02q` (promoción de
categoría), `869f2gn91` (`/history`) y `869f2gtyv` (no-shows, que trae `OrganizationSettings`).
**Una tarea a la vez, en orden. No adelantar tareas ni proponer siguientes pasos fuera de turno.**

**Criterio del módulo, para retomarlo en otra sesión:** lectura para cualquier rol autenticado
(Customer incluido) y escrituras Admin|Manager; baja lógica idempotente; y las verificaciones con
migración se hacen levantando la API contra una base **desechable** creada con los scripts de
`data/`, nunca sobre `ReservArteDB`. **Cuidado con `regenerate-create.sh`:** usa `--no-build`, así que
hay que compilar antes o genera un `create` sin la migración nueva y **aun así informa de éxito**.
En Windows fallaba entero (`DirectoryNotFoundException` de `dotnet ef`) porque usaba una variable
`TMP`, que ahí **ya es variable de entorno**: se la pasaba a `dotnet ef` como directorio temporal.
Renombrada a `SCRIPT_TMP` en `869d7f4j8`; misma precaución con `TEMP` en cualquier script nuevo.

## Traspaso Mac → Windows (2026-09-16)

**Estado al cambiar de equipo.** `develop` en `9370389`, **sincronizado con `origin`**, árbol limpio,
`dotnet build` 0/0 y batería **388/388**. No hay ninguna rama de trabajo abierta ni base de datos de
prueba colgando (solo `ReservArteDB`). Nada a medias.

**Al llegar a la torre:** `git checkout develop && git pull && dotnet build`, y esperar a elegir
tarea. En Windows, `sqlcmd` desde Git Bash necesita `MSYS_NO_PATHCONV=1` (ya documentado arriba); la
nota de que en el Mac hay que usar `npm run test:e2e` en vez de `npx playwright test` **no aplica
aquí**.

**Recorrido de esta sesión en el Mac:** bloque **CRUD Servicios** (`869d7ed7v`) de 0 a **5/6** —PRs
#64 a #68, catálogo completo con servicios, categorías, variaciones, tarifas y paquetes— y apertura
del bloque de **Citas** (`869d7edau`) con sus entidades de dominio (PR #69). Suite de 293 a 388.
Por el camino se crearon tres tareas que no existían: `869f2pjf8` (deuda de `dotnet format`),
`869f2wtrk` (escrituras del catálogo, hueco sin dueño) y `869f2yh9b` (lista de espera).

**Suciedad conocida de ClickUp** (limpiar al arrancar ese bloque, no antes): `869d7edt7` sigue en
`backlog` con fechas 2026-05-24 → 2026-06-05, ya pasadas.

**Limpieza pendiente del repo local (Mac):** quedan **25 ramas locales** de features ya mergeadas
(`feature/869d7f3wa-…`, `feature/869d7f45n-…`, etc.). No afectan al remoto ni a la torre; se pueden
borrar cuando apetezca con `git branch -d`.
