## Descripción

<!-- Explica brevemente QUÉ hace este PR y POR QUÉ es necesario.
     Una o dos frases es suficiente; el detalle va en los apartados de abajo. -->

---

## Tipo de cambio

<!-- Marca con una X el tipo que corresponda. Debe coincidir con el prefijo
     del commit principal según Conventional Commits. -->

| Tipo | Conventional Commit | Descripción |
|------|-------------------|-------------|
| ☐ | `feat:` | Nueva funcionalidad |
| ☐ | `fix:` | Corrección de bug |
| ☐ | `refactor:` | Refactorización sin cambio de comportamiento |
| ☐ | `perf:` | Mejora de rendimiento |
| ☐ | `test:` | Añadir o corregir tests |
| ☐ | `docs:` | Documentación únicamente |
| ☐ | `chore:` | Tareas de mantenimiento (deps, CI, config) |
| ☐ | `style:` | Formato, espacios, punto y coma (sin lógica) |

> ⚠️ **Breaking change:** Si este PR rompe compatibilidad con versiones anteriores,
> añade `!` al tipo (`feat!:`) y describe el impacto en la sección de notas finales.

---

## Rama origen → destino (Git Flow)

<!-- Verifica que la rama y el destino sean correctos. -->

| | Rama |
|---|---|
| **Origen** | `feature/XXX` / `bugfix/XXX` / `hotfix/XXX` / `release/X.Y.Z` |
| **Destino** | `develop` / `main` |

---

## ClickUp

<!-- Enlaza la tarea correspondiente. Sustituye XXX por el ID real. -->

- Tarea: [RA-XXX](https://app.clickup.com/t/XXX)
- Space: `Backend (.NET)` / `Frontend (Vue 3)` / `Mobile` / `Infrastructure` / `Documentation`
- Lista: `Backend` / `Frontend` / `Backlog` / `AWS / Docker / CI-CD` / `Technical Specs`

---

## Módulo afectado

<!-- Marca todos los que apliquen. -->

- ☐ Auth / 2FA / OAuth
- ☐ Empleados
- ☐ Clientes / Métodos de pago
- ☐ Servicios / Paquetes
- ☐ Citas / Calendario / Lista de espera
- ☐ Pagos / Redsys InSite
- ☐ Recordatorios / Email / WhatsApp
- ☐ Fotografías / Cloudinary
- ☐ Configuración / Organización
- ☐ Multi-tenant / Middleware
- ☐ Infraestructura / CI-CD / AWS
- ☐ Accesibilidad / i18n
- ☐ Reportes (Futuro)

---

## Cambios principales

<!--
Lista los cambios técnicos relevantes. Sé concreto.
Ejemplos:
- Añadido endpoint `POST /api/v1/payments/redsys/insite/complete`
- Nuevo componente `PaymentForm.vue` con iframes de Redsys InSite
- Migración EF Core: tabla `customer_payment_methods`
-->

-
-
-

---

## Cómo probar

<!--
Pasos para que el revisor pueda verificar el comportamiento manualmente.
Si hay variables de entorno necesarias, indícalas (sin valores secretos).
-->

1.
2.
3.

**Variables de entorno necesarias (si aplica):**

```
VITE_XXX=
```

**Datos de prueba / tarjetas Redsys test (si aplica):** usa los escenarios de
[`Documentation/redsys-development-guide.md` §2](https://github.com/TakerVare/ReservArte/blob/develop/Documentation/redsys-development-guide.md#2-tarjetas-de-prueba-y-escenarios),
que es la fuente única, e indica aquí qué números de escenario has probado. No copies la tabla.

- Escenarios probados: #

---

## Tests

<!-- Marca lo que aplique y añade contexto si es necesario. -->

- ☐ Tests unitarios añadidos / actualizados
- ☐ Tests de integración añadidos / actualizados
- ☐ E2E de Playwright añadidos / actualizados o reejecutados (`npm run test:e2e`)
- ☐ Probado manualmente en entorno `dev`
- ☐ Probado manualmente en entorno `staging` — N/A hasta que exista staging
- ☐ No requiere tests (documentación / estilo / configuración)

**Batería:** unit `___/___` (antes `___`) · E2E `___/___` (o «no reejecutados» y por qué)

**Evidencia:** <!-- salida de comandos, SQL, respuestas HTTP reales por rol, aislamiento Org A ≠ Org B
si aplica. Lo que no se ha probado no se da por hecho. -->

<!-- La cobertura no se mide hasta que el CI la publique. -->

---

## Base de datos

<!-- Marca lo que aplique. -->

- ☐ Este PR incluye una migración de EF Core
- ☐ La migración es reversible (`Down()` implementado)
- ☐ Regenerado `data/schema/create_ReservArteDB.sql` con `bash data/schema/regenerate-create.sh` (obligatorio si hay migración; no hay CI que lo detecte)
- ☐ Revisado `data/demo/seed_demo_ReservArteDB.sql` (si la migración toca una tabla que siembra o cambia `DevSeeder`)
- ☐ Verificado creando una base de prueba con los scripts de `data/` y arrancando la API contra ella (nunca sobre la base de desarrollo `reservarte`)
- ☐ Requiere seed data adicional
- ☐ No hay cambios en base de datos

> ⚠️ Si hay migración, confirma que se ha ejecutado en `staging` antes del merge a `main`
> (N/A hasta que exista staging).

---

## Seguridad y RGPD

<!-- Responde solo si el PR toca datos de usuarios, pagos o autenticación. -->

- ☐ No se almacenan PANs ni datos sensibles de tarjeta (solo tokens Redsys)
- ☐ Los nuevos campos con PII tienen `OrganizationId` (aislamiento multi-tenant)
- ☐ Los nuevos endpoints están protegidos con `[Authorize]` y rol correcto
- ☐ No aplica (el PR no toca datos personales ni pagos)

---

## Definition of Done

<!-- Todos los ítems deben estar marcados (o justificados como N/A) antes de pedir la revisión. -->

- ☐ El código compila sin errores ni warnings nuevos
- ☐ Los tests existentes siguen pasando (`dotnet test` / `npm run test:e2e`)
- ☐ El linter no reporta avisos (`dotnet format --verify-no-changes` código 0 / `npm run lint`). Línea base de `format` = CERO: un aviso lo introduce este PR. No encadenar con `| tail`.
- ☐ CI en verde (N/A hasta que exista el pipeline)
- ☐ La rama está actualizada con `develop` (o `main` si es hotfix)
- ☐ Dependencia nueva o actualizada: versión fijada y licencia revisada para uso comercial (o no hay)
- ☐ La tarea de ClickUp está en `in review` (en Infra sigue en `in progress` hasta el merge)
- ☐ Revisión propia con evidencia + revisión de Claude Code (proyecto de un solo desarrollador: no hay reviewer humano)
- ☐ `.claude/contexto/estado.md` de la rama actualizado («PR #N abierto»)
- ☐ Los commits siguen Conventional Commits
- ☐ No hay `console.log`, `TODO` urgentes ni credenciales en el código

---

## Capturas de pantalla (si aplica)

<!-- Para cambios de UI, añade antes/después. Arrastra las imágenes aquí. -->

| Antes | Después |
|-------|---------|
| | |

---

## Notas para el revisor

<!-- Contexto adicional, decisiones de diseño, deuda técnica generada,
     o cualquier punto que quieras destacar al revisor. -->

---

## Breaking changes (si aplica)

<!--
Si marcaste `feat!:` o `fix!:` arriba, describe aquí:
- Qué se rompe
- Qué deben hacer los consumidores de la API / otros módulos
- Si hay cambio en el contrato de respuesta de la API
-->