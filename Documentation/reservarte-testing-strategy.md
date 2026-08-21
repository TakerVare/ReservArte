# RESERVARTE — Estrategia de testing

**Documento:** Estrategia de pruebas automatizadas (backend, frontend y E2E)  
**Versión:** 1.0  
**Fecha:** mayo 2026  
**Proyecto:** ReservArte — Sistema multi-tenant de gestión para centros de diseño de cejas  
**Ubicación:** España  
**Stack de referencia:** ASP.NET Core 8.0, Vue 3 + Vite, SQL Server, AWS, Redsys

---

## Índice

1. [Filosofía y objetivos](#1-filosofía-y-objetivos)
2. [Pirámide de tests](#2-pirámide-de-tests)
3. [Capa unitaria](#3-capa-unitaria)
4. [Capa de integración](#4-capa-de-integración)
5. [Capa E2E](#5-capa-e2e)
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
| **Integración** | Contrato con BD real, pipeline HTTP completo, EF Core | Media | Repositorios, `WebApplicationFactory`, migraciones aplicadas a SQL Server efímero |
| **E2E** | Flujos críticos de usuario en navegador real | Baja | Pocos escenarios, alta confianza en regresiones de producto |

**Regla práctica:** si un caso puede resolverse con un unitario sin mentir sobre el sistema, no subirlo a integración; si integración basta (sin UI), no subirlo a E2E.

---

## 3. Capa unitaria

### 3.1 Backend

**Qué se testea**

- **Servicios de `ReservArte.Application`:** reglas de negocio con repositorios y servicios colaboradores sustituidos por **Moq** (p. ej. cancelación de cita con penalización según `OrganizationSettings`, coordinación con `IRedsysPaymentService`).
- **Validadores FluentValidation** (`ReservArte.Application/Validators`): reglas de entrada (fechas, rangos, obligatoriedad) sin levantar el API.
- **Helpers de firma HMAC / parámetros Redsys** (p. ej. en `ReservArte.Shared` o utilidades de infraestructura dedicadas): vectores conocidos — el orden de campos y el resultado de firma deben coincidir con la especificación Redsys.
- **`JwtTokenService`** (`ReservArte.Infrastructure/Services/JwtTokenService.cs`, volumen 2 **§9.2.1**): presencia de claims (`organization_id`, rol), expiración y validación con clave simétrica de prueba.

**Herramientas:** **xUnit** + **Moq** + **FluentAssertions**.

> **Estado del proyecto (2026-08-21, RA-869d7ezp3):** `tests/ReservArte.UnitTests` **existe y está operativo** (referenciado en `ReservArte.sln`). Primera suite: `JwtTokenServiceTests` — **17** tests (claims del access token, expiración, validación con clave simétrica **de prueba** —literal del test, no User Secrets—, aleatoriedad del refresh token, ticket `mfa_pending` sin claim `role`). Es la **semilla** de la capa unitaria backend. Integración (Testcontainers) y E2E (Playwright) siguen pendientes según el roadmap de este documento (§4–§5) y el volumen 3.
>
> **Versiones de paquetes de test:** **Moq** y **FluentAssertions** no están atados al target ASP.NET Core / EF Core **8.0.x**; se referencian con su última versión compatible con **net8.0** (numeración independiente de la familia Microsoft.AspNetCore.*).
**Servicios de aplicación (p. ej. `AppointmentService.CancelAppointmentAsync`, volumen 2 §7.6):** se prueban sustituyendo por **Moq** los mismos colaboradores que aparecen en el fragmento de implementación — `IAppointmentRepository`, `IOrganizationSettingsRepository`, `IRedsysPaymentService`, `INotificationService` — y asertando llamadas a `CancelAsync` vs `CaptureAsync` según `OrganizationSettings.CancellationHoursThreshold` y el tiempo restante hasta la cita. El constructor concreto de `AppointmentService` debe coincidir con el del repositorio; no fijar aquí una firma de DI que pueda divergir del código real.

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

> **Nota:** Los nombres de entidades (`Appointment`, `AppointmentStatus`, `OrganizationSettings`, `CustomerPaymentMethod`) y de servicios (`IRedsysPaymentService`, `RedsysPaymentService`) siguen el volumen 1 y el volumen 2.

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
    var user = new User { Id = 42, Email = "empleada@morethanbrows.com", Rol = "employee" };
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

**Herramientas:** **Vitest** + **Vue Test Utils** (y `@vue/test-utils` según versión del proyecto). **axe-core** + **vitest-axe** para humo de accesibilidad en componentes críticos (véase [`accessibility-and-i18n.md`](accessibility-and-i18n.md) §6).

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

**Qué se testea**

- **Repositorios** contra **SQL Server real** (esquema y restricciones reales, no sustitutos en memoria que ocultan tipos o `CHECK`).
- **Endpoints completos** con **`WebApplicationFactory`** (o equivalente minimal API): pipeline de middleware (**tenant**, autenticación JWT de prueba, autorización por rol/organización).
- **Migraciones de EF Core** aplicadas al arranque del contenedor: detecta roturas de modelo antes de desplegar.

**Herramientas:** **xUnit** + **Testcontainers** levantando **SQL Server** en Docker (`Testcontainers.MsSql` u paquete equivalente mantenido para .NET 8).

**Aislamiento multi-tenant:** en entorno de test de integración se usa la misma convención que en desarrollo (**cabecera** `X-Organization-Id` o la definida en volumen 1 **§5.1.3**). Los datos sembrados por test deben pertenecer a **dos organizaciones** y verificar que una petición con JWT/cabecera de la org A **no** devuelve filas de la org B.

**Ejemplo representativo (factory + tenant)**

```csharp
// tests/ReservArte.IntegrationTests/Controllers/AppointmentsControllerTests.cs
public class AppointmentsIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AppointmentsIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Organization-Id", _factory.SeededOrganizationId.ToString());
        // JWT de prueba emitido con el mismo Issuer/Audience/Secret que la factory inyecta en configuración
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateJwtForEmployee());
    }

    [Fact]
    public async Task Get_appointments_filtra_por_organizacion_del_contexto()
    {
        var response = await _client.GetAsync("/api/v1/appointments");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain(_factory.OtherOrganizationAppointmentId.ToString());
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
```

> La `CustomWebApplicationFactory` encapsula: arranque de **Testcontainers** MsSQL, `dotnet ef database update` (o `Migrate()`), semilla mínima (`Organization`, `Employee`, citas de dos tenants).

---

## 5. Capa E2E

**Decisión:** **Playwright** (no Cypress).

| Criterio | Playwright |
|----------|------------|
| TypeScript nativo | Tipos y fixtures de primer nivel |
| Paralelismo | Workers y sharding en CI |
| Interceptación de red | `page.route` / `route.fulfill` para simular API o Redsys sin tocar backend |

**Alcance deliberadamente reducido** — solo flujos críticos:

1. **Creación de cita con pago** (happy path o pago simulado vía red).
2. **Cancelación con penalización** (según reglas de `OrganizationSettings`).
3. **Login social** (o flujo acordado con mock del IdP en red si no hay sandbox estable en CI).
4. **Wizard de reserva pública** (multi-paso hasta confirmación).

**Ejemplo representativo (interceptación de red)**

```typescript
// tests/ReservArte.E2ETests/scenarios/booking-with-payment.spec.ts
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

**Tabla de tarjetas de prueba (entorno Redsys test)**

> Los PAN, caducidad y CVC exactos pueden variar según la versión del manual del **entorno de pruebas** del comercio. Mantener esta tabla sincronizada con la documentación oficial y con `user-secrets-guide.md` (volumen 1 **§5.1.3**).

| Escenario | PAN de ejemplo (test) | Resultado esperado (orientativo) |
|-----------|------------------------|----------------------------------|
| Autorización correcta | `4548810000000008` | Operación autorizada (`Ds_Response` OK) |
| Operación denegada | `4548810000000003` | Denegada por emisor (validar manejo `PAY_REDSYS_DECLINED`; confirmar PAN en manual vigente) |
| Autenticación fuerte / 3DS | Consultar tabla «SCA» del manual Redsys test | Flujo adicional en InSite; E2E puede limitarse a mock |
| Bizum / wallet | Según anexo de medios del entorno de pruebas | Solo en humo o entorno dedicado |

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

| Momento del pipeline | Tests | Notas |
|----------------------|-------|-------|
| **Cada PR** (backend) | Unitarios + integración (Testcontainers; job con Docker disponible) | Fallo bloquea merge |
| **Cada PR** (frontend) | Vitest (unit + componentes críticos) | Paralelo al job backend |
| **Merge a `develop`** (o rama de integración acordada en volumen 3 **§10.1.2**) | E2E Playwright (navegador headless, artefactos de vídeo/trace en fallo) | Subconjunto o suite completa según tiempo |
| **Pre-deploy a staging/producción** | **Humo Redsys** manual o job opcional con credenciales secrets | Una transacción test + webhook; checklist operaciones |
| **Nightly** (recomendado) | E2E extendido + integración larga | Detecta flaky tests y dependencias externas |

Los secretos de Redsys test no se almacenan en el repositorio (volumen 1 **§5.1.3**); en CI se inyectan vía **GitHub Actions Secrets** o el proveedor equivalente.

---

## 10. Resumen de herramientas y decisiones

| Área | Herramienta / decisión | Rol |
|------|------------------------|-----|
| Backend unitario | **xUnit**, **Moq**, **FluentAssertions** | Tests rápidos de servicios, JWT, validadores. Proyecto `tests/ReservArte.UnitTests` operativo (RA-869d7ezp3): semilla = `JwtTokenServiceTests` (17). Moq/FluentAssertions: última compatible con net8.0 (no fijadas a 8.0.x de ASP.NET Core). |
| Backend integración | **xUnit**, **Testcontainers** (SQL Server), **WebApplicationFactory** | BD real, middleware tenant, EF migrations — **pendiente** |
| Frontend | **Vitest**, **Vue Test Utils** | Composables y utilidades |
| Accesibilidad (front) | **axe-core**, **vitest-axe**, **axe DevTools** (manual) | Humo A11y en PR; ver [`accessibility-and-i18n.md`](accessibility-and-i18n.md) |
| E2E | **Playwright** (TypeScript) | Flujos críticos, interceptación de red — **pendiente** |
| Redsys | Moq / route mock / entorno test real | Por capa; sin WireMock |
| CI | PR: unit + integración; post-merge: E2E; pre-deploy: humo Redsys | Ver §9 |

---

## Referencias cruzadas

- **Volumen 1** (`reservarte-memoria-1-analisis.md`): entidades, envelope API **§5.1.1–5.1.2**, configuración **§5.1.3**.
- **Volumen 2** (`reservarte-memoria-2-implementacion-y-desarrollo.md`): Redsys, JWT, cancelaciones **§7.6**, seguridad **§9**.
- **Volumen 3** (`reservarte-memoria-3-planificacion-y-gestion.md`): roadmap y checklist de arranque **§12.2**.
- **Estructura de carpetas** (`Análisis de pantallas y estructura.md`): `tests/ReservArte.UnitTests`, `tests/ReservArte.IntegrationTests`, `tests/ReservArte.E2ETests`.
- **Accesibilidad e i18n** (`accessibility-and-i18n.md`): WCAG 2.1 AA, vue-i18n, axe.

---

**Fin del documento de estrategia de testing**
