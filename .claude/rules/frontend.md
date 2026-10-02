---
paths:
  - "reservarte-web/**"
---

# Frontend (Vue) — reglas

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

Hoy es solo disciplina de tokens: no hay inyección en runtime (llega con `869f6r6xv`, post-piloto,
sobre la identidad de marca de `869f74u8c`), y la paleta `.dark` es la plantilla de shadcn
(`869f0w75h`). Las gráficas también leen sus colores de los tokens.

## Estructura y patrones

- `src/pages/<área>/…Page.vue`: páginas contenedoras (ruta, carga de datos y orquestación).
- `src/components/`: componentes presentacionales (props y emits); `src/components/ui/` es la base
  sobre Reka UI (`869d7fbuf`): `Input`, `Select`, `Dialog`, `Tabs`, `Badge`, `Table` (piezas nativas),
  `Toaster` (montado en `App.vue`; se usa con `uiStore.addToast`), además de `Button` y `Text`.
  - Listados de gestión (`869d7fbxn`, Figma «CRUD» `387:56720`): `DataList` (cabecera `HeroBanner`
    con «Volver», «Nuevo» y buscador; filas `ListItem` en talla `sm`; carga, vacío, error y
    paginación) con el estado en `useDataList` (`src/lib/composables/`): búsqueda con espera, página,
    filtros y descarte de respuestas atrasadas. La feature aporta el `fetcher` que devuelve
    `{ items, pagination }` a partir de `data.items` y `meta.pagination`. Sin
  diseño propio en Figma: siguen `styles-reference.html` (ángulos rectos, foco rosa con halo, modal
  blanco con sombra) y los tokens. Antes de escribir un campo, diálogo o desplegable a mano, úsalos.
  - `Text` fija `leading-[normal]` (la «auto» de Figma) después de combinar clases: el preflight de
    Tailwind pone 1,5 y tailwind-merge descarta `leading-*` si llega detrás un tamaño de letra.
- `src/features/<área>/api/*.api.ts` y `src/features/<área>/types/`: llamadas a la API por feature.
  Las nuevas usan `apiRequest` (`src/lib/api/request.ts`, `869fagpyg`), que desenvuelve el envelope y
  lanza `ApiRequestError` con `code` y `details`; `auth.api.ts` conserva su propio desenvuelto.
- `src/lib/api/client.ts`: Axios, Bearer e interceptores (semántica en la regla de contrato de API).
- `src/stores/`: Pinia (`authStore`, `uiStore`). `src/styles/globals.css`: los tokens.
- Formularios con VeeValidate + Zod; textos con vue-i18n 11 (`es`), sin literales en las plantillas;
  fechas e importes con `date.utils.ts` y `currency.utils.ts`.
- vue-i18n 11 solo con Composition API: `useI18n()` en `<script setup>` y `$t` en plantillas
  (`globalInjection`). Los flags de compilación van en `define` de `vite.config.ts`
  (`__VUE_I18N_LEGACY_API__: false`): `createI18n({ legacy: true })` no funcionaría. Nada de `$tc`
  ni `v-t` (retirados o desaconsejados en la 11). **Hoy las pantallas de autenticación todavía llevan
  los textos escritos a mano** (solo hay cuatro claves en `src/locales/es`): lo nuevo va con claves,
  y lo existente se migra cuando se toque.
- Sin `enum` (`erasableSyntaxOnly`): uniones de literales u objetos `as const`. Los estados de cita
  son los 8 del backend, en snake_case.
- Navegación (`869ep9p36`): BottomNav global en `App.vue` (Inicio, Contacto y Mi cuenta) y **pantallas
  planas**, sin layouts: no hay Sidebar, Header, `DashboardLayout` ni `AuthLayout`. La gestión se abre
  desde la pantalla de Usuario (`/cuenta`, `pages/account/AccountPage.vue`, componente `ui/menu`):
  «Área de administración» para Admin, Manager y Employee, y «Área de usuario» para todos. Una
  pantalla nueva es una ruta plana con `meta.requiresAuth` y su entrada en ese menú, no un menú propio.
  - El rol sale de `authStore.user.rol`: tras recargar, el usuario es `null` hasta `869f6r6hc` y el
    área de administración no se muestra (se vuelve a ver al iniciar sesión).
  - Cabeceras: `Banner` con el logo. Fondo de las cabeceras de sección del menú: token `highlight`.
  - Citas (H-45, `869fagpyg`): la reserva y modificación es `/reservar` (`pages/booking/BookingPage.vue`,
    Figma `387:56629`), con la lógica en `useBooking` (`features/appointments/composables/`). La abren
    «Reservar Cita» y «Modificar» de Mis citas, y «Nueva cita» del listado del personal.
  - Listado de citas del personal (`869fajn7g`, sin diseño: estilo de la app): `/citas`
    (`AppointmentsPage`, «Citas» del Área de administración), con `useAgenda` (día, semana de lunes a
    domingo y mes; navegación por bloques; filtro por empleada en la SPA) y `AppointmentDetailDialog`.
    Colores por estado y acciones según estado y rol en `utils/appointment-status.ts` (espejo de la
    máquina de estados del backend). «Modificar» abre `/reservar?cita=<id>`, que modifica esa cita en
    concreto y al terminar vuelve al listado.
  - Cancelar (`869d7fcfy`, CancelModal): `CancelAppointmentDialog` con motivos de clienta o de personal
    y «Otro motivo» con texto; sin penalización en el piloto. En «Cancelar» de Mis citas y en el detalle
    del listado (`canCancel`: pendiente, confirmada o en curso). Quién cancela lo deduce la API.
    Calendario `BookingCalendar` (Reka UI + `@internationalized/date`, lunes primero, hoy en `primary`,
    días con hueco en `accent`), huecos con `EmployeeAvailability` y, para el personal,
    `CustomerPicker`. Tras recargar, el rol no se conoce hasta `869f6r6hc` y la pantalla actúa como
    clienta: el personal entra navegando, no recargando.
  - Aterrizaje con sesión (`869faaunu`): `/mis-citas` (login, 2FA, OAuth y la raíz `/`, que redirige).
    Mis citas enseña solo las citas de la cuenta conectada como clienta: pide `customerId` =
    `authStore.currentUserId` también para el personal, al que la API devuelve el centro entero
    (`869fajbw0`). Lo mismo al buscar la cita activa propia en `useBooking`. `currentUserId` toma el
    usuario cargado o, tras recargar, el `sub` del token (`jwt.utils.ts`, sin verificar: solo filtra).
    No hay ruta de panel de métricas hasta `869d7fc7e`.
  - Datos del centro que la API aún no da (horario, teléfono, Instagram, dirección del mapa): en
    `src/config/center.ts`, provisional hasta que salgan de la base de datos (`869fabu4m`, H-42).
    Contacto usa el mapa de Google con esa dirección (sin dirección no lo carga); los E2E simulan
    `www.google.com` para no depender de un tercero.
  - Empleados (`869d7fbyt` + `869d7fc0h`, Figma «CRUD» `387:56720` y «Detalle usuario» `387:56778`):
    `/empleados` (`EmployeesPage`, `DataList` con foto, rol y filtro Activos/De baja; la baja pide
    `ConfirmDialog`), `/empleados/nuevo` y `/empleados/:id` (`EmployeeDetailPage`, carga diferida
    como el resto de la gestión que se añada). La ficha tiene pestañas en la URL (`?tab=schedule`,
    `?tab=absences`): Datos (`EmployeeForm`, VeeValidate + Zod espejo del validador; el email repetido
    sale en su campo), Servicios (`EmployeeServicesEditor`, 4.1b: casillas del catálogo activo por
    categoría, sin nivel, el conjunto entero en un PUT), Horario (`ScheduleEditor`: lunes = 0, tramos por día, mismas reglas que la API,
    y la semana entera en un PUT) y Ausencias (`AbsenceList` y `AbsenceDialog`). El alta lleva a la
    ficha nueva en Servicios. Patrón a reutilizar en Clientes (4.2).
  - Clientes (`869d7fc34` + `869d7fc51`, sin Figma propio: el patrón de Empleados por indicación de
    Guillermo): `/clientes` (filtros de estado y categoría; categoría, «Bloqueado» y «De baja» en la
    segunda línea), `/clientes/nuevo` y `/clientes/:id`. Pestañas: Datos (`CustomerForm`; en el alta,
    consentimientos RGPD con `data_processing` obligatorio y sin `saved_cards` en el piloto; al editar,
    `CustomerConsents` para dar o retirar cada uno, con confirmación al retirar el de datos porque da
    de baja la ficha, H-47, y `CustomerBlock` con motivo), Notas (`CustomerNotes`, con autora),
    Alergias (`AllergyTestPanel`: última prueba en hora del centro; `CustomerAllergies`: alta, edición
    y baja con gravedad) y Citas (`CustomerHistory`, «Ver más»).
  - Fichas con pestañas: el `watch` que recarga observa una clave de texto
    (`` `${route.name}:${route.params.id}` ``). Con un array, cada `?tab=` recargaba la ficha y se
    perdía lo editado sin guardar.
 (foto o iniciales sobre `accent`; si la imagen falla, iniciales). La subida es
    `869d7ee5t`: hasta entonces la ficha conserva `profileImageUrl` y no ofrece cámara ni subida.
  - Zona horaria: `CENTER_TIME_ZONE` (`config/center.ts`). El horario y las citas van en hora local
    sin zona; las ausencias se piden en hora del centro y viajan en UTC
    (`features/employees/utils/absence-dates.ts`, con `@internationalized/date`). Una ausencia de días
    completos va de las 00:00 del primero a las 00:00 del día siguiente al último.
  - Listados paginados: `apiPagedRequest` (`lib/api/request.ts`) devuelve `{ items, pagination }` de
    `data.items` y `meta.pagination`, listo para `useDataList`.
  - No hay gestión de usuarios genéricos (H-43): clientes y empleados se gestionan por separado. En
    el Área de administración, «Clientes» va a `/clientes`; no existe `/usuarios`.
- Diseño en Figma: fichero `Trabajo` (`JSScv098x1yPk40ec6xRrv`), pantallas de iPhone 14/15 Pro (393 px):
  Usuario (admin) `387:56701`, Contacto `387:56672`, Home con cita `387:56617` y sin cita `387:56660`.
  Contacto tiene además su versión por anchos (`Contact-Page`, `387:57554`: 375, 576, 768, 992, 1200 y
  1440). Cuando Figma da cortes que no son los de Tailwind (576, 992 y 1200), se usan tal cual con
  `min-[576px]:`, `min-[992px]:` y `min-[1200px]:`, sin redefinir `sm`/`lg`/`xl` para todo el proyecto. El kit es Material 3: traduce sus colores a
  los tokens del proyecto (p. ej. #FFB6C1 → `primary`, #FFE4E1 → `accent`) y los SVG a `currentColor`.
  El plugin tiene cupo de lecturas: pide solo los nodos necesarios.
- URL de la API: rutas relativas `/api/...` en el mismo origen (`869f6r69b`); sin URLs absolutas ni
  fallbacks a localhost en `src/`. Detalle en la regla de contrato de API.
- Gráficas: nada de recharts (es de React); la librería Vue se decide en `869f6r6nx`.

## Dependencias y lockfile

- Versión explícita y licencia revisada (MIT, ISC, Apache-2.0, BSD y BlueOak-1.0.0 valen para uso
  comercial). En `devDependencies` de test, versión exacta (`npm install -D -E`).
- El `package-lock.json` se genera con un npm tan reciente como el del CI (Node 24 más reciente;
  npm 11.19 con Node 24.21 el 30-sep). npm 11.6.2 (Node 24.11) borra del lockfile dependencias peer opcionales de
  plataforma (`@emnapi/core` y `@emnapi/runtime`, del binario WebAssembly de Rolldown) y el `npm ci`
  del CI falla con «Missing: … from lock file» (PR de `869eqxm8z`). Si el Node local es antiguo:
  `npx -y npm@<versión> install …`. Comprueba siempre con `npm ci` en limpio antes del PR.

## Tests

- E2E: Playwright + `@axe-core/playwright` en tres navegadores. En el Mac, `npm run test:e2e`.
- Red simulada con `page.route`, respondiendo con envelope. Sin cabeceras CORS: la SPA llama a su
  mismo origen, y `page.route` resuelve el CORS por su cuenta (por eso un test de respuesta no
  detecta un cliente que apunte a otro origen; lo cubre `e2e/api-origin.spec.ts`).
- Unit y componente: **Vitest** + `@vue/test-utils` en `happy-dom` (`869eqxm8z`), versiones fijadas
  sin `^`. `npm run test:unit` (`test:unit:watch` en desarrollo); corre en «Frontend CI / lint-build».
  - Convención: cada módulo guarda sus tests en `__tests__/` a su lado, como `*.spec.ts`
    (`src/lib/api/__tests__/client.spec.ts`). `vitest.config.ts` hereda la config de Vite (alias
    incluidos) y `tsconfig.vitest.json` los tipa: `npm run build` falla si un test no compila.
  - La lógica nueva de stores, interceptores, composables, esquemas Zod y utilidades lleva test
    unitario. La red se sustituye con el adaptador de Axios (`apiClient.defaults.adapter`), no con
    mocks del módulo: así se prueba el cliente real con sus interceptores.
  - `window.location` se sustituye con `vi.spyOn(window, 'location', 'get')`; `localStorage` y
    Pinia (`setActivePinia(createPinia())`) se reinician en cada test.
- Un spec que inicia sesión con el token falso y no simula todas sus llamadas registra primero una
  ruta de reserva (`**/api/v1/**` → vacío) y después las suyas, que mandan. Sin ella, si la API real está en marcha
  en 5555, una llamada no simulada recibe 401 y cierra la sesión (pasó con `agenda.spec.ts`).
- Accesibilidad con avisos visibles: los «focus proxies» de `ToastViewport` (Reka UI, patrón de
  Radix) son `aria-hidden` y enfocables a propósito; axe los marca con `aria-hidden-focus`. En una
  comprobación con avisos en pantalla se desactiva solo esa regla, nunca en general.
- Foco por teclado en WebKit: con Tab, Safari en macOS no llega a los botones y Alt+Tab se salta el
  bucle de foco de Reka (ignora Tab con modificadores). El foco atrapado se prueba en Chromium y Firefox.
- Accesibilidad: el test no certifica el contraste AA (excepción consciente, deuda `869f0v6vm`);
  quedan tokens light por repasar (`869f0w7r2`).

## Evidencia mínima al cerrar una tarea de frontend

- `npm run lint -- --max-warnings 0`, `npm run test:unit` y `npm run build` sin errores.
- E2E afectados en verde, y los de accesibilidad si cambia la interfaz.
- Comportamiento comprobado en el navegador, contra la API real cuando la haya.
