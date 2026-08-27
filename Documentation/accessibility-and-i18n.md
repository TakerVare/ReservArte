# RESERVARTE — Accesibilidad e internacionalización

**Documento:** WCAG 2.1 AA, vue-i18n v9 y convenciones de producto  
**Versión:** 1.0  
**Fecha:** mayo 2026  
**Proyecto:** ReservArte — Sistema multi-tenant de gestión para centros de diseño de cejas  
**Referencias:** volumen 1 (**§2.2**, **§4.1.2**), volumen 3 (**§10.2**, **§10.3**, **§12.2**), [`Análisis de pantallas y estructura.md`](Análisis%20de%20pantallas%20y%20estructura.md), [`Documentation/Project-Init/Scripts de instalación.md`](Project-Init/Scripts%20de%20instalación.md)

---

## Índice

### Bloque A — Accesibilidad (WCAG 2.1 AA)

1. [Marco legal y alcance](#1-marco-legal-y-alcance)
2. [Cuatro principios WCAG aplicados al stack](#2-cuatro-principios-wcag-aplicados-al-stack)
3. [Reka UI y responsabilidad en `src/components/ui/`](#3-reka-ui-y-responsabilidad-en-srccomponentsui)
4. [Requisitos mínimos por tipo de componente](#4-requisitos-mínimos-por-tipo-de-componente)
5. [Contraste de colores (tokens `globals.css`)](#5-contraste-de-colores-tokens-globalscss)
6. [Herramientas: @axe-core/playwright, axe DevTools](#6-herramientas-axe-coreplaywright-axe-devtools)
7. [Ejemplos Vue: `button.vue` correcto e incorrecto](#7-ejemplos-vue-buttonvue-correcto-e-incorrecto)
8. [Ejemplos Vue: `dialog.vue` e `input.vue`](#8-ejemplos-vue-dialogvue-e-inputvue)

### Bloque B — Internacionalización (vue-i18n v9)

9. [Decisión de librería e instalación](#9-decisión-de-librería-e-instalación)
10. [Estructura de ficheros bajo `src/`](#10-estructura-de-ficheros-bajo-src)
11. [Idiomas por fase](#11-idiomas-por-fase)
12. [Convención de claves `modulo.componente.elemento`](#12-convención-de-claves-modulocomponenteelemento)
13. [Fechas, horas y moneda (España)](#13-fechas-horas-y-moneda-españa)
14. [Mensajes de error Redsys en i18n](#14-mensajes-de-error-redsys-en-i18n)
15. [Composable de ejemplo (`usePayments.ts`)](#15-composable-de-ejemplo-usepaymentsts)

---

## Bloque A — Accesibilidad (WCAG 2.1 AA)

### 1. Marco legal y alcance

El **Real Decreto 1112/2018** transpone la normativa europea sobre **accesibilidad de los sitios web y aplicaciones para dispositivos móviles del sector público**. ReservArte es un **producto de software privado** (SaaS multi-tenant), por lo que **no queda automáticamente sujeto** al mismo régimen que un portal de administración pública.

**Por qué aplica de facto al proyecto:**

- **Riesgo legal y de contratación:** clientes del sector (centros, cadenas, futuros integradores) pueden exigir **accesibilidad** en licitaciones, anexos RGPD o contratos tipo; alinear el producto con **WCAG 2.1 nivel AA** reduce exclusión de usuarios y reclamaciones por barreras digitales en relaciones B2B.
- **Sector y usuarios:** la aplicación la usan **personal del centro** y **clientes finales** (reservas, pagos); una UI operable con teclado y lectores de pantalla evita pérdida de negocio y mejora la tasa de finalización de flujos críticos.
- **Diferenciación:** el cumplimiento demostrable (tests, informes axe) es argumento comercial frente a competidores sin criterios claros.

**Objetivo técnico del proyecto:** WCAG **2.1** nivel **AA** como referencia de diseño e implementación (véase volumen 1 **§2.2**).

---

### 2. Cuatro principios WCAG aplicados al stack

| Principio | En ReservArte (Vue 3 + Vite + Tailwind + Reka UI) |
|-----------|---------------------------------------------------|
| **Perceptible** | Textos alternativos en iconos decorativos vs funcionales; no depender solo del color (estados error/éxito con texto); contraste revisado en tokens (§5). |
| **Operable** | Navegación teclado en `dialog.vue`, menús, calendario; foco visible (`ring` Tailwind); evitar trampas de foco; timeouts de sesión con aviso si aplica. |
| **Comprensible** | Etiquetas de formulario explícitas (`label.vue` + `input.vue`); mensajes de error comprensibles; idioma de página `lang` en `index.html` (ya `es` en el script de instalación). |
| **Robusto** | HTML semántico en layouts; componentes Reka UI alineados con patrones ARIA; pruebas automatizadas con **axe** en CI (§6). |

---

### 3. Reka UI y responsabilidad en `src/components/ui/`

Según la documentación de **Reka UI** ([Accessibility | Reka UI](https://reka-ui.com/docs/overview/accessibility)), la librería prioriza **accesibilidad**, roles **WAI-ARIA**, **teclado** y **gestión de foco** en los primitivos.

**Qué cubre Reka UI:** semántica y comportamiento de patrones complejos (diálogo, menú, etc.) según buenas prácticas WAI-ARIA.

**Qué sigue siendo responsabilidad del equipo en `src/components/ui/`** (nombres en `Análisis de pantallas y estructura.md`):

- No romper la semántica al envolver primitivos (p. ej. eliminar `role` o interceptar teclas sin reenvío).
- Garantizar **etiquetas accesibles** (`aria-label` solo cuando el texto visible no basta).
- Combinar utilidades Tailwind (`focus-visible:ring-*`) sin ocultar el foco.
- Probar **flujos reales** (formulario + modal + tabla), no solo el primitivo aislado.

**Importante:** la documentación de Reka UI **no sustituye** una auditoría WCAG 2.1 AA del producto completo.

---

### 4. Requisitos mínimos por tipo de componente

Estructura alineada con carpetas y nombres del análisis (`components/ui/`, `forms/`, `features/appointments/`, etc.).

| Tipo | Foco y teclado | ARIA / anuncios | Notas |
|------|----------------|-----------------|-------|
| **Formularios** (`FormField.vue`, `input.vue`, `label.vue`) | Orden de tab lógico; `focus` en primer error tras validación | `for`/`id` en label e input; `aria-describedby` al mensaje de error; `aria-invalid="true"` si falla | VeeValidate + Zod: enlazar mensajes al DOM para lectores |
| **Modales** (`dialog.vue`, `ConfirmDialog.vue`) | `focus trap` dentro del diálogo; **Escape** cierra; foco vuelve al disparador | `role="dialog"` / composición Reka; `aria-modal="true"`; título con `id` y `aria-labelledby` | No encadenar modales sin gestionar foco |
| **Calendario de citas** (FullCalendar + vistas en `appointments`) | Atajos documentados; foco en celda y en eventos | Anunciar cambio de vista si es crítico (`aria-live="polite"` en mensaje de estado) | Comprobar contraste de eventos y “hoy” |
| **Tablas** (`table.vue`, listas en empleados/clientes) | Navegación fila/columna; encabezados | `<th scope="col">`; datos complejos: patrón tabla vs grid según caso | Paginación accesible (`Pagination.vue`) |
| **Botones de acción** (`button.vue`) | `type="button"` en SPA salvo submit explícito | Nombre accesible por texto o `aria-label` | Icon-only: obligatorio `aria-label` |
| **Estados de carga** (`LoadingSpinner.vue`) | No robar foco salvo overlay modal | `aria-busy="true"` en contenedor; `role="status"` o texto “Cargando…” | Evitar spinners infinitos sin mensaje |

---

### 5. Contraste de colores (tokens `globals.css`)

Valores **HSL** tal como los genera el **Paso 5** de [`Documentation/Project-Init/Scripts de instalación.md`](Project-Init/Scripts%20de%20instalación.md) en **`:root`** (modo claro). **No se certifica cumplimiento AA por inspección:** cada par debe medirse con **WebAIM Contrast Checker** o **axe DevTools** antes del **primer deploy a staging**.

Leyenda de estado:

- **✅ Conforme probable** — contraste aparentemente holgado; aun así **medir** antes de release.
- **⚠️ Requiere medición** — plausible pero no obvio; **medición obligatoria**.
- **❌ Riesgo de incumplimiento** — combinación típicamente problemática para texto normal; ajustar token o peso/tamaño de fuente.

| Par de uso (fondo → texto) | Valores HSL actuales (`:root`) | Evaluación visual orientativa | Estado |
|----------------------------|--------------------------------|-------------------------------|--------|
| `--background` → `--foreground` | `0 0% 100%` → `222.2 84% 4.9%` | Texto casi negro sobre blanco | ✅ Conforme probable |
| `--primary` → `--primary-foreground` | `262.1 83.3% 57.8%` → `210 40% 98%` | Texto muy claro sobre púrpura saturado | ⚠️ Requiere medición |
| `--muted` → `--muted-foreground` | `210 40% 96.1%` → `215.4 16.3% 46.9%` | Gris medio sobre gris muy claro | ⚠️ Requiere medición |
| `--background` → `--muted-foreground` | `0 0% 100%` → `215.4 16.3% 46.9%` | Texto secundario sobre blanco | ⚠️ Requiere medición |
| `--destructive` → `--destructive-foreground` | `0 84.2% 60.2%` → `210 40% 98%` | Blanco sobre rojo | ✅ Conforme probable |

**Nota (modo `.dark`):** el mismo script define valores distintos para `.dark` (p. ej. `--destructive: 0 62.8% 30.6%`). Los pares deben **volver a evaluarse** al activar tema oscuro.

**Verificación definitiva:** ejecutar **axe DevTools** en páginas reales (`LoginPage.vue`, calendario, tablas) y **WebAIM Contrast Checker** sobre los hex/RGB derivados de los tokens antes del primer despliegue a **staging**.

---

### 6. Herramientas: `@axe-core/playwright`, axe DevTools

El canal de accesibilidad **automatizada** del frontend es **Playwright + `@axe-core/playwright`**, no Vitest ni `vitest-axe` (plan previo **abandonado**). Los checks (WCAG 2.1 AA / RD 1112/2018) se ejecutan en **navegador real** (contraste y CSS computado), con config en `reservarte-web/playwright.config.ts` y tests en `reservarte-web/e2e/`. Tres navegadores: Chromium, Firefox y WebKit. Scripts: `test:e2e`, `test:e2e:ui`, `test:e2e:report`. Detalle de infra: [`reservarte-testing-strategy.md`](reservarte-testing-strategy.md) §5.1.

| Herramienta | Uso | Cuándo |
|-------------|-----|--------|
| **axe DevTools** (extensión navegador) | Inspección manual, flujos completos, informes antes de release | Cada feature de UI sensible; obligatorio antes de staging (§5) |
| **`@axe-core/playwright`** | Tests de accesibilidad en **Playwright** (DOM real, tres navegadores) | Specs en `reservarte-web/e2e/`; el test axe concreto de `LoginPage` (RA-869d7fbpp) **sigue pendiente** |

**Instalación:** el paquete ya está en `devDependencies` de `reservarte-web`. Tras `npm install`, descargar binarios de navegador con `npx playwright install` (no viajan con el repo). No instalar `vitest-axe` para este canal.

**Humo actual:** hay un test E2E de humo (carga de login en navegador). **No** equivale al test axe de `LoginPage`.

---

### 7. Ejemplos Vue: `button.vue` correcto e incorrecto

Nombres de ficheros según `Análisis de pantallas y estructura.md` (`src/components/ui/button.vue`).

**Incorrecto (anti-patrón):** acción crítica con `div` clickeable, sin teclado ni rol.

```vue
<script setup lang="ts">
// Anti-ejemplo: no reutilizar en producción
const emit = defineEmits<{ click: [] }>()
</script>

<template>
  <div class="cursor-pointer rounded bg-primary px-4 py-2 text-primary-foreground" @click="emit('click')">
    Guardar
  </div>
</template>
```

**Correcto (patrón alineado):** elemento nativo **`<button>`**, tipo explícito, foco visible con utilidades del proyecto.

```vue
<script setup lang="ts">
// Ejemplo orientativo: envolver luego el primitivo Reka UI si el proyecto unifica API
const props = withDefaults(
  defineProps<{
    variant?: 'default' | 'destructive'
  }>(),
  { variant: 'default' }
)
const emit = defineEmits<{ click: [MouseEvent] }>()
</script>

<template>
  <button
    type="button"
    class="inline-flex items-center justify-center rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
    :class="variant === 'destructive' ? 'bg-destructive text-destructive-foreground' : 'bg-primary text-primary-foreground'"
    @click="(e) => emit('click', e)"
  >
    <slot />
  </button>
</template>
```

---

### 8. Ejemplos Vue: `dialog.vue` e `input.vue`

**`dialog.vue` — ideas clave:** usar la composición **Reka UI** (`DialogRoot`, `DialogContent`, etc. según documentación actual); título visible; cerrar con Escape; devolver foco al elemento que abrió el modal.

```vue
<script setup lang="ts">
// Fragmento ilustrativo: sustituir por imports reales desde 'reka-ui' cuando el wrapper exista en el repo
import { ref, watch } from 'vue'

const open = defineModel<boolean>({ default: false })
const titleId = 'dialog-title'

watch(open, (v) => {
  if (import.meta.env.DEV && v) console.debug('[a11y] dialog abierto')
})
</script>

<template>
  <!-- En implementación real: envolver con primitivos Reka UI y aria-labelledby=titleId -->
  <div v-if="open" role="dialog" aria-modal="true" :aria-labelledby="titleId" class="fixed inset-0 z-50">
    <div class="fixed inset-0 bg-background/80" @click="open = false" />
    <div class="fixed left-1/2 top-1/2 w-full max-w-lg -translate-x-1/2 -translate-y-1/2 rounded-lg border bg-card p-6 shadow-lg">
      <h2 :id="titleId" class="text-lg font-semibold">
        <slot name="title" />
      </h2>
      <div class="mt-4">
        <slot />
      </div>
    </div>
  </div>
</template>
```

**`input.vue` — ideas clave:** asociar **siempre** `label` + `id`; errores con `aria-describedby`.

```vue
<script setup lang="ts">
defineProps<{
  id: string
  label: string
  errorId?: string
  invalid?: boolean
}>()
const model = defineModel<string>({ required: true })
</script>

<template>
  <div class="space-y-1">
    <label :for="id" class="text-sm font-medium leading-none">{{ label }}</label>
    <input
      :id="id"
      v-model="model"
      class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      :aria-invalid="invalid ? 'true' : undefined"
      :aria-describedby="errorId"
    />
  </div>
</template>
```

---

## Bloque B — Internacionalización (vue-i18n v9)

### 9. Decisión de librería e instalación

**Librería:** **vue-i18n v9** con `legacy: false` (API de Composition / `useI18n`).

**Justificación breve:** integración oficial con Vue 3, tipado mejorable con el esquema de mensajes, ecosistema maduro. Alternativas como **@nuxtjs/i18n** no aplican a esta **SPA Vite** sin Nuxt; soluciones mínimas caseras no aportan pluralización, fallback de locale ni lazy-loading futuro sin reimplementar.

**Comando (mismo bloque que Paso 2 de [`Documentation/Project-Init/Scripts de instalación.md`](Project-Init/Scripts%20de%20instalación.md)):**

```bash
npm install vue-i18n@9
```

El **Paso 5** del mismo script genera `src/i18n/index.ts`, `src/locales/es/index.ts` y registra `i18n` en `main.ts`.

---

### 10. Estructura de ficheros bajo `src/`

Coherente con el análisis y el script:

```
src/
├── i18n/
│   └── index.ts          # createI18n, locale 'es', fallback 'es'
├── locales/
│   └── es/
│       └── index.ts      # Mensajes MVP (se parten por módulo en evolución)
├── lib/utils/
│   ├── date.utils.ts     # Generado en Paso 5 del script
│   └── currency.utils.ts # Generado en Paso 5 del script
```

En **Fase 4** (volumen 3 **§10.2**) se añaden `src/locales/en/`, `fr/`, `pt/` (o la convención acordada) y la carga/detección de locale.

---

### 11. Idiomas por fase

| Fase | Idiomas |
|------|---------|
| **MVP / Sprint 1** | **Español (`es`)** único locale activo; arquitectura i18n lista |
| **Fase 4** | **Inglés, francés, portugués** (ficheros de mensajes + detección automática) |

---

### 12. Convención de claves `modulo.componente.elemento`

Usar **punto** como separador, alineado con carpetas **`features/`** y páginas del análisis:

- `auth.login.title` — `LoginPage.vue` / flujo login  
- `appointments.calendar.today` — `CalendarPage.vue`  
- `customers.list.searchPlaceholder` — listado clientes  
- `employees.form.roleLabel` — formulario empleados  
- `services.edit.duration` — edición servicios  
- `payments.redsys.declinedGeneric` — mensaje genérico rechazo Redsys  
- `common.errorUnexpected` — error genérico (stub `src/locales/es/index.ts`, Paso 5 del script)
- `settings.organization.title` — área configuración (`settings/` en páginas; contenido “organización” bajo el mismo módulo de traducción si se prefiere `organization.*`)

**Regla:** el primer segmento es el **módulo de negocio** (nombre de carpeta en `features/` o `pages/` en minúsculas y plural/singular como en el repo: `appointments`, `auth`, `customers`, `employees`, `services`, `payments`, `settings`).

---

### 13. Fechas, horas y moneda (España)

Las funciones **`formatDateSpain`** y **`formatTimeSpain`** en **`src/lib/utils/date.utils.ts`**, y **`formatCurrencyEur`** en **`src/lib/utils/currency.utils.ts`**, están **configuradas en el Paso 5** de [`Documentation/Project-Init/Scripts de instalación.md`](Project-Init/Scripts%20de%20instalación.md):

- Fecha corta: **dd/MM/yyyy** (`date-fns` + locale `es`).
- Hora: **24 h** (`HH:mm`) coherente con citas.
- Moneda: **`Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' })`** (coma decimal, símbolo euro en convención local).

**i18n:** las cadenas literales van en vue-i18n; los **números y fechas** se formatean con utilidades anteriores o con **`Intl`** según `locale` cuando existan `en`/`fr`/`pt` en Fase 4.

---

### 14. Mensajes de error Redsys en i18n

El API devuelve códigos de negocio (p. ej. `PAY_REDSYS_DECLINED`, volumen 1 **§5.1.2**). La UI **no** debe mostrar códigos técnicos al usuario final.

- Añadir claves bajo **`payments.redsys.*`** (genérico + variantes opcionales mapeadas desde `error.code` o desde códigos Redsys no sensibles).
- Mantener textos legales/PCI: **sin PAN, sin CVC**.

El stub del script ya incluye **`payments.redsys.declinedGeneric`** en `src/locales/es/index.ts`.

---

### 15. Composable de ejemplo (`usePayments.ts`)

Nombre alineado con `src/features/payments/composables/usePayments.ts` en el análisis. Fragmento ilustrativo:

```typescript
// src/features/payments/composables/usePayments.ts — patrón recomendado (completar con lógica real)
import { useI18n } from 'vue-i18n'

export function useRedsysUserMessage() {
  const { t } = useI18n()

  return function mapApiErrorToMessage(errorCode: string | undefined): string {
    if (errorCode === 'PAY_REDSYS_DECLINED') {
      return t('payments.redsys.declinedGeneric')
    }
    return t('common.errorUnexpected')
  }
}
```

Cuando existan más claves (`payments.redsys.timeout`, etc.), ampliar el `switch` o un mapa centralizado.

---

## Referencias cruzadas

- **Volumen 3 §10.2 / §10.3:** i18n en Sprint 1; traducciones adicionales y detección en Fase 4.  
- **`Documentation/Project-Init/Scripts de instalación.md`:** Pasos 2–5 (dependencias, carpetas, stubs).  
- **`reservarte-testing-strategy.md`:** Vitest y calidad de tests frontend.

---

**Fin del documento de accesibilidad e internacionalización**
