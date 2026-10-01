# Prompt para la IA de documentación — cierre de la Fase 3 (frontend de la agenda) (2026-10-01)

> Preparado por Claude Code con `/cerrar-bloque` (`869d7edvq`). Guillermo lo pega entero, en **modo
> Agent**, en un chat nuevo de Cursor. Copia solo lo que va entre las dos líneas `~~~`.

~~~text
# Documentación del bloque «Agenda, reserva y modales» (ClickUp 869d7edvq) y del resto de la
# Fase 3 del frontend (PRs #105-#118, 30-sep y 1-oct de 2026)

## 0. Auditoría de coherencia (obligatoria, antes de cambiar nada)
Comprueba que está aplicado el prompt anterior («Sistema de Citas», 2026-09-30). Señales: existen
Documentation/adr/ADR-034-alta-citas-personal.md y ADR-035-prueba-alergia-aviso.md y figuran en el
índice de Documentation/adr/README.md; el vol. 2 §9.9 describe la API de citas. Si no, detente y
repórtalo.

Fuentes de contexto que puedes LEER (no editar):
- .claude/contexto/decisiones.md: texto exacto de H-42, H-43, H-44, H-45 y DP-07;
- .claude/rules/frontend.md y .claude/rules/backend.md: reglas vigentes (navegación, componentes,
  reserva, listado de citas, cancelación; módulo de citas del backend);
- reservarte-web/src/router/index.ts: rutas reales; reservarte-web/src/locales/es/index.ts: textos;
- ReservArte-API/Controllers/AppointmentsController.cs y AvailabilityController.cs: rutas y roles;
- ReservArte-Application/DTOs/Appointments/: nombres exactos de los campos;
- data/schema/create_ReservArteDB.sql y data/README.md: columnas y datos demo reales.
Si algo de este prompt contradice esas fuentes, manda el código: repórtalo sin corregirlo.

## 1. Qué se ha hecho (contexto; no lo copies como registro de estado)
Cimientos del frontend:
- 869f6r69b (PR #105): la SPA llama a /api en su mismo origen. Proxy de Vite hacia API_PROXY_TARGET
  (sin prefijo VITE_, por defecto http://localhost:5555). Se retiran VITE_API_BASE_URL y VITE_APP_URL;
  el returnUrl de OAuth sale de window.location.origin. En producción, /api se sirve en el mismo
  origen que la SPA (proxy inverso delante de la API).
- 869eqxm8z (PR #106): tests unitarios y de componente con Vitest + @vue/test-utils en happy-dom,
  convención __tests__/*.spec.ts, en el CI («Frontend CI / lint-build»). Lockfile generado con
  Node 24 y npm reciente (npm 11.6.2 borraba dependencias peer opcionales de Rolldown).
- 869f6r6dk (PR #107): vue-i18n 11 solo con Composition API (flags en vite.config.ts). Las pantallas
  nuevas usan claves; las de autenticación aún llevan textos escritos a mano.
- 869d7fbuf (PR #111): componentes base sobre Reka UI 2.9.7 (MIT), sin diseño en Figma, según
  Documentation/Desing/styles-reference.html y los tokens: Input, Select, Dialog, Tabs, Badge, Table
  (piezas nativas) y Toaster (montado en App.vue, alimentado por uiStore.addToast). Text fija la altura
  de línea «normal» de Figma (el preflight de Tailwind imponía 1,5).
- 869d7fbxn (PR #113): listado de gestión según Figma «CRUD» (387:56720): DataList (HeroBanner con
  Volver, Nuevo y buscador; filas ListItem; carga, vacío, error y paginación) y el composable
  useDataList (búsqueda con espera, página, filtros).
Navegación y pantallas:
- 869ep9p36 (PR #108): sin Sidebar ni Header; pantallas planas bajo el BottomNav (Inicio, Contacto y
  Mi cuenta). La pantalla de Usuario (/cuenta, Figma 387:56701) es el acceso a la gestión: «Área de
  administración» (Admin, Manager y Employee) y «Área de usuario» (todos), con «Cerrar sesión» al
  final. Token de color nuevo «highlight» (cabeceras de sección del menú).
- 869faaunu (PR #109), H-42: Mis citas (/mis-citas, Figma 387:56617 y 387:56660), aterrizaje tras el
  login y destino de «Inicio»: la próxima cita con «Modificar» y «Cancelar», o «No hay citas
  asignadas» con «Reservar Cita». Contacto (/contacto, Figma 387:56672). El CTA «Cuenta» pasa a
  «Mi cuenta».
- 869fabu5a (PR #110): Contacto según su diseño por anchos (Figma 387:57554, 375-1440 px): mapa de
  Google con la dirección del centro (provisional en src/config/center.ts: «Calle Bolonia, 4,
  Zaragoza (50008)»; saldrá de la base de datos con 869fabu4m) y bloque de datos alineado.
- 869d7fbxn y H-43: no hay gestión de usuarios genéricos: «Usuarios» del Área de administración pasa
  a «Clientes» (/clientes); clientas y empleados se gestionan por separado.
Reserva y citas (H-44 y H-45):
- 869fagpx9 (PR #114), backend: ventana de reserva por organización (Organizations.
  CustomerBookingWindowWeeks = 6 y StaffBookingWindowWeeks = 10, CHECK 1-52, migración
  AddOrganizationBookingWindows); GET /api/v1/appointments/availability/by-service?serviceId&date
  (huecos agrupados por los empleados que prestan el servicio) y
  GET /api/v1/appointments/availability/days?serviceId&from&to (días con hueco, 62 como mucho), ambos
  recortados a la ventana del rol de quien consulta. La clienta crea y modifica su propia cita (la
  API la toma del token), con una sola cita activa (409 APT_ACTIVE_EXISTS, código nuevo) y dentro de
  su ventana (400 con código OutsideBookingWindow). Semilla de servicios de ejemplo, asignaciones y
  horarios en DevSeeder y data/demo.
- 869fagpyg (PR #115): pantalla de reserva y modificación (/reservar, Figma «Selección de cita»
  387:56629): servicio, calendario (lunes primero; hoy con círculo primary #FFB6C1; días con hueco
  con círculo accent #FFE4E1; pasados y fuera de la ventana deshabilitados), huecos por empleada; para
  el personal, «Seleccionar cliente» (buscador con foto y nombre) y «Cita para: …», y si la clienta ya
  tiene cita activa, diálogo «Modificar esa cita» o «Crear una nueva». Al reservar, aviso «Cita
  reservada» y vuelta a Mis citas.
- 869fajbw0 (PR #116), corrección: Mis citas muestra solo las citas de la cuenta conectada como
  clienta (también para el personal); el personal consulta las del centro en el listado.
- 869fajn7g (PR #117): listado de citas del personal (/citas, desde «Citas» del Área de
  administración; sin diseño en Figma, con el estilo de la app): vistas de día, semana (lunes a
  domingo) y mes con navegador por bloques y «Hoy», filtro por empleada, estado en color, detalle
  (servicios, precio, avisos de alergia) con acciones según estado y rol (Confirmar, Iniciar,
  Completar; No presentada solo Admin y Manager) y «Modificar» esa cita en /reservar?cita=<id>.
  «Nueva cita» abre la reserva.
- 869d7fcfy (PR #118): cancelación con selector de motivo (de clienta o de personal) y «Otro motivo»
  con texto libre, sin penalización en el piloto, desde Mis citas y desde el detalle del listado.
- 869faedz3 (PR #112): solo desarrollo: administrador takervare@gmail.com que entra con Google, sin
  contraseña local, sembrado en DevSeeder y data/demo.
Canceladas por H-45: agenda con FullCalendar (869d7fc8y), wizard de 6 pasos (869d7fcd0 y 869d7fcen),
RescheduleModal (869d7fch0, cubierto por la reserva), AppointmentCard (869d7fcbu, cubierto por el
detalle del listado) y colores con arrastre (869d7fca1: colores hechos, arrastre descartado). La
pantalla de lista de espera sale del piloto (869fakvt9, post-piloto, como su backend 869f7axfq).
DP-07 (pendiente): FullCalendar sigue en package.json sin usarse hasta después del MVP.

## 2. Cambios por documento
### Vol. 1 — Análisis
- §3.1.5 (Sistema de Agenda y Citas): la clienta autenticada reserva, modifica (una cita activa) y
  cancela la suya; el personal reserva para cualquier clienta y gestiona las del centro desde el
  listado; ventana de reserva por organización (6 y 10 semanas). La agenda con FullCalendar y el
  wizard de 6 pasos ya no forman parte del producto (H-45).
- §4.1.2 (Frontend Web) y §4.1.2.1 (design system): Reka UI 2.9.7 y los componentes base;
  @internationalized/date 3.12.1 (Apache-2.0) para el calendario; vue-i18n 11; Vitest. Si algo
  afirma que la UI ya está internacionalizada, corrígelo: solo las pantallas nuevas usan claves.
- §5.1.2 (catálogo de códigos): añade APT_ACTIVE_EXISTS (409).
- §5.1.3 (configuración): API_PROXY_TARGET y la llamada a /api en el mismo origen; fuera
  VITE_API_BASE_URL y VITE_APP_URL.
- §5.1 o donde se describan los contratos de citas: availability/by-service y availability/days;
  POST y PUT de citas abiertos a la clienta para sí misma (customerId opcional; obligatorio para el
  personal); cancelación con reason opcional.
- §5.2 (esquema): las dos columnas nuevas de Organizations con su CHECK.
### Análisis de pantallas y estructura.md
- §2 (dashboard), §6 (agenda y citas) y §13 (reserva pública): sustitúyelos por lo que existe:
  navegación plana con BottomNav y pantalla de Usuario (/cuenta); Mis citas (/mis-citas) como inicio;
  Contacto (/contacto); reserva y modificación (/reservar); listado de citas del personal (/citas)
  con su detalle y la cancelación. Marca la reserva pública anónima como fuera del piloto. Indica el
  nodo de Figma de cada pantalla que lo tenga, y que /citas y los componentes base no tienen diseño.
- §3 y §4: no hay gestión de usuarios genéricos (H-43); el menú lleva «Clientes».
- Actualiza la versión y la fecha de la cabecera (sigue en 1.0, octubre de 2025).
### Vol. 2 — Implementación y desarrollo
- §9.2.4 (navegación global): BottomNav con Inicio, Contacto y Mi cuenta; sin layouts; gestión desde
  /cuenta.
- §9.2 (patrones de la SPA), apartado nuevo: capa de API (apiRequest en src/lib/api/request.ts y
  ApiRequestError), composables (useDataList, useBooking, useAgenda), componentes base y el patrón
  de listado; currentUserId del authStore (id del usuario o, tras recargar, el sub del token, solo
  para filtrar la vista).
- §9.9 (módulo de citas): reserva por la clienta, ventana de reserva, disponibilidad por servicio
  (IServiceAvailabilityService y la rejilla compartida SlotGrid). Aprovecha para quitar de §9.9 lo que
  sea registro de tareas (PRs, recuentos, «siguiente»).
### Vol. 3 — Planificación y gestión
- Fase 3 del plan: refleja H-45 (una sola pantalla de citas, con el listado del personal) y las
  cancelaciones; la lista de espera fuera del piloto.
- §12.2 (checklist de arranque, frontend y testing): Vitest ya no está pendiente.
### Otros
- reservarte-testing-strategy.md: capa unitaria y de componente del frontend (Vitest, __tests__/,
  sustitución de la red con el adaptador de Axios); E2E con la API simulada (page.route) y axe;
  excepción documentada de aria-hidden-focus con avisos visibles (focus proxies de Reka UI).
- accessibility-and-i18n.md: componentes de Reka UI (foco atrapado en diálogos, teclado en Select,
  Tabs y calendario); textos con claves en las pantallas nuevas.
- Scripts de instalación.md (y el vol. 3 si lo cita): sustituye VITE_API_BASE_URL por
  API_PROXY_TARGET; Node 24.

## 3. ADR
- Nuevo ADR-036: «Pantallas de la clienta y aterrizaje en Mis citas» (H-42). Contexto: navegación
  plana y diseños de Figma. Decisión: tras el login, todos aterrizan en /mis-citas; Contacto con mapa
  de Google; «Cuenta» pasa a «Mi cuenta». Consecuencias: los datos del centro van provisionales en la
  SPA hasta 869fabu4m; el mapa de Google exige contemplar sus cookies en los textos legales.
- Nuevo ADR-037: «Clientes y empleados por separado, sin gestión de usuarios genéricos» (H-43).
- Nuevo ADR-038: «La clienta reserva y modifica su cita» (H-44). Sustituye la parte de ADR-034 que
  remitía la reserva de la clienta a la reserva pública. Ventana por organización; una sola cita
  activa; alternativas descartadas: solo el personal reserva; reserva pública anónima en el piloto.
- Nuevo ADR-039: «Una sola pantalla de reserva y un listado de citas en lugar de agenda y wizard»
  (H-45). Alternativas descartadas: agenda con FullCalendar; wizard de 6 pasos. Consecuencias:
  FullCalendar queda sin uso (DP-07, se decide tras el MVP); reagendar es «Modificar»; el arrastre se
  descarta.
- Enlaza ADR-038 desde ADR-034 y actualiza el índice de Documentation/adr/README.md.

## 4. Restricciones
- No añadas registros de estado, PRs ni recuentos a los volúmenes.
- Un dato, una fuente: enlaza en vez de copiar.
- No verifiques IDs de ClickUp.
- Si algo contradice otro documento o una decisión, repórtalo sin corregirlo.

## 5. Advertencias
Señala todo lo que veas dudoso o desfasado, aunque no esté en este prompt. En particular, avisa de
cualquier sitio que aún describa la agenda con FullCalendar, el wizard de 6 pasos, el Sidebar o la
gestión de usuarios genéricos.
~~~

## Pendiente de la documentación que no entra en este prompt

Va a la **auditoría mensual de octubre** (plantilla B), que se hace a continuación:
- Vol. 3: el bloque de Citas del MVP aún incluye penalización, lista de espera y no-shows, y dice
  «5/12» y que faltan los endpoints.
- Vol. 3, meses 6-7 y cuadro de costes: siguen con React Native contra ADR-020 (PWA).
- Registros de estado en los volúmenes 1-3 y en el checklist del vol. 3, incluidas las notas
  históricas «Runtime (PR #nn): SQL Server…».
- Vol. 3 §12.1: región `eu-west-1` (es `eu-south-2`, D-29); §11.2, §11.6 y §11.7: costes por recalcular.
- `appsettings.Production.json`: `Serilog:Region` en `eu-west-1` (se corrige en la Fase 6).
