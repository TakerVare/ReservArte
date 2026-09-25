# Auditoría del proyecto — 2026-09-23

> Informe entregado en claude.ai el 2026-09-23. Alcance: tablero de ClickUp (los cinco espacios,
> casi 200 tareas), código indexado en el proyecto (backend, frontend, tests, migraciones y
> configuración) y documentos de `Documentation/`; no se modificó nada. Guillermo aprobó todas las
> recomendaciones el 2026-09-24 (`decisiones.md`, D-01 a D-27). La columna «Destino» y la sección
> final se añadieron el 2026-09-25. Las cifras de «Porcentaje realizado» son la línea base del
> modelo de avance (`gestion.md` §7).

## Resumen ejecutivo

El backend de ReservArte tiene una calidad claramente superior a la habitual en un proyecto de una sola persona, y la disciplina de ejecución por tarea es muy buena. Los problemas no están en cómo se escribe cada pieza, sino en qué se construye y en qué orden:

- El MVP va unos tres meses por detrás de las fechas del propio ClickUp.
- Se ha construido en horizontal: casi todo el backend y casi nada de interfaz.
- No hay CI ni infraestructura.
- La plataforma elegida (.NET 8) se queda sin soporte en siete semanas.

Mi estimación del código hecho es un **39 % del MVP** y un **20 % del proyecto completo** (fases 1 a 3). El desglose está al final. Si tuviera que elegir tres acciones: montar CI esta semana, migrar a .NET 10 antes del 10 de noviembre y pasar a entregar en vertical (API + pantalla + E2E), empezando por la agenda.

## Estado real frente al plan

**El plan original.** Según las fechas límite de ClickUp: setup el 15 de mayo, autenticación el 22, CRUD de empleados y clientes el 5 de junio, agenda el 19 de junio, Redsys y recordatorios el 3 de julio, app móvil el 18 de septiembre y SaaS entre el 6 de noviembre y el 4 de diciembre.

**La realidad:**
- Autenticación cerrada el 21 de agosto.
- Empleados cerrado el 14 de septiembre y Clientes el 15.
- Servicios, abierto desde el 16 en 5/6, con su fecha del 18 ya vencida. Espera al dashboard, que a su vez espera a Citas.
- Citas en 5/12, con fecha límite el 25 de septiembre, que no se va a cumplir.
- Redsys, recordatorios, toda la UI de gestión, toda la infraestructura y la app móvil siguen en backlog con fechas vencidas.

**La aceleración de septiembre.** Entre el 12 y el 23 se cerraron una treintena de subtareas de backend y más de 40 PRs. A ese ritmo, el backend del MVP estaría listo en 4-6 semanas. Pero el cuello de botella es el frontend, con un 80 % pendiente, y todavía no ha tenido ese ritmo.

**Proyección.** Entre el ritmo de septiembre y la media desde mayo, el código del MVP estaría completo entre diciembre de 2026 y abril de 2027. Ponerlo en producción depende además de una infraestructura sin empezar y de cuentas externas que no se han iniciado.

**Cuidado con leer el avance por tarjetas.** Backend tiene 57 de 92 tareas en shipped y frontend 23 de 57. Pero lo pendiente son tarjetas grandes (agenda, wizard, Redsys), y las fases 2 y 3 son solo cinco tarjetas-épica sin desglosar. Contar tarjetas sobreestima el avance.

## Metodología: 6/10

**La ejecución por tarea merece un 9.** Hay rama por tarea, plantilla de PR, verificación con evidencia (SQL, HTTP, E2E), deuda registrada en el momento y decisiones con su porqué. La IA de documentación ha hecho de revisor independiente en un proyecto sin revisor humano, y ha cazado fallos reales.

**La planificación y la gestión de la entrega merecen un 4.** Estos son los motivos:

**Lo documentado no es lo que se practica.**
- El vol. 3 describe Scrum: sprints de dos semanas, dailies, el cliente como Product Owner, QA, Slack, story points y velocity.
- La realidad es flujo continuo con una tarea en curso cada vez, una persona con IA, sin estimaciones, sin asignaciones y sin sprints.
- La lista «Active Sprint» contiene todo el backlog desde mayo, y «Bugs» y «Architecture Decisions» están vacías.
- Trabajar en Kanban probablemente es lo correcto aquí. Pero entonces hay que medir como Kanban: throughput y tiempo de ciclo, que ClickUp ya registra. Hoy nada convierte el retraso en una re-planificación.

**Se ha construido en horizontal.** Hay API completa de empleados, clientes, catálogo y paquetes que ninguna pantalla consume. Esto tiene tres consecuencias:
- El cliente piloto no puede usar ni opinar sobre nada salvo el login.
- Los contratos de la API se han diseñado sin su consumidor, así que habrá retoques cuando llegue la UI.
- El mayor riesgo de producto sigue intacto tras cinco meses: si esto es lo que necesita More Than Brows.

**Los bloques no pueden cerrarse.**
- Servicios espera al dashboard, que espera a Citas.
- Citas incluye la penalización económica, que depende de Redsys (planificado después).
- Citas incluye también los no-shows, que dependen de `OrganizationSettings`. Esa entidad sigue en `Ignore` y no tiene tarea de backend propia.
- Los bloques crecen porque lo que se descubre se cuelga del bloque activo: Empleados pasó de 6 a 10 subtareas y Citas de 7 a 12 (tres heredadas de Clientes).
- Definiría un DoD por bloque y movería esas subtareas a su épica natural.

**Sin CI, todas las puertas de calidad son casillas.** La tarea del pipeline lleva en backlog desde mayo. `dotnet format` con línea base cero, los tests y el lint dependen de que alguien los ejecute. Con el volumen de código que genera la IA, CI es la inversión con más retorno del proyecto.

**El proceso pesa y el estado está triplicado.**
- Cada tarea recorre ocho pasos, con un ciclo de documentación antes de empezar la siguiente.
- El estado se mantiene a mano en ClickUp, en el vol. 3 y en CLAUDE.md, que ya divergen: el vol. 3 dice Citas 4/11 con «siguiente: 869d7f4xf», y ClickUp ya tiene esa tarea en shipped.
- Las instrucciones de este proyecto de Claude contradicen el CLAUDE.md que cambiaste hoy. Aquí, shipped va «tras verificar» y no existe el estado «in review»; allí, shipped va tras el merge. Una IA que trabaje desde este proyecto seguirá el flujo viejo. *(Resuelto el 2026-09-25: instrucciones del proyecto reescritas para remitir al repo.)*

**Nada de lo que tiene plazo externo está en marcha**, según el checklist del vol. 3:
- Cuenta de comercio Redsys con el banco.
- Dominio y salida del sandbox de SES.
- Apple Developer.
- Revisión de la app de Meta para producción.
- Textos legales y EIPD.

No consumen horas de desarrollo, y cada uno puede tardar semanas.

## Documentación: 6/10

**Lo bueno.** El contenido técnico es riguroso y trazable como pocas veces se ve: contratos (envelope, catálogo de errores, configuración), decisiones con las alternativas descartadas, advertencias explícitas y enumeraciones en vez de reglas generales.

**El problema.** Los volúmenes hacen tres trabajos a la vez: especificación, registro de cambios (PRs y fechas) y cuadro de estado. Eso cuesta tres cosas:
- Son difíciles de leer para un humano. El roadmap del vol. 3 es ya un log.
- Las secciones que ninguna tarea toca no se auditan y envejecen: stack y estructura del vol. 1, metodología, equipo y cifras del vol. 3.
- Los datos duplicados divergen (tarjetas Redsys, puertos).

La auditoría de la IA es incremental: mira lo que cambia cada prompt. Por eso no puede ver estas inconsistencias.

**Lo que haría:**
- Volúmenes solo como referencia: qué es el sistema y por qué.
- ADR cortos para las decisiones. Tienes decenas, fechadas y enterradas en párrafos.
- El estado solo en ClickUp, y el detalle de cada tarea en su PR, que ya lo recoge.
- CLAUDE.md de vuelta a un fichero de reglas corto. Ahora acumula la historia de cada tarea y se carga entero en cada sesión.
- Una auditoría completa periódica, por ejemplo al cerrar cada bloque.

**Un apunte normativo.** La documentación cita el RD 1112/2018 como obligatorio, pero ese real decreto regula al sector público. A un SaaS privado le aplicaría, en su caso, la Ley 11/2023 (Acta Europea de Accesibilidad), que exime a las microempresas de servicios. Conviene revisar esa base antes de presentarla así a un cliente.

## Código: backend 8/10, frontend 6/10

### Backend

Es lo mejor del proyecto:
- Comentarios que explican el porqué.
- Cerca de 500 tests unitarios cuyos nombres son reglas de negocio.
- Aislamiento multi-tenant con defensa en profundidad, y un test de metadatos que impide mapear una entidad sin filtro.
- Fail-fast de configuración y CHECKs generados desde constantes de dominio.
- Decisiones de seguridad finas: 404 en vez de 403 para no revelar existencia, anti-enumeración, ticket MFA sin rol y lockout al dar de baja.

Lo que corregiría, por orden de riesgo:

**1. Los filtros globales fallan en abierto.** Sin tenant resuelto, `CurrentOrganizationId == null` deja ver todas las organizaciones. Hoy lo compensan los repositorios con `Where(_ => false)`. Pero los jobs de Hangfire de recordatorios correrán sin petición HTTP, y por tanto sin tenant: cualquier consulta que no pase por un repositorio verá todos los centros. Antes de ese bloque, invertiría el diseño: cerrado por defecto, con un ámbito de sistema explícito.

**2. El mapa código de error → HTTP está copiado controlador a controlador, y ya diverge.**
- `AvailabilityController` traduce `APT_SLOT_UNAVAILABLE` a 409 pero manda `GEN_CONFLICT` a 500.
- Empleados, Clientes, Servicios y Paquetes hacen justo lo contrario.
- `AppointmentsController` traerá `APT_INVALID_STATE`, y el mismo error acabará siendo 409 en un endpoint y 500 en otro.
- La tarea `869f17y6k` está en prioridad baja. Yo la haría justo antes de `869d7f519`, junto con dos cosas más: el envelope de los 400 de model binding y un manejador global de excepciones.
- No he encontrado ese manejador en el pipeline. Si no existe, un error no controlado sale como 500 sin envelope, y la tabla de cobertura del vol. 1 no recoge ese hueco.

**3. La Clean Architecture es nominal.** Los casos de uso viven en Infrastructure; Application solo tiene contratos, DTOs y validadores. No es un bug, pero explica que los servicios dependan de `UserManager` y que `EmployeeService` tenga nueve dependencias. Si se queda así, que conste como decisión.

**4. Los tests no cubren integración.** Los repositorios se prueban contra SQLite, que no se comporta como SQL Server en colaciones, `LIKE` o `DateOnly`/`TimeOnly`. No hay ni un test de integración HTTP ni Testcontainers, aunque la estrategia de testing los da por base.

**5. Pendientes de cara a producción:**
- La resolución de tenant consulta la BD en cada petición y va antes del rate limiter.
- El rate limiter particiona por `RemoteIpAddress`; detrás de un ALB, todos los usuarios compartirían IP.
- `Europe/Madrid` está fijado en código: un centro en Canarias vería los huecos desplazados una hora.
- `MultiTenantOptions` no tiene validación al arranque, a diferencia de `App` y `LegalDocuments`.
- El 400 de tenant devuelve al cliente el motivo interno y la estrategia activa.
- El salto del 2FA por login social (`869f151x1`) debería cerrarse antes de cualquier despliegue.

### Frontend

Lo que hay está bien hecho: separación entre página y componente, disciplina de tokens, E2E con axe en tres navegadores y un interceptor con semántica cuidada. Pero tiene huecos que afectan a todo lo que venga:

- **La sesión no se renueva.** `refreshAccessToken()` es un stub y el refresh token ni se persiste: al caducar el access token, el usuario vuelve a login. La tarea `869d7f79y`, con «refresh» en el título, está en shipped, y no hay ninguna tarea pendiente que lo cubra. Decidiría aquí el almacenamiento (refresh en cookie httpOnly, access token en memoria) antes de construir más pantallas.
- **Tras recargar, el store solo recupera el token** y `user` queda a null. Los guards por rol necesitarán rehidratar con `/account/me`. Hoy un Customer registrado puede navegar a `/empleados`: la API le deniega el acceso, pero la SPA no.
- **El theming por tenant es solo disciplina de tokens.** No hay inyección en runtime, y `.dark` es la plantilla de shadcn.
- **No hay tests unitarios.**

### Riesgos de plataforma y dependencias

No dependen del código escrito, pero afectan a todo:

- **.NET 8.** Microsoft deja de publicar parches de seguridad para .NET 8 el 10 de noviembre de 2026 y recomienda pasar a .NET 10, que es LTS con soporte hasta noviembre de 2028. La regla «8.0.x» del proyecto garantiza lanzar el MVP sobre un runtime sin parches.
- **FluentAssertions.** Desde la versión 8, su uso comercial exige licencia de pago; la 7 sigue siendo abierta. La regla «sin fijar versión» metió la 8.10.
- **AutoMapper y MediatR.** Ambas pasaron a doble licencia, con ediciones comerciales de pago. Usas AutoMapper 16 y MediatR 14, y no he encontrado ningún uso de MediatR en el código.
- **vue-i18n 9.** El propio `package-lock` la marca como sin soporte y pide pasar a la 11.
- **recharts.** El stack la prevé para el dashboard, pero es una librería de React.

## Incoherencias concretas

| Hallazgo | Dónde | Destino |
|---|---|---|
| La regla «no fijar puertos» se incumple en todo el repo. Conviven `baseURL` absoluta y proxy `/api`, así que el proxy no se usa, y un build sin `VITE_API_BASE_URL` llamaría a localhost en producción | `vite.config.ts`, `client.ts`, `auth.api.ts`, `.env.*`, vol. 1-2, guía de secrets, tarea `869f18fuy` | `869f6r69b`; la regla pasa a «5555 como convención» (D-18) |
| Tres tablas de tarjetas Redsys con valores distintos: `4548810000000003` es la de uso normal en una y la denegada en otra | `user-secrets-guide.md`, `reservarte-testing-strategy.md`, plantilla de PR | `869f6r58r`, `869f6r4hm` |
| Stack y estructura anticuados: TS 5.3, Vite 5, Vue Router 4, MediatR 12, Swashbuckle 6.5, `src/ReservArte.API`, `frontend-web/`, `tests/ReservArte.IntegrationTests`. Lo real: TS 6, Vite 8, Router 5, MediatR 14, Swashbuckle 10 | vol. 1 §4.1 | `869f6r58r` |
| El script de instalación genera `enums.ts` con `enum` (prohibido por `erasableSyntaxOnly: true`), y con 6 estados de cita en PascalCase frente a los 8 en snake_case del backend | `Scripts de instalación.md`, paso 5 | `869f6r58r` |
| La inversión total aparece como 215.586 € y como 215.739 €. Los escenarios de MRR no cuadran con los precios: el conservador da 20×49 + 5×99 + 2×199 = 1.873 €, no 1.675 €, y los otros dos tampoco cuadran | vol. 3 §11.6-11.7 | `869f6r58r` |
| Cabecera «v1.0 · octubre 2025» con dos desarrolladores, sin ninguna tarea asignada ni actividad de un segundo miembro | vol. 3 | `869f6r58r` |
| Tareas cerradas cuyo título dice otra cosa: «PR obligatorio + CI verde» (develop no exige PR y no hay CI) y «vitest-axe» (se hizo con Playwright) | `869d7ewu5`, `869d7fbpp` | `869f74uca` |
| Espacio Docs sin mantener: la guía de secrets existe pero su tarea sigue en draft, y la corrección 6/10 del vol. 3 está aplicada pero su tarea también sigue en draft | `869d7ee0w`, `869f18nq5` | `869f74uca` |
| Comentario de clase obsoleto: dice que `Employees` aún no tiene query filter global | `EmployeeRepository.cs` | `869f6r5jf` |
| Roles en minúsculas en datos de test, pese al catálogo en PascalCase | `AppDbContextTenantResolutionTests`, `session-ending.spec.ts` | `869f6r5jf` |
| Claves de configuración muertas: `IpRateLimiting` (la doc la declara fuera del esquema) y `Email:Provider` (el código elige proveedor con `IsDevelopment()`) | `appsettings.json`, `AuthServiceExtensions.cs` | `869f6r5jf` |
| Ruta inexistente: `cd src/ReservArte.API` | `user-secrets-guide.md` | `869f6r58r` |
| DoD imposible de cumplir hoy: «1 reviewer», «probado en staging», «cobertura antes/después» | plantilla de PR | `869f6r4hm` |

## Decisiones que revisaría y cómo trabajaría

**El orden de construcción.** Es la decisión que más cambiaría. Acordaría con More Than Brows un «MVP piloto» mínimo: agenda, clientes, servicios, empleados y recordatorio por email, con cobro en el centro. Lo entregaría en rebanadas verticales:
- Primero, los endpoints de citas junto con el calendario.
- Después, las pantallas de gestión sobre la API que ya existe.
- Una demo al cliente cada dos semanas.

Redsys, con preautorización, COF y penalizaciones, es la parte más cara y arriesgada del MVP. Mejor que llegue cuando alguien ya use la agenda a diario.

**Cambios técnicos:**
- CI mínimo en GitHub Actions antes de la siguiente tarea: build, test, format y lint en cada PR, obligatorio en `main`.
- Migración a .NET 10 ahora, mientras la base de código es mediana y los 500 tests sirven de red.
- Quitar MediatR.
- Sustituir AutoMapper por mapeo manual o un generador como Mapperly, o bien comprar la licencia.
- Fijar FluentAssertions en la 7 o cambiar de librería.
- Pasar a vue-i18n 11 y elegir una librería de gráficas para Vue antes del dashboard.

**La app en React Native.** La replantearía. La web ya es mobile-first con BottomNav, y una PWA o Capacitor sobre la misma app Vue cubriría buena parte de la fase 2. Costaría una fracción de las 480 h previstas y evitaría un segundo stack de UI para una sola persona.

**SQL Server en Docker para producción.** El presupuesto de unos 55 €/mes no incluye licencia. La edición Developer no se puede usar en producción, y Express limita cada base de datos a 10 GB. O se presupuesta una edición de pago (o RDS con licencia incluida), o se decide el motor conscientemente.

**En la gestión:**
- Formalizar Kanban con las métricas que ClickUp ya registra.
- Rehacer las fechas con el throughput real.
- Arrancar ya los trámites externos.
- Limpiar ClickUp: renombrar «Active Sprint», archivar las listas vacías y poner al día Docs o archivarla.
- Sincronizar las instrucciones de este proyecto con CLAUDE.md.

**La IA de documentación.** Mantendría la auditoría, que es valiosa, pero la haría al cierre de cada bloque, más una revisión completa mensual. El detalle por tarea quedaría en el PR.

## Porcentaje realizado

**Método:**
- El peso de cada fase sale de las horas de código de tu propio vol. 3 §11.1 (backend, frontend, full-stack y móvil, sin DevOps, UX ni QA): 1.600 h la fase 1, 1.320 h la fase 2 y 800 h la fase 3.
- Dentro del MVP, el peso de cada módulo es una estimación mía.
- Cuento como hecho lo implementado y verificado, aunque ninguna pantalla lo consuma.
- Dejo fuera infraestructura, CI y despliegue.

| Bloque | Avance |
|---|---|
| Backend: fundación, multi-tenant base, Empleados, Clientes y Catálogo | ~100 % |
| Backend: autenticación (faltan 2FA en OAuth, consentimiento en alta social y refinamientos) | ~90 % |
| Backend: Citas (faltan endpoints, lista de espera, no-shows, historial y penalización) | ~50 % |
| Backend: Redsys, recordatorios, dashboard y configuración del centro | ~2 % |
| **Backend del MVP** | **≈ 56 %** |
| Frontend: setup, layout y pantallas de autenticación | ~85 % |
| Frontend: componentes base | ~30 % |
| Frontend: gestión, agenda, pagos y configuración | 0 % |
| **Frontend del MVP** | **≈ 21 %** |
| **MVP (fase 1)** | **≈ 39 %** |
| Fase 2 (reserva pública, fotos, fidelización, app móvil) | ≈ 3 % |
| Fase 3 (onboarding, suscripciones, panel SaaS) | ≈ 10 % |
| **Proyecto completo (fases 1-3)** | **≈ 20 %** |

Hay un margen de ±5 puntos por los pesos internos. Si con «integración» te referías a las integraciones con terceros (Redsys, SES, Hangfire) y no a infraestructura y despliegue, al sacarlas del cálculo el MVP sube a ≈ 46 % y el total a ≈ 22 %.

## Seguimiento de las recomendaciones (2026-09-25)

| Recomendación | Decisión | Tareas |
|---|---|---|
| CI mínimo antes de la siguiente tarea | D-08 | `869d7ex56`, `869d7ex8r`, `869f6r4t8` |
| Migrar a .NET 10 antes del 10-nov | D-09 | `869f6r5ca` |
| Entregar en vertical hacia un MVP piloto | D-03 | `plan.md`, `869f6r4zt` |
| Kanban con métricas y fechas rehechas | D-01, D-05 | `869f6r4ec` |
| Arrancar los trámites externos | D-22, D-26 | `869f6r4nz` y subtareas |
| Limpiar ClickUp y DoD por bloque | D-23 | `869f74uca` |
| Sincronizar las instrucciones del proyecto de claude.ai | — | instrucciones nuevas (2026-09-25) |
| Volúmenes como referencia, ADR, estado fuera, auditoría periódica | D-06, D-07 | `869f6r52d`, `869f6r54r` |
| `CLAUDE.md` corto | D-02, D-27 | `869f6r4ba` |
| Filtros de tenant cerrados antes de Hangfire | D-14 | `869f6r5vy` |
| Mapa de errores, manejador global y 400 con envelope | D-13 | `869f6r5r2`, `869f1k17q` |
| Clean Architecture nominal: dejarlo como decisión | D-15 | `869f6r54r` |
| Tests de integración | D-16 | `869f6r5ng`, `869f2gh37` |
| Pendientes de cara a producción | D-19 | `869f6r65a`, `869f74u7y`, `869f6r5jf` |
| Sesión de la SPA y almacenamiento de tokens | D-17 | `869f6r61z`, `869f6r6hc`, `869f1auqv` |
| Dependencias y licencias | D-10, D-11, D-12 | `869f6r5eu`, `869f6r6dk`, `869f6r6nx` |
| Replantear la app en React Native | D-20 | `869f6r74n` |
| SQL Server en producción | D-21 | `869f6r4ww` |
| Base legal de accesibilidad | D-25 | `869f6r58r` |
| Incoherencias concretas | — | columna «Destino» de su tabla |
