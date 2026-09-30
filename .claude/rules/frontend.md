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
  sobre Reka UI.
- `src/features/<área>/api/*.api.ts` y `src/features/<área>/types/`: llamadas a la API por feature,
  que desenvuelven el envelope y traducen los errores (precedente: `features/auth/api/auth.api.ts`).
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
- Navegación (`869ep9p36`): BottomNav global en `App.vue` (Inicio, Contacto y Cuenta) y **pantallas
  planas**, sin layouts: no hay Sidebar, Header, `DashboardLayout` ni `AuthLayout`. La gestión se abre
  desde la pantalla de Usuario (`/cuenta`, `pages/account/AccountPage.vue`, componente `ui/menu`):
  «Área de administración» para Admin, Manager y Employee, y «Área de usuario» para todos. Una
  pantalla nueva es una ruta plana con `meta.requiresAuth` y su entrada en ese menú, no un menú propio.
  - El rol sale de `authStore.user.rol`: tras recargar, el usuario es `null` hasta `869f6r6hc` y el
    área de administración no se muestra (se vuelve a ver al iniciar sesión).
  - Cabeceras: `Banner` con el logo. Fondo de las cabeceras de sección del menú: token `highlight`.
- Diseño en Figma: fichero `Trabajo` (`JSScv098x1yPk40ec6xRrv`), pantallas de iPhone 14/15 Pro (393 px):
  Usuario (admin) `387:56701`, Contacto `387:56672`. El kit es Material 3: traduce sus colores a
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
- Accesibilidad: el test no certifica el contraste AA (excepción consciente, deuda `869f0v6vm`);
  quedan tokens light por repasar (`869f0w7r2`).

## Evidencia mínima al cerrar una tarea de frontend

- `npm run lint -- --max-warnings 0`, `npm run test:unit` y `npm run build` sin errores.
- E2E afectados en verde, y los de accesibilidad si cambia la interfaz.
- Comportamiento comprobado en el navegador, contra la API real cuando la haya.
