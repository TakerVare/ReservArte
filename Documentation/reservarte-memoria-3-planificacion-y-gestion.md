# RESERVARTE — Documentación técnica

## Sistema multi-tenant de gestión para centros de diseño de cejas

**Volumen 3 de 3: Planificación y gestión**

---

**Versión:** 1.0  
**Fecha:** Octubre 2025  
**Cliente:** More Than Brows  
**Ubicación:** España  
**Equipo de desarrollo:** Gabriel Sánchez-Vallejo Millán y Guillermo Algárate del Arco

---

## Índice (volumen 3)

1. [PLAN DE DESARROLLO - ROADMAP](#10-plan-de-desarrollo-roadmap)
2. [ESTIMACIÓN DE COSTOS](#11-estimaciÃ³n-de-costos)
3. [PRÓXIMOS PASOS](#12-prÃ³ximos-pasos)
4. [ANEXOS](#anexos)

> **Documentación complementaria:** [Estrategia de testing](reservarte-testing-strategy.md) — pirámide de pruebas, herramientas (xUnit, Testcontainers, Vitest, Playwright), CI/CD y cobertura por fase; enlazada desde **§12** y la subsección **Testing** del checklist **§12.2**. [Accesibilidad e i18n](accessibility-and-i18n.md) — WCAG 2.1 AA, vue-i18n v9, contraste y axe; coherente con **§10.2** y `Documentation/Project-Init/Scripts de instalación.md`.

---



## 10. PLAN DE DESARROLLO - ROADMAP



### 10.1 Metodología

**Enfoque:** Agile Scrum

- Sprints de 2 semanas
- Daily standups (15 minutos)
- Sprint review y retrospective
- Continuous Integration/Continuous Deployment (CI/CD)

**Roles:**

- **Product Owner:** Cliente (centro de cejas)
- **Scrum Master:** Líder técnico del equipo
- **Development Team:** Desarrolladores Full-Stack
- **QA Engineer:** Testing y calidad

**Herramientas:**

- **Gestión de proyecto:** **ClickUp** (workspace, espacios y listas definidos en §10.1.1)
- **Comunicación:** Slack
- **Control de versiones:** Git en **GitHub** — estrategia de ramas **Git Flow**, mensajes **Conventional Commits** y revisión mediante **Pull Requests** con plantilla (§10.1.2)
- **CI/CD:** GitHub Actions
- **Documentación técnica:** repositorio Git (`Documentation/`, volúmenes de análisis, implementación y planificación); seguimiento de tareas de documentación en ClickUp — Space **Documentation**, listas **Technical Specs** y **Architecture Decisions**



#### 10.1.1 ClickUp — Workspace y espacios

La planificación del trabajo, el backlog, los sprints y el seguimiento transversal se centralizan en **ClickUp** con la siguiente estructura:

**Workspace:** `ReservArte`


| Space                     | Listas                                  |
| ------------------------- | --------------------------------------- |
| **Backend (.NET)**        | Sprint Activo; Backlog; Bugs            |
| **Frontend (Vue 3)**      | Sprint Activo; Backlog                  |
| **Mobile (React Native)** | Backlog                                 |
| **Infrastructure**        | Tareas AWS / Docker / CI-CD             |
| **Documentation**         | Technical Specs; Architecture Decisions |


- **Sprint Activo:** tareas comprometidas para el sprint en curso (donde exista lista homónima).
- **Backlog:** trabajo priorizado pendiente de asignar a un sprint.
- **Bugs:** incidencias y regresiones del backend (Space Backend).
- **Tareas AWS / Docker / CI-CD:** despliegue, contenedores, pipelines y operación (Space Infrastructure).
- **Technical Specs:** especificaciones y entregables técnicos alineados con el repositorio `Documentation/`.
- **Architecture Decisions:** decisiones de arquitectura (p. ej. ADR), debates y cierres de diseño.



#### 10.1.2 Git Flow, Conventional Commits y Pull Requests

**Modelo de ramas (Git Flow)** — referencia clásica [nvie Git Flow](https://nvie.com/posts/a-successful-git-branching-model/):


| Rama                | Propósito                                                                                                                     |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `main`              | Código **en producción**; solo recibe merges desde `release/`* o `hotfix/*` (o etiquetas de versión).                         |
| `develop`           | Rama de **integración** continua del siguiente release; destino habitual de `feature/`* y origen de `release/*`.              |
| `feature/<nombre>`  | Nuevo desarrollo o mejora (p. ej. `feature/appointments-calendar`); se abre desde `develop` y se fusiona en `develop` vía PR. |
| `release/<versión>` | Preparación de un despliegue (congelar versión, ajustes finos); merge a `main` y de vuelta a `develop`.                       |
| `hotfix/<nombre>`   | Corrección urgente en producción; parte de `main`, merge a `main` y a `develop`.                                              |


**Reglas operativas:**

- No pushear directamente a **`main`** sin PR. A **`develop`** el propietario **puede** pushear sin PR (decisión 2026-09-14); el flujo habitual de features sigue siendo PR, pero no está forzado.
- **Antes de `git add` / commit:** `git status` y **no** `git add -A` a ciegas. El working tree puede llevar documentación en curso (IA de docs) que no pertenece al cambio. Incidente **PR #44:** se arrastraron cuatro archivos de `/Documentation`; se corrigió en la **rama feature** (`reset --soft`, sacar del índice, recommit, `--force-with-lease` **solo sobre la feature**, nunca sobre `develop`). Falló la ejecución, no la norma.
- **Prompts de documentación:** al describir comportamiento, **enumerar casos** (emisor, status, ¿envelope?) en lugar de reglas sintéticas («todos los 403…», «nunca por HTTP»). Las reglas se leen bien y se verifican mal; tres generalizaciones consecutivas las desmintió el código.
- **`develop` y `main` (RA-869d7ewu5; decisión 2026-09-14):** **`main`:** PR obligatorio, **0 aprobaciones**, sin force-push ni borrado. **`develop`:** **sin** PR obligatorio, **por decisión del propietario**. Sin CI, no hay checks obligatorios. `enforce_admins` = **false**. Eso **contradice** un Git Flow estricto en `develop`; no es un olvido.
- Los workflows de **GitHub Actions** deben dispararse en PR hacia `develop` / `main` y en push según política del equipo (documentar en cada workflow).
- Si el código vive en **varios repositorios** (API, web, móvil), replicar la misma convención en todos. **Decisión 2026-09-14:** el código está en el **monorepo** `TakerVare/ReservArte` (**RA-869d7ewqv** adaptada y done); no hay tres repos.
- **PR #3** se **cerró sin merge** (el contenido ya estaba en `develop`).

**Conventional Commits** — especificación [conventionalcommits.org](https://www.conventionalcommits.org/):

- Formato: `<tipo>[ámbito opcional]: <descripción breve>`  
Ejemplos: `feat(auth): add Google OAuth challenge`, `fix(appointments): validate slot overlap`, `docs: update API envelope §5.1.1`
- Tipos habituales: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- Cuerpo y pie opcionales; para cambios rupturistas: pie con `BREAKING CHANGE:` o `!` tras el tipo (`feat(api)!: ...`).
- Permite generar **changelog** y versionado semántico de forma coherente con **release/**.

**Plantilla de Pull Request**

- Ubicación en el repositorio: `.github/PULL_REQUEST_TEMPLATE.md` (GitHub la aplica al abrir un PR).
- Si hay monorepo único, un solo fichero basta; si hay varios repos, copiar la misma plantilla a cada uno o adaptarla.
- El contenido debe guiar: descripción del cambio, tipo (feature/fix/docs…), checklist (tests, documentación, breaking changes), enlace a tarea ClickUp, capturas si aplica UI.
- **Base de datos (PR #57, RA-869f1xc0u, 2026-09-15):** la plantilla **cubre** regenerar `data/schema/create_ReservArteDB.sql` con `regenerate-create.sh`, revisar `data/demo/seed_demo_ReservArteDB.sql` y verificar sobre una base de prueba creada con los scripts (**nunca** `ReservArteDB`). Mitigación mientras no haya CI. No hay job que falle si se olvida marcar las casillas.
- **Linter (RA-869f2pjf8, PR #72 + #73):** la casilla de `dotnet format` **ya no** es «sin errores nuevos». `dotnet format --verify-no-changes` es puerta de calidad con línea base **CERO**: cualquier aviso lo introduce el PR que se revisa. Medir **sin** encadenar `| tail` (se leería el código de salida de `tail`). Frontend: `npm run lint` / Prettier como hasta ahora. Aún **no** hay job de CI que lo ejecute.

---



### 10.2 Fases del Proyecto

> **Leyenda de marcas:** ✅ hecho y verificado · ⏳ en curso o parcial · ⬜ no empezado.



#### FASE 1: MVP - Funcionalidades Esenciales (3-4 meses)

**Objetivo:** Aplicación web funcional con lo mínimo indispensable para gestionar un centro

---

**Sprints 1-2 (Mes 1): Fundación**

**Semana 1-2:**

- ⏳ Setup de infraestructura AWS — **no hecho** (dev: SQL Server Docker `reservarte-sql`; sin `docker-compose.yml` — RA-869d7ewec; SES/CloudWatch/VPC pendientes)
  - Crear cuenta AWS
  - Configurar VPC, subnets, security groups
  - Aprovisionar SQL Server en Docker (entorno dev, p. ej. `docker-compose`)
  - Crear cuenta **Cloudinary** y carpetas / upload presets (dev/staging/prod)
  - Configurar variables o secrets con `CloudName`, `ApiKey`, `ApiSecret`
  - Configurar Amazon SES (verificar dominio)
- ✅ Configuración de proyecto .NET
  - Crear solución con Clean Architecture
  - Configurar Entity Framework Core
  - Setup de migraciones de BD
  - Serilog: pipeline en dos fases + sink consola + enriquecimiento por petición — **hecho**; sink CloudWatch — **pendiente** (infra)
- ✅ Configuración de proyecto Vite
  - Crear proyecto Vue 3 + TypeScript + Vite
  - Configurar Tailwind CSS + componentes UI alineados con Vue (p. ej. Reka UI / Radix-Vue)
  - **Arquitectura i18n (Sprint 1):** instalar **vue-i18n v9**, carpetas `src/locales/` y `src/i18n/`, mensajes base en **español** y registro en `main.ts` según `Documentation/Project-Init/Scripts de instalación.md` (Pasos 2–5)
  - Utilidades de formato **es-ES** generadas en el mismo script (Paso 5): `src/lib/utils/date.utils.ts`, `currency.utils.ts` (dd/MM/yyyy, moneda EUR)
  - Setup de Pinia para estado global
  - Configurar Vue Router
  - **Accesibilidad (linea base):** criterios WCAG 2.1 AA, contraste y pruebas con **axe** según `[accessibility-and-i18n.md](accessibility-and-i18n.md)`. Línea base = tests axe de humo (RA-869d7fbpp); deuda de contraste WCAG AA 1.4.3 del color de marca en RA-869f0v6vm.
- ✅ Base de datos inicial
  - Migración: tablas core (organizations, users, employees)
  - Seed data para desarrollo
  - Índices iniciales
- ✅ Autenticación básica
  - Login/Registro con JWT (access + refresh)
  - Login social: **Google**, **Apple**, **Instagram** (OAuth **Meta**; permisos y revisión en Meta Developers) con **mismo** par de tokens que el login local
  - **2FA opcional** (TOTP + códigos de recuperación): flujo `mfa/verify` tras login; ajustes en cuenta
  - Middleware de autenticación y validación `JwtBearer`
  - Tabla / entidad de logins externos (`AspNetUserLogins`) y reglas de vinculación por email
  - Rate limiting nativo + CAPTCHA (Turnstile) en login
  - Tests unitarios del `JwtTokenService` (`tests/ReservArte.UnitTests`)
  - **Andamiaje auth frontend** (router con guards, authStore, rutas, interceptor Axios) — **hecho**
  - **UI de login funcional (`LoginPage`, RA-869d7f7kn, 2026-08-23)** — **shipped.** Verificado en runtime: login local E2E (200 + hidratación de `authStore` + redirect). Formulario de credenciales, botones OAuth (Google / Apple / Instagram) **cableados**, enlace «¿has olvidado tu contraseña?», estados de carga y error. CAPTCHA: contador de fallos (umbral 3) y hueco de montaje verificados; **widget Turnstile real pendiente**. **Pendientes no bloqueantes de LoginPage:** (a) widget Turnstile; (b) OAuth en runtime (credenciales de proveedor por entorno); (c) tokens secundarios del modo claro (restos shadcn en `:root`) — **RA-869f0w7r2**; (d) paleta del modo oscuro (`.dark` placeholder, **no** diseñada) — **RA-869f0w75h**. Contraste de `--primary`: **RA-869f0v6vm** (deuda aparte, no es (c) ni (d)).
  - **UI de verificación 2FA (`MfaVerifyPage`, RA-869d7f7vw, 2026-08-24)** — **shipped.** Verificado en runtime el flujo 2FA de extremo a extremo (login → `mfaRequired` → verificación TOTP/recuperación → dashboard), incluidos código de recuperación y rechazo de código incorrecto.
  - **UI de registro (`RegisterPage`, RA-869d7fbhg, 2026-08-25)** — **shipped.** Verificado: validación Zod (política alineada con `RegisterRequestValidator`), consentimiento versionado (`GET /legal/versions` + checkboxes), alta y **login automático**. **RA-869f1xc2n (PR #59):** tercer checkbox `acceptedDataProcessing` (sin enlace) y ficha `Customer` `regular` al registrarse. **Desde RA-869d7f369** la ficha nace `new`.
  - **UI de recuperación (`ForgotPasswordPage` + `ResetPasswordPage`, RA-869d7fbmy, 2026-08-27)** — **shipped.** Forgot: solicitud por email y estado «enviado» (anti-enumeración). Reset: token de `/reset-password/:token?`, email + nueva contraseña (Zod = política del backend), estados sin-token / formulario / éxito. **Contrato del token (RA-869f18rp7, PR #42):** Vue Router decodifica el segmento; el POST lleva el token **en claro, una sola vez**. E2E `e2e/reset-password.spec.ts`. **Runtime (2026-09-13):** forgot → `DevFileEmailService` → reset (ese camino de fichero no está en Playwright).
  - **Test a11y `LoginPage` (RA-869d7fbpp, 2026-09-11)** — **shipped.** Spec `reservarte-web/e2e/login.a11y.spec.ts`: WCAG 2.1 AA con `@axe-core/playwright` en tres estados (inicial, error, CAPTCHA). **No es conformidad plena:** el test **excluye** `color-contrast` (marca rosa `#FFB6C1`, ~1.62 vs 4.5:1); deuda **RA-869f0v6vm**.
  - **Retorno OAuth (`OAuthCallbackPage`, RA-869d7f7r1, 2026-09-12)** — **shipped** (PR #33, commit `f25f872`). Verificado: contrato del callback con E2E Playwright (`reservarte-web/e2e/oauth-callback.spec.ts`, 4 casos × 3 navegadores = 12 tests: éxito con `/account/me` interceptado, error del proveedor, fragmento ausente, ausencia de sesión tras el fallo). Suite completa **24/24** en chromium/firefox/webkit. `npm run build` ✓ y ESLint sin errores ni warnings en los archivos tocados. **No verificado:** flujo contra un proveedor OAuth **real** (credenciales por entorno); sigue como pendiente no bloqueante de **LoginPage**, **desacoplado** del estado de esta tarea (lo bloqueado era la verificación contra el IdP, no la pantalla). **Arreglo colateral:** `npm run build` estaba roto en `develop` (`TS2614` en `reservarte-web/src/components/ui/login-form/index.ts`: reexportaba `OAuthProvider` desde `LoginForm.vue`, donde nunca se exportó; vive en `@features/auth/types/auth.types`). **RA-869d7f7ef** figura `shipped`, pero la regresión entró después; se corrigió en este PR (nadie consumía el reexport). Bloque padre **RA-869d7edpt:** **7/7 — completo** (layouts + LoginPage + MfaVerifyPage + RegisterPage + Forgot/Reset + test a11y LoginPage + OAuthCallback). Pendientes **fuera** del recuento 7/7: widget Turnstile real, contraste AA (**RA-869f0v6vm**), tokens del modo claro (**RA-869f0w7r2**), paleta oscura (**RA-869f0w75h**), migración de `LoginForm` a VeeValidate+Zod (**RA-869epnt88**) y reconciliación de layouts (**RA-869ep9p36**).

> **Cierre de módulo Auth — RA-869d7ed03 (2026-08-21):** **completo (9/9 subtareas)** — Identity; `JwtTokenService`; endpoints locales; Google/Apple; Instagram/Meta; 2FA TOTP (enable/confirm/disable); 2FA verify + códigos de recuperación; rate limiting + CAPTCHA; tests del `JwtTokenService` (RA-869d7ezp3). Pendiente **no bloqueante** en backlog: **RA-869en8a17** (*Refinamientos de auth: completar políticas de rate limiting +* `AUTH_MFA_INVALID` *en verify*). **Alta en backlog (prioridad high):** **RA-869f151x1** — el login social (OAuth) se salta el 2FA: emitir ticket `mfa_pending` si el usuario tiene TOTP activo (no bloqueó el cierre del módulo Auth ni el del bloque UI).

- ✅ Panel de administración — **navegación de diseño: solo BottomNav** (RA-869ep9b52); ver vol. 2 §9.2.4
  - **Diseño:** sin Sidebar. Hub de gestión en `/cuenta` (bloques administración / usuario según rol). Citas desde la pantalla de Citas.
  - **Código actual (deuda):** `DashboardLayout` (Sidebar + Header, 8 módulos) y `AuthLayout` (huérfano) existen; **no** son el diseño. Retirada planificada (reconciliación de layouts, backlog).
  - **Páginas de auth implementadas (`LoginPage`, `MfaVerifyPage`, `RegisterPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `OAuthCallbackPage`):** patrón **Banner + contenido centrado**, **no** `AuthLayout`, salvo **`OAuthCallbackPage`** (sin Banner; tránsito de milisegundos). `BottomNav` global en `App.vue` (3 destinos; también se pinta en `/auth/callback`).
  - Dashboard placeholder (contenido de negocio pendiente)

**Entregables Sprint 1-2:**

- ⏳ Infraestructura AWS configurada y funcional — **no** (SQL Server local/Docker; AWS pendiente)
- ⏳ Repositorios Git con CI/CD básico y convenciones **Git Flow** + **Conventional Commits** (§10.1.2) — Git + convenciones **sí**; **GitHub Actions no** (checklist §12.2)
- ✅ Login **backend** funcional (API Auth completa; módulo RA-869d7ed03 cerrado 9/9)
- ✅ Login **frontend** local (`LoginPage`, RA-869d7f7kn) + verificación 2FA (`MfaVerifyPage`, RA-869d7f7vw) + registro (`RegisterPage`, RA-869d7fbhg) + recuperación (`ForgotPasswordPage` / `ResetPasswordPage`, RA-869d7fbmy) + test a11y LoginPage (RA-869d7fbpp) + retorno OAuth (`OAuthCallbackPage`, RA-869d7f7r1): shipped. Bloque RA-869d7edpt **7/7 — completo**. Turnstile real sigue pendiente (no es ítem del recuento 7/7). El test a11y **no** certifica contraste AA (deuda RA-869f0v6vm). OAuth contra proveedor **real** sigue pendiente de credenciales por entorno (pendiente de LoginPage, desacoplado de RA-869d7f7r1).
- ✅ Panel de administración: **diseño = BottomNav only**; `DashboardLayout`/Sidebar en código = deuda a retirar (no el estado deseado)
- ✅ **i18n operativo en español** (vue-i18n, estructura de claves y ficheros de traducción base) y **utilidades** `date.utils.ts` / `currency.utils.ts` según script de instalación
- ✅ Documentación de setup para nuevos desarrolladores

---

**Sprints 3-4 (Mes 2): Gestión Básica**

**Semana 5-6:**

- CRUD de empleados — bloque **RA-869d7ed2j: 10/10** (**cerrado**, 2026-09-14)
  - API endpoints completos
  - Formularios de creación/edición
  - Lista con búsqueda y paginación
  - Gestión de roles
  - **Entidades Domain (`Employee` + `EmployeeAvailability` + `EmployeeException`, RA-869d7ezrr, 2026-09-12)** — **shipped** (PR #34 `57a3077`; ajuste PR #35 `2126f75`). Completa y documenta entidades que ya existían desde `InitialCreate` (no las crea). Convención de semana `0 = lunes` + helper `WeekDay`. Detalle: vol. 1 **§3.1.2**, vol. 2 **§9.6**.
  - **Repositorio + migración (`IEmployeeRepository` / `EmployeeRepository`, RA-869d7ezv0, 2026-09-13)** — **shipped** (PR #36 `26196e1`). Primer repositorio del proyecto (`AddRepositories()`). `PagedResult<T>`, `EmployeeFilter` (`IsActive` null = solo activos; página máx. 100). Migración `AddEmployeeAvailabilityAndExceptions` aplicada a la BD de desarrollo; esquema verificado en SQL Server (2 tablas, 3 CHECK, 6 índices).
  - **`OrganizationId` en disponibilidades y excepciones (RA-869f17myx, 2026-09-13)** — **shipped** en el **mismo** PR/migración que RA-869d7ezv0 (decisión de usuario): la columna **nace con las tablas** y **se evitó el backfill**. Query filter global; escritura impone tenant/empleado desde la petición. El hueco de aislamiento de estas dos tablas **queda cerrado**.
  - **Servicio + validadores + AutoMapper (RA-869d7ezwy, 2026-09-13)** — **shipped** (PR #37 `cf64817`). `IEmployeeService` / `EmployeeService`, `Result<T>`, `EmployeeDto` (sin `organizationId`), alta sin contraseña, sincronización ficha↔Identity, baja lógica idempotente. Validadores Create/Update alineados (`profileImageUrl` máx. 500). Registro `AddApplicationServices()`.
  - **Tests del módulo (RA-869d7f043)** — **shipped** y **cuenta en el numerador** (RA-869f18nq5).
  - **Lockout al dar de baja (RA-869f180e5)** — **shipped.** Baja de ficha → lockout permanente de Identity; reactivación lo retira. Auth rechaza login/refresh/MFA. Límite: el access token vigente sobrevive hasta caducar. Vol. 2 **§9.6**.
  - **Endpoints CRUD + reglas de rol (RA-869d7ezz4, 2026-09-14)** — **shipped** (PR #49, merge `dbb55e9` en `develop`). `EmployeesController` `[Authorize(Roles = Admin,Manager)]`; lista `data.items` + `meta.pagination`; `POST …/reactivate`; reglas por dato en `EmployeeService` (`ICurrentUserService`). Colateral **RA-869f1anz3** (envelope 401/403 JwtBearer) **shipped en el mismo PR**; no cuenta en el 10.
  - **Disponibilidad horario + ausencias (RA-869d7f01b, 2026-09-14)** — **shipped** (PR #50, merge `697012b` en `develop`). GET/PUT `…/availability`; POST/DELETE `…/exceptions`. Lectura de Admin permitida; escritura de Manager sobre Admin → 403. Vol. 1 **§3.1.2** / **§5.1**, vol. 2 **§9.6**.
  - **Invitación por email (RA-869f17y68, 2026-09-14)** — **shipped** (PR #51, merge `04e5f91` en `develop`). Proveedor `Invitation` 7 días; alta envía correo (fallo ≠ rollback); `POST …/invitation`; `POST /api/v1/auth/set-password`; SPA `/set-password/:token?`. Vol. 1 **§3.1.2** / **§4.4.1** / **§5.1**, vol. 2 **§8.1.1** / **§9.2.3** / **§9.6**.
  - **Transacción explícita en alta/edición/baja (RA-869f1811u, 2026-09-14)** — **shipped** (PR #53, merge `5c723d0` en `develop`). `IUnitOfWork` / `EfUnitOfWork`; Identity + ficha en una transacción; invitación **después** del commit; cada `IdentityResult` se comprueba. Cierra el bloque. Vol. 1 **§3.1.2**, vol. 2 **§9.6**.
  - Backend **10/10**; **no** incluye UI de Empleados (formularios, lista, horarios en SPA). Horarios: persistencia API sí (RA-869d7f01b); pantalla no.
  - **Renombrado entidad puente `EmployeeService` → `EmployeeServiceAssignment` (RA-869f17y7n, 2026-09-13)** — **shipped** (PR #38 `3a3bf2d`). **No es ítem de backlog ni del denominador.** Tabla SQL **sigue** `EmployeeServices`.
  - **Scripts `data/` (RA-869f17mzg, 2026-09-14)** — **done** (PR #55, merge `9e52ad9`). `data/schema/` (create generado + drop) y `data/demo/seed_demo_ReservArteDB.sql`. Cierra también **RA-869d7ewka** y **RA-869d7fd6p**. Convención `0 = lunes`. Vol. 1 **§5.2**, [`data/README.md`](../data/README.md).

> **Módulo Empleados — RA-869d7ed2j (2026-09-14, PR #53):** **10/10, cerrado.** Tarea padre en **shipped**. Numerador: ezrr, ezv0, myx, ezwy, 180e5, f043, ezz4, f01b, y68, **1811u**. Colaterales (no cuentan): **RA-869f17y7n**, **RA-869f1anz3**, **RA-869f1m12x**.
>
> Evidencia (2026-09-14, PR #53): unit **182/182** (172 tras #51; +10: 5 `EmployeeServiceTests` con `FakeUnitOfWork` + 5 `EmployeeAtomicityTests` SQLite). E2E **51/51**. Runtime SQL Server: PUT email del admin → **409** y datos intactos; edición legítima en ficha y cuenta; alta 201 + invitación post-commit; baja `IsActive=0` + `LockoutEnd=9999-12-31`; reactivar `IsActive=1` + `LockoutEnd=NULL`; alta con email del admin → 409 sin crear cuenta.
>
> **Criterio de trabajo (2026-09-13, usuario):** todo cambio se contrasta con el código ya desarrollado y se verifica que no rompe lo existente. Aplicado: `LoginAsync` no exige consentimiento RGPD y ya admite cuentas sin contraseña local.
>
> **Nota de método:** Clientes **RA-869d7f2z5** no estaban en `InitialCreate`; **RA-869d7f32r** ya las mapeó salvo `CustomerPaymentMethod`; Servicios **RA-869d7f3wa** + **RA-869d7f3z0** mapeados. Citas **RA-869d7f4f1** (PR #69) alineó el dominio; **RA-869d7f4j8** (PR #70 + #71) las **mapeó**. **Además:** no nombrar esas entidades `CustomerService` / `ServiceService` — colisión con la capa de aplicación (RA-869f17y7n). El servicio de aplicación del catálogo se llama **`ServiceCatalogService`**.
>
> **`OrganizationId int` en dominio aún no mapeado:** **8** entidades en `Ignore` declaran `public int OrganizationId`, incompatible con `Organization.Id` (`Guid`): `Payment`, `CancellationPolicy`, `Configuration`, `MessageTemplate`, `ReminderConfiguration`, `Product`, `ProductCategory`, `ProductSale`. `Appointment`, `AppointmentServiceItem` y `WaitingList` ya son `Guid` (**RA-869d7f4f1**, PR #69) y están **mapeadas** (**RA-869d7f4j8**). Cada subtarea «Entidades en Domain» debe pasarlas a `Guid` **antes** de mapear. Lo exigen el test de metadatos (query filter) y la FK a `Organizations`.
>
> **Alta en backlog (Infra, prioridad high):** **RA-869f17mzg** — (texto histórico.) Sincronizar `data/` con EF. **Cerrado 2026-09-14, PR #55 (`9e52ad9`).** Desbloqueó **RA-869d7ewka** (done) y **RA-869d7fd6p** (publish).
>
> **Alta en backlog: RA-869f17vet** — (texto histórico 2026-09-13.) Query filters para el resto de entidades. Identity sin tenant en el login era el riesgo citado. **Cerrado el 2026-09-14, PR #54:** el login no se rompe; el hueco era el refresh cruzado.
>
> **Alta en backlog: RA-869f17y6k** — unificar `Result<T>` y `AuthResult<T>`.
>
> **RA-869f18116 → shipped (2026-09-13), PR #47.** Catálogo `Roles.cs` (PascalCase, 4 valores); registro y OAuth → `Customer`; migración `NormalizeRolesToPascalCase`. Desbloqueó **RA-869d7ezz4**. Evidencia entonces: unit **110/110** (el prompt de aquella entrega citaba 7 tests nuevos; la base documentada anterior era 100). **PR #48** (misma tarea / default del DTO desde el catálogo): +1 test → **111/111**.
>
> **Constancia (diagnosticada y corregida en PR #66, 2026-09-16):** una ejecución de `dotnet test` sobre `develop` dio **33/34** (antes de RA-869f18116, 2026-09-13); no reproducido en 10 ejecuciones posteriores y **entonces** sin nombre de test capturado. Causa: `ValidateToken_rechaza_un_token_manipulado` alteraba el **último carácter de la firma** HMAC-SHA256. Los 2 últimos bits de ese carácter son relleno base64url y se descartan; solo `Y` colisiona con la `a` del test (1/16 = 6,25 %). **No era un fallo de producción**: el token seguía siendo válido. El test ahora altera el **payload**.

> **Auditoría de humo (2026-09-13) — verde:** backend `dotnet build` 0/0 y tests **100/100**; frontend lint **0 errores** (warnings a 0: **RA-869f18nqh**, PR #41), `npm run build` ✓, Playwright entonces **30/30** (24 previos + 6 de reset-password; **39/39** tras PR #44; **48/48** tras PR #51; **51/51** tras PR #52; **vigente 57/57** tras PR #59); migraciones EF **5/5** aplicadas. Runtime: `/health`, registro, login, `GET /api/v1/account/me`, rotación de refresh (reuso rechazado), **401 sin Bearer** (**cuerpo vacío**, sin envelope). Tenant: **400** `ORG_TENANT_NOT_RESOLVED` (org inexistente), **403** `ORG_TENANT_MISMATCH` (org existente ≠ claim; org temporal luego eliminada). Forgot/reset: runtime contra fichero + contrato de token en Playwright. **CLAUDE.md** actualizado (**RA-869f18fuy**, PR #40).
>
> **RA-869f18rp7 → shipped (2026-09-13), PR #42 (`acdfc8b`).** Cierra las advertencias de la auditoría de coherencia: (1) código de tenant ambiguo → `ORG_TENANT_MISMATCH` vs `ORG_TENANT_NOT_RESOLVED`; (2) contrato del token de reset sin fijar → spec E2E + comentario de `auth.api.ts`; (3) protección de **`main`** (PR obligatorio, 0 aprobaciones). **No** aplicar esa frase a `develop`: el 2026-09-14 el propietario dejó **`develop` sin PR obligatorio**. Evidencia entonces: unit **100/100**, E2E **30/30**.
>
> **RA-869f18urw → shipped (2026-09-13), PR #43.** La SPA cierra sesión ante `ORG_TENANT_MISMATCH` (`SESSION_ENDING_ERROR_CODES` en `client.ts`). Evidencia en aquel cierre: lint frontend 0/0, `npm run build` ✓, E2E **36/36**; backend sin tocar, **100/100**. **Método:** el primer borrador del caso «403 de otro código NO cierra sesión» usaba un `fetch` suelto que **no atraviesa el interceptor** (habría dado verde contra código roto). Se reescribió provocando la llamada **desde la app** y se validó por mutación: ampliar la regla a «cualquier 403» hace fallar el test; revertir, pasa.
>
> **PR #44 (`f37b3a7`) mergeado (2026-09-13).** Caso E2E de **403 sin cuerpo** (forma **entonces** de `[Authorize(Roles)]`); comentarios de contrato en `client.ts`. Recuento entonces **39/39**. Unit **100/100**. **No** cerró **RA-869f1anz3** (cerrado el 2026-09-14 en PR #49).
>
> **PR #45 (`b3737b7`) mergeado (2026-09-13).** Solo comentarios y nombres de test en `e2e/session-ending.spec.ts` (sin cambio de comportamiento entonces). Desde PR #49 el caso de rol con envelope es el 403 real `GEN_FORBIDDEN`.
>
> **Alta en backlog: RA-869f1anz3** — (texto histórico 2026-09-13.) Hueco = 401 challenge JwtBearer y 403 de autorización ASP.NET Core (sin cuerpo). **No** incluye `ORG_TENANT_*` ni los 401 `AUTH_*`.
>
> **RA-869f1anz3 + RA-869d7ezz4 → shipped (2026-09-14), PR #49 (`dbb55e9`).** Decisión: **se envuelven** (`OnChallenge`/`OnForbidden`). `SESSION_ENDING_ERROR_CODES` sigue siendo solo `ORG_TENANT_MISMATCH`. En `client.ts` solo comentarios. E2E: el caso «403 GEN_FORBIDDEN» es el 403 real de `[Authorize(Roles)]`; «403 sin cuerpo» queda como robustez WAF. Unit entonces **125/125**.
>
> **RA-869d7f01b → shipped (2026-09-14), PR #50 (`697012b`).** GET/PUT `…/availability`; POST/DELETE `…/exceptions`. Unit entonces **164/164**. Módulo entonces **8/10**. **Advertencia:** el cálculo horario−ausencias y la zona del centro quedan en **RA-869d7f4rd**; la UI de Empleados debe replicar validación y no cerrar sesión ante `GEN_FORBIDDEN`. Semana 7-8 del roadmap original («Horarios de empleados») **no** está hecha en frontend; el backend de persistencia sí.
>
> **RA-869f17y68 → shipped (2026-09-14), PR #51 (`04e5f91`).** Invitación + `/set-password`. Unit entonces **172/172**. E2E entonces **48/48**. Módulo entonces **9/10**. El correo **después del commit** quedó implementado en **RA-869f1811u**. El 401 de `reset-password` quedó como hueco; cerrado el mismo día en **RA-869f1m12x**.
>
> **RA-869f1m12x → shipped (2026-09-14), PR #52 (`3c53de7`).** Lista Frontend; **no cuenta** en el 10. `reset-password` entra en `AUTH_ENDPOINTS_WITHOUT_SESSION`. El 401 mandaba a `/login` **sin necesidad de sesión** (`endSession()` no comprueba si había token). E2E **51/51**. Unit entonces **172/172**.
>
> **RA-869f1811u → shipped (2026-09-14), PR #53 (`5c723d0`).** Atomicidad ficha+Identity. **Bloque RA-869d7ed2j 10/10 cerrado.** Unit entonces **182/182**. E2E **51/51**. **Límites sin tarea:** sin concurrencia optimista en `Employee`; `EmailExistsAsync` (entonces) solo miraba `Employees` sin cruzar org.
>
> **RA-869f17vet → shipped (2026-09-14), PR #54 (`19d00f2`).** Query filters en `Employee`/`User`/`RefreshToken`; `GlobalUniqueUserValidator`; filtro manual del repositorio **se mantiene**. Unit **195/195**. E2E **51/51**. Refresh A→B: 200→401. **OAuth no verificado en runtime.**
>
> **RA-869f17mzg → done (2026-09-14), PR #55 (`9e52ad9`).** Scripts `data/schema/` + `data/demo/`. Cierra **RA-869d7ewka** (done) y **RA-869d7fd6p** (publish). Unit **195/195**, E2E **51/51**. Verificación en `ReservArteDB_ScriptCheck` (esquema idéntico a EF en 140 elementos; API no remigra ni siembra). Método: el primer intento falló por `QUOTED_IDENTIFIER`, por una API que sembró una base a medias, y por `Msg 451` en FK (corregido con `COLLATE DATABASE_DEFAULT`). Tras el merge, regenerar el `create` no cambia el fichero.
>
> **Gestión (2026-09-14):** monorepo `TakerVare/ReservArte` (RA-869d7ewqv). `main` con PR; **`develop` sin PR obligatorio**. ClickUp coherente: hechas **RA-869d7edpt**, **RA-869d7ewh0**, **RA-869d7ewwq**, **RA-869d7ex22**, **RA-869d7ewu5**. **RA-869f17mzg**, **RA-869d7ewka**, **RA-869d7fd6p** → cerradas (PR #55). Siguen pendientes: **RA-869d7ewec** (no hay `docker-compose.yml`), **RA-869d7ewzg** (husky + commitlint), **RA-869d7ewnz** (backup EBS), CI, guards por rol, Vitest, `LoginForm` con VeeValidate, componentes UI base. **RA-869f1mqah** — IdentityResult ignorados en `AuthService` y `MfaController` (hipótesis; **no cerrado** en PR #59: solo se cubren `CreateAsync`/`AddLoginAsync` del alta pública).
>
> **Alta en backlog: RA-869f1k17q** — 400 `ProblemDetails` (`application/problem+json`) de `[ApiController]` sin envelope (JSON mal formado / parámetro no convertible). Lista Backend.
>
> **RA-869d7f2z5 → shipped (2026-09-14), PR #56.** Dominio Clientes. Unit entonces **207/207** (`CustomerDomainTests` 12). Sin cambio de esquema.
>
> **RA-869f1xc0u → shipped (2026-09-15), PR #57.** Email único **por organización**. Independiente (no suma al 8 de Clientes). Migración `20260915101445_ScopeEmailAndExternalLoginsToOrganization`: `EmailIndex`/`UserNameIndex` = `(OrganizationId, Normalized*)`; `Employees` único `(OrganizationId, Email)`; PK `AspNetUserLogins` = `(OrganizationId, LoginProvider, ProviderKey)` con backfill. `UserLogin` + query filter; `OrganizationUserStore`; **sin** `GlobalUniqueUserValidator` ni `IgnoreQueryFilters` en producción. `create` regenerado; `seed_demo` solo cabecera. Plantilla de PR cubre `data/`. Unit entonces **219/219**. E2E **51/51**. Runtime: mismo email en dos centros 200/200; duplicado 409; alta de empleada cruzando orgs 201/409. Desbloqueó **RA-869d7f32r**. Cadena: **RA-869f1xc0u** (hecha) → **RA-869d7f32r** → **RA-869f1xc2n**.
>
> **RA-869d7f32r → shipped (2026-09-15), PR #58.** Esquema y repositorio de Clientes. Migración `20260915112149_AddCustomers`. `ICustomerRepository` / `CustomerRepository`. Demo Carmen/Sofía (una sola org). `.bak` de Configurations eliminados. Unit entonces **237/237**. E2E **51/51**. Desbloqueó **RA-869f1xc2n**. Cadena: **RA-869f1xc0u** (hecha) → **RA-869d7f32r** (hecha) → **RA-869f1xc2n**.
>
> **RA-869f1xc2n → shipped (2026-09-15), PR #59.** Alta pública con ficha de cliente. `POST /auth/register`: campo **`acceptedDataProcessing`** obligatorio (400 `GEN_VALIDATION_FAILED` si falta o es `false`). Cuenta + ficha `regular`/`email` + `data_processing` en `IUnitOfWork`; tokens tras el commit. Alta social de cuenta nueva: cuenta + vínculo (`AddLoginAsync` comprobado) + ficha, **sin** consentimientos granulares. Vincular a cuenta existente no toca fichas. Migración `20260915151444_BackfillCustomerProfiles` (solo SQL, `Down()` vacío; salta colisión de email en el centro; sin consentimientos). SPA: tercer checkbox; E2E `e2e/register.spec.ts`. Unit **246/246**. E2E **57/57**. Recuento del padre: **3/8**. Siguiente: **RA-869d7f369**. **RA-869f1mqah no se cierra** (solo `CreateAsync`/`AddLoginAsync` del alta pública). Cadena: **RA-869f1xc0u** (hecha) → **RA-869d7f32r** (hecha) → **RA-869f1xc2n** (hecha).
>
> Los recuentos x/8 de estas entradas son los vigentes en su fecha. El 2026-09-15 se canceló RA-869d7f3q4 (absorbida por RA-869d7f369 y RA-869d7f3ka) y el bloque pasó a 7 subtareas.
>
> **RA-869d7f369 → shipped (2026-09-15), PR #60.** `ICustomerService` / `CustomerService` + validadores. Sin endpoints. Dual ficha (añadir ficha a cuenta existente sin invitación; 403 al cambiar email de personal; baja sin lockout). Categoría `new` por defecto (registro y alta social incluidos). `IncrementNoShowAsync` **trasladado a RA-869d7f3ka**. Promoción `new` → `regular`: **RA-869f2g02q** (Citas **RA-869d7edau**). Unit **279/279**. E2E **57/57** (reejecutados tras PR #60; Chromium, Firefox y WebKit sobre `develop`). Recuento del padre al merge: **4/8**; **el mismo día** RA-869d7f3q4 se cancela y el padre pasa a **4/7**. Siguiente: **RA-869d7f3bt**.
>
> **RA-869d7f3q4 → cancelada (2026-09-15).** «Crear sin `data_processing` falla» y «con `data_processing` persiste»: ya en PR #60 (`CustomerServiceTests`, `CustomerValidatorTests`). «`IncrementNoShowAsync` al alcanzar el umbral bloquea»: se entrega **con RA-869d7f3ka**. El bloque **RA-869d7ed68** queda en **7 subtareas**. **RA-869d7f3ka trasladada a RA-869f2gtyv el 2026-09-15.**
>
> **RA-869d7f3bt → shipped (2026-09-15), PR #61.** Endpoints `GET|POST|PUT|DELETE /api/v1/customers` y `POST …/reactivate`. Lectura Admin|Manager|Employee; escrituras Admin|Manager; Customer 403 en todo el módulo. Baja sin lockout. Unit **279/279** (sin tests de controlador). E2E **57/57**. Recuento del padre: **5/7**. Pendientes: **RA-869d7f3fw**, **RA-869d7f3ka**.
> Desde PR #62 (RA-869d7f3fw), Employee también escribe notas internas (`POST /notes`); las escrituras de ficha siguen siendo Admin|Manager. Pendientes vigentes: solo RA-869d7f3ka. **Trasladada a RA-869f2gtyv el 2026-09-15.**
>
> **RA-869d7f3fw → shipped (2026-09-15), PR #62 (alcance reducido a notas).** POST/DELETE `/api/v1/customers/{id}/notes`. Autoría: ficha Employee activa (`CustomerNotes.EmployeeId`); admin demo sin ficha y empleada de baja → 403. DELETE: autora, Admin o Manager; 404 nota/cliente/centro incorrectos; baja lógica idempotente. Unit **293/293** (`CustomerServiceTests` +9, `CustomerValidatorTests` +5). Mutación: 2 tests esperados. Runtime contra `ReservArteDB` recreada (drop → create → demo). E2E **57/57**. Recuento del padre: **6/7**. `/history` → **RA-869f2gn91** (Citas). `/payment-methods` → **RA-869f2gnbm** (Redsys). Pendiente del bloque: **RA-869d7f3ka**. **Trasladada a RA-869f2gtyv el 2026-09-15.**
>
> **RA-869d7f3ka → cancelada (2026-09-15).** El no-show lo marca **RA-869d7f4xf** — «AppointmentService: máquina de estados Pending→Confirmed→InProgress→Completed/Cancelled/NoShow» (lista Backend, padre **RA-869d7edau**, backlog, prioridad **urgent**); en Clientes no hay disparador. Continúa en **RA-869f2gtyv** (Citas **RA-869d7edau**, prioridad normal). El bloque **RA-869d7ed68** queda en **6 subtareas** (shipped).
>
> **Cierre RA-869d7ed68 → shipped 6/6 (2026-09-15), PR #63** (`CLAUDE.md` solo). Hechas: f2z5, f32r, f1xc2n, f369, f3bt, f3fw. Canceladas: f3q4, f3ka. Traslados: gn91, gnbm, gtyv. Frontend: **RA-869d7fc34**, **RA-869d7fc51**, subtareas de **RA-869d7edt7** — «Módulos Empleados, Clientes, Servicios y Dashboard (UI completa)» (lista Frontend, sin padre, backlog, prioridad high, fechas 2026-05-24 → 2026-06-05). Unit **293/293**, E2E **57/57**.
>
> **RA-869d7f3wa → shipped (2026-09-16), PR #64 (`deb39ba`).** Entidades del catálogo de Servicios en Domain. Padre **RA-869d7ed7v** («CRUD Servicios + endpoint Dashboard»): entonces **1/5**. Solo dominio, **sin migración**. 7 entidades (`OrganizationId` Guid); las cuatro hijas estrenan tenant. Catálogo `EmployeeLevels`. `ServiceDomainTests` (21). Unit entonces **314/314**. E2E **57/57** (SPA no se toca; no reejecutados). El mapeo llegó en **RA-869d7f3z0**. `data/` no cambia en ese PR. El bloque se **adelanta al de Citas** (RA-869d7edau): `AppointmentServiceItem` y `WaitingList` apuntan a Servicios; duración e importe salen del catálogo.
>
> **RA-869d7f3z0 → shipped (2026-09-16), PR #65 (`c653d24`).** Persistencia y servicio del catálogo. Recuento del padre entonces: **2/5**. Migración `20260916084021_AddServiceCatalog` (solo crea tablas; `Down()` sí las borra). Query filter en las siete. CHECKs vía `CatalogCheck`. `IServiceRepository` en `Domain/Interfaces`. `IServiceCatalogService` / `ServiceCatalogService` (sin `IUnitOfWork`). Demo 2 categorías / 3 servicios / 1 variación / 3 tarifas / 5 asignaciones / 0 paquetes. `create` regenerado. Unit **344/344**. E2E **57/57** (SPA no se toca; no reejecutados). Runtime sobre `ReservArteTestDB` (drop→create→demo; API sin migrar; `/health` 200; CHECK de `EmployeeLevel` rechaza un nivel inventado).
>
> **RA-869d7f42u → shipped (2026-09-16), PR #66 (`c01c566`).** Endpoints `/api/v1/services`. Recuento del padre **entonces: 3/5** (el denominador aún era 5). Quedaban paquetes y dashboard; las escrituras de categorías/variaciones/tarifas se dieron por incluidas en esta subtarea y **no tenían dueño**. Lectura: cualquier rol autenticado, **Customer incluido**; escrituras de servicio: Admin o Manager. Sin tests de controlador; suite entonces **344/344**. E2E **57/57** (SPA no se toca; no reejecutados). Runtime sobre `ReservArteTestDB`. Corrige el test frágil de `JwtTokenServiceTests` (cierra la Constancia). Título de **RA-869d7f3z0** ya dice `ServiceCatalogService`.
>
> **RA-869f2wtrk → shipped (2026-09-16), PR #67 (`9abae79`).** Escrituras del catálogo (categorías, variaciones, tarifas). **El padre pasa de 5 a 6 subtareas** y el recuento **entonces a 4/6**. Origen: la auditoría del PR #66 detectó que esas escrituras no tenían dueño (un prompt anterior las daba por incluidas en RA-869d7f42u; el título de f42u decía «con variaciones y categorías»). Nueve endpoints, todos Admin|Manager. El repositorio **no se toca** (upsert de tarifas por nivel). Unit entonces **352/352** (+8 `ServiceCatalogWriteValidatorTests`). E2E **57/57** (SPA no se toca; no reejecutados). Runtime sobre `ReservArteTestDB`. Completó las escrituras de categorías/variaciones/tarifas; los paquetes llegaron en **RA-869d7f45n**.
>
> **RA-869d7f45n → shipped (2026-09-16), PR #68 (`d8c23af`).** Paquetes del catálogo. Recuento del padre: **5/6**. **Solo queda RA-869d7f4b4** (dashboard). `ServicePackagesController` (`/api/v1/service-packages`) con repositorio y servicio **propios**. Seis endpoints (lista, detalle, alta, PUT que reemplaza la composición, baja, reactivar). Lectura autenticada (Customer incluido); escrituras Admin|Manager. Unit **370/370** (+18). E2E **57/57** (SPA no se toca; no reejecutados). Runtime sobre `ReservArteTestDB`. El seed demo **sigue en 0 paquetes**. **El catálogo queda completo** (capa de acceso a datos).
>
> **Advertencia — borrado físico de líneas al reemplazar (única excepción a la baja lógica del módulo):** el PUT hace `RemoveRange` de la composición, precedente de `ReplaceAvailabilitiesAsync`. Una línea de paquete no es histórico de negocio (ninguna cita apunta a ella); dejarla con `IsActive = false` acumularía filas muertas.
>
> **Advertencia — el repositorio impone paquete y tenant a cada línea.** Una petición no puede colar líneas en otro paquete ni en otra organización (test de entrada maliciosa, mismo criterio que Empleados).
>
> **Advertencia — desglose calculado, no guardado.** `totalPrice` es el importe pactado; `discountPercentage` es informativo; `itemsTotalPrice`, `savings` y `totalDurationMinutes` salen de los servicios en el momento de la consulta. **`savings` no se recorta a cero**: un paquete más caro que sus partes muestra un negativo (runtime: `-2,0`).
>
> **Advertencia — dashboard (RA-869d7f4b4) sin datos que medir.** Pide citas de hoy, ingresos del mes y próximas citas. `Appointment` **ya está mapeado** (RA-869d7f4j8); `Payment` sigue en `Ignore` y no hay servicio de citas ni datos demo. Hacerlo ahora serían ceros o métricas provisionales; rinde más **después de Citas** (`RA-869d7edau`). La decisión es del usuario. El bloque de Servicios queda **parado en 5/6**, no cerrado: se retomará el dashboard cuando Citas dé datos.
>
> **RA-869d7f4f1 → shipped (2026-09-16), PR #69 (`55feccd`).** Entidades de Citas en Domain. Se abre el bloque **RA-869d7edau** («Sistema de Citas: API completa, disponibilidad, máquina de estados y tests»): padre en `in development`, fechas 2026-09-16 → 2026-09-25, recuento **1/11**. Nació con **10** subtareas; al alinear `WaitingList` se creó **RA-869f2yh9b** (repositorio, servicio y endpoints de lista de espera) y el denominador pasó a **11**. Solo dominio, **sin migración** (mismo criterio que RA-869d7f2z5 y RA-869d7f3wa). Lo desbloqueó el catálogo: `AppointmentServiceItem` y `WaitingList` apuntan a `Services` (fuera de `Ignore` desde el PR #65). `OrganizationId` **Guid** en `Appointment` y `WaitingList`; `AppointmentServiceItem` **estrena** tenant + navegación `Organization` (RA-869f17myx). Catálogo `AppointmentStatuses` (**ocho** valores del CHECK de diseño) y `AppointmentCancelledByTypes` (`customer`, `business`); colecciones `Cancellations` y `Terminal`. Retiradas de `Appointment` las navegaciones a módulos inexistentes, **incluido `PaymentMethodId`**. Se conservan `RedsysOrderNumber` y `RedsysPreAuthToken`. Las tres **siguen en `Ignore`**. `AppointmentDomainTests` (18). Unit **388/388**. E2E **57/57** (SPA no se toca; no reejecutados). `has-pending-model-changes`: sin cambios; `data/` no cambia. **Sin runtime, a propósito.** El mapeo es **RA-869d7f4j8**. Detalle: vol. 1 **§3.1.5** / **§5.2.2**, vol. 2 **§9.9**.
>
> **RA-869d7f4j8 → shipped (2026-09-16), PR #70 (`fe6bf60`) + PR #71 (`de94fa8`).** Mapeo de Citas. Recuento del padre **RA-869d7edau:** **2/11**. Las tres salen de `Ignore`. Tablas `Appointments`, `AppointmentServiceItems`, `WaitingLists`. Migraciones `AddAppointments` y `RenameWaitingListToWaitingLists`. `WaitingList` entra en la misma migración (RA-869f2yh9b no necesitará una propia). Índice Redsys único **filtrado**. FK de cita Restrict a clienta y empleada. Entidad `WaitingList`, tabla plural. `AppointmentMappingTests` (22). Unit **410/410**. E2E **57/57** (SPA no se toca; no reejecutados). Runtime sobre base desechable. `create` regenerado; `seed_demo` no se toca. Fix `regenerate-create.sh` (`SCRIPT_TMP`). Siguiente: **RA-869d7f4n4**. Detalle: vol. 1 **§3.1.5** / **§5.2**, vol. 2 **§9.9**.
>
> **Advertencia — ocho valores en `Status` Y se mantiene `CancelledByType` (decisión del usuario):** el mismo dato vive en dos columnas y pueden contradecirse. **`Status` es la fuente de verdad** (documentado en la entidad). Imponer la coherencia al cancelar es **RA-869d7f4xf**: hoy nada impide `Status = cancelled_by_customer` con `CancelledByType = business`. Para no repetir los tres literales existe `AppointmentStatuses.Cancellations`.
>
> **Advertencia — `PaymentMethodId` se retiró con su navegación, no solo la navegación.** Es FK a `CustomerPaymentMethod`, que sigue en `Ignore` y llega con Redsys (**RA-869f2gnbm**); dejar la columna habría creado un `int?` sin FK que nadie rellena. El sketch de vol. 1 §5.2 sí la conserva: es diseño objetivo, no el estado actual.
>
> **Advertencia — subtarea nueva RA-869f2yh9b (lista de espera).** `WaitingList` se alineó aquí, pero ninguna de las 10 subtareas le daba repositorio, servicio ni endpoints: habría quedado mapeada sin capa de datos, como `ServicePackages` antes de RA-869d7f45n (y el hueco que obligó a crear RA-869f2wtrk). El bloque pasa de 10 a **11**.
>
> **Advertencia — cascadas y `WaitingList` en RA-869d7f4j8 (RESUELTO, PR #70 + #71):** las dos FK de `Appointments` son Restrict. `WaitingList` entra en `AddAppointments`. La tabla nació `WaitingList` y se renombró a `WaitingLists` en migración propia (PR #71). `regenerate-create.sh`: `--no-build` sigue vigente; además no usar variable `TMP` en Windows (`SCRIPT_TMP`).
>
> **Advertencia — baja de categoría con servicios (decidido):** se **permite**. La baja es lógica: la fila no se borra, la FK `Restrict` no interviene, ningún servicio queda sin clasificar y la categoría retirada sigue en `GET /categories` sin filtro. Runtime: tras bajar la categoría 1, los servicios 1 y 2 conservan su `categoryId`.
>
> **Advertencia — tarifas como upsert por nivel, no CRUD por id:** el nivel es la clave natural; el índice único filtrado admite una vigente por servicio y nivel. `PUT …/pricings/{level}` es idempotente y **no puede chocar con el índice** (desaparece una clase de 409). Por eso **no hizo falta tocar el repositorio** (`GetPricingAsync(serviceId, level)` ya bastaba).
>
> **Advertencia — asimetría de idempotencia:** `DELETE` de variación es idempotente (la consulta ve también las retiradas); `DELETE` de tarifa **no** lo es (404 sin vigente): el recurso *es* la tarifa vigente y una retirada ya no se direcciona por nivel. La interfaz llegó a decir «idempotente» para tarifas por error; se corrigió **antes** de implementarla.
>
> **Advertencia — duración resultante en el servicio, no en FluentValidation:** un `durationModifier` negativo es legítimo, pero no puede dejar la duración total ≤ 0 (no ocuparía hueco en la agenda). Depende del servicio; `ServiceCatalogService` lo comprueba con `field = durationModifier`.
>
> **Advertencia — nivel normalizado antes de comparar:** llega por la ruta (`SENIOR`, ` senior ` → minúsculas). Un nivel desconocido se rechaza **antes** de tocar la BD (el CHECK lo pararía igual, pero como error de infraestructura y sin campo).
>
> **Advertencia — dos escalas de «nivel» (decidido en RA-869d7f3z0):** se mantienen **independientes**. `ProficiencyLevel` (1-5) responde a *quién puede* prestar el servicio; `EmployeeLevel` (`junior`/`senior`/`expert`) a *cuánto cuesta*. No se deriva una de otra. **Queda sin regla de negocio que las relacione**: si destreza 5 debiera implicar tarifa `expert`, hay que introducirla explícitamente.
>
> **Advertencia — `dotnet format` (RESUELTO, RA-869f2pjf8, PR #72 + #73, 2026-09-16).** Estaba abierta al cerrar RA-869d7f3wa (PR #64): `--verify-no-changes` salía con código **2** (**101** avisos entonces; **113** tras el mapeo de Citas). **Camino 1** (alinear el espaciado), no el 2 (retirarlo como puerta). PR #72: 15 ficheros, solo espacios; 113 → **0**, código **0**. PR #73: `.editorconfig` en la raíz; al aplicarlo salieron 74 desviaciones (61 sin salto de línea final, 9 `using System…` desordenados, 4 `Class1.cs` con BOM) y se corrigieron. Tarea en ClickUp **`done`** (lista Infra: `backlog` → `in progress` → `blocked` → `done` → `cancelled`; no usa `shipped`). Línea base **CERO**. Detalle: vol. 2 **§9.10**, estrategia de testing.
>
> **Advertencia — `Class1.cs` del scaffolding (sin tarea):** los cuatro `ReservArte-*/Class1.cs` son los vacíos de `dotnet new classlib` (4 líneas, sin uso). El PR #73 solo les quitó el BOM. Borrarlos sería limpieza razonable; no hay ID de ClickUp.
>
> **Advertencia — `regenerate-create.sh`:** usa `--no-build`; si se ejecuta sin compilar antes, regenera un `create` **sin la migración nueva** y aun así informa de éxito. En RA-869d7f3z0 la primera pasada dejó `BackfillCustomerProfiles` como última. No hay tarea ClickUp.
>
> **Advertencia — el seed SQL no lo cubre la batería:** el `INSERT` de `EmployeeServices` del `seed_demo` tenía 5 columnas y 4 valores. La batería unitaria no ejecuta los scripts; solo apareció al sembrar `ReservArteTestDB`. Causa: las columnas **no llevan `DEFAULT` en BD** (los pone la entidad), así que el SQL debe dar `IsActive` explícito. Refuerza verificar con los scripts siempre que haya migración.
>
> **Advertencia — longitudes sin respaldo del sketch:** vol. 1 §5.2 usaba `NVARCHAR(MAX)` y no detallaba el resto. Se ha acotado: nombre 200, descripción 1000 (categoría 500, variación 100), URL 500, color 20, nivel 20, `decimal(10,2)` importes, `decimal(5,2)` descuento. Cambiar cualquiera exige migración; el producto no lo ha confirmado.
>
> **Advertencia — decisión de roles reversible en una línea:** lectura abierta a Customer (el catálogo no es dato personal). Si el producto prefiere el criterio conservador (Customer → 403 en todo el módulo), basta `[Authorize(Roles = StaffRoles)]` en la clase. Primera excepción al backoffice.
>
> **Advertencia — cuarta réplica de helpers de controlador (histórica, PR #66):** `ValidateAsync`, `FromFailure` y `ToCamelCase` estaban en Auth, Empleados, Clientes y Servicios.
>
> **Advertencia — quinta réplica (PR #68):** las mismas helpers van ya en **Paquetes**. Unificación **RA-869f17y6k**; cada módulo nuevo encarece el coste.
>
> **Suciedad de ClickUp (no tocada):** sigue viva **RA-869d7edt7** (backlog, fechas pasadas). El título de **RA-869d7f3z0** ya nombra `ServiceCatalogService`. En el árbol de `Análisis de pantallas y estructura.md`, `IServiceRepository.cs` ya no es `# futuro` (commit `d07d9c7`); la separación completa entre árbol actual y objetivo sigue en **RA-869f2g60e**.
>
> **Alta en backlog (Docs, prioridad baja): RA-869f2g60e** — separar estructura actual y objetivo en el árbol de `Análisis de pantallas y estructura.md`. No bloquea.
>
> **Alta en backlog: RA-869f18uta** — E2E de integración del flujo completo forgot → email → reset con backend real (hoy solo runtime manual + spec que intercepta el POST). Alternativa más ligera: tests de integración .NET con `WebApplicationFactory` (cubren backend, no la SPA). Se cruza con **RA-869eqxm7w** (E2E en CI): ambos necesitan API y BD en el runner.
>
> **Alta en backlog: RA-869f2gh37** — Tests de integración HTTP de la API con `WebApplicationFactory` (roles, envelope y contrato de Empleados, Clientes, Servicios y **Paquetes**). Lista Backend, prioridad normal. Origen: advertencia de PR #61; los PR #66, #67 y #68 reinciden (sin tests de controlador). Relacionada con **RA-869f18uta** y **RA-869eqxm7w**.
>
> **Alta en backlog: RA-869f2gtz8** — Diseñar e implementar AuditLog transversal (quién, qué, cuándo, entidad, organización). Lista Backend, prioridad baja. Origen: RA-869d7f3ka pedía `AuditLog`, que no existe; se diseña aparte para todo el sistema. Pendiente de decidir: esquema (`OrganizationId` Guid, query filter), mecanismo (servicios o interceptor de `SaveChanges`), qué se audita y retención/acceso (RGPD).
- ✅ CRUD de clientes (backend) — bloque **RA-869d7ed68: 6/6 shipped** (2026-09-15)
  - El recuento **6/6 cuenta las subtareas de ClickUp de RA-869d7ed68** (dos canceladas: RA-869d7f3q4, RA-869d7f3ka). Las viñetas de producto (formularios, lista con filtros) describen frontend, no el denominador.
  - **Entidades Domain (RA-869d7f2z5, 2026-09-14)** — **shipped** (PR #56). PK compartida, `OrganizationId` Guid, catálogos snake_case, sin `Rol`/`MarketingConsent` en ficha. Dual ficha empleada+clienta: **RA-869d7f369** (**shipped**, PR #60). Detalle: vol. 1 **§3.1.3**, vol. 2 **§9.7**.
  - **Esquema + repositorio (RA-869d7f32r, 2026-09-15)** — **shipped** (PR #58). Migración `AddCustomers`; `ICustomerRepository`; query filters; demo Carmen/Sofía. `CustomerPaymentMethod` sigue en `Ignore` (**RA-869f2gnbm**; antes RA-869d7f3fw).
  - **Alta pública con ficha (RA-869f1xc2n, 2026-09-15)** — **shipped** (PR #59). Registro local: cuenta + ficha `regular` + `data_processing` (**desde RA-869d7f369** nace `new`). Alta social: ficha sin consentimientos granulares. Migración `BackfillCustomerProfiles`. Checkbox `acceptedDataProcessing` en la SPA.
  - **Servicio + validadores (RA-869d7f369, 2026-09-15)** — **shipped** (PR #60). `GetPagedAsync` / perfil / alta / edición / baja-reactivación. Sin HTTP en ese PR.
  - **API endpoints (RA-869d7f3bt, 2026-09-15)** — **shipped** (PR #61). GET/POST/PUT/DELETE + reactivate. Lectura para personal; escrituras Admin|Manager. Frontend **no**.
  - **Notas internas (RA-869d7f3fw, 2026-09-15)** — **shipped** (PR #62, alcance reducido). POST/DELETE `…/notes`. Autoría: ficha Employee activa. Frontend **no**.
  - Formularios de creación/edición — **no empezado** — **RA-869d7fc51** (`CustomerForm.vue` con checkboxes RGPD + `AddPaymentMethodModal.vue` placeholder Redsys; subtarea de **RA-869d7edt7**)
  - Lista y detalle — **backend shipped**; frontend **RA-869d7fc34** (`CustomersPage.vue` + `CustomerDetailPage.vue`; subtarea de **RA-869d7edt7**)
  - Sistema de categorías (VIP, Regular, etc.) — entidad sí; asignación `new` **hecha** (RA-869d7f369); promoción `new` → `regular` **RA-869f2g02q**
  - **RA-869d7f3ka** — **cancelada** (2026-09-15) → **RA-869f2gtyv** (Citas)
  - Historial de citas del cliente (`GET …/history`) — **RA-869f2gn91** (Citas; no cuenta en el 6)
  - `CustomerPaymentMethod` y `/payment-methods` — **RA-869f2gnbm** (Redsys; no cuenta en el 6)
  - **RA-869d7f3q4** — **cancelada** (2026-09-15); consentimiento absorbido en PR #60; no-shows en RA-869d7f3ka y luego **RA-869f2gtyv**
- ⏳ Validaciones y manejo de errores — **parcial**, no cierre de sprint
  - FluentValidation en backend — **sí** en auth, empleados, clientes (servicio y API) y **catálogo de servicios** (validadores de RA-869d7f3z0); **no** en citas
  - Zod en frontend — **sí** en pantallas de auth; **no** en maestros
  - Mensajes de error consistentes — envelope en API de auth/empleados/clientes/servicios/paquetes; 400 ProblemDetails **RA-869f1k17q**

**Semana 7-8:**

- ⏳ CRUD de servicios — bloque **RA-869d7ed7v: 5/6 parado** (catálogo completo; 2026-09-16; no cerrado)
  - **Entidades Domain (RA-869d7f3wa, 2026-09-16)** — **shipped** (PR #64). 7 entidades, `OrganizationId` Guid, catálogo `EmployeeLevels`. Detalle: vol. 1 **§3.1.4**, vol. 2 **§9.8**.
  - **Esquema + repositorio + servicio (RA-869d7f3z0, 2026-09-16)** — **shipped** (PR #65). Migración `AddServiceCatalog`; query filters. Paquetes: tablas en esa migración; capa de acceso en **RA-869d7f45n**.
  - **API de servicios (RA-869d7f42u, 2026-09-16)** — **shipped** (PR #66). `ServicesController`. Lectura autenticada (Customer incluido); escrituras de servicio Admin|Manager. `GET /categories` (todas por defecto). Sin tests de controlador (**RA-869f2gh37**).
  - **Escrituras del catálogo (RA-869f2wtrk, 2026-09-16)** — **shipped** (PR #67). Categorías, variaciones y tarifas (9 endpoints, Admin|Manager). Alta de categoría con `Location` a la lista. Tarifas: upsert por nivel. El denominador del bloque pasa de 5 a 6.
  - **Paquetes (RA-869d7f45n, 2026-09-16)** — **shipped** (PR #68). `ServicePackagesController` (`/api/v1/service-packages`). Repositorio y servicio propios. PUT reemplaza la composición (borrado físico de líneas). Seed demo: **0 paquetes**.
  - Formularios con precios y duración — **no empezado**
  - Categorías de servicios — entidad, lectura y **escritura** sí; UI **no**
  - Gestión de variaciones — entidad, repositorio y **escritura** sí; UI **no**
  - Dashboard — **RA-869d7f4b4** (único pendiente; necesita datos de citas)
- ⏳ Horarios de empleados — **backend de persistencia shipped** (RA-869d7f01b); **frontend no**; cálculo horario−ausencias **RA-869d7f4rd**
  - Disponibilidad semanal recurrente
  - Excepciones (vacaciones, bajas)
  - Validación de solapamientos
- ⏳ Dashboard con métricas básicas — **no empezado** (ruta stub)
  - Citas del día
  - Ingresos del mes
  - Clientes totales
  - Servicios más solicitados

**Entregables Sprint 3-4:**

- Gestión de maestros: **empleados backend 10/10**; **clientes backend 6/6 shipped** (frontend **RA-869d7fc34**, **RA-869d7fc51**); **servicios 5/6 parado** (catálogo completo, PRs #64–#68; no cerrado). Queda el dashboard (**RA-869d7f4b4**), a retomar cuando Citas dé datos. UI de empleados/clientes/servicios **no**.
- ⏳ Posibilidad de configurar el centro completamente — **no** (configuración en `Ignore`)
- ⏳ Dashboard operativo con datos en tiempo real — **no** (placeholder)
- ⏳ Testing unitario de endpoints críticos — **sí** empleados + auth (incl. alta pública, `PublicSignupCustomerTests`); **sí** clientes servicio (`CustomerServiceTests`, notas PR #62); clientes API **verificada en runtime** (PR #61 y #62), **sin tests de controlador** (**RA-869f2gh37**: integración HTTP con `WebApplicationFactory`; se cruza con **RA-869f18uta** y **RA-869eqxm7w**); **sí** dominio de servicios (`ServiceDomainTests`, PR #64) y persistencia/servicio (`ServiceRepositoryTests`, `ServiceValidatorTests`, `ServiceCatalogProfileTests`, PR #65); API de servicios **verificada en runtime** (PR #66); escrituras de catálogo **verificadas en runtime** (PR #67) + `ServiceCatalogWriteValidatorTests`; paquetes **verificados en runtime** (PR #68) + `ServicePackageRepositoryTests` / `ServicePackageValidatorTests`; **sí** dominio de citas (`AppointmentDomainTests`, PR #69) y mapeo (`AppointmentMappingTests`, PR #70 + #71); **sin tests de controlador** (**RA-869f2gh37**); **no** API de citas / pagos. Repositorio de clientes: **sí** (`CustomerRepositoryTests`). Test de bloqueo por no-shows con **RA-869f2gtyv**.

---

> **Lectura del roadmap (2026-09-14; actualizado 2026-09-16):** a partir de **Sprints 5-6**, las casillas son **alcance previsto**, no estado de implementación (convertidas a ⏳), **salvo** el dominio y el mapeo de Citas (**RA-869d7f4f1** + **RA-869d7f4j8**; tablas `Appointments` / `AppointmentServiceItems` / `WaitingLists`). **Fase 2+ (Sprints 9 en adelante)** usa ⬜ (no empezado), salvo los ítems parciales anotados: **tampoco** significa hecho. Pagos, recordatorios, móvil y el resto de entidades de negocio (salvo las cuatro tablas de Clientes, las siete del catálogo de Servicios y las tres de Citas ya mapeadas) siguen en `Ignore`. Lo hecho de verdad: Sprints 1-4 (auth, UI auth, empleados backend, **CRUD Clientes backend 6/6**, **catálogo Servicios 5/6 parado**, no cerrado) y el arranque de Citas **2/11**. El dashboard de Servicios (**RA-869d7f4b4**) se retomará cuando Citas dé datos.

**Sprints 5-6 (Mes 3): Sistema de Citas (Core del Sistema)**

> **Bloque Citas (RA-869d7edau) — «Sistema de Citas: API completa, disponibilidad, máquina de estados y tests».** Padre en `in development` (2026-09-16 → 2026-09-25). Recuento **2/11** (PR #70 + #71). Nació con 10 subtareas; **RA-869f2yh9b** (lista de espera) sube el denominador a 11. **Shipped:** **RA-869d7f4f1** (entidades Domain, PR #69) y **RA-869d7f4j8** (mapeo, PR #70 + #71). **Siguiente:** **RA-869d7f4n4** (repositorio de citas). En backlog, entre otras: **RA-869d7f4xf** — «AppointmentService: máquina de estados Pending→Confirmed→InProgress→Completed/Cancelled/NoShow» (prioridad **urgent**; impone la coherencia `Status`/`CancelledByType`); **RA-869f2yh9b** — repositorio, servicio y endpoints de lista de espera (**sin** migración propia); **RA-869f2g02q** — promoción `new` → `regular` tras citas completadas (prioridad normal); **RA-869f2gn91** — `GET /api/v1/customers/{id}/history`; **RA-869f2gtyv** — no-shows y `OrganizationSettings` (prioridad **normal**; absorbida de RA-869d7f3ka). Ninguna de las trasladadas desde Clientes forma parte del 6 de Clientes.

**Semana 9-10:**

- ⏳ Modelo de datos de citas — bloque **RA-869d7edau: 2/11** (2026-09-16)
  - **Entidades Domain (RA-869d7f4f1, 2026-09-16)** — **shipped** (PR #69). `Appointment`, `AppointmentServiceItem`, `WaitingList`; `OrganizationId` Guid; catálogo de 8 estados. Detalle: vol. 1 **§3.1.5**, vol. 2 **§9.9**.
  - **Migraciones BD (RA-869d7f4j8, 2026-09-16)** — **shipped** (PR #70 + #71). Query filter; FK Restrict; tabla `WaitingLists`.
  - Repositorios — **RA-869d7f4n4** (siguiente); lista de espera: **RA-869f2yh9b**
  - **RA-869f2g02q** (backlog, prioridad normal): promoción `Customer.Category` `new` → `regular` tras citas completadas
- ⏳ API de citas
  - CRUD completo
  - Validación de disponibilidad
  - Asignación de empleado y servicio
  - Estados de cita — **RA-869d7f4xf** (máquina de estados; dispara el no-show hacia **RA-869f2gtyv**)
  - **RA-869f2gn91** (backlog): `GET /api/v1/customers/{id}/history` — historial de citas del cliente (paginado); roles de lectura de Clientes
  - **RA-869f2gtyv** (backlog): contador de no-shows y bloqueo automático (`OrganizationSettings.MaxNoShowsBeforeBlock`)
- ⏳ Calendario visual (FullCalendar)
  - Vista diaria/semanal/mensual
  - Drag & drop para reorganizar
  - Código de colores
  - Modal de detalles de cita

**Semana 11-12:**

- ⏳ Crear cita (modo interno - personal)
  - Wizard paso a paso
  - Selección de cliente
  - Selección de servicio(s)
  - Selección de empleado (o auto)
  - Selección de fecha/hora
  - Confirmación
- ⏳ Validaciones de disponibilidad
  - Horarios de empleado
  - Solapamiento de citas
  - Horarios de operación
  - Tiempo suficiente para servicio
- ⏳ Notificaciones básicas por email
  - Confirmación de cita creada
  - Template HTML responsive
  - Integración con Amazon SES

**Entregables Sprint 5-6:**

- ⏳ Sistema de citas funcional
- ⏳ Agenda visual interactiva y profesional
- ⏳ Personal puede crear y gestionar citas
- ⏳ Emails transaccionales funcionando
- ⏳ Testing de flujos críticos

---

**Sprints 7-8 (Mes 4): Pagos y Finalización MVP**

> **Bloque Redsys (RA-869d7eden) — «Integración Redsys InSite: pagos, pre-auth, tokenización COF y webhook»** (lista Backend, backlog). Subtarea en backlog: **RA-869f2gnbm** — mapear `CustomerPaymentMethod` (`OrganizationId` Guid, query filter, retirar navegaciones a `Appointment`/`Payment`, migración y `create` regenerado) y endpoints `GET/POST/DELETE /api/v1/customers/{id}/payment-methods`. El POST registra tarjeta solo **tras tokenización verificada en servidor** (nunca un token de la SPA sin verificar) y exige consentimiento `saved_cards`; depende de **RA-869d7f5gx** — «SaveCustomerPaymentMethodAsync: persistir token + CofTxnid + PayWithSavedCardAsync (COF_INI=N)» (subtarea de RA-869d7eden, backlog). Roles por decidir al implementar. Recoge los puntos abiertos de RA-869d7f2z5. Trasladado desde el alcance original de RA-869d7f3fw el 2026-09-15.

**Semana 13-14:**

- ⏳ Integración con Redsys InSite
  - Configuración de cuenta Redsys (test)
  - SDK JavaScript en frontend
  - Servicio de pagos en backend
  - Pre-autorizaciones
  - Captura de pagos
  - Cancelación de pre-autorizaciones
- ⏳ Guardado de tarjetas (tokenización)
  - Flujo de primera transacción con COF
  - Almacenamiento de tokens
  - Gestión de tarjetas guardadas — **RA-869f2gnbm** (backlog; mapeo + endpoints)
  - Pago con tarjeta guardada
- ⏳ Gestión de cancelaciones
  - Política de penalización configurable
  - Cálculo automático de penalización
  - Captura parcial en cancelación tardía
  - Liberación en cancelación a tiempo

**Semana 15-16:**

- ⏳ Sistema de recordatorios
  - Configuración de recordatorios
  - Jobs programados con Hangfire
  - Recordatorios por email
  - Template de recordatorio HTML
  - Enlaces de confirmación/cancelación
- ⏳ Testing end-to-end
  - Flujo completo de reserva
  - Flujo de pago con Redsys (test)
  - Flujo de cancelación con penalización
  - Recordatorios automáticos
- ⏳ Documentación
  - Manual de usuario (personal del centro)
  - Documentación técnica (API)
  - Guía de despliegue
  - Troubleshooting común

**Entregables Sprint 7-8:**

- ⏳ MVP completo y funcional
- ⏳ Sistema de pagos con Redsys operativo
- ⏳ Pre-autorizaciones y penalizaciones funcionando
- ⏳ Recordatorios automáticos por email
- ⏳ Aplicación desplegada en producción (cliente piloto)
- ⏳ Documentación completa para uso y mantenimiento

---

**🎯 HITO 1: MVP EN PRODUCCIÓN**

- **Fecha objetivo:** Fin de Mes 4
- **Criterio de éxito:**
  - ⏳ Centro piloto usando la aplicación diariamente
  - ⏳ 50+ citas gestionadas sin incidencias críticas
  - ⏳ Sistema de pagos Redsys funcionando correctamente
  - ⏳ 0 violaciones de seguridad
  - ⏳ Uptime > 99%
  - ⏳ NPS (Net Promoter Score) > 7/10 del cliente piloto

---



#### FASE 2: Mejoras y Aplicación Móvil (2-3 meses)

**Objetivo:** Añadir funcionalidades avanzadas y crear apps móviles

---

**Sprints 9-10 (Mes 5): Funcionalidades Avanzadas Web**

**Semana 17-18:**

- ⬜ Reserva pública (clientes)
  - Landing page de reserva
  - Wizard de reserva simplificado
  - Registro/login de cliente
  - Pago con Redsys InSite
  - Confirmación por email
- ⬜ Configuración avanzada
  - Modo público/privado
  - Restricciones de clientes
  - Aprobación manual
  - Lista blanca
- ⬜ Lista de espera
  - Apuntarse a lista de espera
  - Notificación cuando se libera hueco
  - Prioridad por categoría de cliente

**Semana 19-20:**

- ⬜ Cupones y descuentos
  - Creación de cupones
  - Código promocional
  - Validez temporal
  - Límite de usos
  - Aplicación en reserva
- ⬜ Programa de fidelización
  - Acumulación de puntos
  - Reglas de puntos por servicio
  - Canje de puntos por descuentos
  - Historial de puntos
- ⬜ Fotografías antes/después
  - Subida a **Cloudinary**
  - Asociación a cita
  - Galería privada del cliente
  - Marca de agua
  - Expiración automática (RGPD)

**Entregables Sprint 9-10:**

- ⬜ Booking público funcional
- ⬜ Sistema de fidelización operativo
- ⬜ Gestión de fotografías implementada
- ⬜ Clientes pueden reservar por su cuenta
- ⬜ Configuración avanzada para cada organización

---

**Sprints 11-14 (Mes 6-7): Aplicación Móvil**

**Semana 21-22: Setup y Pantallas Cliente (Parte 1)**

- ⬜ Setup React Native
  - Crear proyecto con TypeScript
  - Configurar React Navigation
  - Setup de Zustand
  - Integrar React Native Paper
  - Configurar API client
- ⬜ Autenticación móvil
  - Login/Registro (local y **Sign in with Apple** / **Google Sign-In** / **Instagram (Meta SDK o web OAuth)** según plataforma, mismo backend emisor de JWT)
  - **2FA** con misma semántica que web (TOTP tras login parcial si está activo)
  - JWT handling
  - Refresh tokens
  - Biometría (FaceID/TouchID)

**Semana 23-24: Pantallas Cliente (Parte 2)**

- ⬜ Pantallas principales
  - Home con servicios destacados
  - Catálogo completo de servicios
  - Detalle de servicio
  - Wizard de reserva
  - Pago (WebView Redsys InSite)
- ⬜ Gestión de perfil
  - Ver/editar datos personales
  - Gestionar tarjetas guardadas
  - Preferencias de notificaciones
  - Historial de citas

**Semana 25-26: Pantallas Personal**

- ⬜ App para empleados
  - Agenda del día
  - Detalle de cita
  - Check-in de cliente
  - Marcar como completado
  - Ver perfil de cliente
  - Registrar pago en efectivo
- ⬜ Notificaciones push
  - Integración Firebase Cloud Messaging
  - Notificaciones de nuevas citas
  - Recordatorios personalizados
  - Deep linking a pantallas

**Semana 27-28: Testing y Publicación**

- ⬜ Testing en dispositivos
  - iOS (iPhone 12+, iPad)
  - Android (varios fabricantes)
  - Diferentes tamaños de pantalla
- ⬜ Beta testing
  - TestFlight (iOS)
  - Google Play Console (Android Beta)
  - Feedback de 10-20 usuarios beta
- ⬜ Publicación en stores
  - App Store (iOS)
  - Google Play (Android)
  - Screenshots y descripción
  - Video preview

**Entregables Sprint 11-14:**

- ⬜ Apps móviles iOS y Android publicadas
- ⬜ Paridad de funcionalidades con web
- ⬜ Push notifications funcionando
- ⬜ Integración con Redsys en WebView
- ⬜ 50+ descargas y valoración > 4.0/5 en stores

---

**🎯 HITO 2: APLICACIÓN COMPLETA**

- **Fecha objetivo:** Fin de Mes 7
- **Criterio de éxito:**
  - ⬜ Apps móviles publicadas y disponibles
  - ⬜ 100+ clientes del centro usando la app
  - ⬜ 40%+ de citas reservadas vía app móvil
  - ⬜ Valoración promedio > 4.0/5 en stores
  - ⬜ < 1% crash rate

---



#### FASE 3: Multi-Tenant y SaaS (2 meses)

**Objetivo:** Convertir en plataforma SaaS lista para reventa

---

**Sprints 15-16 (Mes 8): Multi-Tenant**

**Semana 29-30:**

- ⏳ Arquitectura multi-tenant — **parcial.** Base: `TenantMiddleware` (cabecera / subdominio), query filters globales (RA-869f17vet), email único por organización (RA-869f1xc0u), tests de aislamiento en unit. Sin onboarding, registro de organizaciones ni subdominios en producción.
  - Aislamiento de datos por OrganizationId
  - Query filters globales en EF Core
  - Tenant resolution middleware
  - Testing de aislamiento exhaustivo
- ⬜ Página de registro de organizaciones
  - Landing page pública
  - Formulario de registro
  - Verificación de email
  - Configuración de Redsys por organización
  - Subdominio personalizado

**Semana 31-32:**

- ⬜ Onboarding wizard
  - Paso 1: Datos de negocio
  - Paso 2: Configuración de horarios
  - Paso 3: Primer empleado
  - Paso 4: Primer servicio
  - Paso 5: Configuración de pagos (Redsys)
  - Paso 6: ¡Listo para usar!
- ⬜ Gestión de subdominios
  - DNS wildcard en Route 53
  - Certificados SSL dinámicos
  - Resolución de tenant por subdomain
- ⏳ Testing de aislamiento — **parcial.** Unitarios de aislamiento (query filters, tenant, email por org). Pendientes de este ítem: integration tests, pentest y verificación exhaustiva Org A vs Org B.
  - Unit tests
  - Integration tests
  - Penetration testing básico
  - Verificar que Org A no puede acceder a datos de Org B

**Entregables Sprint 15-16:**

- ⬜ Sistema multi-tenant operativo
- ⬜ Proceso de onboarding fluido y profesional
- ⬜ 5-10 organizaciones de prueba activas
- ⬜ Aislamiento de datos verificado
- ⬜ Subdominios personalizados funcionando

---

**Sprints 17-18 (Mes 9): Monetización y Facturación**

**Semana 33-34:**

- ⬜ Planes de suscripción
  - Definir 4 planes (Básico/Pro/Premium/Enterprise)
  - Límites por plan
  - Features por plan
  - Página de pricing
- ⬜ Gestión de suscripciones
  - Crear suscripción al registrarse
  - Pagos recurrentes con Redsys
  - Upgrade/downgrade de plan
  - Cancelación de suscripción
  - Período de prueba (14 días)

**Semana 35-36:**

- ⬜ Dashboard de administrador SaaS
  - Métricas de negocio
    - MRR (Monthly Recurring Revenue)
    - Churn rate
    - New signups
    - Active organizations
  - Gestión de organizaciones
    - Lista de todas las orgs
    - Cambiar plan manualmente
    - Suspender/reactivar
    - Ver logs y actividad
- ⬜ Facturación automática
  - Generación de facturas (FUTURO - marcado)
  - Envío automático por email (FUTURO - marcado)
  - Descarga en PDF (FUTURO - marcado)
- ⬜ Análisis y reportes
  - Cohort analysis (FUTURO - marcado)
  - Customer lifetime value (FUTURO - marcado)
  - Funnel de conversión (FUTURO - marcado)

**Entregables Sprint 17-18:**

- ⬜ Modelo SaaS completamente funcional
- ⬜ Sistema de suscripciones operativo con Redsys
- ⬜ Dashboard de administración SaaS
- ⬜ Proceso de pago recurrente automatizado
- ⬜ 15+ organizaciones de pago activas

---

**🎯 HITO 3: LANZAMIENTO SAAS**

- **Fecha objetivo:** Fin de Mes 9
- **Criterio de éxito:**
  - ⬜ 20+ organizaciones de pago usando la plataforma
  - ⬜ MRR > €1,500/mes
  - ⬜ Churn < 10%/mes
  - ⬜ Tiempo de onboarding < 20 minutos
  - ⬜ Satisfacción del cliente (NPS) > 8/10

---



#### FASE 4: Optimización y Escalado (Continuo)

**Objetivo:** Mejorar, escalar y añadir features avanzados

**Sprints 19+ (Mes 10 en adelante):**

**Prioridad Alta:**

- ⬜ WhatsApp Business API
  - Integración con 360dialog o Twilio
  - Recordatorios por WhatsApp
  - Templates aprobados por Meta
  - Opt-in/opt-out management
- ⬜ Integraciones externas
  - Google Calendar (sincronización bidireccional)
  - Apple Calendar
  - Outlook Calendar
  - Zapier webhooks
- ⏳ Multi-idioma (fase de contenidos e idiomas adicionales) — **parcial.** Solo i18n en español de la fase 1; idiomas adicionales y detección automática no.
  - **Ya en Sprint 1:** arquitectura **vue-i18n v9**, convención de claves, **español** como único locale activo en MVP, ficheros bajo `src/locales/` (véase `Documentation/Project-Init/Scripts de instalación.md` y `[accessibility-and-i18n.md](accessibility-and-i18n.md)`)
  - **Fase 4 (esta entrega):** ficheros de traducción para **inglés, francés y portugués**, contenidos de UI y mensajes de negocio migrados o ampliados, e **implementación de detección automática de idioma** (cabecera HTTP, `Accept-Language`, preferencia de usuario o equivalente acordado)

**Prioridad Media:**

- ⬜ Gestión de múltiples locales
  - Una organización puede tener varios locales
  - Empleados por local
  - Transferencia de citas entre locales
- ⬜ Analytics avanzado (marcar como FUTURO inicialmente)
  - Dashboard de BI
  - Reportes personalizables
  - Exportación a Excel/PDF
  - Gráficos interactivos
- ⬜ Marketplace de integraciones
  - SDK para desarrolladores externos
  - Documentación de API pública
  - OAuth2 para **apps de terceros** (clientes de API / integradores; distinto del **login social** de usuarios —Google, Apple, Instagram/Meta— con JWT descrito en el **volumen de análisis**)

**Prioridad Baja / Experimental:**

- ⬜ Inteligencia artificial
  - Recomendación de horarios óptimos (ML)
  - Predicción de no-shows
  - Chatbot de atención al cliente (GPT)
  - Análisis de sentimiento en comentarios
- ⬜ Funcionalidades avanzadas
  - Video llamadas para consultas virtuales
  - Programa de referidos
  - Sistema de reseñas y valoraciones público
  - Integración con redes sociales

**Entregables continuos:**

- ⬜ Mejoras de rendimiento
- ⬜ Nuevas features basadas en feedback
- ⬜ Escalado de infraestructura según necesidad
- ⬜ Optimización de costos AWS

---



### 10.3 Cronograma Visual

```
MES 1-2: FUNDACIÓN + GESTIÓN BÁSICA
├─ Sprint 1-2: Setup + Auth + Infraestructura + i18n (vue-i18n, ES) + utilidades fecha/moneda
└─ Sprint 3-4: CRUD Maestros (Empleados, Clientes, Servicios)

MES 3: SISTEMA DE CITAS (CORE)
└─ Sprint 5-6: Agenda + Crear Citas + Validaciones

MES 4: PAGOS + MVP
└─ Sprint 7-8: Redsys + Pre-auth + Recordatorios Email
   └─ 🎯 HITO 1: MVP EN PRODUCCIÓN

MES 5: FUNCIONALIDADES AVANZADAS
└─ Sprint 9-10: Booking Público + Fidelización + Fotos

MES 6-7: APLICACIÓN MÓVIL
├─ Sprint 11-12: App React Native - Cliente
└─ Sprint 13-14: App React Native - Personal + Testing + Publicación
   └─ 🎯 HITO 2: APP MÓVIL PUBLICADA

MES 8: MULTI-TENANT
└─ Sprint 15-16: Onboarding + Subdominios + Aislamiento

MES 9: MONETIZACIÓN SAAS
└─ Sprint 17-18: Suscripciones + Facturación Automática con Redsys
   └─ 🎯 HITO 3: LANZAMIENTO SAAS

MES 10+: OPTIMIZACIÓN CONTINUA
└─ WhatsApp + IA + Integraciones + locales EN/FR/PT + detección automática de idioma
```

---



### 10.4 Equipo Requerido



#### Para MVP (Fase 1 - 4 meses)


| Rol                                 | Dedicación | Responsabilidades                                 |
| ----------------------------------- | ---------- | ------------------------------------------------- |
| **Backend Developer (.NET/C#)**     | 100%       | API, BD, Integración Redsys, Servicios            |
| **Frontend Developer (Vue 3/Vite)** | 100%       | UI/UX web, Integración Redsys InSite, Componentes |
| **Full-Stack Developer**            | 50%        | Apoyo backend y frontend, Code review             |
| **DevOps/Infra (AWS)**              | 25%        | Infraestructura, CI/CD, Monitoring                |
| **UI/UX Designer**                  | 25%        | Diseños, Wireframes, Prototipos                   |


**Total personas equivalentes:** ~3.5 FTE

---



#### Para Fase 2 (Apps Móviles - 3 meses)


| Rol                                 | Dedicación | Responsabilidades                   |
| ----------------------------------- | ---------- | ----------------------------------- |
| **Backend Developer**               | 75%        | APIs para móvil, Push notifications |
| **Frontend Web Developer**          | 50%        | Mantenimiento y bugs                |
| **Mobile Developer (React Native)** | 100%       | Apps iOS/Android                    |
| **Full-Stack Developer**            | 50%        | Apoyo general                       |
| **QA/Tester**                       | 50%        | Testing manual y automatizado       |
| **DevOps**                          | 25%        | Infraestructura y despliegues       |


**Total personas equivalentes:** ~3.5 FTE

---



#### Para Fase 3 (SaaS - 2 meses)


| Rol                        | Dedicación | Responsabilidades            |
| -------------------------- | ---------- | ---------------------------- |
| **Backend Developer**      | 100%       | Multi-tenancy, Suscripciones |
| **Frontend Web Developer** | 75%        | Dashboard admin, Onboarding  |
| **Mobile Developer**       | 25%        | Actualizaciones necesarias   |
| **Full-Stack Developer**   | 50%        | Apoyo y testing              |
| **DevOps**                 | 50%        | Escalabilidad, Subdominios   |


**Total personas equivalentes:** ~3.0 FTE

---



#### Roles Adicionales (Externo/Consultivo)

- **Asesor Legal RGPD/LOPD:** Consultoría puntual
- **Contador/Fiscalista:** Para facturación y fiscalidad
- **Product Manager:** El cliente puede asumir este rol
- **Marketing/Growth:** Para lanzamiento SaaS (Fase 3+)

---



## 11. ESTIMACIÓN DE COSTOS



### 11.1 Costos de Desarrollo (Recursos Humanos)



#### Fase 1: MVP (4 meses)


| Rol                             | Horas                 | Tarifa/h | Subtotal    |
| ------------------------------- | --------------------- | -------- | ----------- |
| Backend Developer (.NET)        | 640h (4 meses × 160h) | €40/h    | €25,600     |
| Frontend Developer (Vue 3/Vite) | 640h                  | €40/h    | €25,600     |
| Full-Stack Developer            | 320h (50% × 4 meses)  | €40/h    | €12,800     |
| DevOps (AWS)                    | 160h (25% × 4 meses)  | €50/h    | €8,000      |
| UI/UX Designer                  | 160h (25% × 4 meses)  | €35/h    | €5,600      |
| **SUBTOTAL FASE 1**             |                       |          | **€77,600** |


**Con margen de contingencia (+15%):** **€89,240**

---



#### Fase 2: Mejoras + App Móvil (3 meses)


| Rol                             | Horas                 | Tarifa/h | Subtotal    |
| ------------------------------- | --------------------- | -------- | ----------- |
| Backend Developer               | 360h (75% × 3 meses)  | €40/h    | €14,400     |
| Frontend Web Developer          | 240h (50% × 3 meses)  | €40/h    | €9,600      |
| Mobile Developer (React Native) | 480h (100% × 3 meses) | €40/h    | €19,200     |
| Full-Stack Developer            | 240h (50% × 3 meses)  | €40/h    | €9,600      |
| QA/Tester                       | 240h (50% × 3 meses)  | €30/h    | €7,200      |
| DevOps                          | 120h (25% × 3 meses)  | €50/h    | €6,000      |
| **SUBTOTAL FASE 2**             |                       |          | **€66,000** |


**Con margen de contingencia (+15%):** **€75,900**

---



#### Fase 3: Multi-Tenant SaaS (2 meses)


| Rol                    | Horas                 | Tarifa/h | Subtotal    |
| ---------------------- | --------------------- | -------- | ----------- |
| Backend Developer      | 320h (100% × 2 meses) | €40/h    | €12,800     |
| Frontend Web Developer | 240h (75% × 2 meses)  | €40/h    | €9,600      |
| Mobile Developer       | 80h (25% × 2 meses)   | €40/h    | €3,200      |
| Full-Stack Developer   | 160h (50% × 2 meses)  | €40/h    | €6,400      |
| DevOps                 | 160h (50% × 2 meses)  | €50/h    | €8,000      |
| **SUBTOTAL FASE 3**    |                       |          | **€40,000** |


**Con margen de contingencia (+15%):** **€46,000**

---

**TOTAL DESARROLLO (9 meses):** **€211,140**

**Notas sobre costos de desarrollo:**

- Estos son costos estimados para un equipo en España/Europa
- Pueden reducirse significativamente:
  - **Equipo remoto de Latinoamérica:** -40% a -60% (~€85k-€125k total)
  - **Freelancers vs. Empresa:** -20% a -40% (~€125k-€170k total)
  - **Equipo interno o founders** que asuman el desarrollo sin facturación externa: el coste principalmente es **tiempo propio** (coste de oportunidad), no una tarifa de mercado imputada

---



### 11.2 Costos de Infraestructura AWS (Mensual)



#### Configuración Inicial (1 organización, ~500 citas/mes)


| Servicio                            | Especificación                                                   | Costo Mensual |
| ----------------------------------- | ---------------------------------------------------------------- | ------------- |
| **Compute (ECS Fargate)**           | 0.5 vCPU, 1GB RAM × 730h                                         | ~€30          |
| **SQL Server (Docker + host/EBS)**  | Contenedor con volumen; host tipo t3.medium (2 vCPU, 4GB RAM)    | ~€55          |
|                                     | 50GB storage SSD                                                 | Incluido      |
| **Cloudinary**                      | Imágenes y CDN (plan según volumen; free tier posible al inicio) | ~€8           |
| **ALB (Load Balancer)**             | Fijo + data processing                                           | ~€22          |
| **CloudFront CDN**                  | 50GB transfer out                                                | ~€5           |
| **SES (Email)**                     | 2,000 emails/mes                                                 | €0.20         |
| **Route 53**                        | 1 hosted zone                                                    | €0.50         |
| **CloudWatch**                      | Logs + métricas                                                  | ~€5           |
| **Secrets Manager**                 | 5 secrets                                                        | €2            |
| **Backups SQL / snapshots volumen** | 50GB                                                             | ~€5           |
| **Certificate Manager**             | SSL/TLS certificates                                             | Gratis        |
| **TOTAL INICIAL**                   |                                                                  | **~€133/mes** |


---



#### Escalado (5 organizaciones, 2,500 citas/mes)


| Servicio                | Cambios                                      | Costo Mensual |
| ----------------------- | -------------------------------------------- | ------------- |
| **Compute**             | t3.medium (más potencia)                     | ~€60          |
| **SQL Server (Docker)** | Host + contenedor ampliados (2 vCPU, 8GB)    | ~€115         |
| **Cloudinary**          | Mayor volumen de imágenes / transformaciones | ~€18          |
| **ALB**                 | Mayor tráfico                                | ~€30          |
| **CloudFront**          | 200GB transfer                               | ~€15          |
| **Otros**               | Similar                                      | ~€15          |
| **TOTAL (5 ORGS)**      |                                              | **~€250/mes** |


---



#### Escalado (50 organizaciones, 25,000 citas/mes)


| Servicio                           | Cambios                                                           | Costo Mensual |
| ---------------------------------- | ----------------------------------------------------------------- | ------------- |
| **Compute**                        | Múltiples instancias + autoscaling                                | ~€300         |
| **SQL Server (Docker / dedicado)** | Clúster o instancia potente (4 vCPU, 32GB) + réplica según diseño | ~€480         |
| **Cloudinary**                     | Alto volumen multimedia                                           | ~€55          |
| **CloudFront**                     | 1TB transfer                                                      | ~€60          |
| **SES**                            | 100,000 emails                                                    | ~€10          |
| **WAF**                            | Protección DDoS                                                   | ~€25          |
| **Otros**                          | Monitoring avanzado                                               | ~€50          |
| **TOTAL (50 ORGS)**                |                                                                   | **~€980/mes** |


---



### 11.3 Costos de Servicios Externos (Mensual)



#### Cloudinary (imágenes y medios)

- **Uso:** fotografías antes/después, logos de organización, transformaciones y CDN.
- **Coste:** plan gratuito con límites; planes de pago según almacenamiento, ancho de banda y transformaciones — ver [precios Cloudinary](https://cloudinary.com/pricing).
- Las estimaciones de la tabla **11.2** son orientativas; conviene revisar la calculadora oficial según volumen real.

---



#### Redsys (Pasarela de Pago)

**Estructura de costos:**

- Redsys es procesador contratado a través del banco
- Costos varían por entidad bancaria y volumen

**Estimación típica 2025:**


| Transacciones/mes | Importe promedio | Comisión | Costo Mensual |
| ----------------- | ---------------- | -------- | ------------- |
| 500               | €25              | 1.2%     | €150          |
| 2,500             | €25              | 1.1%     | €687.50       |
| 10,000            | €25              | 1.0%     | €2,500        |


**Costos adicionales Redsys:**

- **Cuota mensual:** €0 - €50 (según banco)
- **Setup:** €0 (puede haber costos del banco)
- **Bizum:** ~€0.50 por transacción

**Nota:** Estos costos los absorbe cada organización cliente, no el desarrollador de la plataforma SaaS.

---



#### WhatsApp Business API (Fase 3+)

**Proveedor recomendado:** 360dialog

**Costos de mensajes en España (2025):**

- **Categoría Utility:** €0.0095 por mensaje
- **Categoría Marketing:** €0.0436 por mensaje
- **Conversaciones de Servicio:** Gratis (cuando el cliente escribe primero)

**Estimación para recordatorios (Utility):**


| Organizaciones | Citas/mes | Recordatorios | Costo Mensual |
| -------------- | --------- | ------------- | ------------- |
| 1              | 500       | 1,000         | €10           |
| 5              | 2,500     | 5,000         | €50           |
| 50             | 25,000    | 50,000        | €500          |


---



#### Otros Servicios


| Servicio               | Plan              | Costo Mensual |
| ---------------------- | ----------------- | ------------- |
| **Dominio (.com/.es)** | Anual             | €1/mes        |
| **GitHub**             | Team (5 usuarios) | €20           |
| **Sentry**             | Error monitoring  | €26           |
| **Google Analytics**   | Free tier         | Gratis        |
| **Figma**              | Professional      | €12           |
| **TOTAL OTROS**        |                   | **~€60/mes**  |


---

**SUBTOTAL Servicios Externos (inicial):** **~€70/mes** (sin WhatsApp)  
**SUBTOTAL Servicios Externos (con WhatsApp):** **~€80/mes** (1 org)

---



### 11.4 Costos Legales y Compliance


| Concepto                              | Costo               | Frecuencia |
| ------------------------------------- | ------------------- | ---------- |
| **Asesoría RGPD inicial**             | €800 - €1,500       | Una vez    |
| **Elaboración de Políticas**          | €600 - €1,200       | Una vez    |
| (Privacidad, Cookies, T&C)            |                     |            |
| **DPO externo** (si requerido)        | €80 - €200          | Mensual    |
| **Revisión anual de compliance**      | €500                | Anual      |
| **Auditoría PCI-DSS** (SAQ A-EP)      | €2,000 - €5,000     | Anual      |
| **TOTAL INICIAL**                     | **€2,000 - €3,500** | Una vez    |
| **TOTAL ANUAL** (después del inicial) | **€2,500 - €5,000** | Anual      |


---



### 11.5 Costos de Publicación App Móvil


| Concepto                    | Costo       | Frecuencia |
| --------------------------- | ----------- | ---------- |
| **Apple Developer Program** | $99 (€95)   | Anual      |
| **Google Play Console**     | $25 (€24)   | Una vez    |
| **TOTAL AÑO 1**             | **€119**    |            |
| **TOTAL AÑOS SIGUIENTES**   | **€95/año** | Anual      |


---



### 11.6 Resumen de Costos Totales



#### Inversión Inicial (Fase 1 - MVP)


| Concepto                      | Costo         |
| ----------------------------- | ------------- |
| Desarrollo (4 meses)          | €89,240       |
| Infraestructura AWS (4 meses) | €532 (€133×4) |
| Servicios externos (4 meses)  | €280 (€70×4)  |
| Legal y compliance            | €2,500        |
| **TOTAL INVERSIÓN MVP**       | **€92,552**   |


---



#### Costos Operativos Mensuales (Después de MVP)


| Concepto            | 1 Org         | 5 Orgs   | 50 Orgs    |
| ------------------- | ------------- | -------- | ---------- |
| AWS Infraestructura | €133          | €250     | €980       |
| WhatsApp (Fase 3+)  | €10           | €50      | €500       |
| Otros servicios     | €60           | €80      | €120       |
| DPO (si aplica)     | €0-200        | €150     | €200       |
| **TOTAL MENSUAL**   | **€203-€403** | **€528** | **€1,800** |


---



#### Inversión Total (Fases 1-3)


| Concepto                                 | Costo        |
| ---------------------------------------- | ------------ |
| Desarrollo completo (9 meses)            | €211,140     |
| Infraestructura AWS (9 meses desarrollo) | €1,197       |
| Servicios externos (9 meses)             | €630         |
| Legal y compliance inicial               | €2,500       |
| Publicación apps móviles                 | €119         |
| **TOTAL PROYECTO COMPLETO**              | **€215,586** |


---



### 11.7 Modelo de Monetización SaaS (Fase 3)



#### Planes Propuestos


| Plan            | Precio/mes | Citas/mes  | Empleados  | Características                        |
| --------------- | ---------- | ---------- | ---------- | -------------------------------------- |
| **Básico**      | €49        | 200        | 3          | Email, 1 local, Web + Móvil            |
| **Profesional** | €99        | 1,000      | 10         | + WhatsApp, Reportes básicos           |
| **Premium**     | €199       | Ilimitadas | Ilimitados | + IA, Multi-local, Soporte prioritario |
| **Enterprise**  | €399+      | Ilimitadas | Ilimitados | + Personalización, Onboarding dedicado |


**Notas:**

- Período de prueba: 14 días gratis (todos los planes)
- Descuento anual: 20% (2 meses gratis)
- Costos de transacción Redsys: pagados por cada organización
- Setup fee: €0 (incluido en todos los planes)

---



#### Análisis Break-Even

**Costos fijos mensuales (50 clientes):**

- Infraestructura AWS: €980
- Servicios externos: €120
- DPO: €200
- Soporte/Mantenimiento: €500 (estimado)
- **TOTAL FIJOS:** €1,800/mes

**Ingresos mensuales objetivo:**


| Escenario       | Distribución                                           | MRR        |
| --------------- | ------------------------------------------------------ | ---------- |
| **Conservador** | 20 Básico + 5 Profesional + 2 Premium                  | €1,675/mes |
| **Moderado**    | 25 Básico + 15 Profesional + 8 Premium + 2 Enterprise  | €4,418/mes |
| **Optimista**   | 15 Básico + 25 Profesional + 15 Premium + 5 Enterprise | €7,215/mes |


**Break-even:** ~**15-20 clientes** (mix de planes) = €1,800-€2,000/mes

**Objetivos:**

- **Mes 12:** 30 clientes = €2,500/mes MRR
- **Mes 18:** 50 clientes = €4,500/mes MRR
- **Mes 24:** 100 clientes = €10,000/mes MRR

---



#### Análisis de ROI

**Inversión total:** €215,739

**Escenario conservador:**

- Año 1: MRR promedio €2,000/mes × 12 = €24,000
- Año 2: MRR promedio €6,000/mes × 12 = €72,000
- Año 3: MRR promedio €10,000/mes × 12 = €120,000
- **Total 3 años:** €216,000
- **Recuperación inversión:** 24-30 meses

**Escenario optimista:**

- Año 1: MRR promedio €3,500/mes × 12 = €42,000
- Año 2: MRR promedio €9,000/mes × 12 = €108,000
- Año 3: MRR promedio €15,000/mes × 12 = €180,000
- **Total 3 años:** €330,000
- **Recuperación inversión:** 18-20 meses

**Conclusión:** ROI positivo esperado entre 18-30 meses según tasa de adquisición.

---



## 12. PRÓXIMOS PASOS

La **estrategia de pruebas automatizadas** (unitarios, integración, E2E, simulación Redsys y qué ejecutar en cada pipeline) está descrita en `[reservarte-testing-strategy.md](reservarte-testing-strategy.md)`. El checklist **§12.2** (incluida la subsección **Testing**) debe implementarse de forma coherente con ese documento y con el volumen 2 **§9.5**. La **accesibilidad (WCAG 2.1 AA)** y la **internacionalización (vue-i18n)** están recogidas en `[accessibility-and-i18n.md](accessibility-and-i18n.md)` y en el script `Documentation/Project-Init/Scripts de instalación.md`.

### 12.1 Pasos Inmediatos (Semana 1-2)



#### 1. Validación y Aprobación del Cliente

**Acciones:**

- [ ] Entregar al cliente el **conjunto de documentación técnica** (los tres volúmenes) para revisión y aprobación
- [ ] Revisar todas las funcionalidades propuestas
- [ ] Confirmar prioridades y alcance del MVP
- [ ] Discutir presupuesto y timeline
- [ ] Definir criterios de éxito
- [ ] Firmar contrato o acuerdo de desarrollo

**Entregables:**

- Documentación técnica revisada y **aprobada por el cliente** (acta de conformidad o firma en el contrato / SOW)
- Statement of Work (SOW) detallado
- Cronograma acordado
- Presupuesto aprobado

---



#### 2. Planificación Detallada

**Acciones:**

- [ ] Definir equipo de desarrollo
  - Identificar desarrolladores disponibles
  - Asignar roles y responsabilidades
  - Establecer dedicación por persona
- [ ] Crear workspace **ReservArte** en ClickUp replicando la estructura del §10.1.1 (Spaces **Backend (.NET)**, **Frontend (Vue 3)**, **Mobile (React Native)**, **Infrastructure**, **Documentation** y todas sus listas)
  - Crear épicas por módulo
  - Desglosar en user stories
  - Asignar story points
  - Priorizar backlog
- [ ] Planificar Sprint 1 en detalle
  - Seleccionar user stories
  - Crear tareas técnicas
  - Asignar responsables
  - Definir Definition of Done
- [ ] Definir métricas de éxito (KPIs)
  - Velocity del equipo
  - Quality metrics (bugs, coverage)
  - Performance metrics (response time)
  - Business metrics (conversión, satisfacción)

**Entregables:**

- Backlog completo y priorizado
- Sprint 1 planificado
- KPIs definidos y acordados
- Calendario de ceremonias Scrum

---



#### 3. Setup Técnico Inicial

**Acciones:**

**AWS:**

- [ ] Crear cuenta AWS (o usar existente)
- [ ] Configurar AWS Organizations si multi-cuenta
- [ ] Configurar billing alerts
- [ ] Crear usuarios IAM con MFA
- [ ] Configurar VPC en región eu-west-1 (Irlanda)
- [ ] Crear subnets públicas y privadas
- [ ] Configurar Security Groups
- [ ] Aprovisionar SQL Server en Docker (entorno dev; **no hay `docker-compose.yml`** — RA-869d7ewec; el contenedor se creó con `docker run`)
- [ ] Configurar **Cloudinary** (clouds o carpetas por entorno; API keys en Secrets Manager)
- [ ] Verificar dominio en Amazon SES

**Repositorios:**

- [ ] Crear organización en GitHub
- [x] Crear repositorio: monorepo **`TakerVare/ReservArte`** (RA-869d7ewqv, 2026-09-14; no tres repos)
- [ ] Crear repositorio móvil (reservarte-mobile) — fuera del monorepo actual
- [ ] Aplicar **Git Flow** (`main`, `develop`, `feature/`*, `release/*`, `hotfix/*`) — §10.1.2
- [ ] Exigir **Conventional Commits** en mensajes — husky + commitlint **pendiente (RA-869d7ewzg)**
- [x] Añadir `.github/PULL_REQUEST_TEMPLATE.md` (RA-869d7ewwq)
- [x] Configurar branch protection (**RA-869d7ewu5**, decisión 2026-09-14): **`main`** PR obligatorio, 0 aprobaciones, sin force-push ni borrado; **`develop` sin PR obligatorio**. Sin CI, no hay checks. `enforce_admins` false.
- [ ] Configurar GitHub Actions para CI (no hay `.github/workflows`)

**Entornos:**

- [ ] Configurar 3 entornos: Dev / Staging / Production
- [ ] Crear bases de datos por entorno
- [ ] Configurar subdominios:
  - dev.reservarte.com
  - staging.reservarte.com
  - app.reservarte.com
- [ ] Configurar certificados SSL/TLS

**CI/CD:**

- [ ] Pipeline de build para backend
- [ ] Pipeline de build para frontend
- [ ] Pipeline de tests automatizados
- [ ] Pipeline de deployment a staging
- [ ] Pipeline de deployment a production (manual approval)

**Entregables:**

- Infraestructura AWS funcional
- Repositorios Git configurados
- CI/CD pipelines operativos
- Entornos de desarrollo, staging y producción listos

---



#### 4. Gestión de Cuentas de Servicios Externos

**Redsys:**

- [ ] Contactar con banco para cuenta de comercio
- [ ] Solicitar credenciales de entorno de pruebas
- [ ] Obtener FUC (código de comercio)
- [ ] Obtener Terminal
- [ ] Obtener clave secreta (256 bits)
- [ ] Documentar configuración en Secrets Manager
- [ ] Realizar transacción de prueba exitosa

**Otros:**

- [ ] Crear cuenta Amazon SES y verificar dominio
- [ ] Configurar SPF, DKIM, DMARC
- [ ] Solicitar salir de sandbox de SES
- [ ] Crear cuenta GitHub (si no existe)
- [ ] Crear cuenta Sentry para error tracking

**Entregables:**

- Credenciales de Redsys (test) funcionando
- Amazon SES operativo y fuera de sandbox
- Documentación de todas las credenciales en lugar seguro

---



#### 5. Legal y Compliance

**Acciones:**

- [ ] Contactar asesor legal especializado en RGPD
- [ ] Iniciar elaboración de políticas:
  - Política de Privacidad
  - Política de Cookies
  - Términos y Condiciones
  - Política de Cancelación
- [ ] Planificar EIPD (Evaluación de Impacto en Protección de Datos)
- [ ] Definir proceso de gestión de consentimientos
- [ ] Revisar necesidad de DPO

**Entregables:**

- Asesor legal contratado
- Borradores de políticas en revisión
- Plan para EIPD
- Documentación de compliance

---



### 12.2 Checklist de Arranque (Semana 3-4)

Detalle de herramientas, umbrales de cobertura y jobs de CI: `[reservarte-testing-strategy.md](reservarte-testing-strategy.md)`.

#### Backend (.NET Core)

- [x] Crear solución con Clean Architecture
- [x] Instalar paquetes NuGet necesarios
- [x] Configurar Entity Framework Core
- [x] Crear primera migración (tablas core)
- [x] Configurar ASP.NET Core Identity
- [x] Implementar JWT authentication (Bearer como autorización API)
- [x] Registrar proveedores: **Google** (OAuth), **Apple** (Sign in with Apple), **Meta/Instagram** (OAuth; app y permisos en Meta Developers)
- [x] Implementar **2FA opcional** (TOTP Identity, códigos de recuperación, endpoints `mfa` / `account/mfa`)
- [x] Persistir logins externos (`AspNetUserLogins`) y política de cuentas duplicadas por email **por organización** (RA-869f1xc0u)
- [x] Rate limiting nativo (login / mfa-verify) + CAPTCHA (`ICaptchaService`)
- [x] Proyecto `tests/ReservArte.UnitTests` + JWT + `WeekDayTests` + repositorio/tenant + servicio/validadores/mapping/lockout + `RolesTests` + reglas de rol CRUD + disponibilidad + invitación + atomicidad + query filters + `CustomerDomainTests` + `AuthServiceTenantTests` + `CustomerRepositoryTests` + `PublicSignupCustomerTests` + `RegisterRequestValidatorTests` + `CustomerServiceTests` + `CustomerValidatorTests` + `CustomerProfileTests` + `ServiceDomainTests` + `ServiceRepositoryTests` + `ServiceValidatorTests` + `ServiceCatalogProfileTests` + `ServiceCatalogWriteValidatorTests` + `ServicePackageRepositoryTests` + `ServicePackageValidatorTests` + `AppointmentDomainTests` + `AppointmentMappingTests`; suite **410/410** (2026-09-16, PR #71)
- [x] **Serilog — pipeline + sink consola:** patrón en dos fases (bootstrap logger + configuración definitiva desde `appsettings`), sink de consola y enriquecimiento por petición (`RequestId`, `OrganizationId` vía middleware) — hecho (Setup Backend)
- [ ] **Serilog — sink CloudWatch:** envío de logs a AWS — **pendiente** (tareas de infraestructura; mismo criterio que SES, key ring de Data Protection en prod, etc.)
- [x] Configurar Swagger/OpenAPI con esquema reutilizable del **envelope** `{ success, data, error, meta }` y códigos `error.code` (volumen 1 §5.1.1–5.1.2)
- [x] Definir `appsettings.json` **como contrato** (volumen 1 §5.1.3): todas las secciones y claves con valores vacíos o placeholders; **sin secretos** en el repositorio
- [x] Completar `appsettings.Development.json` y `appsettings.Production.json` en el repo solo con valores **no sensibles** (localhost, CORS, flags, `MultiTenant:ResolutionStrategy = Header` en dev, URLs públicas en prod)
- [x] **CORS conectado al pipeline HTTP:** `Cors:AllowedOrigins` no basta por sí solo. Registrar `AddCorsPolicy` (`ReservArte-API/Extensions/CorsServiceExtensions.cs`) y `app.UseCors(...)` — hallazgo 2026-08-23: la clave existía y se usaba para validar `returnUrl` OAuth, pero **no había middleware CORS**; el navegador bloqueaba en silencio toda petición del SPA a la API. Lección: no dar por hecho CORS en un módulo nuevo sin comprobarlo contra un frontend real (vol. 1 §5.1.3, vol. 2 §9.3.4)
- [x] Redactar `Documentation/Project-Init/user-secrets-guide.md`: comandos `dotnet user-secrets set` por secreto, tarjetas de prueba Redsys, **ngrok** para webhook local, FAQ
- [ ] Producción: **variables de entorno** y **AWS Secrets Manager** según la jerarquía del volumen 1 §5.1.3
- [ ] Producción: **`LegalDocuments__TermsVersion` y `LegalDocuments__PrivacyVersion` obligatorios** (no van en `appsettings.Production.json`; el base está vacío). Sin ellos `ValidateOnStart` impide arrancar la API (vol. 1 **§5.1.3**, RA-869epf0rt). Development las cubre en `appsettings.Development.json`.
- [x] Escribir primer endpoint de health check (`GET /health` + smoke test de BD vía `AddDbContextCheck`)
- [x] Configurar cadena de conexión a SQL Server (contenedor Docker `reservarte-sql`, base `ReservArteDB`)

> **Módulo Auth (RA-869d7ed03):** cerrado **9/9** (2026-08-21). Backlog no bloqueante: **RA-869en8a17** (refinamientos rate limiting + `AUTH_MFA_INVALID`). **Alta en backlog (prioridad high):** **RA-869f151x1** — el login social se salta el 2FA (emitir ticket `mfa_pending` si hay TOTP activo).

> **Módulo Empleados (RA-869d7ed2j):** **10/10 cerrado** (2026-09-14, PR #53). Numerador: **RA-869d7ezrr**, **RA-869d7ezv0**, **RA-869f17myx**, **RA-869d7ezwy**, **RA-869f180e5**, **RA-869d7f043**, **RA-869d7ezz4**, **RA-869d7f01b**, **RA-869f17y68**, **RA-869f1811u**. Colaterales **RA-869f17y7n**, **RA-869f1anz3**, **RA-869f1m12x**. Scripts `data/` vs EF: **RA-869f17mzg done** (PR #55). Query filters (**RA-869f17vet**, PR #54) **shipped**. Unicidad email por org: **RA-869f1xc0u shipped** (PR #57). `Result<T>` vs `AuthResult<T>` (+ `ValidateAsync`/`ToCamelCase` divergentes): **RA-869f17y6k**. 400 ProblemDetails: **RA-869f1k17q**. `EmailConfirmed` al completar invitación: **RA-869f1812p**. IdentityResult en auth (**RA-869f1mqah**, acotado en PR #59: cubiertos `CreateAsync`/`AddLoginAsync` del alta pública; el resto sigue sin auditar). Detalle: vol. 2 **§9.6**.
>
> **Módulo Clientes (RA-869d7ed68):** **6/6 shipped** (2026-09-15, PR #63). Subtareas hechas: **RA-869d7f2z5** (PR #56), **RA-869d7f32r** (PR #58), **RA-869f1xc2n** (PR #59), **RA-869d7f369** (PR #60), **RA-869d7f3bt** (PR #61), **RA-869d7f3fw** (PR #62). Canceladas: **RA-869d7f3q4**, **RA-869d7f3ka** → **RA-869f2gtyv** (Citas). Cadena: **RA-869f1xc0u** (hecha, fuera del 6) → **RA-869d7f32r** → **RA-869f1xc2n** → **RA-869d7f369** → **RA-869d7f3bt** → **RA-869d7f3fw**. Historial: **RA-869f2gn91**. Tarjetas: **RA-869f2gnbm**. No-shows: **RA-869f2gtyv**. Promoción: **RA-869f2g02q**. Frontend: **RA-869d7fc34**, **RA-869d7fc51** (subtareas de **RA-869d7edt7** — «Módulos Empleados, Clientes, Servicios y Dashboard (UI completa)»). AuditLog: **RA-869f2gtz8**. Deuda de docs (árbol): **RA-869f2g60e**. Integración HTTP: **RA-869f2gh37**. `ReservArteDB` recreada en PR #62. Detalle: vol. 2 **§9.7**.
>
> **Módulo Servicios (RA-869d7ed7v):** **5/6 parado** (2026-09-16, PR #68), **no cerrado**. Shipped: **RA-869d7f3wa** (PR #64), **RA-869d7f3z0** (PR #65), **RA-869d7f42u** (PR #66), **RA-869f2wtrk** (PR #67) y **RA-869d7f45n** (PR #68, paquetes). **Solo queda RA-869d7f4b4** (dashboard; se retomará cuando Citas dé datos). El catálogo tiene capa de acceso a datos completa. El bloque se adelantó al de Citas (**RA-869d7edau**). Detalle: vol. 2 **§9.8**. El padre sigue en `in development` (fechas 2026-09-16 → 2026-09-18).
>
> **Módulo Citas (RA-869d7edau):** **2/11** (2026-09-16, PR #70 + #71). Padre en `in development` (fechas 2026-09-16 → 2026-09-25). Shipped: **RA-869d7f4f1** (entidades Domain) y **RA-869d7f4j8** (mapeo; tablas `Appointments`, `AppointmentServiceItems`, `WaitingLists`). El denominador pasó de 10 a 11 al crear **RA-869f2yh9b**. Siguiente: **RA-869d7f4n4**. Detalle: vol. 2 **§9.9**.
>
> **Infra — `dotnet format` (RA-869f2pjf8):** **done** (2026-09-16, PR #72 + #73; lista Infra, no `shipped`). Línea base **0**. Convenciones: vol. 2 **§9.10**.



#### Frontend Web (Vue 3 + Vite)

- [x] Crear proyecto con Vite + Vue 3 + TypeScript (proxy Vite `/api` → backend; URL de la API vía `VITE_API_BASE_URL`; puerto de la API en `launchSettings.json` — sin puerto literal de máquina; build de producción verificado) — andamiaje Setup Frontend, 2026-08-21
- [x] Configurar Tailwind CSS (**3.4.17**, PostCSS/Vite) — andamiaje Setup Frontend
- [x] Instalar librería de componentes compatible con Vue (**Reka UI** / paquete `reka-ui`; primitivos headless sobre los que se asienta shadcn-vue) — andamiaje Setup Frontend
- [x] Configurar Pinia para estado global (`authStore`, `uiStore`; registrado en `main.ts`)
- [x] Configurar Vue Router (guards `requiresAuth` / `requiresMfa`). **No se fija un recuento de rutas** (crece con cada módulo). Organización actual: **auth públicas** (login, mfa-verify, oauth-callback, register, forgot-password, reset-password, **set-password**); **legales públicas** (`/legal/terminos`, `/legal/privacidad`, stubs); **BottomNav** (`/mis-citas` y `/cuenta` con `requiresAuth`; `/contacto` público); **privadas bajo layout con `requiresAuth`** (dashboard, empleados, clientes, servicios, citas, pagos, recordatorios, configuracion). El `DashboardLayout`/Sidebar de ese grupo es **deuda** (reconciliación de layouts, backlog; vol. 2 §9.2.4).
- [x] Crear estructura de carpetas (`src/`: stores, router, i18n, locales, lib, styles; backend: Clean Architecture de 5 proyectos + `tests/`) — Setup Frontend/Backend
- [x] Implementar axios client con interceptors (`client.ts`: **401** protegido por status salvo `AUTH_ENDPOINTS_WITHOUT_SESSION` — `login`, `mfa/verify`, `refresh-token`, `set-password`, **`reset-password`** (RA-869f1m12x); **403** solo si `error.code` ∈ `SESSION_ENDING_ERROR_CODES`; `endSession()` con `window.location` a propósito — RA-869f18urw / PR #44)
- [x] Crear layout principal (`DashboardLayout` + Sidebar + Header; `AuthLayout`) — **hecho como código** (RA-869d7edpt, 2026-08-23). **No es el diseño:** navegación deseada = solo `BottomNav`. **Deuda:** retirar `DashboardLayout`/`Sidebar` y `AuthLayout` (reconciliación de layouts, backlog; vol. 2 §9.2.4)
- [x] Implementar página de login (`LoginPage` + `LoginForm`: credenciales, botones OAuth cableados, hueco CAPTCHA tras 3 fallos) — **RA-869d7f7kn shipped** (2026-08-23); login local verificado en runtime
- [x] Vista **Verificación 2FA** (`MfaVerifyPage`, RA-869d7f7vw) — **shipped** (2026-08-24); flujo E2E verificado (TOTP, recuperación, rechazo de código incorrecto). Ajustes **Seguridad de cuenta** (activar/desactivar TOTP) siguen pendientes
- [x] **Registro (`RegisterPage`, RA-869d7fbhg)** — **shipped** (2026-08-25); patrón Banner (no `AuthLayout`). Verificado: Zod, consentimiento versionado, login automático. **RA-869f1xc2n (PR #59):** tercer checkbox `acceptedDataProcessing` y ficha `regular` al registrarse. **Desde RA-869d7f369** la ficha nace `new`. Detalle: vol. 2 **§9.2.3**.
- [x] **Forgot-Password / Reset-Password (`ForgotPasswordPage`, `ResetPasswordPage`, RA-869d7fbmy)** — **shipped** (2026-08-27); patrón Banner (no `AuthLayout`). Forgot: anti-enumeración; Reset: `:token?`, Zod = política backend. **Contrato del token (RA-869f18rp7, PR #42):** POST en claro tras una decodificación de Vue Router; E2E `e2e/reset-password.spec.ts`. Runtime 2026-09-13 contra `DevFileEmailService`. **Set-Password (`SetPasswordPage`, RA-869f17y68, PR #51):** `/set-password/:token?`; esquema reexportado. **RA-869f1m12x (PR #52):** `reset-password` y `set-password` en `AUTH_ENDPOINTS_WITHOUT_SESSION`. Detalle: vol. 2 **§9.2.3**. `AuthLayout` y `DashboardLayout`: deuda de retirada.
- [x] Vista de retorno OAuth (`OAuthCallbackPage`, **RA-869d7f7r1**) — **shipped** (2026-09-12, PR #33). Lee el fragmento, hidrata `authStore`, redirige. Verificado contra el contrato backend (E2E `e2e/oauth-callback.spec.ts`, 12 tests / suite 24/24). **No** verificado contra un proveedor OAuth real (credenciales por entorno; pendiente de LoginPage, no de esta tarea). Detalle: vol. 2 **§9.2.3**. Bloque Auth UI **RA-869d7edpt:** **7/7 — completo**.
- [ ] Widget **Turnstile** real en login (camino B: contador + hueco hechos; site key `VITE_TURNSTILE_SITE_KEY` + script pendientes)
- [ ] Configurar variables de entorno
- [x] Instalar y configurar **vue-i18n v9** (registrado en `main.ts`; locale **`es`** cargado desde `src/locales/es/`) — andamiaje Setup Frontend; uso en pantallas de auth funcionales pendiente
- [ ] Definir convención de claves y documentación operativa en `[Documentation/accessibility-and-i18n.md](accessibility-and-i18n.md)` (Bloque B)
- [x] Canal de accesibilidad automatizada: **Playwright + `@axe-core/playwright`** en `reservarte-web/e2e/` (RA-869eqxdk3). **Test a11y `LoginPage` (RA-869d7fbpp, 2026-09-11) shipped:** `e2e/login.a11y.spec.ts`, tres estados (inicial, error, CAPTCHA), tags WCAG 2.1 AA. **Excepción consciente:** regla `color-contrast` desactivada (marca `#FFB6C1` ~1.62:1; deuda **RA-869f0v6vm**). La LoginPage **no** está plenamente accesible. Revisión manual con **axe DevTools** antes de merge de UI sensible y de staging.
- [ ] Objetivo de contraste WCAG 1.4.3: **incumplido a propósito** en el color de marca rosa (`#FFB6C1` / `--primary`); deuda **RA-869f0v6vm**. El resto de pares sigue pendiente de medición (WebAIM / axe) antes del primer deploy a staging. Ver `[Documentation/accessibility-and-i18n.md](accessibility-and-i18n.md)` Bloque A.

> **Andamiaje vs UI (2026-08-21, 3ª pasada post RA-869d7ezp3):** Vite + Tailwind 3.4.17 + Reka UI (`reka-ui`) + Pinia + Vue Router + axios + vue-i18n (`es`) + estructura de carpetas = **hecho**. En esa fecha la UI de auth (login/OAuth/2FA/CAPTCHA) estaba **pendiente**.
>
> **Superado el 2026-08-23 — LoginPage:** la **UI de login (`LoginPage`) está implementada y verificada** (login local end-to-end + CAPTCHA tras 3 fallos).
>
> **Superado el 2026-09-11 — test a11y LoginPage (RA-869d7fbpp):** spec shipped (tres estados; `color-contrast` excluido; deuda RA-869f0v6vm). Widget Turnstile real pendiente (hueco y umbral 3 hechos). Backend reset: vol. 1 **§4.4.1** (RA-869eq5tg3). Patrón SPA + Zod: vol. 2 **§9.2.3**.
>
> **Superado el 2026-09-12 — retorno OAuth (RA-869d7f7r1, PR #33):** `OAuthCallbackPage` shipped. Bloque RA-869d7edpt: **7/7 — completo**. Verificado: E2E `reservarte-web/e2e/oauth-callback.spec.ts` (4 casos × 3 navegadores = 12 tests) y suite **24/24**. `npm run build` ✓; ESLint limpio en archivos tocados. **No verificado:** IdP OAuth real (pendiente de LoginPage, desacoplado). **Arreglo colateral de build:** `TS2614` en `login-form/index.ts` (reexport de `OAuthProvider`); **RA-869d7f7ef** estaba `shipped` pero el build de `develop` se había roto después. **Alta en backlog:** **RA-869f151x1** (Backend, high) — el login social emite tokens sin gate 2FA. Pendientes del bloque que **no** cambian: widget Turnstile real, contraste AA (RA-869f0v6vm), tokens del modo claro (RA-869f0w7r2), paleta oscura (RA-869f0w75h), migración de `LoginForm` a VeeValidate+Zod (RA-869epnt88) y reconciliación de layouts (RA-869ep9p36).



#### Testing (unitarios, integración y E2E)

- [x] **Backend unitario:** proyecto `tests/ReservArte.UnitTests` con xUnit + Moq + FluentAssertions; suite **410/410** (2026-09-16, PR #71). Repositorios: SQLite en memoria. `[reservarte-testing-strategy.md](reservarte-testing-strategy.md)` §3.1
- [x] **`dotnet format --verify-no-changes`:** puerta de calidad con línea base **CERO** (RA-869f2pjf8, PR #72 + #73). `.editorconfig` en la raíz. Vol. 2 **§9.10**.
- [ ] **Backend integración:** `tests/ReservArte.IntegrationTests` + Testcontainers (SQL Server) + `WebApplicationFactory`; migraciones EF Core; semilla multi-tenant
- [ ] **Frontend (unitario):** instalar y configurar **Vitest** + **Vue Test Utils**; scripts `test` / `test:watch` en `package.json`; carpetas `tests/unit` o convención alineada con el monorepo. Capa **distinta** de Playwright (E2E/accesibilidad). Backlog: **RA-869eqxm8z**.
- [x] **E2E frontend:** **Playwright** + **`@axe-core/playwright`** en `reservarte-web` (`playwright.config.ts`, tests en `reservarte-web/e2e/`, Chromium / Firefox / WebKit). Scripts `test:e2e`, `test:e2e:ui`, `test:e2e:report`. Humo E2E, **test a11y `LoginPage` (RA-869d7fbpp)**, **retorno OAuth (`e2e/oauth-callback.spec.ts`, RA-869d7f7r1)**, **reset-password (`e2e/reset-password.spec.ts`, RA-869f18rp7 + caso caducado RA-869f1m12x)**, **fin de sesión (`e2e/session-ending.spec.ts`, RA-869f18urw; PRs #44–#45)**, **set-password (`e2e/set-password.spec.ts`, RA-869f17y68)** y **registro (`e2e/register.spec.ts`, RA-869f1xc2n)** verificados (suite **57/57**; antes **51**). En Mac: **`npm run test:e2e`** (`npx playwright test` puede resolver otra instalación). Plan previo `tests/ReservArte.E2ETests` **abandonado**. Escenarios de producto E2E **siguen pendientes**. El test a11y **excluye** `color-contrast` (deuda RA-869f0v6vm). El E2E OAuth **no** cubre un IdP real. El flujo forgot→email→reset con backend real: **RA-869f18uta**.
- [ ] **CI:** jobs acordados con la estrategia (unitarios + integración en PR; E2E en merge a `develop`; humo Redsys pre-deploy) — detalle en `reservarte-testing-strategy.md` §9. La infra E2E **local** (RA-869eqxdk3) **no** cubre el pipeline. Integrar Playwright en CI: backlog **RA-869eqxm7w**. Se cruza con **RA-869f18uta** (forgot→reset con API/BD en el runner).



#### DevOps

- [ ] Dockerfile para backend
- [ ] Dockerfile para frontend
- [ ] docker-compose.yml para desarrollo local
- [ ] GitHub Actions workflow para backend (disparo en PR a `develop` / `main` según §10.1.2)
- [ ] GitHub Actions workflow para frontend
- [ ] Script de deployment a staging
- [ ] Script de deployment a production
- [ ] Configurar Secrets en GitHub Actions
- [ ] Configurar CloudWatch Dashboards



#### Base de Datos

- [x] Crear esquema inicial — migraciones EF + `data/schema/create_ReservArteDB.sql` generado (RA-869f17mzg)
- [x] Tablas actuales: organizations, AspNetUsers, employees, disponibilidad, Identity, RefreshTokens (el resto del diseño §5.2 **aún no** tiene migración)
- [x] Índices iniciales (los de las migraciones; aviso Identity `PK_AspNetUserTokens` > 900 bytes)
- [x] Seed data para desarrollo — `DevSeeder` y `data/demo/seed_demo_ReservArteDB.sql` (RA-869d7ewka)
- [ ] Procedimientos almacenados (si necesarios)
- [ ] Backup schedule configurado (**RA-869d7ewnz**)

---



### 12.3 Riesgos y Mitigaciones


| Riesgo                               | Probabilidad | Impacto | Mitigación                                                                  |
| ------------------------------------ | ------------ | ------- | --------------------------------------------------------------------------- |
| **Retrasos en desarrollo**           | Alta         | Alto    | Planificación realista con buffer del 15%, revisiones semanales             |
| **Problemas con aprobación Redsys**  | Media        | Alto    | Iniciar proceso bancario cuanto antes, tener entorno de test funcionando    |
| **Costos AWS más altos**             | Media        | Medio   | Monitorización constante, alertas de billing, optimización continua         |
| **Cambios en regulación RGPD**       | Baja         | Alto    | Asesor legal continuo, revisión trimestral de compliance                    |
| **Baja adopción SaaS**               | Media        | Alto    | Marketing pre-lanzamiento, precio competitivo, periodo prueba, UX excelente |
| **Problemas de escalabilidad**       | Baja         | Alto    | Arquitectura escalable desde inicio, load testing antes de producción       |
| **Brecha de seguridad**              | Baja         | Crítico | Auditorías de seguridad, penetration testing, seguro cibernético            |
| **Dependencia de terceros (Redsys)** | Media        | Medio   | Documentación exhaustiva, fallbacks, monitoreo 24/7                         |
| **Rotación del equipo**              | Media        | Alto    | Documentación detallada, code reviews, knowledge sharing                    |
| **Competencia en mercado**           | Alta         | Medio   | Diferenciación por UX, soporte local, precio competitivo                    |


---



### 12.4 Criterios de Éxito

Criterios **pendientes de medir**; se evaluarán al alcanzar cada hito. Nada está alcanzado: el MVP no está desplegado.



#### MVP (Fin Mes 4)

**Técnicos:**

- ⬜ Aplicación desplegada en producción
- ⬜ Tiempo de respuesta API < 300ms (p95)
- ⬜ Uptime > 99.5%
- ⬜ 0 vulnerabilidades de seguridad críticas
- ⬜ Test coverage > 70% en backend
- ⬜ Lighthouse score > 85 en frontend

**Negocio:**

- ⬜ 1 centro piloto usando el sistema diariamente
- ⬜ 100+ citas gestionadas exitosamente
- ⬜ 50+ transacciones con Redsys sin errores
- ⬜ NPS (Net Promoter Score) > 7/10 del cliente piloto
- ⬜ < 5 bugs críticos reportados
- ⬜ Tiempo promedio de creación de cita < 3 minutos

---



#### App Móvil (Fin Mes 7)

**Técnicos:**

- ⬜ Apps publicadas en App Store y Google Play
- ⬜ Crash-free rate > 99%
- ⬜ App startup time < 3 segundos
- ⬜ API calls < 2 segundos

**Negocio:**

- ⬜ 100+ instalaciones totales
- ⬜ 50+ usuarios activos mensuales
- ⬜ 30% de citas reservadas vía app
- ⬜ Valoración > 4.0/5 en stores
- ⬜ Tasa de retención (Day 7) > 40%

---



#### Lanzamiento SaaS (Fin Mes 12)

**Técnicos:**

- ⬜ Multi-tenancy funcionando sin problemas
- ⬜ Aislamiento de datos verificado
- ⬜ Onboarding completo < 20 minutos
- ⬜ 0 downtime en últimos 30 días

**Negocio:**

- ⬜ 20+ organizaciones de pago activas
- ⬜ MRR (Monthly Recurring Revenue) > €1,500
- ⬜ Churn rate < 10%/mes
- ⬜ CAC (Customer Acquisition Cost) < €300
- ⬜ NPS promedio > 8/10
- ⬜ LTV/CAC ratio > 3:1

---



## ANEXOS



### Anexo A: Glosario de Términos

**A-E:**

- **API:** Application Programming Interface
- **AWS:** Amazon Web Services
- **BSP:** Business Solution Provider (WhatsApp)
- **CAC:** Customer Acquisition Cost
- **CAPTCHA:** Completely Automated Public Turing test
- **CDN:** Content Delivery Network
- **CI/CD:** Continuous Integration / Continuous Deployment
- **Conventional Commits:** convención de mensajes de commit (`feat:`, `fix:`, `docs:`, …) alineada con [conventionalcommits.org](https://www.conventionalcommits.org/)
- **COF:** Credential On File (Redsys)
- **CRUD:** Create, Read, Update, Delete
- **DPA:** Data Processing Agreement
- **DPO:** Data Protection Officer (Delegado de Protección de Datos)
- **EIPD:** Evaluación de Impacto en Protección de Datos

**F-M:**

- **FUC:** Número de comercio en Redsys
- **Git Flow:** modelo de ramas Git (`main`, `develop`, `feature/`*, `release/*`, `hotfix/*`) para integración y releases
- **HMAC:** Hash-based Message Authentication Code
- **HMR:** Hot Module Replacement (Vite)
- **JWT:** JSON Web Token (access token emitido por la API de ReservArte; la autorización en controladores se basa en este Bearer token)
- **MFA / 2FA:** Autenticación multifactor / doble factor; en el producto es **opcional** por usuario (TOTP)
- **OIDC:** OpenID Connect (p. ej. Google y Apple; Meta/Instagram usa principalmente OAuth 2.0). Distinto del OAuth2 «para apps de terceros» del marketplace
- **KPI:** Key Performance Indicator
- **LOPD:** Ley Orgánica de Protección de Datos
- **LSSI-CE:** Ley de Servicios de la Sociedad de la Información
- **MRR:** Monthly Recurring Revenue

**N-Z:**

- **MVP:** Minimum Viable Product
- **NPS:** Net Promoter Score
- **ORM:** Object-Relational Mapping
- **PAN:** Primary Account Number (número de tarjeta)
- **PCI-DSS:** Payment Card Industry Data Security Standard
- **RGPD:** Reglamento General de Protección de Datos (GDPR en inglés)
- **ROI:** Return On Investment
- **SaaS:** Software as a Service
- **SAQ:** Self-Assessment Questionnaire (PCI-DSS)
- **SCA:** Strong Customer Authentication
- **SDK:** Software Development Kit
- **SES:** Simple Email Service (AWS)
- **TLS:** Transport Layer Security
- **TOTP:** Time-based One-Time Password (autenticadores tipo Google Authenticator; base de la 2FA opcional)
- **TPV:** Terminal Punto de Venta
- **VPC:** Virtual Private Cloud

---



### Anexo B: Referencias y Recursos



#### Documentación Técnica

**Frameworks y Librerías:**

- ASP.NET Core: [https://docs.microsoft.com/aspnet/core](https://docs.microsoft.com/aspnet/core)
- Vue 3: [https://vuejs.org](https://vuejs.org)
- Vite: [https://vitejs.dev](https://vitejs.dev)
- React Native: [https://reactnative.dev](https://reactnative.dev)
- Entity Framework Core: [https://docs.microsoft.com/ef/core](https://docs.microsoft.com/ef/core)
- Pinia: [https://pinia.vuejs.org](https://pinia.vuejs.org)
- Vue Router: [https://router.vuejs.org](https://router.vuejs.org)
- Tailwind CSS: [https://tailwindcss.com/docs](https://tailwindcss.com/docs)
- Reka UI (ejemplo headless Vue): [https://reka-ui.com](https://reka-ui.com)

**Git y flujo de entrega:**

- Conventional Commits: [https://www.conventionalcommits.org](https://www.conventionalcommits.org)
- Git Flow (modelo de ramas): [https://nvie.com/posts/a-successful-git-branching-model/](https://nvie.com/posts/a-successful-git-branching-model/)
- GitHub — Pull Request templates: [https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/creating-a-pull-request-template-for-your-repository](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/creating-a-pull-request-template-for-your-repository)

**Infraestructura:**

- AWS Documentation: [https://docs.aws.amazon.com](https://docs.aws.amazon.com)
- SQL Server en Linux (contenedor): [https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-configure](https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-configure)
- Amazon SES: [https://docs.aws.amazon.com/ses/](https://docs.aws.amazon.com/ses/)
- Cloudinary (imágenes / DAM): [https://cloudinary.com/documentation](https://cloudinary.com/documentation)

**Redsys:**

- Portal desarrolladores Redsys: [https://pagosonline.redsys.es](https://pagosonline.redsys.es)
- Documentación técnica: [https://pagosonline.redsys.es/desarrolladores.html](https://pagosonline.redsys.es/desarrolladores.html)
- Manual de integración InSite: Solicitar a banco adquirente
- Códigos de respuesta: Consultar documentación oficial

---



#### RGPD y Legal

**Recursos oficiales:**

- AEPD (Agencia Española de Protección de Datos): [https://www.aepd.es](https://www.aepd.es)
- RGPD Texto completo: [https://gdpr.eu](https://gdpr.eu)
- Guía de Cookies AEPD: [https://www.aepd.es/guias/guia-cookies.pdf](https://www.aepd.es/guias/guia-cookies.pdf)
- Guía de Análisis de Riesgos: [https://www.aepd.es/sites/default/files/2019-09/guia-analisis-de-riesgos.pdf](https://www.aepd.es/sites/default/files/2019-09/guia-analisis-de-riesgos.pdf)

**Plantillas útiles:**

- Política de Privacidad template: Solicitar a asesor legal
- Registro de Actividades de Tratamiento: Template AEPD
- Modelo de consentimiento RGPD: Template AEPD

---



#### WhatsApp Business

**Documentación oficial:**

- WhatsApp Business API: [https://business.whatsapp.com](https://business.whatsapp.com)
- Meta for Developers: [https://developers.facebook.com/docs/whatsapp](https://developers.facebook.com/docs/whatsapp)
- Precios WhatsApp: [https://business.whatsapp.com/products/platform-pricing](https://business.whatsapp.com/products/platform-pricing)
- 360dialog Docs: [https://docs.360dialog.com](https://docs.360dialog.com)

**Categorías de mensajes:**

- Utility: Recordatorios, confirmaciones
- Marketing: Promociones, ofertas
- Authentication: Códigos OTP
- Service: Respuestas a consultas

---



#### Herramientas y Servicios

**Desarrollo:**

- GitHub: [https://github.com](https://github.com)
- Vite documentation: [https://vitejs.dev/guide/](https://vitejs.dev/guide/)
- Figma: [https://www.figma.com](https://www.figma.com)
- Postman: [https://www.postman.com](https://www.postman.com)

**Monitoreo:**

- Sentry: [https://sentry.io](https://sentry.io)
- AWS CloudWatch: [https://aws.amazon.com/cloudwatch/](https://aws.amazon.com/cloudwatch/)
- Google Analytics: [https://analytics.google.com](https://analytics.google.com)

**Testing:**

- TestFlight (iOS): [https://testflight.apple.com](https://testflight.apple.com)
- Google Play Console (Android): [https://play.google.com/console](https://play.google.com/console)

**Calculadoras:**

- AWS Pricing Calculator: [https://calculator.aws](https://calculator.aws)
- Redsys Simulator: Solicitar acceso a banco

---



### Anexo C: Contactos Recomendados



#### Proveedores de Servicios

**Redsys:**

- Contratar a través de tu banco comercial
- Bancos recomendados con Redsys:
  - BBVA
  - Santander
  - CaixaBank
  - Banco Sabadell

**WhatsApp BSP:**

- 360dialog: [https://www.360dialog.com](https://www.360dialog.com)
- Twilio: [https://www.twilio.com/whatsapp](https://www.twilio.com/whatsapp)
- Vonage (ex-Nexmo): [https://www.vonage.com](https://www.vonage.com)

**Asesoría Legal RGPD:**

- Buscar despacho local especializado en RGPD y tech
- Verificar experiencia con startups SaaS
- Solicitar referencias

**Hosting Alternativo:**

- DigitalOcean: [https://www.digitalocean.com](https://www.digitalocean.com)
- Vultr: [https://www.vultr.com](https://www.vultr.com)
- Hetzner Cloud: [https://www.hetzner.com/cloud](https://www.hetzner.com/cloud)

---



#### Comunidades y Soporte

**Desarrollo:**

- Stack Overflow: Para dudas técnicas
- Reddit r/dotnet: Comunidad .NET
- Reddit r/vuejs: Comunidad Vue.js
- Dev.to: Artículos y tutoriales

**AWS:**

- AWS Support: Plan Developer (€29/mes) o Business (€100/mes)
- AWS re:Post: Comunidad de preguntas y respuestas

**Redsys:**

- Soporte técnico: A través de tu banco adquirente
- Comunidad de desarrolladores: Foros bancarios

**WhatsApp:**

- Meta for Business Help Center
- WhatsApp Business Developers Facebook Group

---



### Anexo D: Checklist de Go-Live



#### Pre-producción (1 semana antes)

**Técnico:**

- [ ] Todos los tests pasan (unit, integration, e2e)
- [ ] Performance testing completado
- [ ] Security audit realizado
- [ ] Backup strategy configurada y probada
- [ ] Disaster recovery plan documentado
- [ ] Monitoring y alertas configurados
- [ ] SSL certificates instalados y verificados
- [ ] DNS configurado correctamente
- [ ] Redsys en modo producción configurado

**Legal:**

- [ ] Políticas de Privacidad, Cookies, T&C publicadas
- [ ] EIPD completada y aprobada
- [ ] Consentimientos implementados
- [ ] Banner de cookies funcionando

**Negocio:**

- [ ] Cliente piloto formado
- [ ] Documentación de usuario finalizada
- [ ] Videos tutoriales grabados
- [ ] Plan de soporte definido
- [ ] Pricing final confirmado

---



#### Día del Go-Live

**Mañana:**

- [ ] 09:00 - Backup completo de BD de staging
- [ ] 09:30 - Deployment a producción
- [ ] 10:00 - Smoke tests en producción
- [ ] 10:30 - Verificar Redsys en producción
- [ ] 11:00 - Activar DNS hacia producción
- [ ] 11:30 - Verificar email (SES) funcionando
- [ ] 12:00 - Meeting con cliente piloto

**Tarde:**

- [ ] 14:00 - Monitoreo activo de métricas
- [ ] 15:00 - Primera cita de prueba real
- [ ] 16:00 - Verificar logs sin errores
- [ ] 17:00 - Cliente piloto crea primera cita
- [ ] 18:00 - Retrospectiva del día

---



#### Post Go-Live (Primera semana)

**Diario:**

- [ ] Revisar logs de errores
- [ ] Revisar métricas de performance
- [ ] Revisar transacciones Redsys
- [ ] Recolectar feedback del cliente
- [ ] Actualizar documentación según sea necesario

**Semanal:**

- [ ] Meeting de retrospectiva
- [ ] Planificar fixes urgentes
- [ ] Actualizar roadmap basado en feedback
- [ ] Comunicar progreso al cliente

---



### Anexo E: Templates de Documentos



#### Template: User Story

```
Como [rol]
Quiero [funcionalidad]
Para [beneficio]

Criterios de Aceptación:
- [ ] Dado [contexto]
  Cuando [acción]
  Entonces [resultado esperado]

Notas Técnicas:
- [Consideraciones de implementación]

Definition of Done:
- [ ] Código implementado
- [ ] Tests unitarios escritos y pasando
- [ ] Code review completado
- [ ] Documentación actualizada
- [ ] Desplegado a staging y verificado
```

---



#### Template: Bug Report

```
**Título:** [Descripción breve del bug]

**Severidad:** [Crítico / Alto / Medio / Bajo]

**Entorno:** [Producción / Staging / Desarrollo]

**Pasos para Reproducir:**
1. [Paso 1]
2. [Paso 2]
3. [...]

**Comportamiento Esperado:**
[Qué debería pasar]

**Comportamiento Actual:**
[Qué está pasando]

**Screenshots/Videos:**
[Adjuntar si es posible]

**Información Adicional:**
- Navegador: [Chrome 120, Safari 17, etc.]
- SO: [Windows 11, macOS 14, etc.]
- Versión de la app: [1.0.5]
- Logs relevantes: [Adjuntar]
```

---



#### Template: Sprint Retrospective

```
**Sprint:** [Número]
**Fecha:** [DD/MM/YYYY]
**Participantes:** [Lista]

**Qué Fue Bien ✅**
- [Item 1]
- [Item 2]

**Qué Puede Mejorar 🔧**
- [Item 1]
- [Item 2]

**Acciones para el Próximo Sprint 🎯**
- [ ] [Acción 1] - Responsable: [Nombre]
- [ ] [Acción 2] - Responsable: [Nombre]

**Métricas del Sprint:**
- Velocity: [Story points completados]
- Bugs encontrados: [Número]
- Bugs resueltos: [Número]
- Test coverage: [Porcentaje]
```

---



## CONCLUSIÓN

Esta documentación describe un plan completo, detallado y viable para el desarrollo de **ReservArte**, una aplicación multi-tenant de gestión para centros de diseño de cejas en España.

### Puntos Clave del Proyecto

Mitigaciones previstas en el plan; su estado real se sigue en §10.2 y §12.2.

**Tecnología Moderna y Robusta:**

- Backend: ASP.NET Core 8.0
- Autenticación y autorización API: ASP.NET Core Identity (local + **Google, Apple, Instagram/Meta**) y JWT (Bearer); **2FA opcional** (TOTP)
- Frontend Web: Vue 3 + Vite (HMR ultra-rápido)
- Frontend Móvil: React Native
- Base de Datos: Microsoft SQL Server en contenedor Docker
- Infraestructura: AWS con alta disponibilidad

**Gestión de proyecto (ClickUp):**

- Workspace **ReservArte** con Spaces **Backend (.NET)**, **Frontend (Vue 3)**, **Mobile (React Native)**, **Infrastructure** y **Documentation**; listas según §10.1.1 (Sprint Activo, Backlog, Bugs, tareas de infra, **Technical Specs**, **Architecture Decisions**)

**Git, revisiones y CI/CD:**

- **Git Flow** en GitHub, mensajes **Conventional Commits**, plantilla de PR en `.github/PULL_REQUEST_TEMPLATE.md`, branch protection y **GitHub Actions** (§10.1.2)

**Cumplimiento Legal Estricto:**

- RGPD y LOPD compliant desde el diseño
- PCI-DSS SAQ A-EP con Redsys InSite
- Políticas de privacidad, cookies y términos
- EIPD para datos sensibles
- Datos permanecen en España/UE

**Sistema de Pagos Robusto:**

- Redsys InSite como método principal (PCI simplificado)
- Pre-autorizaciones para reducir no-shows
- Tokenización nativa para guardar tarjetas
- Penalizaciones automáticas por cancelaciones tardías
- Soporte para Bizum

**Arquitectura Escalable:**

- Multi-tenant desde el inicio
- Escalado horizontal posible
- Optimización de costos por etapas
- Preparado para 100+ organizaciones

**Notificaciones Multi-Canal:**

- Email con Amazon SES
- WhatsApp Business API (Fase 3+)
- Push notifications en apps móviles
- Recordatorios configurables



### Viabilidad Económica

**Inversión:**

- MVP (4 meses): €92,560
- Proyecto completo (9 meses): €215,739

**Costos operativos:**

- Inicio (1 org): ~€200/mes
- Escalado (50 orgs): ~€1,800/mes

**Ingresos potenciales (SaaS):**

- Mes 12: €2,500/mes (30 clientes)
- Mes 24: €10,000/mes (100 clientes)

**ROI esperado:** 18-30 meses

### Riesgos Mitigados

Mitigaciones previstas en el plan; su estado real se sigue en §10.2 y §12.2.

- Seguridad de pagos garantizada por Redsys
- Cumplimiento legal desde el diseño
- ⏳ Arquitectura probada y escalable — aislamiento multi-tenant **parcial** (`TenantMiddleware`, query filters RA-869f17vet, email único por org RA-869f1xc0u; vol. 1 §4.3.1)
- Stack tecnológico maduro y bien soportado
- Roadmap realista con hitos claros



### Próximos Pasos Inmediatos

1. **Aprobación del cliente** y firma de contrato
2. **Setup de infraestructura** AWS y repositorios
3. **Inicio del Sprint 1** de desarrollo
4. **Contacto con banco** para credenciales Redsys
5. **Contratación de asesor legal** RGPD

---

**El proyecto ReservArte está técnicamente bien fundamentado, es viable económicamente, cumple con toda la normativa legal española y europea, y tiene un camino claro hacia la rentabilidad como plataforma SaaS.**

---

**Documento elaborado por el equipo de producto e ingeniería de ReservArte**  
**Fecha:** Octubre 2025  
**Versión:** 1.0  
**Confidencialidad:** Este documento puede contener información confidencial. Su reproducción o distribución requiere autorización por escrito de las partes.

---



## FIRMAS DE CONFORMIDAD



### Por parte del Cliente

**Nombre:** Sofía Fatás Ounka___________  
**Cargo:** CEO y propietaria___________  
**Empresa:** More Than Brows__________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

---



### Por parte del proveedor / equipo de desarrollo

**Nombre:** Gabriel Sánchez-Vallejo Millán  
**Cargo:** Desarrollador de software________________  
**Organización:** ________________________________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

## **Nombre:** Guillermo Algárate del Arco  
**Cargo:** Desarrollador de software________________  
**Organización:** ________________________________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

**Fin de la documentación técnica (volumen 3 de 3)**

**Conjunto documental:**

1. Análisis y especificaciones técnicas
2. Implementación y desarrollo
3. Planificación y gestión

---

