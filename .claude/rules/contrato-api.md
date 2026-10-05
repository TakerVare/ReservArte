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

**Status de cada código: un único mapa**, `ErrorStatusCodes` (`ReservArte-Shared/Api`), junto al
catálogo (`869f6r81n`). Un código nuevo se añade a `ErrorCodes` **y** a `ErrorStatusCodes` (un test
falla si falta); un código fuera del catálogo sale como 500. Los controladores heredan de
`ApiControllerBase` y responden los errores con `FromFailure(result)` o `Failure(código, mensaje)`,
nunca con `BadRequest`/`NotFound`/`StatusCode` escritos a mano (quedan algunos en `MfaController`, a
revisar con `869en8a17`). Un único tipo de resultado: `Result<T>` (`AuthResult<T>` se retiró).

Fuera de los controladores (middleware de tenant, límite de peticiones, eventos de JwtBearer), el
error se escribe con `ApiErrorWriter.WriteAsync`, que toma el status del mismo mapa. Una excepción
no controlada la recoge `GlobalExceptionHandler` (`869f74u70`): 500 `GEN_INTERNAL_ERROR` con
envelope; solo en Development `error.details` lleva el tipo y el mensaje, nunca la traza. Si el
cliente corta la petición, 499 sin cuerpo.

Lo que falla antes de la acción también lleva envelope (`869f1k17q`):
- 400 de model binding → `GEN_VALIDATION_FAILED` (`InvalidModelStateResponse`), con `details` por campo
  y mensajes fijos en español (nunca los del parser): `InvalidJson` (JSON roto o tipo equivocado; el
  campo es la ruta JSON en camelCase, `weeklySchedule[0].dayOfWeek`), `MissingBody` (campo `body`) e
  `InvalidFormat` (parámetro de ruta o consulta no convertible, `page=abc`).
- 404 de ruta inexistente → `GEN_NOT_FOUND`; 405 → `GEN_METHOD_NOT_ALLOWED` con la cabecera `Allow`
  (`ApiStatusCodePages`, solo bajo `/api` y solo si la respuesta sale vacía).

## Fechas con hora (RA-869f8pmnm)

- Toda fecha con hora (`DateTime`) de entrada, en el cuerpo JSON o en la query, va en ISO 8601
  **con zona**: `Z` o desplazamiento (`+02:00`). La API la convierte al instante UTC exacto.
- **Sin zona → 400 `GEN_VALIDATION_FAILED`**, con el campo y el código `MissingTimeZone`: es ambigua
  (y PostgreSQL la rechazaría). En el cuerpo lo marca `UtcDateTimeJsonConverter` y lo rechaza el
  validador; en la query, el servicio.
- Toda fecha con hora de salida va **en UTC con `Z`** (`UtcDateTimeJsonConverter`, registrado en
  `AddJsonOptions`). Las fechas sin hora (`DateOnly`) y las horas (`TimeOnly`) no llevan zona.
- Un campo `DateTime` nuevo en un DTO de entrada lleva la misma regla en su validador
  (`MissingZoneCode`/`MissingZoneMessage` de `CreateEmployeeExceptionRequestValidator`).

## Endpoints

- Base URL dev: API en `http://localhost:5555` (convención documentada; puerto real de
  `launchSettings.json`; NUNCA 5000 — colisiona con AirPlay en macOS). SPA en `http://localhost:3000`.
- **Un solo mecanismo de URL** (`869f6r69b`): la SPA llama a la API con rutas relativas (`/api/...`)
  en su mismo origen. El cliente Axios no tiene `baseURL` y el reto OAuth es relativo. En
  desarrollo, el proxy `/api` de `vite.config.ts` reenvía a `API_PROXY_TARGET` (sin prefijo
  `VITE_`, para que no llegue al navegador; por defecto `http://localhost:5555`) con `changeOrigin`,
  así que la API ve su propio Host y el `redirect_uri` de OAuth no cambia. En producción, quien
  sirve la SPA debe servir también `/api` en el mismo origen.
  - No hay `VITE_API_BASE_URL` ni `VITE_APP_URL`: el `returnUrl` de OAuth sale de
    `window.location.origin`. Una variable `VITE_*` se incrusta en el bundle al compilar, y un build
    hecho con el `.env` de desarrollo acabaría llamando a localhost en producción.
  - No añadas URLs absolutas ni fallbacks a localhost en `src/`. `e2e/api-origin.spec.ts` comprueba
    la URL real de las peticiones: `page.route` resuelve el CORS por su cuenta, así que un test que
    solo mire la respuesta no detecta un cliente que apunte a otro origen.
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
  GET devuelve `{ weeklySchedule, exceptions, exceptionsFrom, exceptionsTo }`; `from`/`to` (con
  zona; sin ella, 400) acotan las ausencias, por defecto desde hoy y 90 días. PUT **reemplaza la semana entera** (lista
  vacía = sin horario); valida día 0-6, fin > inicio y ausencia de solapes. Las ausencias son baja
  lógica. La **lectura** la permite a cualquiera del módulo; las **escrituras** aplican la regla de
  que un Manager no toca a un Admin.
- **Servicios que presta** (4.1b): `GET|PUT /api/v1/employees/{id}/services`, mismos roles. GET
  devuelve `{ employeeId, services: [{ serviceId, name, durationMinutes, proficiencyLevel,
  serviceIsActive }] }` (asignaciones activas, por nombre). PUT `{ serviceIds }` **reemplaza el
  conjunto entero** (lista vacía = sin servicios, y fuera de la reserva); los repetidos cuentan una vez.
  Las asignaciones no se borran: las que salen se desactivan y las que vuelven se reactivan con su
  nivel; las nuevas nacen con nivel 1. Servicio inexistente, retirado o de otro centro → 400
  `serviceIds[i]` con código `UnknownService`. Un Manager no toca a un Admin (403).
- **Clientes** (RA-869d7f3bt): la clase admite **Admin, Manager y Employee** (lectura); POST/PUT/DELETE y
  reactivate exigen además **Admin o Manager**; Customer → 403. `GET /api/v1/customers?search&category&
  isBlocked&isActive&page&pageSize` (`data.items` + `meta.pagination`; sin `isActive` = solo activos),
  `GET /{id}` (perfil con consentimientos, alergias y notas vigentes), `POST` (201 + Location;
  `grantedConsents` con `data_processing` obligatorio → si no, 400 `field=grantedConsents`; 409 si el email
  ya tiene ficha), `PUT /{id}` (403 si cambia el email de una cuenta de personal), `DELETE /{id}` (baja
  lógica idempotente, **sin** lockout) y `POST /{id}/reactivate`. **Ficha completa** (4.2b):
  `PUT /{id}/consents/{consentType}` (`{ granted }`, Admin o Manager; devuelve el perfil; retirar
  conserva `grantedAt` y sella `revokedAt`; **retirar `data_processing` da de baja la ficha**, H-47;
  finalidad desconocida → 400 `consentType` `UnknownConsent`), `POST|PUT|DELETE /{id}/allergies[/{allergyId}]`
  (todo el personal; baja lógica idempotente; una retirada no se edita, 404) y `POST /{id}/block`
  (`{ reason }` obligatorio ≤500) / `POST /{id}/unblock` (Admin o Manager; desbloquear borra el motivo). **Notas** (RA-869d7f3fw), todo el personal:
  `POST /{id}/notes` (`{ note }` ≤2000; 201; cada nota lleva `employeeName`, el nombre de su autora, porque la ficha la ve todo el personal y la lista de empleados no; la firma la ficha `Employee` **activa** de quien llama, sin ella
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
- **Configuración del centro** (`869f74u7y`): `GET|PUT /api/v1/organization/settings`. **La lee
  cualquier rol autenticado** (`869f6r71x`: la SPA necesita la zona del centro) y la cambian **Admin
  o Manager** (Employee y Customer → 403 en el PUT). Devuelve `{ timeZone,
  cancellationHoursThreshold, maxNoShowsBeforeBlock, updatedAt }`. Un centro que no la ha guardado
  recibe los valores por defecto (`Europe/Madrid`, 24, 3) con `updatedAt: null`. El PUT **reemplaza
  la configuración entera** (los tres campos obligatorios; el primero crea la fila): `timeZone` es un
  identificador IANA que el servidor resuelva (si no, 400 `timeZone` con código `UnknownTimeZone`; los
  nombres de Windows no valen), umbral entre 0 y 720 horas y máximo de no presentaciones entre 1 y 99.
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
  `/cuenta` con sesión, `/contacto` público; `/` redirige a `/mis-citas`); y el área privada, con
  `requiresAuth`: `empleados`, `clientes`, `servicios`, `citas`, `pagos`,
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
