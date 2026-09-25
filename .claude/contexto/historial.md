# Historial del proyecto

> Registro de lo hecho, para leer bajo demanda. Las entradas nuevas van arriba, en «Entradas»,
> con este formato: fecha, ID de ClickUp, PR, qué se hizo, decisiones (con su ID de
> `decisiones.md`), evidencia y batería. Debajo está, íntegro, el registro del `CLAUDE.md` anterior
> (hasta el 2026-09-23), con las correcciones del 2026-09-24 marcadas en cursiva.

## Entradas

### 2026-09-25 — `869f6r4ba` Estructura de contexto de Claude Code (PR #77)

- Sustituye el `CLAUDE.md` monolítico (577 líneas) por `CLAUDE.md` corto + `.claude/rules/` con
  ámbito de rutas + `.claude/contexto/` + cinco skills (`estado`, `siguiente`, `cerrar-tarea`,
  `cerrar-bloque`, `traspaso`). Decisiones D-02 y D-27.
- Evidencia (sesión nueva en el Mac): `CLAUDE.md` y `estado.md` cargados al arrancar; `/estado`
  cruza git, `estado.md` y ClickUp; leer `ReservArte-API/Program.cs` carga `backend.md` y
  `contrato-api.md`; `dotnet build` en `develop` con 0 avisos y 0 errores.
- Lección de proceso: el PR se mergeó sin pasar la tarea por `in progress` ni anotar «PR abierto» en
  `estado.md`; el cierre se hizo en la sesión siguiente. La primera tarea del modelo nuevo ya mostró
  que el flujo debe seguir siendo ligero.
- Batería sin cambios (no toca código): unit 506/506; E2E 57/57.

### 2026-09-24/25 — Auditoría y reestructuración del contexto (claude.ai)

- Auditoría completa del proyecto (`auditoria-2026-09-23.md`): metodología 6/10, documentación
  6/10, backend 8/10 y frontend 6/10; MVP ≈ 39 % y proyecto completo ≈ 20 %.
- Guillermo aprobó todas las recomendaciones (D-01 a D-27 en `decisiones.md`) y el modelo de
  trabajo: Claude Code como desarrollador y coordinador, 25 h/semana sin fechas comprometidas,
  Gabriel fuera de la documentación, More Than Brows como piloto con las decisiones de producto
  delegadas en Guillermo, 5555 como convención de puerto y documentación por bloque.
- ClickUp: 45 tareas y subtareas nuevas y 21 dependencias `waiting_on` (orden en `plan.md`). De lo
  existente solo se añadieron dependencias; la reorganización queda descrita en `869f74uca`.
- `CLAUDE.md` reestructurado (tarea `869f6r4ba`): reglas con ámbito de rutas en `.claude/rules/`,
  contexto bajo demanda en `.claude/contexto/`, skills de coordinación en `.claude/skills/` y
  `estado.md` como traspaso entre equipos. Nada del fichero anterior se ha perdido: lo vigente está
  en `CLAUDE.md` y en las reglas, y lo reescrito se conserva abajo con su texto original.
- Errores internos del `CLAUDE.md` anterior, corregidos (los 1, 2, 5 y 7, marcados también en su
  sitio más abajo):
  1. `CustomerPaymentMethod` sigue en `Ignore` hasta `869f2gnbm`, no hasta `869d7f3fw`.
  2. `ServicePhoto` está fuera por alcance de módulo, no porque «necesite `Appointment`».
  3. «Ya existe en frontend: … router con 7 rutas …» estaba desfasado; el estado real está en
     `.claude/rules/contrato-api.md`.
  4. Los estados de ClickUp no incluían `in review`, que el propio flujo usaba; ahora constan los
     de todas las listas.
  5. `draft` es el estado inicial de la lista Docs, no «suciedad».
  6. La sección «Estado actual (2026-09-17)» llegaba en realidad hasta el 2026-09-23.
  7. `OrganizationSettings` ya no llega con `869f2gtyv`: tiene tarea propia (`869f74u7y`) y los
     no-shows esperan a ella.
  8. «No proponer siguientes pasos fuera de turno» se sustituye por las reglas de intervención.
  9. «Moq / FluentAssertions sin fijar versión» metió FluentAssertions 8, de licencia comercial:
     ahora toda versión se fija (D-10).

---

## Registro anterior (verbatim del `CLAUDE.md` hasta el 2026-09-23)

### Estado a 2026-09-23 (sección «Estado actual», iniciada el 2026-09-17)

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
  `869f2gnbm` *(corregido 2026-09-24: decía `869d7f3fw`; el mapeo de tarjetas se trasladó a `869f2gnbm`)*; el historial de citas llega con Citas. Demo: `carmen.lopez@example.com` y
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
  Fuera de alcance: `ServiceProduct` (necesita `Product`), `ServicePhoto` (fuera por alcance de módulo *(corregido 2026-09-24: decía «necesita `Appointment`»; la documentación ya lo había corregido)*),
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
- 🚧 Backend **Sistema de Citas** (`869d7edau`) **en curso (5/12)**, abierto 2026-09-16. Es el núcleo
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
  de los paquetes. El bloque pasó de 10 a **11** subtareas, y a **12** con `869f6ae9h` (ver más abajo).
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
  Hecho también: **repositorio de citas** (`869d7f4n4`, PR #74): `IAppointmentRepository` +
  `AppointmentFilter` en `Domain/Interfaces` y `AppointmentRepository` en
  `Persistence/Repositories` (agenda paginada, detalle con líneas, rango de fechas para la agenda,
  búsqueda por pedido de Redsys). **Ningún método acepta la organización por parámetro** y sin tenant
  resuelto no devuelve nada.
  Hecho también: **disponibilidad** (`869d7f4rd`, PR #75): `IAvailabilityService` en
  `Application/Interfaces` + `AvailabilityService` en `Infrastructure/Services` (la descripción de
  ClickUp pedía `Application/Services/Appointments/`, que el repo no usa en ningún módulo), y
  `GET /api/v1/appointments/availability` en un **`AvailabilityController` propio**, con lectura para
  cualquier rol autenticado (Customer incluido). `GetAvailableSlotsAsync` resta al horario del día las
  ausencias y las citas vivas; `EnsureSlotAvailableAsync` devuelve **409 `APT_SLOT_UNAVAILABLE`** si el
  tramo se sale del horario, pisa una ausencia o pisa una cita, y su `excludeAppointmentId` es lo que
  permitirá reagendar sin chocar consigo misma. Nace `AppointmentStatuses.Blocking`
  (`pending`/`confirmed`/`in_progress`): cancelada, no presentada y completada **liberan** el hueco.
  **Decisiones del usuario:** rejilla de **15 minutos** anclada al inicio de cada tramo del horario (no
  a la hora actual, para que los huecos no se desplacen según cuándo se consulte); **sí** se descartan
  los huecos ya pasados cuando la fecha es hoy, asumiendo **`Europe/Madrid`** —zona fija en el código y
  **deuda conocida** hasta que exista `OrganizationSettings` (`869f2gtyv`; *desde el 2026-09-25, tarea propia `869f74u7y`*); si la máquina no resuelve
  la zona, avisa por log y no filtra—; y controlador propio en vez de adelantar el
  `AppointmentsController` de `869d7f519`. Detalles que no hay que volver a decidir: los intervalos son
  **semiabiertos** `[inicio, fin)` (dos citas contiguas no solapan), todo el cálculo va **en minutos
  desde medianoche** porque `TimeOnly.AddMinutes` da la vuelta al pasar de las 23:59, empleado de baja
  → 404, y `EnsureSlotAvailableAsync` **no mira el reloj** a propósito (registrar una cita que acaba de
  ocurrir lo deciden `869d7f4xf` y `869d7f519`). `TimeProvider.System` queda registrado en DI.
  Hecho también: **máquina de estados** (`869d7f4xf`, PR #76): `IAppointmentService` +
  `AppointmentService` con `ConfirmAsync`, `StartAsync`, `CompleteAsync`, `CancelAsync` y
  `MarkNoShowAsync`, fieles al diagrama de vol. 1 §5.2.2. **Sin endpoints** (son de `869d7f519`).
  Desde un estado terminal no se vuelve atrás → **409 `APT_INVALID_STATE`**; confirmar dos veces
  **no** es idempotente; `Start` exige pasar por `confirmed`. **La coherencia `Status` ↔
  `CancelledByType` se impone por construcción:** el estado de cancelación lo decide quién cancela y
  el tipo se rellena a juego. **Decisiones del usuario:** cancelan el personal **y la clienta dueña**
  de la cita (una clienta sobre una cita ajena recibe **404**, no 403, para no confirmarle que
  existe); y el genérico **`cancelled` no lo escribe nadie** —se sigue aceptando al leer—. Roles:
  Admin/Manager/Employee confirman, empiezan, cierran y cancelan; el **no-show solo Admin o
  Manager**. En las cuatro transiciones fijas el rol se comprueba **antes** de cargar la cita (al
  revés que en `EmployeeService`, donde el permiso depende del dato), para que la diferencia entre
  403 y 404 no sirva para sondear qué citas hay; **`CancelAsync` es la excepción** y carga primero,
  porque el permiso sí depende del dato: hay que saber si la clienta es la dueña. Lo señaló la IA de
  documentación al auditar. `UpdatedAt` lo sella **el repositorio** en `Update()` y `CancelledAt` **el
  servicio** con `TimeProvider`: el servicio sellaba los dos y lo destapó el test de integración.
  **Alcance recortado (decisión del usuario):** la penalización económica al cancelar necesita
  `OrganizationSettings` (`869f2gtyv`) y Redsys (`869d7eden`), así que sale a la **subtarea nueva
  `869f6ae9h`** y el bloque pasa de 11 a **12** subtareas; anotado también en las dos tareas dueñas.
  Batería: unit **506/506**, E2E 57/57 (no reejecutados; la SPA no se toca).
- 📋 Backlog no bloqueante: `869en8a17` (rate limiting + `AUTH_MFA_INVALID`), `869f151x1`
  (2FA en OAuth), `869f1812p` (EmailConfirmed), `869f17y6k` (unificar Result/AuthResult),
  `869f1k17q` (400 de model binding sin envelope), `869f1mqah` (resultados de Identity ignorados en auth), `869f2gh37` (tests de integración HTTP con
  `WebApplicationFactory`), `869f2gtz8` (`AuditLog` transversal).

**`dotnet format` ya es puerta de calidad de verdad** (`869f2pjf8`, cerrada 2026-09-16, PR #72 y
#73): `dotnet format --verify-no-changes` sale **0 avisos y código 0** sobre `develop`. Se eligió el
camino de **alinear el espaciado** (los 113 avisos eran todos `WHITESPACE`, 15 ficheros) y después
se añadió **`.editorconfig`** en la raíz (PR #73), que fija por escrito el estilo real: 4 espacios en
C# y 2 en el frontend, namespaces de ámbito de fichero, llaves Allman, `using` de System primero,
salto de línea final y `_camelCase` en campos privados. Reglas de nombres en `suggestion` a
propósito, para que `format` no falle por un nombre. **`end_of_line` NO se fija para el código**:
con `core.autocrlf=true` el índice guarda LF y el árbol de Windows tiene CRLF, así que fijarlo
rompería el formateo en uno de los dos equipos; de eso se encarga git. Las migraciones quedan
excluidas con `generated_code = true`. A partir de ahora, la casilla del DoD
«el linter no reporta errores nuevos» se marca de verdad, no «sin errores nuevos»: **cualquier
aviso que aparezca lo ha introducido el PR**. Al medirlo, NO encadenar con `| tail`: se leería el
código de salida de `tail` (0) y parecería que pasa.

### Dónde continuar (2026-09-23)

**Bloque Sistema de Citas (`869d7edau`) abierto, 5/12.** Es el núcleo del producto. El catálogo de
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

**`869d7f4n4` hecha (PR #74):** `IAppointmentRepository` (en **`Domain/Interfaces`**, no en
`Application/Interfaces` como decía ClickUp: manda el precedente del repo) y `AppointmentRepository`
en `Persistence/Repositories`, con `AppointmentFilter`. **Ningún método recibe la organización por
parámetro** —la descripción pedía `GetByDateRangeAsync(orgId, …)`—: el tenant sale de
`ICurrentOrganizationService`, como en el resto de repositorios, y pasarlo por argumento permitiría
leer la agenda de otro centro. `GetByIdAsync` y `GetByRedsysOrderAsync` van **con seguimiento**
(son lecturas para escribir); `GetPagedAsync` y `GetDetailAsync`, `AsNoTracking`.
`GetByDateRangeAsync` **no filtra por estado** a propósito: si una cancelada ocupa hueco lo decide
quien detecte solapes (`869d7f4rd`). **Documentación aplicada y auditada** (`6b58683`): vol. 2 §9.9
con sus **6** decisiones (las 7 son las del mapeo, `869d7f4j8`; lo detectó la IA de documentación al
auditar el prompt de `869d7f4rd`), vol. 1 §3.1.5 y el contrato de `/history`, vol. 3 (3/11) y estrategia de
testing. Corrigió además **dos contradicciones** que detecté al auditar: el árbol de
`Análisis de pantallas y estructura.md` ponía las implementaciones en `Infrastructure/Repositories`
(lo real es `Persistence/Repositories`) y vol. 2 §9.7 seguía diciendo que el historial de citas
«necesita `Appointment`, que no está mapeado».

**Advertencias abiertas que dejó la documentación** (ninguna bloquea; no volver a decidirlas):
- **La descripción de ClickUp de los repositorios pide rutas que el código no usa**
  (`Application/Interfaces`, `Infrastructure/Repositories`). La fuente de verdad es
  **`Domain/Interfaces` + `Persistence/Repositories`**. Copiar la descripción al pie de la letra haría
  nacer fuera de sitio el repositorio de la **lista de espera** (`869f2yh9b`).
- **Ningún repositorio acepta la organización por parámetro.** Es aislamiento por construcción, no un
  detalle de Citas.
- `AppointmentFilter.Status` es **un** valor: listar las tres cancelaciones exige tres consultas o
  ampliarlo a colección cuando el servicio lo pida.
- La puerta de `dotnet format` es **local**: no hay job de CI que la ejecute (se cruza con
  `869eqxm7w`). Igual que los scripts de `data/`.
- Los cuatro `ReservArte-*/Class1.cs` de `dotnet new classlib` siguen ahí (solo se les quitó el BOM).
  Borrarlos es limpieza razonable y **no tiene tarea**.
- El árbol de `Análisis de pantallas y estructura.md` sigue mezclando estructura actual y objetivo
  (`869f2g60e`, lista Docs, en `draft`): solo se alineó el recorte de repositorios.

**`869d7f4rd` cerrada y mergeada (PR #75):** disponibilidad de la agenda, detalle y decisiones en
«Estado actual». Pendiente de que el usuario aplique la documentación.

**`869d7f4xf` cerrada y mergeada (PR #76):** máquina de estados, detalle y decisiones en «Estado
actual». Pendiente de que el usuario aplique la documentación.

**Siguiente en orden: `869d7f519`** (6/12) — endpoints de citas. Después: `869d7f53r` (tests),
`869f2yh9b` (lista de espera), `869f2g02q` (promoción de categoría), `869f2gn91` (`/history`),
`869f2gtyv` (no-shows, que trae `OrganizationSettings`) y `869f6ae9h` (penalización al cancelar,
bloqueada por las dos anteriores).
**Una tarea a la vez, en orden. No adelantar tareas ni proponer siguientes pasos fuera de turno.**
*(Sustituido el 2026-09-24: el orden vigente está en `plan.md`, y las reglas de intervención, que
permiten proponer en momentos acordados, en `CLAUDE.md`.)*

**Criterio del módulo, para retomarlo en otra sesión:** lectura para cualquier rol autenticado
(Customer incluido) y escrituras Admin|Manager; baja lógica idempotente; y las verificaciones con
migración se hacen levantando la API contra una base **desechable** creada con los scripts de
`data/`, nunca sobre `ReservArteDB`. **Cuidado con `regenerate-create.sh`:** usa `--no-build`, así que
hay que compilar antes o genera un `create` sin la migración nueva y **aun así informa de éxito**.
En Windows fallaba entero (`DirectoryNotFoundException` de `dotnet ef`) porque usaba una variable
`TMP`, que ahí **ya es variable de entorno**: se la pasaba a `dotnet ef` como directorio temporal.
Renombrada a `SCRIPT_TMP` en `869d7f4j8`; misma precaución con `TEMP` en cualquier script nuevo.

### Traspaso Windows → Mac (2026-09-17)

**Estado al cambiar de equipo.** `develop` en `6b58683`, **sincronizado con `origin`**, árbol limpio,
`dotnet build` 0/0, `dotnet format --verify-no-changes` **código 0** y batería **432/432**. No hay
ninguna rama de trabajo abierta, ningún PR sin mergear ni base de datos de prueba colgando (solo
`ReservArteDB`). Documentación de todo lo hecho **aplicada y auditada**. Nada a medias.

**Al llegar al Mac:** `git checkout develop && git pull && dotnet build`, y esperar a elegir tarea.
Allí hay que usar `npm run test:e2e` en vez de `npx playwright test`, y **no** hace falta
`MSYS_NO_PATHCONV=1` para `sqlcmd` (eso es solo de Git Bash en Windows). **El shell del Mac es zsh**,
así que los comandos de `data/README.md` del tipo `SQLCMD="docker exec …"` + `$SQLCMD < fichero.sql`
**fallan** (zsh no parte la variable en palabras) y `${PIPESTATUS[0]}` no existe (es `${pipestatus[1]}`,
y leerlo mal da un éxito falso): esos bloques van en un `.sh` con `#!/usr/bin/env bash` y
`set -euo pipefail`, ejecutado con `bash`.

~~**OJO CON LA BASE DE DATOS DEL MAC: se quedó dos migraciones por detrás.**~~ **Resuelto el
2026-09-23**, y de paso una corrección: eran **seis** migraciones, no dos (la base estaba en
`NormalizeRolesToPascalCase`, del 13-sep). Se aplicaron y después, por decisión del usuario, se
**recreó la base entera con los scripts de `data/`** (drop → create → demo) para tener datos demo de
Clientes y del catálogo, que esa base nunca llegó a ver. Quedó con las 12 migraciones, 24 tablas, 2
empleadas con horario, 2 clientas, 2 categorías y 3 servicios; citas y lista de espera vacías, que es
lo correcto porque nadie las siembra. **Efectos secundarios:** desaparecieron las cuentas de prueba
manual (`prueba@test.com`, `rgpd@test.com`, `guillermo.algarate@flat101.es`) y el **2FA** que tenía
`guille@svalero.com`, que ahora entra sin MFA. Comprobación en cualquier equipo:
`dotnet ef migrations list` (marca las `(Pending)`) o `SELECT MigrationId FROM __EFMigrationsHistory`
frente a `ReservArte-Infrastructure/Persistence/Migrations/`.

**Recorrido de esta sesión en Windows.** Bloque de **Citas** (`869d7edau`) de 1/11 a **3/11**:
migración de las tres tablas (`869d7f4j8`, PRs #70 y #71) y repositorio de citas (`869d7f4n4`,
PR #74). Por el camino se cerró la deuda de **`dotnet format`** (`869f2pjf8`, PRs #72 y #73): línea
base de 113 avisos a **0** y **`.editorconfig` nuevo** en la raíz, que fija el estilo del repo. Suite
de 388 a 432. También se arregló `data/schema/regenerate-create.sh`, que **fallaba entero en
Windows**, y se corrigió `data/README.md`.

**Decisiones del usuario tomadas en esta sesión** (no volver a preguntarlas): `WaitingList` entra en
la migración de citas; su tabla se llama **`WaitingLists`** (la entidad sigue siendo `WaitingList`);
los campos `redsys_auth_code` / `redsys_transaction_type` del sketch los decide `869d7eden` y
`created_by` lo decide `869d7f519`; y la línea base de `dotnet format` se alinea en lugar de retirar
la casilla del DoD.

**Suciedad conocida de ClickUp** (limpiar al arrancar el bloque que toque, no antes): `869d7edt7`
sigue en `backlog` con fechas 2026-05-24 → 2026-06-05, ya pasadas; y `869f2g60e` (árbol mezclado de
`Análisis de pantallas y estructura.md`) está en **`draft`**, no en `backlog`. *(Corregido 2026-09-24: `draft` es el estado inicial de la
lista Docs; no es suciedad.)*

**Limpieza pendiente de los repos locales:** en **Windows** quedan **31 ramas locales** ya mergeadas
(las 5 de esta sesión incluidas) y en el **Mac**, **25**. No afectan al remoto; se pueden borrar
cuando apetezca con `git branch -d`.

### Secciones reescritas del `CLAUDE.md` anterior (texto original)

Lo que la estructura nueva reescribió o corrigió, tal como estaba. El resto (arquitectura, contrato
de API, theming y base de datos) pasó literal a `.claude/rules/`.

~~~markdown
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

[…]

- Base URL dev: `http://localhost:5555` (puerto real de `launchSettings.json`; NUNCA 5000 — colisiona con AirPlay en macOS). SPA en `http://localhost:3000`, proxy Vite `/api` → 5555.

[…]

- **Ya existe en frontend:** `authStore` (hidrata `localStorage['authToken']`), `uiStore`,
  router con 7 rutas y guards `requiresAuth`/`requiresMfa`, `client.ts` (Axios + Bearer + 401→login).
  Las páginas son **stubs** pendientes de implementar (este bloque de trabajo).

[…]

## Flujo de trabajo por tarea (ESTRICTO)

1. Rama `feature/{clickup-id}-{descripcion-corta}` desde `develop`.
2. Mover la tarea de ClickUp a "in development".
3. Implementación por fases, con **verificación por evidencia** antes de cerrar (no dar por
   hecho lo que no se ha probado; en este proyecto las verificaciones "seguras" han cazado
   varios fallos silenciosos).
4. Rellenar plantilla de PR (`.github/PULL_REQUEST_TEMPLATE.md`), abrir el PR, **mover la tarea a
   "in review"** y **PARAR**: el usuario lo aprueba y mergea, y avisa.
5. Marcar la tarea **"shipped" tras el merge**. Manda el DoD de la plantilla de PR, que pide
   `In Review` al pedir revisión (decisión del usuario, 2026-09-23: antes este flujo decía
   «shipped tras verificar», y las dos fuentes se contradecían).
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
Estados: `backlog` → `in development` → `shipped`. **La lista de Infra usa otros**:
`backlog` → `in progress` → `blocked` → `done` → `cancelled` (está en otro space, `90127424786`);
mandarle `in development` da «Status does not exist». Subtareas: `clickup_create_task` con `list_id`
(debe coincidir con la lista del padre) + `parent`. Último bloque cerrado: **CRUD Clientes**
(`869d7ed68`, backend, 6/6). **Bloques en curso:** CRUD Servicios (`869d7ed7v`, backend, 5/6; parado
a la espera de Citas) y **Sistema de Citas** (`869d7edau`, backend, 5/12).
Para trasladar una subtarea a otro bloque (no se puede cambiar el padre):
crear la nueva bajo el padre destino y cancelar la original con comentario que la enlace.

[…]

## Preferencias de trabajo

- **Idioma: español** en todo (comunicación, comentarios, mensajes de commit en inglés convencional).
- Al dar código: **archivos completos** o fragmentos con ruta exacta e indicación precisa de dónde va.
- Verificación con evidencia antes de cerrar cualquier tarea.
- Conventional Commits + Git Flow.
- No hardcodear credenciales; secretos en User Secrets (dev) — ver guía en `/Documentation`.
~~~
