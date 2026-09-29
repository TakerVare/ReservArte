# Historial del proyecto

> Registro de lo hecho, para leer bajo demanda. Las entradas nuevas van arriba, en «Entradas»,
> con este formato: fecha, ID de ClickUp, PR, qué se hizo, decisiones (con su ID de
> `decisiones.md`), evidencia y batería. Debajo está, íntegro, el registro del `CLAUDE.md` anterior
> (hasta el 2026-09-23), con las correcciones del 2026-09-24 marcadas en cursiva.

## Entradas

### 2026-09-29 — `869f1k17q` Envelope en model binding, 404 de ruta y 405 (PR #97)

- Medido antes con una sonda: los 400 de model binding salían como ProblemDetails (inglés, detalles
  del parser, campos `$.password` y `request`); 404 de ruta y 405, vacíos.
- `InvalidModelStateResponse` (`InvalidModelStateResponseFactory`): 400 `GEN_VALIDATION_FAILED` con
  detalles `InvalidJson` (ruta JSON en camelCase), `MissingBody` (`field: "body"`, sin el `request` del
  framework) e `InvalidFormat` (ruta o consulta), con mensajes fijos en español.
- `ApiStatusCodePages` (`UseStatusCodePages`): 404 `GEN_NOT_FOUND` y 405 `GEN_METHOD_NOT_ALLOWED`
  (código nuevo del catálogo, con `Allow`), solo bajo `/api` y con la respuesta vacía.
- Evidencia: unit 566/566, integración 81/81 (11 nuevos), CI verde; mutaciones cazadas (6 y 3
  tests); runtime de auth 17/18; la SPA no lee ProblemDetails.
- Cierra el bloque `869f6r5r2` (cimientos de la API) con `869f6r81n` y `869f74u70`.

### 2026-09-29 — `869f74u70` Manejador global de excepciones (PR #96)

- `GlobalExceptionHandler` (`IExceptionHandler`) tras `UseSerilogRequestLogging`: 500
  `GEN_INTERNAL_ERROR` con envelope y `meta.requestId`, excepción al log con su RequestId; tipo y
  mensaje en `error.details` solo en Development, nunca la traza; 499 si el cliente corta; no toca una
  respuesta ya empezada.
- `ApiErrorWriter`: el middleware de tenant (400/403), el rate limiter (429) y los eventos de
  JwtBearer (401/403) escriben con el mapa único. Solo quedan a mano los dos 400 de MFA (`869en8a17`).
- Tests: `ApiFactory` fija la clave JWT por fixture para que las variantes de `WithWebHostBuilder`
  acepten los mismos tokens; 8 tests nuevos (500 con un servicio que lanza, 400 de tenant, 429 con
  `Retry-After` en una variante con su propio contador, y el manejador en Production/Staging, 499 y
  respuesta empezada).
- Evidencia: unit 565/565, integración 70/70, CI verde; sin `UseExceptionHandler` falla el test del
  500; runtime de auth 17/18 (el falso positivo de siempre), log sin errores.
- Lección: `/api/v1/legal/versions` está exenta de tenant; para probar la resolución del tenant hace
  falta una ruta que la exija.

### 2026-09-29 — `869f6r81n` Mapa único de errores y base común de controladores (PR #95)

- `ErrorStatusCodes` (`ReservArte-Shared/Api`): los 16 códigos del catálogo con su status; fuera del
  catálogo, 500. Las copias por controlador ya divergían (Auth mandaba a 400 lo desconocido; solo
  Disponibilidad conocía `APT_SLOT_UNAVAILABLE`). `PAY_REDSYS_DECLINED` → 402 (el catálogo admitía
  402/422).
- `ApiControllerBase` (`Meta`, `FromFailure`, `Failure`, `ValidateAsync` con camelCase por tramo) en
  los 10 controladores con envelope: −405/+105 líneas en `Controllers/`. Respuestas manuales a
  `Failure(...)` salvo dos 400 de MFA con `AUTH_INVALID_CREDENTIALS`, dejados a propósito
  (`869en8a17`).
- `AuthResult<T>` retirado (idéntico a `Result<T>`); `869f17y6k` cancelada con comentario.
- Evidencia: unit 565/565 (21 del mapa), integración 62/62 (5 de Auth), CI verde; los tests de Auth
  pasan igual sobre `develop` sin el cambio (worktree); runtime de auth 17/18 antes y después de tocar
  MFA/OAuth, log sin errores; mutación 404→400 cazada por 4 tests.
- Lecciones: (1) en macOS no existe `timeout`: un script que lo usa sale vacío sin avisar; (2) `dotnet
  run` no reenvía la señal a la API: pararla por su binario (`pkill -f bin/Debug/.../ReservArte-API`).

### 2026-09-29 — `869f2gh37` Contratos HTTP de Empleados y Clientes (PR #94)

- Sobre la infraestructura de `869f6r5ng` (PostgreSQL real, no SQLite como proponía la tarea). Sin
  cambios de producción.
- Infraestructura: `TokenForAsync` y `MfaTicketForAsync` emiten tokens con el `IJwtTokenService` de la
  API (no gastan el límite de login y el pipeline los valida de verdad); `ShouldBeEnvelope` comprueba
  el envelope completo; `TestSeed.CreateEmployeeAsync` acepta el rol.
- 23 tests: Clientes (11: 401 con `WWW-Authenticate`, ticket de 2FA y firma alterada, 403 de clienta,
  empleada solo lectura, ciclo completo de Manager y Admin, 400 camelCase, 404, 409) y Empleados
  (12: solo gerencia, Manager frente a Admin, límites sobre uno mismo, baja con lockout, 400, 404, 409).
- Evidencia: unit 544/544, integración 57/57, CI verde; tres mutaciones cazadas (sin
  `[Authorize(Roles)]` en el alta, 403 sin envelope, Manager que crea un Admin).
- Límite comprobado con una sonda (no commiteada): el token de una empleada dada de baja sigue dando
  200 hasta que caduca. Anotado en el PR; no se abre tarea.
- Lección: una mutación cuyo patrón no casa «pasa» sin probar nada; comprobar siempre que el fichero
  cambió antes de leer el resultado.

### 2026-09-29 — `869f6r5ng` Tests de integración con PostgreSQL real (PR #93)

- Adelantada a `869f8pmpn` (Windows) con el OK de Guillermo para seguir en el Mac. Antes, con su OK,
  se retiró del Mac `reservarte-sql` (contenedor, volumen `reservarte_sqldata` y el secreto
  `SqlServerLegacy`).
- Proyecto `tests/ReservArte.IntegrationTests`: `WebApplicationFactory<Program>` en Development
  contra `postgres:18` con Testcontainers.PostgreSql 4.15.0 y Mvc.Testing 10.0.12 (todo MIT).
  Fixture con el centro A (`DevSeeder`) y un centro B, configuración propia que se impone a los User
  Secrets, correos capturados, tokens reutilizados (login 10/h). 34 tests: montaje, aislamiento por
  HTTP, emails y búsquedas (CHECK incluido), repositorio de citas, solapes y zona horaria.
- Hallazgo, arreglado en el mismo PR con el OK de Guillermo: `AvailabilityService.DayExceptionsAsync`
  consultaba con `DateTime` sin `Kind` (500 con Npgsql desde el PR #92) y recortaba instantes UTC
  contra la medianoche local (ausencias desplazadas 1-2 h desde el PR #91). Ahora pasa por
  `Europe/Madrid`.
- `catch` de `Program.cs`: se traga el motivo de un fallo previo a `Build()`; no se toca (código de
  salida 1 de `869f6r5jf`), la fixture explica dónde mirar. CI en dos pasos con su `.trx` cada uno
  (un `LogFileName` fijo se sobrescribía). Deuda nueva: `869f8t7h0` (email con espacios en login).
- Evidencia: unit 544/544, integración 34/34 (también en el CI), format 0; mutaciones: sin anular
  Google falla el test de User Secrets, con solo el arreglo del `Kind` fallan 3 de zona horaria, con
  el código antiguo fallan 3 unitarios.
- Lecciones: (1) la verificación en runtime del PR #92 no pasó por la consulta de huecos: los tests
  de integración cubren endpoints que nadie prueba a mano; (2) `--no-build` tras restaurar un
  fichero de una mutación ejecuta binarios viejos: recompilar; (3) `dotnet sln add` añade plataformas
  x64/x86 a toda la solución: editar el `.sln` a mano.

### 2026-09-29 — `869f8pmpa` Cambio del motor a PostgreSQL (PR #92)

- Segunda tarea de la épica `869f8pm99` (D-28). Npgsql 10.0.3 sustituye a SqlServer (fuera
  `Hangfire.SqlServer` y el pin de `Protocols.OpenIdConnect`); SQL a mano del modelo con comillas
  dobles y booleanos `TRUE`/`FALSE`; las 12 migraciones de SQL Server se sustituyen por
  `20260929073713_InitialCreate` (23 tablas, 46 índices, 22 CHECK, 3 filtros).
- H-37: `EmailNormalizer` en altas, ediciones, registro, alta social y repositorios, con CHECK
  `CK_Customers_EmailLowercase` y `CK_Employees_EmailLowercase`; las 10 búsquedas `LIKE` pasan a
  `ToLower().Contains()` (sin distinguir mayúsculas; `%` y `_` como texto).
- `data/` para psql (`-v db=`, `\gexec`, `setval` en el seed). Reglas (`datos.md`, `backend.md`),
  `CLAUDE.md`, skill `cerrar-tarea`, plantilla de PR y `data/README.md` al día.
- Entorno Mac: contenedor `reservarte-pg` (postgres:18.6, solo `127.0.0.1:5432`, volumen
  `reservarte_pgdata`); cadena en User Secrets y la anterior guardada como `SqlServerLegacy`.
- Evidencia: 543/543 (10 nuevos), E2E 57/57, runtime 21/21 sobre una base creada con los scripts
  (aislamiento, emails, búsquedas, secuencias, `timestamptz`), auth 17/18 (el falso positivo
  conocido de `869en8a17`); esquema idéntico por los tres caminos (236 columnas, 70 índices, 45 FK,
  22 CHECK); scripts idempotentes; CI verde en el PR.
- Lecciones: (1) el recuento inicial de «40 filtros» sumaba las copias de las migraciones: contar en
  las configuraciones; (2) `= 1` sobre booleanos compila y solo falla al aplicar la migración;
  (3) el test de metadatos de un CHECK necesita el modelo de diseño (`IDesignTimeModel`);
  (4) en zsh, un heredoc con una línea `EOF` dentro rompe el comando: escribir con Write o Edit.

### 2026-09-29 — `869f8pmnm` Fechas en UTC en la frontera de la API (PR #91)

- Primera tarea de la épica de PostgreSQL (`869f8pm99`). Medido antes con la API real: el cuerpo JSON
  con desplazamiento pasaba a hora local del servidor (Kind = Local) y SQL Server lo guardaba
  desplazado (fallo existente); las fechas sin zona se aceptaban; las salidas de la base iban sin
  `Z`. La query ya convertía los desplazamientos a UTC (el enlazado usa AdjustToUniversal).
- `UtcDateTimeJsonConverter` en `ReservArte-Shared/Json` (lectura por `DateTimeOffset`, escritura
  siempre UTC con `Z`); sin zona → 400 `MissingTimeZone` en el validador de ausencias y en
  `GetAvailabilityAsync`. Regla en `contrato-api.md`.
- Evidencia: 533/533 (11 nuevos; mutación cazada por 5), E2E 57/57, seis casos HTTP, CI verde en
  `develop` (`5c224f4`).
- Lección: medir el comportamiento real antes de diseñar destapó un fallo que la tarea no preveía.

### 2026-09-29 — `869f6r5jf` Correcciones menores de la auditoría (PR #90)

- Empezada el 28-sep sin cuota de ClickUp, con un alcance provisional sacado de la auditoría y D-19;
  al leer la descripción el 29-sep se añadieron `Ses` en `Email:Provider`, Header y
  `DefaultOrganizationId` solo en Development y el borrado de los cuatro `Class1.cs`.
- Hecho: comentario de `EmployeeRepository`; roles en PascalCase en los datos de test; fuera
  `IpRateLimiting`; `Email:Provider` en lugar de `IsDevelopment()`; `MultiTenantOptions` validadas
  al arrancar; 400 de tenant genérico con el motivo en el log; `Class1.cs` borrados. Hallazgo
  aprobado por Guillermo: `Program.cs` salía con código 0 aunque el host fallara (plantilla de dos
  fases de Serilog sin código de salida) → `Environment.ExitCode = 1`.
- Evidencia: 522/522, E2E 57/57, CI verde (`57a6e33`); en runtime, configuraciones malas → no
  arranca con código 1, también en Staging (con un control válido que sí arranca); SIGINT/SIGTERM
  → 0; `dotnet ef` intacto.
- Lecciones: (1) `dotnet run --no-build` usa Debug y la batería compila en Release: recompilar en
  Debug antes de verificar en runtime (una pasada dio falsos resultados por binarios viejos);
  (2) en bash no interactivo un proceso en segundo plano ignora SIGINT: `set -m` para probar Ctrl+C;
  (3) si una prueba no enseña el motivo del resultado, no demuestra nada: leer el log de cada caso.

### 2026-09-28 — Documentación de la Fase 1 aplicada (`869f6r54r`, `869f6r58r`)

- La IA de documentación aplicó el prompt `prompts/2026-09-28-fase-1.md` (commit `b9ec48d`, solo
  `Documentation/`): ADR-001 a ADR-030, stack .NET 10 en vol. 1 §4.1, Mapperly y EF 10 en vol. 2,
  Kanban y ClickUp actual en vol. 3, cifras corregidas (215.586 € y MRR 1.873, 5.100 y 8.190 €,
  comprobadas), rutas y tarjetas enlazadas, `as const` en el script de instalación y la base legal de
  accesibilidad revisada.
- Advertencias contrastadas: D-13 (épica frente a subtareas, no era un error) y D-17 (guards por
  rol) corregidas en `decisiones.md`, que ahora enlaza cada decisión con su ADR; React Native en el
  presupuesto del vol. 3 y el bloque histórico de testing, acumulados; el análisis legal (Ley
  11/2023, RD 193/2023) pasa como nota a `869f6r7e7` para validarlo con un profesional.

### 2026-09-28 — Cierre de la Fase 1 del plan (red de seguridad y plataforma)

- Entregado: CI de backend y frontend (`869d7ex56`, `869d7ex8r`) con checks obligatorios en `main`
  (`869f6r4t8`, H-34); .NET 10 LTS (`869f6r5ca`); bloque de dependencias `869f6r5eu` cerrado
  (MediatR fuera, Mapperly H-36, AwesomeAssertions H-35 que resuelve DP-03). El proyecto queda sin
  dependencias de pago. La deuda de AWSSDK.Core pasó a la decisión de plataforma (`869f8hpfj`).
- Definición de hecho: tareas de código en `shipped`/`done`; CI en verde en `develop`; prompt de
  documentación entregado (`prompts/2026-09-28-fase-1.md`, con los ADR 001-030 y las incoherencias
  de la auditoría); previsión recalculada. `869d7ecqz` (épica de CI/CD) sigue abierta: tiene los
  pipelines de despliegue, que son de la Fase 6.
- Métricas: 12 tareas del 25 al 28-sep; las 7 de código de la Fase 1 (29 h estimadas) en unas 3 h
  de reloj. Sesgo de estimación ×9 en trabajo mecánico; no se extrapola hasta medir producto.
  Previsión probable del MVP piloto: finales de enero de 2027 (antes, mediados de febrero).
- Lecciones del bloque: medir antes de decidir (DP-03); tests de caracterización antes de cambiar
  una librería (Mapperly); ejecutar el script `create` entero sobre una base desechable (cazó el
  fallo de EF 10); una prueba en rojo por cada guarda de CI.

### 2026-09-28 — `869f6r7yh` FluentAssertions 8 → AwesomeAssertions (PR #89)

- DP-03 medida: FluentAssertions 7.2.2 (Apache-2.0) daba 0 errores y 522/522; AwesomeAssertions
  9.6.0 (Apache-2.0), 30 errores de `using` y, tras el cambio mecánico, 0 y 522/522. Guillermo elige
  AwesomeAssertions (H-35): conserva la API de la 8 y se puede actualizar. Cambio: csproj y 30
  `using`, sin tocar ninguna aserción. Resto de librerías de test ya fijadas y permisivas.
- Evidencia: mutación en `EmployeeMapper` → 3 fallos con mensajes de AwesomeAssertions; 522/522;
  format con código 0; CI verde en `develop` (`cfc961b`).
- Lección: `\b` no funciona en el `sed` de macOS (BSD); para sustituciones con límites de palabra,
  `perl -pi -e`.
- Cierra el bloque de dependencias y licencias (`869f6r5eu`): MediatR, AutoMapper y FluentAssertions
  fuera; el proyecto queda sin dependencias de pago. En la épica queda la deuda `869f8fzha`
  (AWSSDK.Core), que depende de DP-01.

### 2026-09-28 — `869f6r7vw` AutoMapper → Mapperly (PR #88)

- Cuatro mappers estáticos de Mapperly 4.3.1 (Apache-2.0) en `Application/Mapping/` con los mismos
  15 mapeos; servicios sin `IMapper` (35 llamadas) y sin `AddAutoMapper`. RMG012/RMG020 como errores
  de compilación en `ReservArte.Application.csproj`; lo no expuesto se ignora con
  `[MapperIgnoreSource]`. Matiz: `[MapPropertyFromSource]` (FullName) apaga RMG020 en su método.
- Método: primero `MappingCharacterizationTests` (26 casos, entidad completa y DTO esperado escrito
  entero) en verde contra AutoMapper, con mutación que los rompe; después, el cambio, en el que solo
  cambia el punto de acceso del test (sin tocar aserciones). Se retiran los tres tests de perfil.
- Evidencia: 26/26 con los dos mapeadores; guardas RMG012 y RMG020 comprobadas por mutación; 14
  respuestas HTTP idénticas entre `develop` y la rama sobre la base de desarrollo (sin escrituras);
  522/522; CI verde en `develop` (`c5f67f9`). Sin dependencias de producción con licencia comercial.
- Tropiezo: el primer commit de tests no pasaba `dotnet format` (inicializadores en una línea); se
  enmendó antes de subir. Comprobar el formato antes del commit, no después.

### 2026-09-28 — `869f6r7rj` Retirar MediatR (PR #87)

- MediatR 14.1.0, con licencia dual comercial, estaba en `ReservArte.Application.csproj` sin ningún
  uso (ni `IMediator`, ni `IRequest`, ni `INotification`, ni `IPipelineBehavior`, ni `AddMediatR`).
  Paquete fuera; `CLAUDE.md` y `backend.md` lo dan por retirado.
- Evidencia: build estricto sin errores, 506/506, format con código 0, MediatR fuera del grafo
  (`dotnet list package --include-transitive`), API arrancada (health, login 200) y CI verde.

### 2026-09-28 — `869f6r5ca` Migración a .NET 10 LTS (PR #86)

- `global.json` (10.0.100, `latestFeature`); `net10.0` en los seis proyectos; paquetes de Microsoft
  8.0.0 → 10.0.12; Apple OAuth 10.0.0, Serilog.AspNetCore 10.0.0, Swashbuckle 10.2.3, Hangfire
  1.8.25; `Microsoft.IdentityModel.*` en una sola versión, 8.19.2 (convivían 7.7.1 por SqlClient,
  8.14.0 por la referencia directa y 8.19.2 por JwtBearer; `Protocols.OpenIdConnect` fijada en
  Infrastructure). CI con el SDK de `global.json`; `dotnet-ef` 10.0.12.
- Fallos cazados: (1) el `create` de EF 10 se paraba en la migración 7/12 porque EF 10 genera un
  lote por migración y el `UPDATE` a mano de `ScopeEmailAndExternalLoginsToOrganization` usaba una
  columna del mismo lote → envuelto en `EXEC` (regla en `datos.md`); (2) `GetQueryFilter()` obsoleto
  en el test de metadatos → `GetDeclaredQueryFilters()`, con mutación que confirma que sigue cazando.
- Evidencia: 506/506 sobre `net10.0` (local y CI); esquema idéntico por tres caminos (create EF 8,
  create EF 10 dos veces, `database update`): 236 columnas, 70 índices, 45 FK, 20 CHECK y 7 defaults;
  seed sin errores; E2E 57/57; runtime (login, claims, 401/403 con envelope, refresh con rotación,
  2FA TOTP completo, Google de ida y vuelta con cuenta real). CI verde en `develop` (`692c4a9`).
- Hallazgos: el Mac tenía marcadores de posición en las credenciales de Google (Guillermo configuró
  las reales y un secreto nuevo); NU1901 de `AWSSDK.Core` (vulnerabilidad baja, ya existente, el SDK
  10 audita transitivos) → deuda `869f8fzha`; el test de TOTP incorrecto esperaba `AUTH_MFA_INVALID`,
  pero sigue pendiente en `869en8a17` (falso positivo de la prueba).
- Plan: .NET 10 en `develop` el 28-sep, 11 días antes del objetivo (9-oct).

### 2026-09-28 — `869f6r4t8` Checks de CI obligatorios en `main` (sin PR)

- Protección de `main` por la API de GitHub: `build-test-format` y `lint-build` obligatorios
  (`app_id` 15368, GitHub Actions), `strict=false` y `enforce_admins=true` (decisión H-34, elegida
  por Guillermo). Se conservan el PR obligatorio con 0 aprobaciones, sin force push y sin borrado.
  `develop` sigue sin protección (H-32).
- Evidencia: GET de la protección; PR de prueba #84 a `main` con un aviso de lint → `lint-build`
  en rojo y `mergeable_state=blocked`; PR #85 igual que `develop` → verde y `clean`. Cerrados sin
  mergear y ramas borradas. No se intentó mergear el bloqueado: `blocked` es la prueba.
- A tener en cuenta: `main` sigue en «Initial commit» (no ha habido ninguna release). Una rama
  `hotfix/*` sacada de `main` no tiene los workflows hasta la primera release, así que su PR se
  quedaría esperando los checks. Tras la primera release deja de pasar.
- Lección: `gh pr checks` devuelve un código distinto de 0 si hay un check en rojo; en scripts con
  `set -e`, añadirle `|| true`.
- Cierra el bloque de CI de la Fase 1 (`869d7ex56`, `869d7ex8r`, `869f6r4t8`).

### 2026-09-28 — `869d7ex8r` CI de frontend (PR #82)

- `.github/workflows/frontend-ci.yml`, job `lint-build` en `reservarte-web`: Node 24 LTS con caché
  de npm, `npm ci`, `npm run lint -- --max-warnings 0` (línea base 0, Prettier incluido) y
  `npm run build` (`vue-tsc` + `vite`). Mismos disparadores y criterios que el Backend CI;
  `actions/setup-node` v7.0.0 fijada por SHA (MIT).
- Evidencia: los mismos comandos sobre una copia limpia (`git archive`) con código 0; run verde en
  el PR (28 s); pruebas en rojo en el PR desechable #83 (`any` → Lint, `TS2322` → Build, lockfile
  desincronizado → `npm ci`); los dos workflows en verde por push en `develop` (`fa724b0`).
- El lint del CI es más estricto que `npm run lint` en local, donde los avisos no hacen fallar el
  comando.
- Deuda detectada: artefactos de Playwright versionados → subtarea `869f8ewx5` (bajo `869eqxm7w`).

### 2026-09-28 — `869d7ex56` CI de backend (PR #80)

- `.github/workflows/backend-ci.yml`, job `build-test-format` en `ubuntu-latest` con .NET 8.0.x:
  restore, build en Release con avisos como errores (salvo la auditoría NuGet NU1901-NU1904),
  `dotnet test` con artefacto TRX y `dotnet format --verify-no-changes`. En PR a `develop`/`main`,
  push a `develop` y a mano; sin filtro de rutas para poder exigirlo en `869f6r4t8`. Actions
  fijadas por SHA: checkout v7.0.1, setup-dotnet v6.0.0 y upload-artifact v7.0.1 (MIT).
- Evidencia: los mismos comandos en local antes de escribirlo (0 avisos, 506/506, format 0); run
  verde en el PR (1 min 27 s); pruebas en rojo en el PR desechable #81 (CS0168 en Build, test roto
  en Tests, WHITESPACE en Formato); run verde por push en `develop` tras el merge (`7e6ad79`).
- Pendiente conocido: en PR a `main` solo se ejecutará cuando el workflow llegue a `main`; sin
  `global.json` hasta .NET 10.

### 2026-09-28 — `869f6r52d` Nuevo régimen de documentación (PR #79 y commit `147dca9`)

- La IA de documentación trabaja en Cursor: sus instrucciones se cargan como regla de proyecto
  `.cursor/rules/ia-documentacion.mdc` (`alwaysApply`), versionada y válida en los dos equipos; la
  plantilla y la regla llevan el mismo texto. La IA tampoco toca `.cursor/`.
- Cursor le carga también `CLAUDE.md`. Guillermo añadió a la regla la sección «Instrucciones de
  otras herramientas» (solo contexto; manda su regla), commiteada directamente en `develop` con los
  ADR (`147dca9`); Claude Code la sincronizó en la plantilla.
- La IA creó `Documentation/adr/README.md` (numeración, estados, índice vacío) y `plantilla.md`,
  tras pasar la auditoría de coherencia. Sin ADR todavía (`869f6r54r`).
- Lecciones: en modo Ask, Cursor no puede crear ficheros; hay que usar el modo Agent. Y revisar
  `git status` antes de commitear en Cursor, porque se coló el `.mdc` en el commit de documentación.
- Cierra la **Fase 0** del plan.

### 2026-09-28 — `869f6r4hm` Plantilla de PR para un solo desarrollador (PR #78)

- DoD: revisión propia con evidencia + revisión de Claude Code en lugar de reviewer obligatorio;
  casillas nuevas de CI (N/A hasta el pipeline), dependencias fijadas con licencia revisada y
  `estado.md` de la rama. Staging como N/A; la cobertura se sustituye por batería y evidencia.
- Tarjetas Redsys: la tabla copiada tenía un PAN distinto al de la guía (`…0004` frente a
  `…0003`); ahora enlaza a `redsys-development-guide.md` §2 con URL absoluta (las relativas no
  resuelven en el cuerpo de un PR). `npm run test` (inexistente) → `npm run test:e2e`.
- Mismo PR, commit aparte: `.claude/rules/backend.md` apunta la promoción a `869f7axh9`.
- Evidencia: URL de la guía con 200; scripts `lint` y `test:e2e` en `package.json`; 0 apariciones de
  `869f2g02q` en reglas y `CLAUDE.md`; `dotnet build` en `develop` tras el merge con 0 errores.
- Cierra la Fase 0 por la parte de desarrollo; queda `869f6r52d`, de Guillermo.

### 2026-09-27/28 — `869f6r4ec` Re-planificación con la capacidad real (sin PR)

- Línea base con git (ClickUp no sirve: `shipped` no rellena `date_closed` y la ClickApp de tiempo
  en estado está desactivada): 57 PRs en 6 semanas, 41 features, unas 7 por semana y muy irregular.
  El tiempo de ciclo se medirá con los commits `empieza`/`cierra` (`gestion.md` §6).
- Guillermo mantiene el orden de `plan.md`: se descartó adelantar la agenda (2.3 y 3.9 a la Fase 4,
  2.7 a la Fase 6), que ganaba unos 10 días y una demo intermedia.
- Previsión registrada: MVP piloto probable a mediados de febrero de 2027 (optimista mediados de
  enero, pesimista finales de marzo); .NET 10 con objetivo el 9-oct.
- Oleada: estimación y fecha (viernes) en 26 tareas de ClickUp hasta el 6-nov; `869f6r5ca` conserva
  el 6-nov como fecha tope.

### 2026-09-25/26 — `869f74uca` Limpieza y reorganización de ClickUp (sin PR)

- Listas «Active Sprint» renombradas a «Backend» y «Frontend»; Guillermo renombró el espacio Mobile
  y archivó ocho listas vacías (cinco «List», «Bugs», «Architecture Decisions», «Backlog» de
  Frontend).
- Traslados con el patrón H-23: dashboard `869d7f4b4` → `869f7axcv`; penalización `869f6ae9h` →
  `869f7axdq` (bajo Redsys, con los tests de penalización de `869d7f53r`); no-shows `869f2gtyv` →
  `869f7axeg` (bajo `869f6r5y8`); lista de espera `869f2yh9b` → `869f7axfq`; promoción `869f2g02q`
  → `869f7axh9`. Bloque **CRUD Servicios cerrado** (5/5); Citas queda en 5/8. React Native
  `869d7ee7c` cancelada en favor de la PWA (D-20).
- Seis títulos ajustados al alcance del piloto, dos comentarios aclaratorios, cinco tareas de Docs a
  `publish`, `869d7ecpg` a `done` y fechas del plan de mayo retiradas de 16 tareas abiertas.
- Evidencia: filtro de ClickUp de tareas con fecha → la única abierta es `869f6r5ca` (6-nov);
  subtareas de `869d7ed7v` comprobadas (5 `shipped` + 1 `cancelled`); comentario de resultado en
  `869f74uca`.
- Lección: la cuota de 100 llamadas al día del conector es compartida con las sesiones de claude.ai.
  Se agotó a mitad de tarea porque no conté las llamadas que ya había hecho esa mañana la sesión que
  creó 45 tareas. Lo pendiente quedó en `estado.md` y se aplicó al día siguiente. Una limpieza de
  este tamaño gasta unas 75 llamadas.

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
