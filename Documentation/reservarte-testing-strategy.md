# RESERVARTE — Estrategia de testing

**Documento:** Estrategia de pruebas automatizadas (backend, frontend y E2E)  
**Versión:** 1.2  
**Fecha:** 29 de septiembre de 2026  
**Proyecto:** ReservArte — Sistema multi-tenant de gestión para centros de diseño de cejas  
**Ubicación:** España  
**Stack de referencia:** .NET 10, Vue 3 + Vite, AWS, Redsys

---

## Índice

1. [Filosofía y objetivos](#1-filosofía-y-objetivos)
2. [Pirámide de tests](#2-pirámide-de-tests)
3. [Capa unitaria](#3-capa-unitaria)
4. [Capa de integración](#4-capa-de-integración)
5. [Capa E2E](#5-capa-e2e) (infra frontend: [§5.1](#51-infraestructura-e2e-del-frontend-ra-869eqxdk3))
6. [Simulación de Redsys en tests](#6-simulación-de-redsys-en-tests)
7. [Qué no se testea y por qué](#7-qué-no-se-testea-y-por-qué)
8. [Cobertura mínima por fase del proyecto](#8-cobertura-mínima-por-fase-del-proyecto)
9. [Integración con CI/CD](#9-integración-con-cicd)
10. [Resumen de herramientas y decisiones](#10-resumen-de-herramientas-y-decisiones)

---

## 1. Filosofía y objetivos

- **Confianza sobre cobertura numérica:** un test que falla con un mensaje claro y reproduce un fallo de negocio vale más que un porcentaje alto de líneas cubiertas con aserciones débiles. La cobertura es **indicador**, no objetivo en sí.
- **Tests rápidos como red de seguridad:** la mayor parte de la ejecución diaria debe ser unitaria (milisegundos por test). Integración y E2E se reservan para contratos reales (BD, HTTP, navegador) sin bloquear el ciclo corto de feedback en cada commit.
- **El test como documentación:** los nombres de tests describen reglas de negocio (p. ej. «no captura penalización si quedan más horas que el umbral de `OrganizationSettings`»). Los ejemplos viven junto al código o en escenarios E2E nombrados por flujo de usuario.
- **Alineación multi-tenant:** cualquier prueba que toque datos de negocio debe dejar explícito el **contexto de organización** (cabecera en dev, subdominio en E2E staging, etc.), coherente con el volumen 1 (**§5.1.3**) y el middleware de tenant del API.

---

## 2. Pirámide de tests

La pirámide tiene **tres capas** con volumen decreciente hacia arriba y coste creciente:

| Capa | Propósito | Velocidad | Alcance típico |
|------|-----------|-----------|----------------|
| **Unitarios** | Lógica pura, validación, cripto/firma sin I/O | Muy alta | Servicios de aplicación con dependencias sustituidas, validadores, helpers |
| **Integración** | Contrato con el motor real, pipeline HTTP completo, EF Core | Media | `WebApplicationFactory` contra PostgreSQL 18 (Testcontainers). Lo que SQLite no reproduce |
| **E2E** | Flujos críticos de usuario en navegador real | Baja | Pocos escenarios, alta confianza en regresiones de producto |

**Regla práctica:** si un caso puede resolverse con un unitario sin mentir sobre el sistema, no subirlo a integración; si integración basta (sin UI), no subirlo a E2E.

---

## 3. Capa unitaria

### 3.1 Backend

**Qué se testea**

- **Servicios de aplicación** (implementados en Infrastructure; contratos en Application): reglas de negocio con repositorios y servicios colaboradores sustituidos por **Moq**.
- **Validadores FluentValidation** (`ReservArte.Application/Validators`): reglas de entrada (fechas, rangos, obligatoriedad) sin levantar el API.
- **Helpers de firma HMAC / parámetros Redsys** (p. ej. en `ReservArte.Shared` o utilidades de infraestructura dedicadas): vectores conocidos — el orden de campos y el resultado de firma deben coincidir con la especificación Redsys.
- **`JwtTokenService`** (`ReservArte.Infrastructure/Services/JwtTokenService.cs`, volumen 2 **§9.2.1**): presencia de claims (`organization_id`, rol), expiración y validación con clave simétrica de prueba.

**Herramientas:** xUnit, Moq y AwesomeAssertions 9.6.0 (Apache-2.0), sobre .NET 10. El proyecto es `tests/ReservArte.UnitTests`. Los repositorios se prueban contra SQLite en memoria. SQLite no reproduce PostgreSQL: comparación de texto, `timestamptz` y el `Kind` de `DateTime`, ni los `CHECK`. Lo que dependa del motor va a integración ([ADR-031](adr/ADR-031-tests-integracion-postgres.md), §4), igual que el contrato HTTP (roles, envelope, status). El mapeo entidad → DTO lo genera Mapperly y lo cubre `MappingCharacterizationTests` (vol. 2 §9.5.1; [ADR-029](adr/ADR-029-awesomeassertions.md), [ADR-030](adr/ADR-030-mapeo-mapperly.md)).

**Servicios de aplicación — visión vs real.** El fragmento `AppointmentService.CancelAppointmentAsync` de vol. 2 §7.6 es **orientativo** (penalización + Redsys). El servicio real (`CancelAsync(int, CancelAppointmentRequest)` → `Result<AppointmentDto>`) se cubre con `AppointmentServiceTests` (Moq de `IAppointmentRepository`, `ICurrentOrganizationService`, `ICurrentUserService`, `TimeProvider`) y con `AppointmentStateMachineIntegrationTests` (SQLite real). La penalización económica (`OrganizationSettings` + `IRedsysPaymentService.CaptureAsync`) no existe: es **RA-869f6ae9h**.

**Ejemplo representativo (FluentValidation)**

```csharp
// ReservArte.Application/Validators/CreateAppointmentValidator.cs
using FluentValidation;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.AppointmentDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date);
        RuleFor(x => x.ServiceIds).NotEmpty();
    }
}

// tests/ReservArte.UnitTests/Validators/CreateAppointmentValidatorTests.cs
public class CreateAppointmentValidatorTests
{
    private readonly CreateAppointmentValidator _sut = new();

    [Fact]
    public void Debe_fallar_si_no_hay_servicios()
    {
        var request = new CreateAppointmentRequest
        {
            CustomerId = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            ServiceIds = Array.Empty<Guid>()
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppointmentRequest.ServiceIds));
    }
}
```

**Ejemplo representativo (firma HMAC Redsys — helper testeable)**

La lógica de `GenerateSignature` / `ValidateSignature` del volumen 2 (**§7.3**) debe residir en una clase **pública y pura** (p. ej. `RedsysSignatureHelper` en `ReservArte.Shared`) para poder fijar vectores de prueba sin HTTP ni secretos reales.

```csharp
// tests/ReservArte.UnitTests/Helpers/RedsysSignatureHelperTests.cs
public class RedsysSignatureHelperTests
{
    [Fact]
    public void ComputeSignature_mismo_merchantParameters_y_clave_produce_firma estable()
    {
        const string merchantParametersBase64 = "eyJ0ZXN0IjoxfQ=="; // ejemplo; usar cadena oficial de pruebas Redsys
        var secretKey = Convert.FromBase64String("Mk9m98IfTs7Zu9Yz9h26cL3o3Ks0HzfA=="); // clave de test inventada para el test

        var sig1 = RedsysSignatureHelper.ComputeMerchantSignatureHmacSha256(merchantParametersBase64, secretKey);
        var sig2 = RedsysSignatureHelper.ComputeMerchantSignatureHmacSha256(merchantParametersBase64, secretKey);

        sig1.Should().NotBeNullOrWhiteSpace().And.Be(sig2);
        RedsysSignatureHelper.ValidateMerchantSignatureHmacSha256(merchantParametersBase64, secretKey, sig1).Should().BeTrue();
    }
}
```

> **Nota:** Los nombres de entidades (`Appointment`, `AppointmentStatuses`, `OrganizationSettings`, `CustomerPaymentMethod`) y de servicios (`IRedsysPaymentService`, `RedsysPaymentService`) siguen el volumen 1 y el volumen 2. No existe el enum `AppointmentStatus` (singular): el dominio persiste constantes texto.

**Ejemplo representativo (JWT) — alineado con `tests/ReservArte.UnitTests/JwtTokenServiceTests.cs` (corrección 2026-08-21, post RA-869d7ezp3)**

```csharp
// tests/ReservArte.UnitTests/JwtTokenServiceTests.cs
[Fact]
public void GenerateAccessToken_incluye_el_claim_organization_id()
{
    // Construcción con IOptions<JwtOptions>, no ConfigurationBuilder
    var options = Options.Create(new JwtOptions
    {
        Issuer = "https://test.reservarte.local",
        Audience = "reservarte-test",
        SecretKey = "clave-de-prueba-para-tests-unitarios-jwt-0123456789", // literal de test, no User Secrets
        AccessTokenMinutes = 60,
        RefreshTokenDays = 30,
    });
    var sut = new JwtTokenService(options);
    var user = new User { Id = 42, Email = "empleada@morethanbrows.com", Rol = "Employee" };
    var orgId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    var token = sut.GenerateAccessToken(user, orgId);

    var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
    jwt.Claims.Should().Contain(c =>
        c.Type == "organization_id" && c.Value == orgId.ToString());
    // También se emiten: sub (= user.Id.ToString()), email, "role" corto, jti
}
```

### 3.2 Frontend

**Qué se testea**

- **Composables** con lógica no trivial (cálculo de slots, pasos del wizard de reserva pública, acumulación de errores de formulario).
- **Funciones puras** en `utils/` (formateo de moneda, construcción de payloads hacia el envelope de API).

**Herramientas:** **Vitest** + **Vue Test Utils** (y `@vue/test-utils` según versión del proyecto) para composables y utilidades. La accesibilidad **no** se prueba con `vitest-axe`: canal **Playwright + `@axe-core/playwright`** ([`accessibility-and-i18n.md`](accessibility-and-i18n.md) §6 y esta estrategia §5.1).

**Ejemplo representativo (utilidad o composable)**

```typescript
// tests/unit/useCancellationPenalty.spec.ts
import { describe, it, expect } from 'vitest'
import { computePenaltyPreview } from '@/composables/useCancellationPenalty'

describe('computePenaltyPreview', () => {
  it('aplica porcentaje cuando faltan menos horas que el umbral', () => {
    const preview = computePenaltyPreview({
      totalPrice: 80,
      hoursUntilStart: 2,
      cancellationHoursThreshold: 24,
      cancellationPenaltyPercentage: 25,
    })
    expect(preview.shouldPenalize).toBe(true)
    expect(preview.penaltyAmount).toBe(20)
  })
})
```

---

## 4. Capa de integración

Decisión: [ADR-031](adr/ADR-031-tests-integracion-postgres.md) (H-39), que sustituye al [ADR-016](adr/ADR-016-tests-integracion-testcontainers.md). Proyecto `tests/ReservArte.IntegrationTests`.

**Cómo corre**

- `WebApplicationFactory<Program>` en Development, contra PostgreSQL 18 (`postgres:18`) con Testcontainers.
- Una colección comparte un contenedor por ejecución. La fixture siembra un centro B.
- Cada test crea sus datos y no depende de recuentos globales: la base es compartida.
- La configuración de la fixture se impone a los User Secrets del equipo.
- Los tokens de rol se emiten con `IJwtTokenService`. El login admite 10 peticiones por hora, así que los tests no pasan por él.
- Las variantes usan `WithWebHostBuilder` para sustituir servicios o aislar el rate limiter.
- Docker es requisito, en los equipos y en el CI.

**Qué va aquí y qué en unitarios**

Aquí va lo que depende del motor: comparación de texto, `CHECK`, fechas, filtros y orden en SQL, y el aislamiento visto por HTTP. También el contrato HTTP: roles, envelope y status. En unitarios se queda lo que SQLite en memoria reproduce y lo que no toca el motor (§3.1).

---

## 5. Capa E2E

**Decisión:** **Playwright** (no Cypress).

| Criterio | Playwright |
|----------|------------|
| TypeScript nativo | Tipos y fixtures de primer nivel |
| Paralelismo | Workers y sharding en CI |
| Interceptación de red | `page.route` / `route.fulfill` para simular API o Redsys sin tocar backend |
| Accesibilidad en navegador | **`@axe-core/playwright`** sobre el DOM real (WCAG 2.1 AA; base legal en [`accessibility-and-i18n.md`](accessibility-and-i18n.md) §1, [ADR-025](adr/ADR-025-base-legal-accesibilidad.md)) |

### 5.1 Infraestructura E2E del frontend (RA-869eqxdk3)

El frontend **`reservarte-web`** usa **Playwright** (`@playwright/test`) y **`@axe-core/playwright`** para E2E y para checks de accesibilidad en navegador.

- **Navegadores:** Chromium, Firefox y WebKit (proyectos en la config).
- **Configuración:** `reservarte-web/playwright.config.ts`.
- **Tests:** `reservarte-web/e2e/`.
- **Scripts npm** (desde `reservarte-web/`): `test:e2e`, `test:e2e:ui`, `test:e2e:report`. En Mac, **`npx playwright test`** puede resolver otra instalación y fallar con «No tests found»; usar **`npm run test:e2e`** (57/57).

`webServer` de Playwright arranca o reutiliza el servidor de desarrollo del frontend. El **puerto del frontend debe estar libre** en la máquina (si otro proceso lo ocupa, los tests no arrancan). En equipos Windows donde **WAHA** usa ese puerto, hay que **parar WAHA** antes de ejecutar la suite E2E. Los binarios de navegador **no viajan con el repositorio**: tras `npm install`, cada equipo ejecuta `npx playwright install` (detalle en [`Documentation/Project-Init/Scripts de instalación.md`](Project-Init/Scripts%20de%20instalación.md)).

**axe-core** comprueba WCAG 2.1 AA en navegador. La base legal no es el RD 1112/2018 ([`accessibility-and-i18n.md`](accessibility-and-i18n.md) §1). Infra: RA-869eqxdk3. **LoginPage (RA-869d7fbpp):** spec shipped; **excluye** `color-contrast` (deuda RA-869f0v6vm). El resto de reglas AA de ese spec sí se cumple.

**Capa de producto (roadmap):** flujos críticos de negocio (cita+pago, cancelación, login social, wizard público) en **`reservarte-web/e2e/`**. El ejemplo de interceptación más abajo usa esa ubicación. El proyecto `tests/ReservArte.E2ETests` **no se usará**.

**Alcance deliberadamente reducido** — solo flujos críticos:

1. **Creación de cita con pago** (happy path o pago simulado vía red).
2. **Cancelación con penalización** (según reglas de `OrganizationSettings`).
3. **Login social** (o flujo acordado con mock del IdP en red si no hay sandbox estable en CI).
4. **Wizard de reserva pública** (multi-paso hasta confirmación).

**Ejemplo representativo (interceptación de red)**

```typescript
// reservarte-web/e2e/booking-with-payment.spec.ts
import { test, expect } from '@playwright/test'

test('reserva pública: confirma cita cuando el pago simulado devuelve éxito', async ({ page }) => {
  await page.route('**/api/v1/payments/redsys/insite/init', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { orderNumber: 'E2E-ORDER-1', merchantParameters: 'stub' },
        error: null,
        meta: { requestId: 'e2e-1' },
      }),
    })
  })

  await page.goto('/book')
  await page.getByRole('button', { name: /siguiente/i }).click()
  // … completar wizard según selectores reales del proyecto
  await expect(page.getByText(/cita confirmada/i)).toBeVisible()
})
```

---

## 6. Simulación de Redsys en tests

Tres estrategias por nivel, sin duplicar esfuerzo innecesario:

| Nivel | Estrategia | Finalidad |
|-------|------------|-----------|
| **Unitarios / integración (servidor)** | **Moq** de `IRedsysPaymentService` o respuestas HTTP controladas en factory | Reglas de negocio y estados (`Appointment`, `CustomerPaymentMethod`) sin red externa |
| **E2E** | **Interceptación Playwright** sobre URLs del API (inicio InSite, callbacks simulados) | Flujo UI + contrato JSON (envelope volumen 1 **§5.1.1**) |
| **Humo pre-deploy** | **Entorno de pruebas real Redsys** (TPV virtual / credenciales de test del comercio) | Validar firma HMAC, códigos Ds_Response y webhooks contra el banco antes de producción |

**Por qué no WireMock**

- Añade **otro runtime** (JVM o contenedor adicional) y contratos que hay que mantener alineados con el API .NET.
- El equipo ya centraliza mocks en **Moq** (C#) y **Playwright** (TypeScript); introducir WireMock fragmenta la propiedad de los stubs y complica los pipelines sin aportar ventaja frente a `WebApplicationFactory` + sustitución de `HttpClient` o mocks de servicio.

**Tarjetas de prueba.** La fuente única es [`redsys-development-guide.md`](redsys-development-guide.md) §2. Este documento no las copia.

**PCI:** nunca registrar PAN/CVC reales en logs, issues ni artefactos de CI.

---

## 7. Qué no se testea y por qué

| Exclusión | Motivo |
|-----------|--------|
| **Controladores que solo delegan** en un servicio ya cubierto por unitarios/integración | Riesgo de duplicación; el contrato HTTP se cubre en integración selectiva |
| **Mapeos de EF Core** (`OnModelCreating`, conversiones triviales) | Probadas indirectamente por integración con BD real |
| **Páginas Vue de solo presentación** (layout, estático) | Coste E2E alto; priorizar composables y flujos |
| **Código generado** (migraciones autogeneradas, client OpenAPI, tipos de herramientas) | Fuente de verdad externa; regenerar ante cambios |
| **SDK de terceros** (script `redsysV3.js`, AWS SDK interno) | Confiar en proveedor; limitar tests a **nuestros** adaptadores |

---

## 8. Cobertura mínima por fase del proyecto

Las fases coinciden con el roadmap del volumen 3 (**§10**): **MVP (Fase 1)**, **Fase 2** (mejoras + app móvil / web ampliada), **Fase 3** (SaaS multi-organización pública). La columna **Producción estable** refiere al estado tras go-live continuado (post-MVP), no a una «fase 4» separada.

| Ámbito | MVP (Fase 1) | Fase 2 | Fase 3 (SaaS) | Producción estable |
|--------|----------------|--------|---------------|---------------------|
| **Unitarios Application / Domain** | ≥ 50 % líneas en servicios críticos (citas, pagos, auth helpers) | ≥ 60 % | ≥ 65 % | Mantener ≥ 65 % en módulos tocados por cambios |
| **Unitarios Validators / Shared** | ≥ 60 % | ≥ 70 % | ≥ 75 % | Sin regresión en PR |
| **Frontend composables / utils** | ≥ 40 % en flujos reserva + auth | ≥ 50 % | ≥ 55 % | Críticos al 100 % |
| **Integración (API + SQL)** | Repositorios core + 5–10 flujos HTTP (auth, citas, pagos stub) | + notificaciones, más políticas tenant | + signup SaaS, límites por plan | Suite completa en cada release mayor |
| **E2E Playwright** | 2–3 escenarios (login, cita+pago mock, cancelación) | + reserva pública estable | + onboarding organización | Suite crítica en nightly + antes de release |

> Los porcentajes son **orientativos** de equilibrio coste/beneficio; el gate real es «¿este cambio rompe un contrato que un test debería haber detectado?».

---

## 9. Integración con CI/CD

Hay dos workflows. Los dos se disparan en cada pull request hacia `develop` o `main`, en cada push a `develop` y a mano. `main` exige los checks `build-test-format` y `lint-build`, también a los administradores ([ADR-008](adr/ADR-008-ci-obligatorio.md), [ADR-028](adr/ADR-028-checks-obligatorios-en-main.md)). `develop` no tiene esa protección.

| Workflow | Job | Qué hace |
| --- | --- | --- |
| Backend CI | `build-test-format` | Instala el SDK que fija `global.json`. `dotnet restore`, build en Release con avisos como errores (salvo la auditoría de NuGet NU1901–NU1904), dos pasos de `dotnet test` (unitarios e integración), cada uno con su fichero TRX, y `dotnet format --verify-no-changes`. Los de integración se ejecutan aunque fallen los unitarios, si el build fue bien. El nombre del job no cambia: sigue siendo el check obligatorio en `main` |
| Frontend CI | `lint-build` | En `reservarte-web`: Node 24 LTS, `npm ci`, `npm run lint -- --max-warnings 0` y `npm run build` (`vue-tsc` + Vite) |

Vitest se añadirá al job de frontend cuando exista. Los E2E de Playwright en CI están pendientes. El humo de Redsys contra el entorno de pruebas del banco no forma parte de estos workflows.

Los secretos de Redsys test no se almacenan en el repositorio (volumen 1 **§5.1.3**); en CI se inyectan vía **GitHub Actions Secrets** o el proveedor equivalente.

---

## 10. Resumen de herramientas y decisiones

| Área | Herramienta / decisión | Rol |
|------|------------------------|-----|
| Backend unitario | xUnit, Moq, AwesomeAssertions 9.6.0 (Apache-2.0), .NET 10 | Servicios, JWT, validadores, dominio y repositorios. Proyecto `tests/ReservArte.UnitTests`. Repositorios sobre SQLite, no InMemory. Mapeo: Mapperly y `MappingCharacterizationTests` |
| Formato backend | **`dotnet format --verify-no-changes`** + **`.editorconfig`** (raíz) | Puerta de calidad, línea base **CERO** (vol. 2 **§9.10**). |
| Backend integración | xUnit, Testcontainers.PostgreSql y Microsoft.AspNetCore.Mvc.Testing (versiones en el vol. 1 §4.1), `WebApplicationFactory`, .NET 10 | Proyecto `tests/ReservArte.IntegrationTests`. PostgreSQL 18 en Docker. [ADR-031](adr/ADR-031-tests-integracion-postgres.md) |
| Frontend | **Vitest**, **Vue Test Utils** | Composables y utilidades |
| Accesibilidad (front) | **`@axe-core/playwright`**, **axe DevTools** (manual) | Checks en navegador real (WCAG 2.1 AA; base legal en [`accessibility-and-i18n.md`](accessibility-and-i18n.md) §1, [ADR-025](adr/ADR-025-base-legal-accesibilidad.md)). LoginPage **RA-869d7fbpp shipped** con exclusión consciente de `color-contrast` (deuda **RA-869f0v6vm**). Plan vitest-axe **abandonado**. |
| E2E | **Playwright** (TypeScript) + **`@axe-core/playwright`** | `reservarte-web/playwright.config.ts` y `reservarte-web/e2e/` (tres navegadores) — **RA-869eqxdk3**. Specs actuales: a11y LoginPage, OAuth callback, reset-password (incl. enlace caducado, RA-869f1m12x), session-ending, set-password, **register** (RA-869f1xc2n). Suite **57/57** (reejecutados tras PR #60, 2026-09-15). En Mac: `npm run test:e2e` (no `npx playwright test`). `tests/ReservArte.E2ETests` **abandonado**. Escenarios de producto **pendientes**. Forgot→reset con API real: **RA-869f18uta**. |
| Redsys | Moq / route mock / entorno test real | Por capa; sin WireMock |
| CI | PR: unit + integración; post-merge: E2E; pre-deploy: humo Redsys | Ver §9 |

---

## Referencias cruzadas

- **Volumen 1** (`reservarte-memoria-1-analisis.md`): entidades, envelope API **§5.1.1–5.1.2**, configuración **§5.1.3**.
- **Volumen 2** (`reservarte-memoria-2-implementacion-y-desarrollo.md`): Redsys, JWT, cancelaciones **§7.6**, seguridad **§9**.
- **Volumen 3** (`reservarte-memoria-3-planificacion-y-gestion.md`): roadmap y checklist de arranque **§12.2**.
- **Estructura de carpetas** (`Análisis de pantallas y estructura.md`): backend `tests/ReservArte.UnitTests`, `tests/ReservArte.IntegrationTests`; E2E del SPA en `reservarte-web/e2e/` y `reservarte-web/playwright.config.ts` (no `tests/ReservArte.E2ETests`).
- **Accesibilidad e i18n** (`accessibility-and-i18n.md` §6): WCAG 2.1 AA, vue-i18n, **`@axe-core/playwright`**.

---

**Fin del documento de estrategia de testing**
