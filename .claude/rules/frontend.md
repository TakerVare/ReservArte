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
- Formularios con VeeValidate + Zod; textos con vue-i18n (`es`), sin literales en las plantillas;
  fechas e importes con `date.utils.ts` y `currency.utils.ts`.
- Sin `enum` (`erasableSyntaxOnly`): uniones de literales u objetos `as const`. Los estados de cita
  son los 8 del backend, en snake_case.
- Navegación: BottomNav global. No crees Sidebar ni pantallas nuevas sobre `DashboardLayout` hasta
  la reconciliación de layouts (`869ep9p36`), que va antes de la primera pantalla de gestión.
- URL de la API: no añadas fallbacks a localhost; el mecanismo único llega con `869f6r69b`.
- Gráficas: nada de recharts (es de React); la librería Vue se decide en `869f6r6nx`.

## Tests

- E2E: Playwright + `@axe-core/playwright` en tres navegadores (57/57). En el Mac, `npm run test:e2e`.
- Red simulada con `page.route`, respondiendo con envelope (y con cabeceras CORS mientras la API
  esté en otro origen; se revisan en `869f6r69b`).
- Unit: Vitest pendiente (`869eqxm8z`). En cuanto exista, la lógica de stores, interceptor y
  utilidades lleva test unitario.
- Accesibilidad: el test no certifica el contraste AA (excepción consciente, deuda `869f0v6vm`);
  quedan tokens light por repasar (`869f0w7r2`).

## Evidencia mínima al cerrar una tarea de frontend

- `npm run lint` y `npm run build` sin errores.
- E2E afectados en verde, y los de accesibilidad si cambia la interfaz.
- Comportamiento comprobado en el navegador, contra la API real cuando la haya.
