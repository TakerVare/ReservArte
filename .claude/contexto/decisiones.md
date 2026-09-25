# Registro de decisiones

> Índice operativo: lo que está aquí no se vuelve a preguntar. Cada decisión nueva se añade con
> fecha y tarea, y va en el siguiente prompt a la IA de documentación para que escriba su ADR en
> `Documentation/adr/`. Cuando existan (tarea `869f6r54r`), cada línea enlazará su ADR.

## Aprobadas el 2026-09-24 (auditoría del 2026-09-23)

| ID | Decisión | Tareas |
|---|---|---|
| D-01 | Metodología real: Kanban con una tarea de desarrollo en curso y un desarrollador con IA; métricas de throughput y tiempo de ciclo. Se retira de la documentación el Scrum que no se practica. | `869f6r4ec`, `869f6r58r` |
| D-02 | Claude Code actúa como desarrollador y coordinador, con las reglas de intervención de `CLAUDE.md`. | `869f6r4ba` |
| D-03 | Entrega en vertical (API + pantalla + E2E), empezando por la agenda, hacia un MVP piloto acotado (`plan.md`). | `869f6r4zt` |
| D-04 | More Than Brows es el cliente piloto y ha delegado en Guillermo las decisiones de producto. | — |
| D-05 | Capacidad de 25 h/semana y sin fechas comprometidas con terceros. Equipo: Guillermo en solitario; Gabriel sale de la documentación. | `869f6r4ec`, `869f6r58r` |
| D-06 | El estado vive en ClickUp y el traspaso entre equipos en `.claude/contexto/estado.md`. Los volúmenes dejan de registrar estado, PRs y recuentos. | `869f6r52d` |
| D-07 | Documentación por bloque, no por tarea, con auditoría completa mensual y ADR en `Documentation/adr/`. | `869f6r52d`, `869f6r54r` |
| D-08 | CI obligatorio: build, test, format y lint en cada PR, con checks requeridos en `main`. | `869d7ex56`, `869d7ex8r`, `869f6r4t8` |
| D-09 | Migración a .NET 10 LTS antes del 10-nov-2026. | `869f6r5ca` |
| D-10 | Fuera MediatR; AutoMapper pasa a Mapperly; FluentAssertions 8 se sustituye por una alternativa con licencia permisiva. Toda dependencia entra con versión fijada y licencia revisada. | `869f6r5eu` y subtareas |
| D-11 | vue-i18n pasa a la versión 11. | `869f6r6dk` |
| D-12 | Gráficas con una librería de Vue y colores desde tokens; recharts descartado. Propuesta: vue-chartjs, a confirmar en el dashboard. | `869f6r6nx` |
| D-13 | Antes de los endpoints de citas: mapa central código → HTTP, manejador global de excepciones y envelope en los 400 de model binding. | `869f6r5r2`, `869f1k17q` |
| D-14 | Query filters cerrados por defecto, con ámbito de sistema explícito, antes de Hangfire. | `869f6r5vy` |
| D-15 | Los casos de uso siguen en Infrastructure; Application tiene contratos, DTOs y validadores. Es una decisión consciente que se documenta; no se mueven. | `869f6r54r` |
| D-16 | Tests de integración con SQL Server real (Testcontainers) y `WebApplicationFactory`. | `869f6r5ng`, `869f2gh37` |
| D-17 | Sesión: refresh token en cookie httpOnly, access token en memoria, retorno OAuth sin tokens en la URL y rehidratación al arrancar la SPA. | `869f6r61z`, `869f6r6hc` |
| D-18 | Puertos: 5555 queda como convención documentada. El código no lleva fallbacks a localhost y la SPA usa rutas relativas con el proxy de Vite. | `869f6r69b` |
| D-19 | Endurecimiento antes de producción: limitador antes del tenant e IP real tras proxy, caché de tenant, cabeceras de seguridad, zona horaria por organización, validación de `MultiTenantOptions` y 400 de tenant sin detalles internos. | `869f6r65a`, `869f74u7y`, `869f6r5jf` |
| D-20 | App móvil como PWA sobre la SPA Vue, con Capacitor si hace falta publicar en tiendas. React Native, descartado. | `869f6r74n` |
| D-21 | La plataforma de producción (motor o edición de base de datos, y hosting) se decide conscientemente antes de montar infraestructura. | `869f6r4ww` |
| D-22 | Los trámites externos arrancan ya, en paralelo al desarrollo. | `869f6r4nz` |
| D-23 | ClickUp se limpia: listas renombradas, vacías archivadas, estados al día, subtareas en su épica natural y definición de hecho por bloque. | `869f74uca` |
| D-24 | La plantilla de PR se adapta a un solo desarrollador. | `869f6r4hm` |
| D-25 | Se revisa la base legal de accesibilidad: el RD 1112/2018 regula el sector público. | `869f6r58r` |
| D-26 | RGPD del piloto: contrato de encargo, registro de actividades y EIPD (las alergias son datos de salud); cifrado según el resultado. | `869f6r7b3`, `869f74ua4` |
| D-27 | Traspaso entre equipos a través del repo: `.claude/contexto/estado.md` se actualiza en cada punto de control, y los ficheros de `.claude/contexto/` son los únicos con commits directos a `develop` (`chore(contexto)`). | `869f6r4ba` |

## Anteriores (no volver a preguntar)

| ID | Decisión | Origen |
|---|---|---|
| H-01 | Roles en PascalCase desde el catálogo `Roles` (Admin, Manager, Employee, Customer). | `869f18116` |
| H-02 | Email único por organización, no global. | `869f1xc0u` |
| H-03 | Las escrituras que tocan ficha y cuenta de Identity van dentro de `IUnitOfWork`. | `869f1811u` |
| H-04 | Cuenta mixta empleada/clienta con el mismo Id; cambiar desde Clientes el email de una cuenta de personal → 403. | `869d7f369` |
| H-05 | Toda ficha de cliente nace en categoría `new`; la promoción llega con Citas. | `869f2g02q` |
| H-06 | Scripts de `data/`: `schema/` generado desde EF, nunca a mano; `demo/` alineado con `DevSeeder`. | `869f17mzg` |
| H-07 | `dotnet format` con línea base 0 y `.editorconfig`; `end_of_line` sin fijar para el código. | `869f2pjf8` |
| H-08 | Dos escalas independientes: `ProficiencyLevel` (quién puede prestar el servicio) y `EmployeeLevel` (cuánto cuesta). | `869d7f3z0` |
| H-09 | Catálogo de servicios: lectura para cualquier rol autenticado, Customer incluido; escrituras Admin o Manager. | `869d7f42u` |
| H-10 | La baja de una categoría se permite aunque tenga servicios; las tarifas son un upsert por nivel. | `869f2wtrk` |
| H-11 | Paquetes: recurso propio; `PUT` reemplaza la composición; las líneas se borran físicamente; `savings` puede ser negativo. | `869d7f45n` |
| H-12 | Servicios se adelantó a Citas por las dependencias de FK. | `869d7ed7v` |
| H-13 | 8 estados de cita, fieles al vol. 1 §5.2.2; se mantiene `CancelledByType`; `Status` es la fuente de verdad. | `869d7f4f1` |
| H-14 | `WaitingList` entra en la migración de citas; su tabla es `WaitingLists`; ninguna tabla va en singular. | `869d7f4j8` |
| H-15 | Índice único filtrado en `RedsysOrderNumber`; FK de las citas a `Customers` y `Employees` en `Restrict`. | `869d7f4j8` |
| H-16 | `redsys_auth_code` y `redsys_transaction_type` los decide `869d7eden`; `created_by`, `869d7f519`. | 2026-09-16 |
| H-17 | Repositorios: interfaces en `Domain/Interfaces` e implementaciones en `Persistence/Repositories`; ninguno recibe la organización por parámetro. | `869d7f4n4` |
| H-18 | Disponibilidad: rejilla de 15 minutos anclada al tramo; se descartan los huecos pasados si es hoy (`Europe/Madrid`, deuda hasta `869f74u7y`); intervalos semiabiertos; controlador propio. | `869d7f4rd` |
| H-19 | Máquina de estados: cancelan el personal y la clienta dueña (cita ajena → 404); el `cancelled` genérico no lo escribe nadie; no-show solo Admin o Manager; el rol se comprueba antes de cargar, salvo en `CancelAsync`. | `869d7f4xf` |
| H-20 | La penalización al cancelar sale del bloque de Citas. | `869f6ae9h` |
| H-21 | No-shows: umbral en `OrganizationSettings`; el desbloqueo manual con motivo pone el contador a 0; sin AuditLog genérico. | `869f2gtyv` |
| H-22 | `shipped` tras el merge; `in review` al abrir el PR. | 2026-09-23 |
| H-23 | Para trasladar una subtarea: crearla bajo el nuevo padre y cancelar la original con comentario. | — |
| H-24 | `appsettings.json` base con las claves vacías; valores de desarrollo en `appsettings.Development.json`. | — |
| H-25 | `/legal/versions` es global en v1 y está exento de tenant; por organización en la fase 3. | — |
| H-26 | `DevFileEmailService` no depende de `IHostEnvironment` (Infrastructure no se acopla al hosting). | `869eq5tg3` |
| H-27 | Panel de administración con BottomNav y sin Sidebar; `DashboardLayout` y Sidebar son deuda. | `869ep9p36` |
| H-28 | El contraste del rosa de marca es una excepción consciente, con deuda registrada. | `869f0v6vm` |
| H-29 | Accesibilidad con Playwright + `@axe-core/playwright`, no con vitest-axe. | `869d7fbpp` |
| H-30 | Tailwind 3.4.17, no la v4. | — |
| H-31 | ESLint flat config; `paths` de TS sin `baseUrl`; sin `enum` por `erasableSyntaxOnly`. | — |
| H-32 | `main` con PR obligatorio (0 aprobaciones); `develop` sin PR obligatorio. | 2026-09-14 |
| H-33 | Entidades fuera de alcance con `modelBuilder.Ignore<T>()`; configuraciones con `ApplyConfiguration` explícito. | — |

## Pendientes

| ID | Decisión | Disparador | Opciones |
|---|---|---|---|
| DP-01 | Plataforma de producción: base de datos y hosting | Paso 2.7 del plan | Ver `869f6r4ww`. |
| DP-02 | Librería de gráficas | Al llegar al dashboard | vue-chartjs (recomendada) o vue-echarts. |
| DP-03 | FluentAssertions 7.x o AwesomeAssertions | Dentro de `869f6r7yh` | Se decide con el recuento de errores de compilación de cada opción. |
| DP-04 | Protección CSRF de la cookie de refresh | Dentro de `869f6r61z` | `SameSite=Strict` + cabecera obligatoria (recomendada) u otra equivalente. |
| DP-05 | Alcance final del piloto: dashboard y no-shows | Al cerrar la Fase 5 | Guillermo decide con el calendario real. |
