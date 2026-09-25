---
paths:
  - "ReservArte-API/**"
  - "ReservArte-Application/**"
  - "ReservArte-Shared/**"
  - "reservarte-web/src/**"
  - "reservarte-web/e2e/**"
---

# Contrato de la API y su consumo desde la SPA

## Envelope y códigos de error

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

Huecos conocidos, cada uno con su tarea: el status de cada código se decide en cada controlador y
ya diverge (`869f6r81n` lo centraliza); los 400 de model binding salen sin envelope (`869f1k17q`);
una excepción no controlada sale como 500 sin envelope (`869f74u70`). Hasta `869f6r81n`, un código
nuevo necesita su status en todas las copias del mapa, o plantear adelantar esa tarea.

## Endpoints

- Base URL dev: API en `http://localhost:5555` (convención documentada; puerto real de
  `launchSettings.json`; NUNCA 5000 — colisiona con AirPlay en macOS). SPA en `http://localhost:3000`.
  Hoy `client.ts` y `auth.api.ts` usan una `baseURL` absoluta (`VITE_API_BASE_URL` con fallback a
  localhost) y el proxy `/api` de Vite no se usa; `869f6r69b` lo deja en un solo mecanismo (rutas
  relativas `/api` y proxy con el target por entorno). No añadas más fallbacks a localhost.
- **Login** (`POST /api/v1/auth/login`): responde con tokens normales, O con
  `{ mfaRequired: true, mfaTicket }` (sin tokens) si el usuario tiene 2FA. El frontend debe
  contemplar ambos casos: si `mfaRequired`, redirigir a `/login/two-factor`.
- **OAuth**: `GET /api/v1/auth/external/{provider}/challenge?returnUrl=...` (302 al IdP) →
  aterriza en `{SPA}/auth/callback#access_token=...&refresh_token=...` (leer del **fragmento**). Cambia con `869f6r61z` (D-17): el retorno fijará la cookie de
  refresh y no llevará tokens en la URL.
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

## Estado del frontend (2026-09-25)

- `authStore` persiste solo el access token (`localStorage['authToken']`, la clave que lee el
  interceptor). El refresh token no se guarda y `refreshAccessToken()` es un stub: al caducar el
  access token, vuelta a login. Tras recargar, `user` queda a `null`. Lo resuelven `869f6r61z`
  (backend) y `869f6r6hc` (SPA).
- `uiStore`: toasts y estado de interfaz.
- Router (`src/router/index.ts`): rutas públicas de autenticación (`/login`, `/login/two-factor`,
  `/auth/callback`, `/register`, `/forgot-password`, `/reset-password`, `/set-password/:token?`) y
  legales (`/legal/terminos`, `/legal/privacidad`); destinos de la BottomNav (`/mis-citas` y
  `/cuenta` con sesión, `/contacto` público); y el área privada, bajo su layout y con
  `requiresAuth`: dashboard, `empleados`, `clientes`, `servicios`, `citas`, `pagos`,
  `recordatorios` y `configuracion`, **todas stubs**.
- Guards `requiresAuth` y `requiresMfa`. No hay guards por rol (`869f1auqv`): un Customer puede
  navegar a `/empleados` aunque la API le deniegue los datos.
- Pantallas de autenticación completas (login local, retorno OAuth, 2FA, registro, recuperar y
  restablecer contraseña, fijar contraseña de invitación), con E2E de Playwright y axe.

## Fin de sesión en la SPA (`client.ts`)

- Petición: `Authorization: Bearer` con el token de `localStorage['authToken']`.
- 401 fuera de `AUTH_ENDPOINTS_WITHOUT_SESSION` → fin de sesión. Se decide por status, traiga el
  cuerpo que traiga. La lista recoge los endpoints cuyo 401 es un resultado de negocio (`login`,
  `mfa/verify`, `refresh-token`, `set-password` y `reset-password`); un endpoint nuevo de ese tipo
  entra en ella.
- 403 → fin de sesión solo si `error.code` está en `SESSION_ENDING_ERROR_CODES` (hoy solo
  `ORG_TENANT_MISMATCH`). **Nunca** añadas códigos de permiso como `GEN_FORBIDDEN`: significan «sin
  permiso para esto», no sesión inválida.
- El fin de sesión borra el token y navega con `window.location.href` (recarga completa) en vez de
  `router.push`: descarta el estado de Pinia sin que `client.ts` dependa del store, lo que crearía
  una dependencia circular.
- El envelope lo desenvuelve cada servicio de feature (por ejemplo `features/auth/api/auth.api.ts`),
  no los interceptores (RA-869d7f79y).
