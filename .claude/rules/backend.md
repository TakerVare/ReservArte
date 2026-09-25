---
paths:
  - "ReservArte-API/**"
  - "ReservArte-Application/**"
  - "ReservArte-Domain/**"
  - "ReservArte-Infrastructure/**"
  - "ReservArte-Shared/**"
  - "tests/**"
---

# Backend (.NET) — reglas

## Capas y dónde va cada cosa

- `ReservArte-API/`: `Controllers/`, `Middleware/`, `Extensions/`, `Options/`, `Program.cs`.
- `ReservArte-Application/`: `DTOs/`, interfaces de servicio (`Interfaces/`), validadores
  FluentValidation y `Mapping/`. Los casos de uso **no** viven aquí (decisión D-15).
- `ReservArte-Domain/`: `Entities/`, constantes de catálogo (strings, nunca `enum`: `Roles`,
  `AppointmentStatuses`, `EmployeeLevels`…) e interfaces de repositorio (`Interfaces/`).
- `ReservArte-Infrastructure/`: EF Core en `Persistence/` (`Repositories/`, `Migrations/`),
  servicios de aplicación en `Services/`, seeders y email.
- `ReservArte-Shared/Api`: envelope (`ApiResponse`, `ApiError`, `ApiErrorDetail`, `ApiMeta`) y
  `ErrorCodes`.
- Si una descripción de ClickUp pide otra ruta (`Application/Services/…`,
  `Infrastructure/Repositories`), manda el precedente del repo, no la descripción.

## Multi-tenant

`TenantMiddleware` resuelve la organización por cabecera `X-Organization-Id`
(dev, con fallback `DefaultOrganizationId`) o subdominio (prod). Valida coherencia con el claim
`organization_id` del JWT si la petición está autenticada (403 si discrepan).
**Query filters globales por `OrganizationId`** en `AppDbContext` para TODA entidad multi-tenant
mapeada (`Employee`, `User`, `UserLogin`, `RefreshToken` vía su usuario, `EmployeeAvailability`,
`EmployeeException`, `Customer`, `CustomerNote`, `CustomerAllergy`, `CustomerConsent`, el catálogo de
servicios completo, y `Appointment`, `AppointmentServiceItem` y `WaitingList`) — RA-869f17vet. Sin
tenant resuelto (migraciones, seeders) no restringen.
Un test de metadatos falla si una entidad nueva con `OrganizationId` se mapea sin filtro: al
añadir módulos (Clientes, Servicios, Citas…), el filtro es obligatorio. Saltarse el filtro
(`IgnoreQueryFilters()`) solo con justificación; hoy no hay ningún uso en código de producción.

**Ojo, fallan en abierto:** sin tenant, `CurrentOrganizationId == null` deja ver todas las
organizaciones; hoy lo compensan los repositorios con `Where(_ => false)`. Cualquier consulta que no
pase por un repositorio, o un job sin petición HTTP, lo vería todo. Se invierte en `869f6r5vy`
(cerrado por defecto con ámbito de sistema explícito), que va antes de Hangfire.

**Email único por organización, no global** (RA-869f1xc0u): la misma persona puede tener cuenta
en varios centros. Índices únicos `(OrganizationId, NormalizedEmail)` y `(OrganizationId,
NormalizedUserName)` en `AspNetUsers`, `(OrganizationId, Email)` en `Employees`, y clave
`(OrganizationId, LoginProvider, ProviderKey)` en `AspNetUserLogins` (entidad `UserLogin`; la
organización la rellena `OrganizationUserStore` al vincular). El `UserValidator` de Identity valida
por organización porque busca a través del filtro; no hay validador propio. Todo índice único de una
entidad multi-tenant nace con `OrganizationId` delante (Clientes incluido). **Sin tenant resuelto**
(seeders, futuros jobs) `FindByEmailAsync` falla si el email está en dos centros: ese camino debe
fijar antes la organización en `ICurrentOrganizationService`.

Ningún repositorio acepta la organización por parámetro: el tenant sale de
`ICurrentOrganizationService`. Pasarlo por argumento permitiría leer datos de otro centro.

## Autenticación

**Auth (completa y verificada):** JWT (claims `sub`/`email`/`organization_id`/`role` [corto,
no URI]/`jti`) con `MapInboundClaims = false` en emisión y validación. Refresh token opaco
con rotación. OAuth Google/Apple/Meta (Meta con esquema "Instagram"), tokens a la SPA por
**fragmento de URL**. 2FA TOTP con ticket intermedio (`mfa_pending`, 5 min, sin `role`) →
`POST /auth/mfa/verify` → JWT final. Códigos de recuperación de un solo uso. Rate limiting
nativo .NET 8 (10/h login, 20/h verify) → 429. CAPTCHA verificable (Turnstile, desactivado en dev).
La baja de un empleado **bloquea su cuenta** (lockout de Identity como interruptor, no como contador;
login/refresh/MFA/OAuth lo comprueban — RA-869f180e5). Hueco conocido: **el login social se salta el
2FA** (el callback externo emite tokens definitivos sin ticket `mfa_pending`) — RA-869f151x1.

Cambio aprobado (D-17, `869f6r61z`): el refresh token pasará a una cookie httpOnly y el retorno OAuth
dejará de llevar tokens en la URL. Hasta entonces, no construyas nada nuevo que dependa del refresh
token en el cuerpo ni del fragmento.

## Transacciones: ficha y cuenta de Identity

**Escrituras que abarcan ficha y cuenta de Identity** (RA-869f1811u): siempre dentro de
`IUnitOfWork.ExecuteInTransactionAsync` (`EfUnitOfWork`). La transacción se abre DENTRO de la
estrategia de ejecución (`EnableRetryOnFailure` rechaza transacciones abiertas a mano); confirma si
el `Result` es éxito y, si no, deshace **y vacía el change tracker**. Comprobar SIEMPRE el
`IdentityResult`: el `UserManager` comparte el `AppDbContext`, y un cambio que Identity rechaza queda
en memoria y lo persistiría el siguiente `SaveChanges`. La operación puede reejecutarse ante un
fallo transitorio: construir entidades dentro y dejar los efectos externos (correos) para después
del commit.

## Empleada y clienta con la misma cuenta

**Empleada y clienta con la misma cuenta** (RA-869d7f369): un `User` puede tener ficha `Employee` y
ficha `Customer` con el mismo Id; `User.Rol` es el rol de personal. `CustomerService` distingue **cuenta
de personal** (`Rol != Customer`, falla cerrado) de cuenta solo de cliente. El alta de cliente con el
email de una cuenta del centro sin ficha **añade la ficha** sin tocar la cuenta ni invitar; con ficha ya
existente → 409. Una cuenta nueva nace `Customer` sin contraseña y recibe la invitación `set-password`
tras el commit. Editar la ficha de una cuenta de personal no toca la cuenta, y cambiar su email es
**403** (se cambia desde Empleados: evita que se secuestre el acceso del personal desde Clientes). En
cuenta solo de cliente, nombre/email/teléfono/imagen se sincronizan (SetEmail solo si cambia). La baja
de la ficha de cliente **no** hace lockout. Toda ficha nace categoría **`new`** (también registro y
alta social); la promoción a `regular` llega con Citas (`869f2g02q`).

## EF Core y migraciones

- Tablas en plural, siempre (ninguna en singular: ver `WaitingLists`).
- Entidades fuera de alcance: `modelBuilder.Ignore<T>()`. Configuraciones con `ApplyConfiguration`
  explícito.
- Entidades hijas con `OrganizationId` `Guid` propio y navegación `Organization` (RA-869f17myx),
  para que el filtro no dependa de un JOIN.
- CHECK de catálogos con `CatalogCheck`, generados desde las constantes del dominio.
- Un único que admite nulos va filtrado (`WHERE [Col] IS NOT NULL`): en SQL Server, un único sin
  filtro solo admite un nulo.
- Histórico de negocio: FK en `Restrict` (citas → clientes y empleados).
- Cada migración arrastra la regla de datos: regenerar el `create` y verificar sobre una base
  desechable (`.claude/rules/datos.md`).

## Módulo de citas (lo ya decidido)

- `AppointmentStatuses`: 8 valores; `Cancellations` (las tres cancelaciones), `Terminal` (de los que
  no se sale) y `Blocking` (`pending`, `confirmed`, `in_progress`: los que ocupan hueco).
- Disponibilidad: rejilla de 15 minutos anclada al tramo; intervalos semiabiertos `[inicio, fin)`;
  cálculo en minutos desde medianoche (`TimeOnly.AddMinutes` da la vuelta pasadas las 23:59);
  empleado de baja → 404; `EnsureSlotAvailableAsync` no mira el reloj y admite
  `excludeAppointmentId` para reagendar.
- Máquina de estados: desde un terminal → 409 `APT_INVALID_STATE`; confirmar dos veces no es
  idempotente; `Start` exige `confirmed`; el rol se comprueba antes de cargar, salvo en
  `CancelAsync`; una clienta sobre una cita ajena recibe 404.
- `UpdatedAt` lo sella el repositorio en `Update()`; `CancelledAt`, el servicio con `TimeProvider`
  (registrado en DI: úsalo en vez de `DateTime.UtcNow`).
- Repositorio: `GetByIdAsync` y `GetByRedsysOrderAsync` con seguimiento; listas `AsNoTracking`;
  `GetByDateRangeAsync` no filtra por estado; `AppointmentFilter.Status` es un único valor.

## Deudas conocidas: no las repliques

- El mapa código de error → HTTP está copiado en cada controlador y ya diverge. No añadas otra copia:
  si necesitas un código nuevo antes de `869f6r81n`, plantea adelantarla.
- No hay manejador global de excepciones (`869f74u70`): una excepción no controlada sale como 500
  sin envelope.
- `Europe/Madrid` está fijo en `AvailabilityService` hasta `869f74u7y`.
- El proveedor de email se elige con `IsDevelopment()` hasta `869f6r5jf`.
- AutoMapper y MediatR están de salida (D-10): no añadas perfiles ni handlers nuevos.

## Tests

- xUnit + Moq + FluentAssertions, con versiones fijadas; no subas FluentAssertions.
- Nombres de test en español que enuncian la regla de negocio.
- Los repositorios se prueban hoy contra SQLite, que no reproduce colaciones, `LIKE`,
  `DateOnly`/`TimeOnly` ni los CHECK de SQL Server. Lo que dependa de eso, a integración con
  Testcontainers en cuanto exista `869f6r5ng`.
- Siempre que añadas una entidad con `OrganizationId`, el test de metadatos debe seguir en verde.
- `dotnet format --verify-no-changes` debe salir con código 0. No lo encadenes con `| tail`: leerías
  el código de salida de `tail` (0) y parecería que pasa.
- `.editorconfig`: 4 espacios en C# y 2 en el frontend, namespaces de ámbito de fichero, llaves
  Allman, `using` de System primero, salto de línea final y `_camelCase` en campos privados (reglas
  de nombres en `suggestion`). `end_of_line` no se fija para el código (con `core.autocrlf=true`
  lo gestiona git); las migraciones van excluidas con `generated_code = true`.

## Evidencia mínima al cerrar una tarea de backend

- `dotnet build` 0 errores y 0 avisos nuevos; `dotnet test` con el recuento; format con código 0.
- Cambios de API: respuesta HTTP real con envelope, por rol, y aislamiento Org A ≠ Org B si aplica.
- Migraciones: base desechable creada con `data/`, API arrancada contra ella y
  `dotnet ef migrations has-pending-model-changes` limpio.
