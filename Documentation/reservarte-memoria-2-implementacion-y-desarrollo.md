# RESERVARTE — Documentación técnica
## Sistema multi-tenant de gestión para centros de diseño de cejas

**Volumen 2 de 3: Implementación y desarrollo**

---

**Versión:** 1.0  
**Fecha:** Octubre 2025  
**Cliente:** More Than Brows  
**Ubicación:** España  
**Desarrolladores:** Gabriel Sánchez-Vallejo Millán y Guillermo Algárate del Arco

---

## Índice (volumen 2)

7. [PASARELAS DE PAGO Y SISTEMA FINANCIERO](#7-pasarelas-de-pago-y-sistema-financiero)
8. [SISTEMA DE NOTIFICACIONES](#8-sistema-de-notificaciones)
9. [SEGURIDAD Y PROTECCIÓN DE DATOS](#9-seguridad-y-protecciÃ³n-de-datos) (incl. **§9.2.3** patrón páginas auth SPA, **§9.2.4** BottomNav global, **§9.3.4** CORS SPA→API, **§9.5** referencia a estrategia de testing en [`reservarte-testing-strategy.md`](reservarte-testing-strategy.md), **§9.6** dominio y persistencia módulo Empleados, **§9.7** dominio módulo Clientes, **§9.8** dominio, persistencia, servicio y API módulo Servicios — cinco subtareas, **§9.9** dominio, mapeo y repositorio módulo Citas, **§9.10** convenciones de formato / `.editorconfig`)

---

## 7. PASARELAS DE PAGO Y SISTEMA FINANCIERO

> **Contrato de respuestas JSON:** los controladores, `TenantMiddleware`, el rate limiter y los 401/403 de JwtBearer (`OnChallenge` / `OnForbidden`, RA-869f1anz3, 2026-09-14) usan el **envelope** `{ success, data, error, meta }` (vol. 1 **§5.1.1**). Excepciones: webhooks Redsys, health checks, y el 400 `ProblemDetails` de `[ApiController]` (**RA-869f1k17q**). Los ejemplos de este capítulo con `BadRequest(new { success = false, error = "..." })` deben evolucionar al envelope (incl. `meta.requestId`).

### 7.1 Comparativa de Pasarelas de Pago en España

| Pasarela | Pre-autorización | Bizum | Costos | Integración | Recomendación |
|----------|------------------|-------|--------|-------------|---------------|
| **Redsys** | ✅ Sí (nativo) | ✅ Sí | 1-1.5% variable | ⭐⭐⭐⭐ Buena | **PRINCIPAL** |
| Stripe | ✅ Excelente | ❌ No | 1.4% + €0.25 | ⭐⭐⭐⭐⭐ Muy fácil | Alternativa (no usada) |
| PayPal | ✅ Sí | ❌ No | 2.9% + €0.35 | ⭐⭐⭐⭐ Fácil | No recomendado (caro) |
| Paycomet | ✅ Sí | ✅ Sí | Negociable | ⭐⭐⭐ Media | Alternativa válida |

---

### 7.2 Decisión: Redsys como Pasarela Principal

**Razones para elegir Redsys:**

1. **✅ Integración bancaria española**: Respaldado por todos los bancos españoles
2. **✅ Bizum incluido**: Método de pago preferido en España
3. **✅ Cumplimiento PCI-DSS simplificado**: Con InSite (SAQ A-EP)
4. **✅ Datos en España/UE**: Sin transferencias internacionales
5. **✅ Costos competitivos**: 1-1.5% para transacciones nacionales
6. **✅ Tokenización nativa**: Guardado seguro de tarjetas
7. **✅ Pre-autorizaciones robustas**: Sistema diseñado para ellas
8. **✅ 3D Secure 2.x integrado**: SCA (Strong Customer Authentication) automático
9. **✅ Sin dependencias USA**: Mayor control regulatorio

**Enfoque de implementación:**
- **Método Principal**: Redsys InSite (iframes seguros)
- **Método Alternativo**: Redsys REST API (mayor control)

---

### 7.3 Redsys InSite: Implementación Detallada

> **Desarrollo local:** credenciales sandbox, tarjetas de prueba, integración con **User Secrets**, exposición del **webhook** (`/api/v1/payments/redsys/webhook`) vía ngrok o Cloudflare Tunnel, checklist paso a paso y depuración en [`redsys-development-guide.md`](redsys-development-guide.md).

#### 7.3.1 Arquitectura de Redsys InSite

```
┌─────────────┐
│   Cliente   │
│  (Browser)  │
└──────┬──────┘
       │
       │ 1. Carga página de pago
       ▼
┌─────────────────────┐
│  Frontend (Vite + Vue 3) │
│  ┌───────────────┐  │
│  │ redsysV3.js   │  │ ◄── SDK JavaScript Redsys
│  │ (iframes)     │  │
│  └───────────────┘  │
└──────┬──────────────┘
       │
       │ 2. Usuario introduce tarjeta en iframes de Redsys
       │    (datos nunca tocan nuestro servidor)
       │
       │ 3. Redsys retorna idOper
       ▼
┌─────────────────────┐
│  Backend (.NET)     │
│  ┌───────────────┐  │
│  │ Confirmar pago│  │
│  │ con idOper    │  │
│  └───────┬───────┘  │
└──────────┼──────────┘
           │
           │ 4. Petición REST con idOper
           ▼
    ┌──────────────┐
    │  Redsys TPV  │
    │   Virtual    │
    └──────┬───────┘
           │
           │ 5. Respuesta + Token (si solicitado)
           ▼
    ┌──────────────┐
    │  Base Datos  │
    │  SQL Server   │
    │   (Docker)    │
    └──────────────┘
```

---

#### 7.3.2 Flujo Completo de Pre-autorización con InSite

**Paso 1: Inicializar pago en Frontend**

```typescript
// frontend-web/src/services/redsys-insite.service.ts
import { v4 as uuidv4 } from 'uuid';

interface RedsysInsiteConfig {
  merchantCode: string;
  terminal: string;
  currency: string; // '978' para EUR
  environment: 'test' | 'production';
}

export class RedsysInsiteService {
  private config: RedsysInsiteConfig;
  
  constructor(config: RedsysInsiteConfig) {
    this.config = config;
  }

  /**
   * Genera un número de pedido único para Redsys
   * Formato: YYYYMMDD + HHMMSS + 4 dígitos aleatorios = 18 caracteres
   */
  generateOrderNumber(): string {
    const now = new Date();
    const datePart = now.toISOString().slice(0, 10).replace(/-/g, ''); // YYYYMMDD
    const timePart = now.toTimeString().slice(0, 8).replace(/:/g, ''); // HHMMSS
    const randomPart = Math.floor(Math.random() * 10000).toString().padStart(4, '0');
    return `${datePart}${timePart}${randomPart}`.slice(0, 12); // Max 12 chars
  }

  /**
   * Inicializa los campos de pago de Redsys InSite
   */
  async initializePaymentFields(containerId: string, amount: number, orderNumber: string) {
    // Estilos para los iframes
    const styles = {
      'font-family': 'Inter, system-ui, sans-serif',
      'font-size': '16px',
      'color': '#1f2937',
      'border': '1px solid #d1d5db',
      'border-radius': '0.5rem',
      'padding': '0.75rem 1rem',
      'width': '100%',
      'box-sizing': 'border-box',
    };

    // Cargar SDK de Redsys (asegurarse de que esté en index.html)
    // <script src="https://sis.redsys.es/sis/NC/redsysV3.js"></script>

    if (typeof getCardInput === 'undefined') {
      throw new Error('Redsys SDK no cargado. Incluir script en index.html');
    }

    // Crear campos de tarjeta
    getCardInput('card-number', styles, 'Número de tarjeta');
    getExpirationMonthInput('expiry-month', styles);
    getExpirationYearInput('expiry-year', styles);
    getCVVInput('cvv', styles, 'CVV');

    // Crear botón de pago con parámetros
    const amountInCents = Math.round(amount * 100).toString();
    
    getPayButton(
      'pay-button',
      styles,
      'Confirmar Pago',
      this.config.merchantCode,
      this.config.terminal,
      orderNumber,
      amountInCents,
      this.config.currency,
      '0', // Tipo transacción: 0=Autorización
      '', // URL OK (no necesario con InSite)
      '', // URL KO
      '', // Idioma: '' = auto-detect
      '', // Datos merchant
      '', // Merchant URL (webhook)
      '', // URL logo
      '', // Nombre titular (opcional)
      '3DES' // Cifrado
    );

    // Escuchar evento de éxito
    inSitePayment.addEventListener('paymentSuccess', (event: any) => {
      const idOper = event.detail.idOper;
      console.log('Pago exitoso, idOper:', idOper);
      return idOper;
    });

    // Escuchar evento de error
    inSitePayment.addEventListener('paymentError', (event: any) => {
      console.error('Error en pago:', event.detail);
      throw new Error(event.detail.error);
    });
  }
}
```

**Componente Vue 3 para el formulario de pago (`<script setup>`):**

```vue
<!-- frontend-web/src/components/features/appointments/PaymentForm.vue -->
<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { RedsysInsiteService } from '@/services/redsys-insite.service'
import { apiClient } from '@/lib/api-client'

const props = defineProps<{
  appointmentId: string
  amount: number
}>()

const emit = defineEmits<{
  success: [paymentId: string]
  error: [message: string]
}>()

const isProcessing = ref(false)
const saveCard = ref(false)
const orderNumber = ref('')
let redsysService: RedsysInsiteService | null = null

async function initRedsys() {
  try {
    const { data: config } = await apiClient.get('/api/v1/payments/redsys/config')
    redsysService = new RedsysInsiteService({
      merchantCode: config.merchantCode,
      terminal: config.terminal,
      currency: '978',
      environment: config.environment,
    })
    const orderNum = redsysService.generateOrderNumber()
    orderNumber.value = orderNum
    await redsysService.initializePaymentFields('payment-container', props.amount, orderNum)
  } catch (err) {
    console.error('Error inicializando Redsys:', err)
    emit('error', 'Error al cargar el sistema de pago')
  }
}

async function onPaymentSuccess(event: CustomEvent) {
  const idOper = event.detail.idOper
  isProcessing.value = true
  try {
    const { data } = await apiClient.post('/api/v1/payments/redsys/insite/complete', {
      appointmentId: props.appointmentId,
      orderNumber: orderNumber.value,
      idOper,
      saveCard: saveCard.value,
    })
    if (data.success) emit('success', data.paymentId)
    else emit('error', data.error || 'Error procesando el pago')
  } catch (err: unknown) {
    console.error('Error completando pago:', err)
    const msg =
      err && typeof err === 'object' && 'response' in err
        ? (err as { response?: { data?: { message?: string } } }).response?.data?.message
        : undefined
    emit('error', msg || 'Error al procesar el pago')
  } finally {
    isProcessing.value = false
  }
}

function attachListeners() {
  if (typeof inSitePayment !== 'undefined') {
    inSitePayment.addEventListener('paymentSuccess', onPaymentSuccess as EventListener)
  }
}

function detachListeners() {
  if (typeof inSitePayment !== 'undefined') {
    inSitePayment.removeEventListener('paymentSuccess', onPaymentSuccess as EventListener)
  }
}

watch(
  () => props.amount,
  () => {
    initRedsys()
  }
)

onMounted(async () => {
  await initRedsys()
  attachListeners()
})

onUnmounted(() => {
  detachListeners()
})
</script>

<template>
  <div class="payment-form">
    <h3 class="text-xl font-semibold mb-4">Información de Pago</h3>

    <div class="space-y-4">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">Número de tarjeta</label>
        <div id="card-number" class="min-h-[48px]" />
      </div>

      <div class="grid grid-cols-3 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Mes</label>
          <div id="expiry-month" class="min-h-[48px]" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Año</label>
          <div id="expiry-year" class="min-h-[48px]" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">CVV</label>
          <div id="cvv" class="min-h-[48px]" />
        </div>
      </div>

      <div class="flex items-start">
        <input
          id="save-card-checkbox"
          v-model="saveCard"
          type="checkbox"
          class="mt-1 h-4 w-4 rounded border-gray-300"
        />
        <label for="save-card-checkbox" class="ml-2 text-sm text-gray-600">
          Guardar esta tarjeta de forma segura para futuros pagos
        </label>
      </div>

      <div id="pay-button" class="mt-6" />

      <div v-if="isProcessing" class="text-center py-4">
        <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
        <p class="mt-2 text-sm text-gray-600">Procesando pago...</p>
      </div>
    </div>

    <div class="mt-6 p-4 bg-gray-50 rounded-lg">
      <div class="flex items-start">
        <svg class="h-5 w-5 text-green-600 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
          <path
            fill-rule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
            clip-rule="evenodd"
          />
        </svg>
        <div class="ml-3">
          <p class="text-sm text-gray-700 font-medium">Pago 100% seguro</p>
          <p class="text-xs text-gray-500 mt-1">
            Procesado por Redsys con cifrado bancario. Tus datos nunca pasan por nuestros servidores.
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
```

---

**Paso 2: Backend - Completar pago con idOper**

```csharp
// ReservArte.API/Controllers/PaymentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ReservArte.Application.Services;
using ReservArte.Application.DTOs.Payments;

namespace ReservArte.API.Controllers
{
    [ApiController]
    [Route("api/v1/payments")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IRedsysPaymentService _redsysService;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IRedsysPaymentService redsysService,
            IAppointmentRepository appointmentRepository,
            IPaymentMethodRepository paymentMethodRepository,
            ILogger<PaymentsController> logger)
        {
            _redsysService = redsysService;
            _appointmentRepository = appointmentRepository;
            _paymentMethodRepository = paymentMethodRepository;
            _logger = logger;
        }

        [HttpGet("redsys/config")]
        public async Task<IActionResult> GetRedsysConfig()
        {
            var config = await _redsysService.GetPublicConfigAsync();
            return Ok(config);
        }

        [HttpPost("redsys/insite/complete")]
        public async Task<IActionResult> CompleteInsitePayment(
            [FromBody] CompleteInsitePaymentRequest request)
        {
            try
            {
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId);
                
                if (appointment == null)
                    return NotFound(new { success = false, error = "Cita no encontrada" });

                // Completar pago usando el idOper
                var result = await _redsysService.CompleteInsitePaymentAsync(
                    appointment,
                    request.IdOper,
                    request.SaveCard
                );

                if (result.IsSuccess)
                {
                    // Actualizar estado de la cita
                    appointment.Status = AppointmentStatus.Confirmed;
                    appointment.RedsysOrderNumber = request.OrderNumber;
                    appointment.RedsysPreAuthToken = result.AuthCode;
                    await _appointmentRepository.UpdateAsync(appointment);

                    // Si se solicitó guardar tarjeta y Redsys devolvió token
                    if (request.SaveCard && !string.IsNullOrEmpty(result.Token))
                    {
                        await SaveCustomerPaymentMethodAsync(
                            appointment.CustomerId,
                            appointment.OrganizationId,
                            result
                        );
                    }

                    return Ok(new { 
                        success = true, 
                        paymentId = result.PaymentId,
                        appointmentId = appointment.Id 
                    });
                }

                return BadRequest(new { success = false, error = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completando pago InSite");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Error interno del servidor" 
                });
            }
        }

        private async Task SaveCustomerPaymentMethodAsync(
            Guid customerId,
            Guid organizationId,
            RedsysPaymentResult result)
        {
            // Verificar si el cliente ya tiene esta tarjeta guardada
            var existingMethod = await _paymentMethodRepository
                .GetByTokenAsync(customerId, result.Token);

            if (existingMethod != null)
            {
                // Actualizar fecha de último uso
                existingMethod.LastUsedAt = DateTime.UtcNow;
                await _paymentMethodRepository.UpdateAsync(existingMethod);
                return;
            }

            // Crear nuevo método de pago
            var paymentMethod = new CustomerPaymentMethod
            {
                CustomerId = customerId,
                OrganizationId = organizationId,
                RedsysToken = result.Token,
                RedsysCofTxnid = result.CofTxnId,
                RedsysCardBrand = result.CardBrand,
                RedsysCardLast4 = result.CardLast4,
                RedsysCardExpiry = result.CardExpiry,
                RedsysCardNumberMasked = result.CardNumberMasked,
                IsDefault = !await _paymentMethodRepository.CustomerHasPaymentMethodsAsync(customerId),
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            await _paymentMethodRepository.AddAsync(paymentMethod);
            
            _logger.LogInformation(
                $"Tarjeta guardada para cliente {customerId}: {result.CardBrand} ****{result.CardLast4}"
            );
        }
    }
}
```

---

**Paso 3: Servicio de Redsys en Backend**

```csharp
// ReservArte.Application/Services/RedsysPaymentService.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReservArte.Application.DTOs.Payments;
using ReservArte.Domain.Entities;

namespace ReservArte.Application.Services
{
    public interface IRedsysPaymentService
    {
        Task<RedsysPublicConfig> GetPublicConfigAsync();
        Task<RedsysPaymentResult> CompleteInsitePaymentAsync(
            Appointment appointment, 
            string idOper, 
            bool saveCard);
        Task<RedsysPaymentResult> PreAuthorizeAsync(Appointment appointment);
        Task<RedsysPaymentResult> CaptureAsync(Appointment appointment, decimal amount);
        Task<RedsysPaymentResult> CancelAsync(Appointment appointment);
    }

    public class RedsysPaymentService : IRedsysPaymentService
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RedsysPaymentService> _logger;
        private readonly IConfiguration _configuration;

        private const string REDSYS_TEST_URL = "https://sis-t.redsys.es:25443/sis/rest/trataPeticionREST";
        private const string REDSYS_PROD_URL = "https://sis.redsys.es/sis/rest/trataPeticionREST";

        public RedsysPaymentService(
            IOrganizationRepository organizationRepository,
            IPaymentRepository paymentRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<RedsysPaymentService> logger,
            IConfiguration configuration)
        {
            _organizationRepository = organizationRepository;
            _paymentRepository = paymentRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<RedsysPublicConfig> GetPublicConfigAsync()
        {
            // Obtener configuración de la organización actual
            var organization = await _organizationRepository.GetCurrentAsync();
            
            return new RedsysPublicConfig
            {
                MerchantCode = organization.RedsysMerchantCode,
                Terminal = organization.RedsysTerminal,
                Environment = organization.RedsysEnvironment
            };
        }

        public async Task<RedsysPaymentResult> CompleteInsitePaymentAsync(
            Appointment appointment,
            string idOper,
            bool saveCard)
        {
            var organization = await _organizationRepository.GetByIdAsync(appointment.OrganizationId);
            var secretKey = await GetSecretKeyAsync(organization.Id);

            // Preparar parámetros para Redsys
            var merchantParams = new Dictionary<string, object>
            {
                { "DS_MERCHANT_ORDER", appointment.RedsysOrderNumber },
                { "DS_MERCHANT_MERCHANTCODE", organization.RedsysMerchantCode },
                { "DS_MERCHANT_TERMINAL", organization.RedsysTerminal },
                { "DS_MERCHANT_TRANSACTIONTYPE", "1" }, // 1 = Pre-autorización
                { "DS_MERCHANT_AMOUNT", ((int)(appointment.TotalPrice * 100)).ToString() },
                { "DS_MERCHANT_CURRENCY", "978" }, // EUR
                { "DS_MERCHANT_IDOPER", idOper },
                { "DS_MERCHANT_MERCHANTURL", $"{_configuration["AppUrl"]}/api/v1/payments/redsys/webhook" }
            };

            // Si se solicita guardar tarjeta, añadir tokenización
            if (saveCard)
            {
                merchantParams.Add("DS_MERCHANT_IDENTIFIER", "REQUIRED");
                merchantParams.Add("DS_MERCHANT_COF_INI", "S"); // Credential On File - Inicio
                merchantParams.Add("DS_MERCHANT_COF_TYPE", "R"); // Recurrente
                merchantParams.Add("DS_MERCHANT_COF_TXNID", Guid.NewGuid().ToString()); // ID único para COF
            }

            // Generar firma
            var signature = GenerateSignature(merchantParams, secretKey);

            // Preparar request para Redsys
            var requestBody = new
            {
                Ds_SignatureVersion = "HMAC_SHA256_V1",
                Ds_MerchantParameters = EncodeParameters(merchantParams),
                Ds_Signature = signature
            };

            // Llamar a Redsys REST API
            var httpClient = _httpClientFactory.CreateClient();
            var redsysUrl = organization.RedsysEnvironment == "production" 
                ? REDSYS_PROD_URL 
                : REDSYS_TEST_URL;

            var response = await httpClient.PostAsJsonAsync(redsysUrl, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error en Redsys: {responseContent}");
                return new RedsysPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Error comunicando con la pasarela de pago"
                };
            }

            // Parsear respuesta
            var redsysResponse = JsonSerializer.Deserialize<RedsysRestResponse>(responseContent);
            var decodedParams = DecodeParameters(redsysResponse.Ds_MerchantParameters);

            // Validar firma de respuesta
            if (!ValidateSignature(
                redsysResponse.Ds_MerchantParameters,
                redsysResponse.Ds_Signature,
                secretKey))
            {
                _logger.LogWarning("Firma de Redsys inválida");
                return new RedsysPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Respuesta de pago inválida"
                };
            }

            // Verificar código de respuesta (0000-0099 = éxito)
            var responseCode = decodedParams["Ds_Response"].ToString();
            var isSuccess = int.TryParse(responseCode, out int code) && code <= 99;

            if (isSuccess)
            {
                // Registrar pago en BD
                var payment = new Payment
                {
                    OrganizationId = appointment.OrganizationId,
                    AppointmentId = appointment.Id,
                    CustomerId = appointment.CustomerId,
                    Amount = appointment.TotalPrice,
                    Currency = "EUR",
                    PaymentMethod = "Card",
                    Status = "Authorized",
                    RedsysOrderNumber = appointment.RedsysOrderNumber,
                    RedsysAuthCode = decodedParams["Ds_AuthorisationCode"]?.ToString(),
                    RedsysResponseCode = responseCode,
                    RedsysTransactionType = "1",
                    RedsysCardNumberMasked = decodedParams["Ds_Card_Number"]?.ToString(),
                    RedsysCardBrand = decodedParams["Ds_Card_Brand"]?.ToString(),
                    ProcessedAt = DateTime.UtcNow,
                    Metadata = JsonSerializer.Serialize(decodedParams)
                };

                await _paymentRepository.AddAsync(payment);

                // Preparar resultado
                var result = new RedsysPaymentResult
                {
                    IsSuccess = true,
                    PaymentId = payment.Id,
                    AuthCode = payment.RedsysAuthCode,
                    CardBrand = payment.RedsysCardBrand,
                    CardLast4 = ExtractLast4Digits(payment.RedsysCardNumberMasked),
                    CardNumberMasked = payment.RedsysCardNumberMasked
                };

                // Si se guardó token de tarjeta
                if (saveCard && decodedParams.ContainsKey("Ds_Merchant_Identifier"))
                {
                    result.Token = decodedParams["Ds_Merchant_Identifier"].ToString();
                    result.CofTxnId = decodedParams["Ds_Merchant_Cof_Txnid"]?.ToString();
                    result.CardExpiry = decodedParams["Ds_ExpiryDate"]?.ToString(); // AAMM
                }

                return result;
            }

            // Pago fallido
            _logger.LogWarning($"Pago rechazado. Código: {responseCode}");
            return new RedsysPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = GetErrorMessage(responseCode)
            };
        }

        public async Task<RedsysPaymentResult> CaptureAsync(Appointment appointment, decimal amount)
        {
            var organization = await _organizationRepository.GetByIdAsync(appointment.OrganizationId);
            var secretKey = await GetSecretKeyAsync(organization.Id);

            var merchantParams = new Dictionary<string, object>
            {
                { "DS_MERCHANT_ORDER", appointment.RedsysOrderNumber },
                { "DS_MERCHANT_MERCHANTCODE", organization.RedsysMerchantCode },
                { "DS_MERCHANT_TERMINAL", organization.RedsysTerminal },
                { "DS_MERCHANT_TRANSACTIONTYPE", "2" }, // 2 = Confirmación
                { "DS_MERCHANT_AMOUNT", ((int)(amount * 100)).ToString() },
                { "DS_MERCHANT_CURRENCY", "978" }
            };

            return await ExecuteRedsysRequestAsync(
                merchantParams, 
                secretKey, 
                organization.RedsysEnvironment,
                appointment,
                "Captured"
            );
        }

        public async Task<RedsysPaymentResult> CancelAsync(Appointment appointment)
        {
            var organization = await _organizationRepository.GetByIdAsync(appointment.OrganizationId);
            var secretKey = await GetSecretKeyAsync(organization.Id);

            var merchantParams = new Dictionary<string, object>
            {
                { "DS_MERCHANT_ORDER", appointment.RedsysOrderNumber },
                { "DS_MERCHANT_MERCHANTCODE", organization.RedsysMerchantCode },
                { "DS_MERCHANT_TERMINAL", organization.RedsysTerminal },
                { "DS_MERCHANT_TRANSACTIONTYPE", "9" }, // 9 = Devolución/Cancelación
                { "DS_MERCHANT_AMOUNT", ((int)(appointment.TotalPrice * 100)).ToString() },
                { "DS_MERCHANT_CURRENCY", "978" }
            };

            return await ExecuteRedsysRequestAsync(
                merchantParams,
                secretKey,
                organization.RedsysEnvironment,
                appointment,
                "Refunded"
            );
        }

        // ================ MÉTODOS AUXILIARES ================

        private async Task<string> GetSecretKeyAsync(Guid organizationId)
        {
            // En producción, obtener de AWS Secrets Manager
            // Por ahora, de configuración
            return _configuration[$"Redsys:{organizationId}:SecretKey"];
        }

        private string GenerateSignature(Dictionary<string, object> parameters, string secretKey)
        {
            var orderNumber = parameters["DS_MERCHANT_ORDER"].ToString();

            // 1. Decodificar clave secreta (Base64)
            var keyBytes = Convert.FromBase64String(secretKey);

            // 2. Cifrar número de pedido con 3DES
            using var des = TripleDES.Create();
            des.Key = keyBytes;
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.Zeros;
            des.IV = new byte[8]; // IV de ceros

            var orderBytes = Encoding.UTF8.GetBytes(orderNumber);
            var encryptedOrder = des.CreateEncryptor()
                .TransformFinalBlock(orderBytes, 0, orderBytes.Length);

            // 3. Calcular HMAC-SHA256
            using var hmac = new HMACSHA256(encryptedOrder);
            var paramsEncoded = EncodeParameters(parameters);
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(paramsEncoded));

            // 4. Convertir a Base64
            return Convert.ToBase64String(signatureBytes);
        }

        private bool ValidateSignature(string merchantParameters, string signature, string secretKey)
        {
            var decodedParams = DecodeParameters(merchantParameters);
            var orderNumber = decodedParams["Ds_Order"].ToString();

            // Misma lógica que GenerateSignature
            var keyBytes = Convert.FromBase64String(secretKey);

            using var des = TripleDES.Create();
            des.Key = keyBytes;
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.Zeros;
            des.IV = new byte[8];

            var orderBytes = Encoding.UTF8.GetBytes(orderNumber);
            var encryptedOrder = des.CreateEncryptor()
                .TransformFinalBlock(orderBytes, 0, orderBytes.Length);

            using var hmac = new HMACSHA256(encryptedOrder);
            var calculatedSignature = Convert.ToBase64String(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(merchantParameters))
            );

            return signature == calculatedSignature;
        }

        private string EncodeParameters(Dictionary<string, object> parameters)
        {
            var json = JsonSerializer.Serialize(parameters);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        private Dictionary<string, object> DecodeParameters(string encodedParameters)
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedParameters));
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }

        private string ExtractLast4Digits(string maskedNumber)
        {
            if (string.IsNullOrEmpty(maskedNumber))
                return null;

            // Formato típico: 454881******0003
            return maskedNumber.Substring(maskedNumber.Length - 4);
        }

        private string GetErrorMessage(string responseCode)
        {
            // Códigos de error comunes de Redsys
            return responseCode switch
            {
                "0101" => "Tarjeta caducada",
                "0102" => "Tarjeta bloqueada temporalmente",
                "0106" => "Intentos de PIN excedidos",
                "0125" => "Tarjeta no efectiva",
                "0129" => "Código de seguridad (CVV) incorrecto",
                "0180" => "Tarjeta no válida",
                "0184" => "Error en autenticación del titular",
                "0190" => "Denegada sin especificar motivo",
                _ => "Pago rechazado. Por favor, intente con otra tarjeta."
            };
        }

        private async Task<RedsysPaymentResult> ExecuteRedsysRequestAsync(
            Dictionary<string, object> merchantParams,
            string secretKey,
            string environment,
            Appointment appointment,
            string paymentStatus)
        {
            var signature = GenerateSignature(merchantParams, secretKey);
            var requestBody = new
            {
                Ds_SignatureVersion = "HMAC_SHA256_V1",
                Ds_MerchantParameters = EncodeParameters(merchantParams),
                Ds_Signature = signature
            };

            var httpClient = _httpClientFactory.CreateClient();
            var redsysUrl = environment == "production" ? REDSYS_PROD_URL : REDSYS_TEST_URL;

            var response = await httpClient.PostAsJsonAsync(redsysUrl, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error en Redsys: {responseContent}");
                return new RedsysPaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Error comunicando con la pasarela"
                };
            }

            var redsysResponse = JsonSerializer.Deserialize<RedsysRestResponse>(responseContent);
            var decodedParams = DecodeParameters(redsysResponse.Ds_MerchantParameters);

            var responseCode = decodedParams["Ds_Response"].ToString();
            var isSuccess = int.TryParse(responseCode, out int code) && 
                            (code <= 99 || code == 400 || code == 900);

            if (isSuccess)
            {
                // Actualizar pago en BD
                var payment = await _paymentRepository.GetByOrderNumberAsync(
                    appointment.RedsysOrderNumber
                );
                
                if (payment != null)
                {
                    payment.Status = paymentStatus;
                    payment.ProcessedAt = DateTime.UtcNow;
                    await _paymentRepository.UpdateAsync(payment);
                }

                return new RedsysPaymentResult
                {
                    IsSuccess = true,
                    AuthCode = decodedParams["Ds_AuthorisationCode"]?.ToString()
                };
            }

            return new RedsysPaymentResult
            {
                IsSuccess = false,
                ErrorMessage = GetErrorMessage(responseCode)
            };
        }
    }

    // DTOs
    public class RedsysPublicConfig
    {
        public string MerchantCode { get; set; }
        public string Terminal { get; set; }
        public string Environment { get; set; }
    }

    public class RedsysPaymentResult
    {
        public bool IsSuccess { get; set; }
        public Guid? PaymentId { get; set; }
        public string AuthCode { get; set; }
        public string Token { get; set; } // Para tarjetas guardadas
        public string CofTxnId { get; set; }
        public string CardBrand { get; set; }
        public string CardLast4 { get; set; }
        public string CardExpiry { get; set; }
        public string CardNumberMasked { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RedsysRestResponse
    {
        public string Ds_SignatureVersion { get; set; }
        public string Ds_MerchantParameters { get; set; }
        public string Ds_Signature { get; set; }
    }

    public class CompleteInsitePaymentRequest
    {
        public Guid AppointmentId { get; set; }
        public string OrderNumber { get; set; }
        public string IdOper { get; set; }
        public bool SaveCard { get; set; }
    }
}
```

---

### 7.4 Uso de Tarjetas Guardadas

#### 7.4.1 Listar Tarjetas del Cliente

```csharp
// ReservArte.API/Controllers/CustomersController.cs
[HttpGet("{customerId}/payment-methods")]
public async Task<IActionResult> GetPaymentMethods(Guid customerId)
{
    var paymentMethods = await _paymentMethodRepository.GetByCustomerIdAsync(customerId);
    
    var response = paymentMethods.Select(pm => new
    {
        id = pm.Id,
        cardBrand = pm.RedsysCardBrand,
        cardLast4 = pm.RedsysCardLast4,
        cardExpiry = pm.RedsysCardExpiry,
        isDefault = pm.IsDefault,
        lastUsedAt = pm.LastUsedAt
    });

    return Ok(response);
}
```

#### 7.4.2 Pagar con Tarjeta Guardada

```csharp
// ReservArte.Application/Services/RedsysPaymentService.cs
public async Task<RedsysPaymentResult> PayWithSavedCardAsync(
    Appointment appointment,
    Guid paymentMethodId)
{
    var paymentMethod = await _paymentMethodRepository.GetByIdAsync(paymentMethodId);
    
    if (paymentMethod == null || paymentMethod.CustomerId != appointment.CustomerId)
        throw new UnauthorizedAccessException("Tarjeta no válida");

    var organization = await _organizationRepository.GetByIdAsync(appointment.OrganizationId);
    var secretKey = await GetSecretKeyAsync(organization.Id);

    var merchantParams = new Dictionary<string, object>
    {
        { "DS_MERCHANT_ORDER", appointment.RedsysOrderNumber },
        { "DS_MERCHANT_MERCHANTCODE", organization.RedsysMerchantCode },
        { "DS_MERCHANT_TERMINAL", organization.RedsysTerminal },
        { "DS_MERCHANT_TRANSACTIONTYPE", "1" }, // Pre-autorización
        { "DS_MERCHANT_AMOUNT", ((int)(appointment.TotalPrice * 100)).ToString() },
        { "DS_MERCHANT_CURRENCY", "978" },
        // USAR TOKEN GUARDADO
        { "DS_MERCHANT_IDENTIFIER", paymentMethod.RedsysToken },
        { "DS_MERCHANT_COF_INI", "N" }, // No es inicio, es uso subsiguiente
        { "DS_MERCHANT_COF_TYPE", "R" },
        { "DS_MERCHANT_COF_TXNID", paymentMethod.RedsysCofTxnid }
    };

    var result = await ExecuteRedsysRequestAsync(
        merchantParams,
        secretKey,
        organization.RedsysEnvironment,
        appointment,
        "Authorized"
    );

    if (result.IsSuccess)
    {
        // Actualizar fecha de último uso
        paymentMethod.LastUsedAt = DateTime.UtcNow;
        await _paymentMethodRepository.UpdateAsync(paymentMethod);
    }

    return result;
}
```

---

### 7.5 Webhook de Redsys

```csharp
// ReservArte.API/Controllers/PaymentsController.cs
[HttpPost("redsys/webhook")]
[AllowAnonymous] // Redsys llama sin autenticación
public async Task<IActionResult> RedsysWebhook()
{
    try
    {
        // Leer parámetros del webhook
        var merchantParameters = Request.Form["Ds_MerchantParameters"].ToString();
        var signature = Request.Form["Ds_Signature"].ToString();
        var signatureVersion = Request.Form["Ds_SignatureVersion"].ToString();

        if (string.IsNullOrEmpty(merchantParameters) || string.IsNullOrEmpty(signature))
            return BadRequest("Parámetros incompletos");

        // Decodificar parámetros
        var decodedParams = DecodeRedsysParameters(merchantParameters);
        var orderNumber = decodedParams["Ds_Order"].ToString();
        var merchantCode = decodedParams["Ds_MerchantCode"].ToString();

        // Obtener organización
        var organization = await _organizationRepository
            .GetByMerchantCodeAsync(merchantCode);

        if (organization == null)
        {
            _logger.LogWarning($"Organización no encontrada para código: {merchantCode}");
            return BadRequest("Comercio no encontrado");
        }

        // Validar firma
        var secretKey = await _redsysService.GetSecretKeyAsync(organization.Id);
        if (!ValidateRedsysSignature(merchantParameters, signature, secretKey))
        {
            _logger.LogWarning("Firma de webhook Redsys inválida");
            return BadRequest("Firma inválida");
        }

        // Buscar cita
        var appointment = await _appointmentRepository.GetByRedsysOrderAsync(orderNumber);
        
        if (appointment == null)
        {
            _logger.LogWarning($"Cita no encontrada para orden: {orderNumber}");
            return NotFound("Pedido no encontrado");
        }

        // Procesar según código de respuesta
        var responseCode = decodedParams["Ds_Response"].ToString();
        var isSuccess = int.TryParse(responseCode, out int code) && code <= 99;

        if (isSuccess)
        {
            // Pago exitoso
            appointment.Status = AppointmentStatus.Confirmed;
            appointment.RedsysAuthCode = decodedParams["Ds_AuthorisationCode"]?.ToString();
            
            await _appointmentRepository.UpdateAsync(appointment);

            // Registrar log de transacción
            await LogRedsysTransaction(
                organization.Id,
                appointment.Id,
                orderNumber,
                "PreAuth",
                decodedParams,
                true,
                null
            );

            _logger.LogInformation($"Webhook: Pre-autorización exitosa para cita {appointment.Id}");
        }
        else
        {
            // Pago fallido
            appointment.Status = AppointmentStatus.PaymentFailed;
            await _appointmentRepository.UpdateAsync(appointment);

            await LogRedsysTransaction(
                organization.Id,
                appointment.Id,
                orderNumber,
                "PreAuth",
                decodedParams,
                false,
                GetRedsysErrorMessage(responseCode)
            );

            _logger.LogWarning($"Webhook: Pre-autorización fallida. Código: {responseCode}");
        }

        return Ok(); // Siempre devolver 200 a Redsys
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error procesando webhook de Redsys");
        return StatusCode(500); // Redsys reintentará
    }
}

private async Task LogRedsysTransaction(
    Guid organizationId,
    Guid appointmentId,
    string orderNumber,
    string transactionType,
    Dictionary<string, object> responseParams,
    bool isSuccess,
    string errorMessage)
{
    var log = new RedsysTransactionLog
    {
        OrganizationId = organizationId,
        AppointmentId = appointmentId,
        RedsysOrderNumber = orderNumber,
        TransactionType = transactionType,
        ResponseParams = JsonSerializer.Serialize(responseParams),
        ResponseCode = responseParams["Ds_Response"]?.ToString(),
        IsSuccess = isSuccess,
        ErrorMessage = errorMessage,
        CreatedAt = DateTime.UtcNow
    };

    await _redsysLogRepository.AddAsync(log);
}
```

---

### 7.6 Manejo de Cancelaciones con Penalización

```csharp
// ReservArte.Application/Services/AppointmentService.cs
public async Task<bool> CancelAppointmentAsync(Guid appointmentId, string reason)
{
    var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
    
    if (appointment == null)
        return false;

    var settings = await _organizationSettingsRepository
        .GetByOrganizationIdAsync(appointment.OrganizationId);

    var hoursUntilAppointment = (appointment.AppointmentDate.AddHours(appointment.StartTime.TotalHours) - DateTime.UtcNow).TotalHours;

    // Determinar si hay penalización
    var shouldPenalize = hoursUntilAppointment < settings.CancellationHoursThreshold;

    if (shouldPenalize && settings.CancellationPenaltyPercentage > 0)
    {
        // Capturar penalización
        var penaltyAmount = appointment.TotalPrice * (settings.CancellationPenaltyPercentage / 100);
        
        var captureResult = await _redsysService.CaptureAsync(appointment, penaltyAmount);

        if (!captureResult.IsSuccess)
        {
            _logger.LogError($"Error capturando penalización para cita {appointmentId}");
            // Continuar con la cancelación de todos modos
        }
    }
    else
    {
        // Cancelar pre-autorización completa (liberar fondos)
        await _redsysService.CancelAsync(appointment);
    }

    // Actualizar estado de cita
    appointment.Status = AppointmentStatus.Cancelled;
    appointment.CancellationReason = reason;
    appointment.CancelledAt = DateTime.UtcNow;
    
    await _appointmentRepository.UpdateAsync(appointment);

    // Enviar notificación al cliente
    await _notificationService.SendCancellationConfirmationAsync(appointment);

    return true;
}
```

---

## 8. SISTEMA DE NOTIFICACIONES

### 8.1 Notificaciones por Email (Amazon SES)

#### 8.1.1 Servicio de Email

**Contrato real (RA-869eq5tg3; invitación RA-869f17y68):** `ReservArte.Application.Interfaces.IEmailService`. Una sola operación: `Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)`. `EmailMessage` lleva `To`, `Subject`, `Body`, `IsHtml`. El cuerpo se **construye en código** (reset e invitación en **texto plano**); **no** hay plantillas nativas de proveedor en el contrato. SES (u otro) será una **implementación futura de esta misma interfaz** (**RA-869d7f65a**): plantilla HTML **común** y personalización por tenant (nombre/marca), no un segundo contrato con `SendTemplatedEmailAsync`.

**DI por entorno (arranque seguro):**
- **Development:** `DevFileEmailService` — escribe cada mensaje a `./sent-emails/` (relativo al directorio de ejecución; fuera de git). El log no incluye el cuerpo (el token de reset **ni el de invitación** van a logs).
- **Fuera de Development:** si aún no hay implementación real (SES pendiente), `AuthServiceExtensions` **lanza al arrancar** `InvalidOperationException` con mensaje claro. Evita un host que arranca y luego falla al resolver `AuthService` en la primera petición. Cuando exista SES: sustituir ese `throw` por `AddScoped<IEmailService, SesEmailService>()`.

Flujo de reset: vol. 1 **§4.4.1**.

```csharp
// ReservArte.Application/Interfaces/IEmailService.cs — contrato activo
public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

// Development: DevFileEmailService escribe To/Subject/Body a ./sent-emails/
// Producción (futuro): SesEmailService : IEmailService — mismo SendAsync;
//   el HTML o texto plano ya viene en message.Body (sin Template de SES).
```

```csharp
// AuthServiceExtensions — registro por entorno (fail-fast fuera de Development)
if (environment.IsDevelopment())
{
    services.AddScoped<IEmailService, DevFileEmailService>();
}
else
{
    // TODO(SES): services.AddScoped<IEmailService, SesEmailService>();
    throw new InvalidOperationException(
        "No hay proveedor de IEmailService configurado para este entorno. " +
        "Configura SES (o el proveedor correspondiente) antes de desplegar fuera de Development.");
}
```

#### 8.1.2 Plantillas de Email

```csharp
// ReservArte.Application/Services/EmailTemplateService.cs
public class EmailTemplateService
{
    public string GenerateAppointmentReminderHtml(AppointmentReminderData data)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ background-color: #4F46E5; padding: 30px; text-align: center; }}
        .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; }}
        .content {{ padding: 40px 30px; }}
        .appointment-card {{ background-color: #F9FAFB; border-left: 4px solid #4F46E5; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .detail-row {{ margin: 12px 0; font-size: 16px; color: #374151; }}
        .detail-label {{ font-weight: 600; color: #1F2937; }}
        .button {{ display: inline-block; padding: 14px 32px; background-color: #4F46E5; color: #ffffff; text-decoration: none; border-radius: 6px; margin: 10px 5px; font-weight: 500; }}
        .button-secondary {{ background-color: #6B7280; }}
        .footer {{ background-color: #F9FAFB; padding: 20px; text-align: center; font-size: 12px; color: #6B7280; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🗓️ Recordatorio de Cita</h1>
        </div>
        <div class=""content"">
            <p style=""font-size: 16px; color: #374151;"">Hola <strong>{data.CustomerName}</strong>,</p>
            <p style=""font-size: 16px; color: #374151;"">Te recordamos tu próxima cita:</p>
            
            <div class=""appointment-card"">
                <div class=""detail-row"">
                    <span class=""detail-label"">📅 Fecha:</span> {data.AppointmentDate:dddd, dd MMMM yyyy}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">🕐 Hora:</span> {data.AppointmentTime:HH:mm}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">💅 Servicio:</span> {data.ServiceName}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">👤 Especialista:</span> {data.EmployeeName}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">📍 Dirección:</span> {data.LocationAddress}
                </div>
            </div>

            <p style=""font-size: 14px; color: #6B7280; margin-top: 20px;"">
                <strong>Importante:</strong> Si necesitas cancelar, hazlo con al menos {data.CancellationHoursThreshold} horas de antelación para evitar cargos.
            </p>

            <div style=""text-align: center; margin-top: 30px;"">
                <a href=""{data.ConfirmUrl}"" class=""button"">✅ Confirmar Asistencia</a>
                <a href=""{data.CancelUrl}"" class=""button button-secondary"">❌ Cancelar Cita</a>
            </div>

            <div style=""margin-top: 30px; text-align: center;"">
                <a href=""{data.AddToCalendarUrl}"" style=""color: #4F46E5; text-decoration: none; font-size: 14px;"">
                    📆 Añadir al calendario
                </a>
            </div>
        </div>
        <div class=""footer"">
            <p>ReservArte - Tu centro de diseño de cejas</p>
            <p>Si no solicitaste esta cita, por favor ignora este email.</p>
        </div>
    </div>
</body>
</html>
        ";
    }

    public string GenerateAppointmentConfirmationHtml(AppointmentConfirmationData data)
    {
        // Similar estructura...
    }

    public string GenerateCancellationConfirmationHtml(CancellationData data)
    {
        // Similar estructura...
    }
}

public class AppointmentReminderData
{
    public string CustomerName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan AppointmentTime { get; set; }
    public string ServiceName { get; set; }
    public string EmployeeName { get; set; }
    public string LocationAddress { get; set; }
    public int CancellationHoursThreshold { get; set; }
    public string ConfirmUrl { get; set; }
    public string CancelUrl { get; set; }
    public string AddToCalendarUrl { get; set; }
}
```

---

### 8.2 Notificaciones por WhatsApp

#### 8.2.1 Servicio de WhatsApp (360dialog)

```csharp
// ReservArte.Infrastructure/Services/WhatsAppService.cs
using System.Net.Http.Json;

namespace ReservArte.Infrastructure.Services
{
    public interface IWhatsAppService
    {
        Task<bool> SendTemplateMessageAsync(
            string phoneNumber,
            string templateName,
            string languageCode,
            params string[] parameters);
    }

    public class WhatsAppService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://waba.360dialog.io/v1/messages";

        public WhatsAppService(
            IHttpClientFactory httpClientFactory,
            ILogger<WhatsAppService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _apiKey = configuration["WhatsApp:ApiKey"];
            _httpClient.DefaultRequestHeaders.Add("D360-API-KEY", _apiKey);
        }

        public async Task<bool> SendTemplateMessageAsync(
            string phoneNumber,
            string templateName,
            string languageCode,
            params string[] parameters)
        {
            try
            {
                // Formatear número de teléfono (debe incluir código de país)
                var formattedPhone = FormatPhoneNumber(phoneNumber);

                var requestBody = new
                {
                    messaging_product = "whatsapp",
                    to = formattedPhone,
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = languageCode },
                        components = new[]
                        {
                            new
                            {
                                type = "body",
                                parameters = parameters.Select(p => new 
                                { 
                                    type = "text", 
                                    text = p 
                                }).ToArray()
                            }
                        }
                    }
                };

                var response = await _httpClient.PostAsJsonAsync(_apiUrl, requestBody);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error WhatsApp API: {responseContent}");
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<WhatsAppResponse>();
                
                _logger.LogInformation($"WhatsApp enviado a {phoneNumber}. MessageId: {result.Messages[0].Id}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando WhatsApp a {phoneNumber}");
                return false;
            }
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            // Eliminar espacios, guiones, paréntesis
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Si no empieza con código de país, asumir España (+34)
            if (!cleaned.StartsWith("34") && cleaned.Length == 9)
            {
                cleaned = "34" + cleaned;
            }

            return cleaned;
        }
    }

    public class WhatsAppResponse
    {
        public List<WhatsAppMessage> Messages { get; set; }
    }

    public class WhatsAppMessage
    {
        public string Id { get; set; }
    }
}
```

#### 8.2.2 Plantillas de WhatsApp (Crear en Meta Business)

```
Nombre: recordatorio_cita_24h
Categoría: UTILITY
Idioma: Spanish (Spain)

Contenido:
---
Hola {{1}}, te recordamos tu cita de {{2}} mañana {{3}} a las {{4}} con {{5}} en {{6}}.

Si necesitas cancelar, hazlo con al menos 24h de antelación para evitar cargos.

¿Confirmas tu asistencia?
---

Botones:
- Sí, confirmo (Quick Reply)
- Cancelar cita (Quick Reply)

Variables:
{{1}} = Nombre cliente
{{2}} = Nombre servicio
{{3}} = Fecha
{{4}} = Hora
{{5}} = Nombre empleado
{{6}} = Dirección
```

---

### 8.3 Servicio de Recordatorios Automatizados

```csharp
// ReservArte.Application/Services/ReminderService.cs
using Hangfire;

namespace ReservArte.Application.Services
{
    public interface IReminderService
    {
        Task ScheduleRemindersForAppointmentAsync(Guid appointmentId);
        Task SendReminderAsync(Guid appointmentId, Guid reminderConfigId);
    }

    public class ReminderService : IReminderService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IReminderConfigRepository _reminderConfigRepository;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(
            IAppointmentRepository appointmentRepository,
            IReminderConfigRepository reminderConfigRepository,
            IEmailService emailService,
            IWhatsAppService whatsAppService,
            ILogger<ReminderService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _reminderConfigRepository = reminderConfigRepository;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        public async Task ScheduleRemindersForAppointmentAsync(Guid appointmentId)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId);
            
            if (appointment == null)
                return;

            var configs = await _reminderConfigRepository
                .GetByOrganizationIdAsync(appointment.OrganizationId);

            var appointmentDateTime = appointment.AppointmentDate
                .Add(appointment.StartTime);

            foreach (var config in configs.Where(c => c.IsActive))
            {
                var reminderTime = appointmentDateTime
                    .AddHours(-config.HoursBeforeAppointment);

                // Solo programar si el recordatorio es en el futuro
                if (reminderTime > DateTime.UtcNow)
                {
                    BackgroundJob.Schedule(
                        () => SendReminderAsync(appointmentId, config.Id),
                        reminderTime
                    );

                    _logger.LogInformation(
                        $"Recordatorio programado para cita {appointmentId} a las {reminderTime}"
                    );
                }
            }
        }

        public async Task SendReminderAsync(Guid appointmentId, Guid reminderConfigId)
        {
            try
            {
                var appointment = await _appointmentRepository
                    .GetByIdAsync(appointmentId, includeCustomer: true, includeEmployee: true);

                if (appointment == null || appointment.Status == AppointmentStatus.Cancelled)
                {
                    _logger.LogInformation($"Cita {appointmentId} cancelada, no enviar recordatorio");
                    return;
                }

                var config = await _reminderConfigRepository.GetByIdAsync(reminderConfigId);
                var customer = appointment.Customer;

                // Preparar datos
                var reminderData = new AppointmentReminderData
                {
                    CustomerName = customer.FirstName,
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentTime = appointment.StartTime,
                    ServiceName = appointment.Services.First().Service.Name,
                    EmployeeName = $"{appointment.Employee.FirstName} {appointment.Employee.LastName}",
                    LocationAddress = appointment.Organization.Address,
                    CancellationHoursThreshold = appointment.Organization.Settings.CancellationHoursThreshold,
                    ConfirmUrl = $"https://app.reservarte.com/appointments/{appointment.Id}/confirm",
                    CancelUrl = $"https://app.reservarte.com/appointments/{appointment.Id}/cancel",
                    AddToCalendarUrl = GenerateICalUrl(appointment)
                };

                bool emailSent = false;
                bool whatsappSent = false;

                // Enviar por email
                if (config.Channel == "Email" || config.Channel == "Both")
                {
                    var emailTemplate = _emailTemplateService
                        .GenerateAppointmentReminderHtml(reminderData);

                    emailSent = await _emailService.SendEmailAsync(
                        customer.Email,
                        $"Recordatorio: Tu cita en {appointment.Organization.Name}",
                        emailTemplate
                    );
                }

                // Enviar por WhatsApp
                if (config.Channel == "WhatsApp" || config.Channel == "Both")
                {
                    if (customer.WhatsAppOptIn && !string.IsNullOrEmpty(customer.Phone))
                    {
                        whatsappSent = await _whatsAppService.SendTemplateMessageAsync(
                            customer.Phone,
                            "recordatorio_cita_24h",
                            "es",
                            customer.FirstName,
                            reminderData.ServiceName,
                            reminderData.AppointmentDate.ToString("dd/MM/yyyy"),
                            reminderData.AppointmentTime.ToString(@"hh\:mm"),
                            reminderData.EmployeeName,
                            reminderData.LocationAddress
                        );
                    }
                }

                // Registrar log
                await _reminderLogRepository.AddAsync(new ReminderLog
                {
                    AppointmentId = appointmentId,
                    ReminderConfigurationId = reminderConfigId,
                    Channel = config.Channel,
                    SentAt = DateTime.UtcNow,
                    Status = (emailSent || whatsappSent) ? "Sent" : "Failed"
                });

                _logger.LogInformation(
                    $"Recordatorio enviado para cita {appointmentId}. Email: {emailSent}, WhatsApp: {whatsappSent}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando recordatorio para cita {appointmentId}");
            }
        }

        private string GenerateICalUrl(Appointment appointment)
        {
            // Generar URL para archivo .ics
            return $"https://app.reservarte.com/api/v1/appointments/{appointment.Id}/calendar.ics";
        }
    }
}
```

---

## 9. SEGURIDAD Y PROTECCIÓN DE DATOS

### 9.1 Cifrado

#### 9.1.1 Cifrado en Tránsito

```csharp
// ReservArte.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Forzar HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

// Configurar HSTS
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

// Usar HTTPS redirect y HSTS
app.UseHttpsRedirection();
app.UseHsts();
```

#### 9.1.2 Cifrado en Reposo

**SQL Server (contenedor Docker / volumen persistente):**
- Cifrado del volumen de datos y del host (EBS cifrado, LUKS, BitLocker, etc.) según proveedor
- Opcionalmente **Transparent Data Encryption (TDE)** si la edición de SQL Server lo permite
- Copias de seguridad cifradas (`BACKUP` con `ENCRYPTION` en T-SQL) como práctica recomendada

```dockerfile
# Ejemplo: variables de entorno habituales en la imagen oficial (documentación Microsoft)
# ACCEPT_EULA=Y
# MSSQL_SA_PASSWORD=<contraseña segura>
# Volumen montado en /var/opt/mssql/data para persistencia
```

**Cloudinary (imágenes / medios):**
```csharp
// ReservArte.Infrastructure/Services/CloudinaryMediaService.cs
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

public class CloudinaryMediaService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryMediaService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["Cloudinary:CloudName"],
            configuration["Cloudinary:ApiKey"],
            configuration["Cloudinary:ApiSecret"]);
        _cloudinary = new Cloudinary(account);
    }

    public Task<string> UploadImageAsync(Stream fileStream, string fileName, string folder)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folder,
            Overwrite = false,
            UniqueFilename = true,
        };
        var result = _cloudinary.Upload(uploadParams);
        if (result.Error != null)
            throw new InvalidOperationException(result.Error.Message);
        return Task.FromResult(result.SecureUrl.ToString());
    }
}
```

#### 9.1.3 Cifrado de Contraseñas

El hashing lo realiza el **hasher oficial de ASP.NET Core Identity (PBKDF2)** registrado con `AddIdentityCore<User>()` — no BCrypt ni un `PasswordHashingService` propio. Alta y verificación vía `UserManager<User>`.

**Política de contraseñas del registro (dos capas coincidentes, RA-869epf0rt):**
- **(a) Contrato de API:** `RegisterRequestValidator` (FluentValidation) corre primero: mínimo **8** caracteres con mayúscula, minúscula, dígito y símbolo.
- **(b) Identity:** `CreateAsync` aplica `options.Password.RequiredLength = 8` (explícito) y los **defaults activos** (`RequireDigit`, `RequireUppercase`, `RequireLowercase`, `RequireNonAlphanumeric`).
- Las dos capas exigen lo mismo; no hay conflicto. El frontend **replica** esta política en **Zod** (`register.schema.ts`; `reset-password.schema.ts`; `set-password.schema.ts` **reexporta** el del reset, RA-869f17y68). Debe mantenerse alineada con los validadores FluentValidation (`RegisterRequestValidator`, `ResetPasswordRequestValidator`, `SetPasswordRequestValidator`). Detalle: vol. 1 **§4.4.1**; patrón VeeValidate: vol. 2 **§9.2.3**.

```csharp
// reservarte-api/Extensions/IdentityServiceExtensions.cs (fragmento)
services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true; // unicidad por organización (RA-869f1xc0u)
        options.Password.RequiredLength = 8;
    })
    .AddUserStore<OrganizationUserStore>()
    .AddDefaultTokenProviders();

// RA-869f1xc0u: OrganizationUserStore (no AddEntityFrameworkStores). UserLogin hereda
// OrganizationId de la cuenta en AddLoginAsync. Query filter + índices compuestos.

// RA-869f17y68: proveedor extra Invitation (SetPassword, 7 días).
// DataProtectionTokenProviderOptions es compartido: el reset sigue en 1 día.
// InvitationTokenProvider vive en ReservArte-API/Identity (no en Infrastructure).

// Alta de usuario con contraseña (Application / Auth)
var result = await _userManager.CreateAsync(user, password);
// CreateAsync hashea con IPasswordHasher<User> y persiste en AspNetUsers.PasswordHash
```

---

### 9.2 Autenticación y Autorización

#### 9.2.1 JWT Service

> **v2 (2026-07-07, RA-869d7eyze):** Adaptado a `User : IdentityUser<int>` (`Id` int, `Rol`, email nullable), `IOptions<JwtOptions>`, interfaz `IJwtTokenService` en Application y `GenerateAccessToken` con expiración `AccessTokenMinutes` (no hardcodeada).
>
> **v3 (2026-07-17, RA-869d7ez3e):** El claim de rol se emite con el nombre literal `"role"` (no `ClaimTypes.Role`). El **valor** es `user.Rol` (catálogo PascalCase, RA-869f18116). `JwtSecurityToken` escribe los claims sin mapeo corto: si se usara `ClaimTypes.Role`, el payload llevaría la URI larga `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. El registro de `AddJwtBearer` debe declarar `TokenValidationParameters.RoleClaimType = "role"` para que `[Authorize(Roles = ...)]` resuelva correctamente.
>
> **v4 (2026-07-21, RA-869d7eze3):** `AddJwtBearer` queda registrado en `AuthServiceExtensions.AddJwtAuthentication` (primer consumidor real de `[Authorize]`: `GET /api/v1/account/me` y `/api/v1/account/mfa/*`). Parámetros de validación **espejo** de `JwtTokenService.ValidateToken` (clave simétrica desde User Secrets / sección `Jwt`, `ValidIssuer`, `ValidAudience`, `ClockSkew = TimeSpan.Zero`), `RoleClaimType = "role"` y **`MapInboundClaims = false`** (sin este último, el middleware remapea `sub` a la URI larga de `ClaimTypes` y `FindFirstValue("sub")` falla). Paquete `Microsoft.AspNetCore.Authentication.JwtBearer` **8.0.0**. La coherencia del claim `organization_id` con el tenant resuelto se aplica en `TenantMiddleware` (vol. 1 §4.3.1).
>
> **v5 (2026-08-20, RA-869d7ezgy):** Login local con `TwoFactorEnabled` emite ticket MFA (`GenerateMfaTicket`: 5 min, claim `mfa_pending`, sin `role`). `OnTokenValidated` rechaza ese ticket en cualquier `[Authorize]`. Canje en `POST /api/v1/auth/mfa/verify`. `JwtTokenService.ValidateToken` fija **`MapInboundClaims = false`** en el `JwtSecurityTokenHandler` (imprescindible para leer `sub` del ticket; coherente con el JwtBearer). `AuthResponse` admite rama MFA (`MfaRequired` / `MfaTicket`, `User` nullable).

> **Política de versiones de paquetes de autenticación (dos familias, políticas opuestas):**
> - **(a) Familia ASP.NET Core** (`Microsoft.AspNetCore.Authentication.JwtBearer`, `.Google`, `.Facebook`, `.Apple` / `AspNet.Security.OAuth.Apple`, EF Core, Identity): versión **8.0.x**, atada al target **.NET 8**. Pedir un paquete ASP.NET Core **sin fijar versión** instala la **9.x** (net9.0), incompatible con este proyecto.
> - **(b) Familia `Microsoft.IdentityModel.*`** (`Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`): versión **8.14.0**, numeración **independiente** del target .NET. Las dependencias transitivas de AutoMapper/MediatR ya resuelven esa versión; fijarla en 8.0.0 provoca **NU1605**.

```csharp
// ReservArte.Application/Interfaces/IJwtTokenService.cs
public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Guid organizationId);
    string GenerateMfaTicket(User user, Guid organizationId);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}

// ReservArte.Application/DTOs/Auth/AuthResponse.cs — contrato de login/registro/verify
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto? User { get; set; }          // null en rama MFA pendiente
    public bool MfaRequired { get; set; }       // true → ticket, sin tokens
    public string? MfaTicket { get; set; }     // JWT 5 min, claim mfa_pending
}
// Login normal / tras verify: tokens + User, MfaRequired = false.
// Login con 2FA: MfaRequired = true + MfaTicket; AccessToken/RefreshToken vacíos, User null.

// ReservArte.Infrastructure/Options/JwtOptions.cs — sección "Jwt" (vol. 1 §5.1.3)
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
}

// ReservArte.Infrastructure/Services/JwtTokenService.cs
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class JwtTokenService : IJwtTokenService
{
    public const string OrganizationIdClaimType = "organization_id";
    public const string MfaPendingClaimType = "mfa_pending";
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(User user, Guid organizationId)
    {
        // User : IdentityUser<int> — Id es int; el claim sub sigue siendo user.Id.ToString()
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(OrganizationIdClaimType, organizationId.ToString()),
            new("role", user.Rol), // literal "role"; ver nota v3 (RoleClaimType en JwtBearer)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateMfaTicket(User user, Guid organizationId)
    {
        // Claims mínimos: sub + organization_id + mfa_pending. SIN role.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(OrganizationIdClaimType, organizationId.ToString()),
            new(MfaPendingClaimType, "true"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        // ... firma igual; expires = UtcNow + 5 min
        return /* JWT */;
    }

    public string GenerateRefreshToken()
    {
        // Token opaco de 64 bytes (no JWT); se persistirá en §9.2.2
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        // MapInboundClaims = false: imprescindible para leer "sub" del ticket
        // MFA; coherente con el mismo flag del JwtBearer.
        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

// ReservArte.API/Extensions/AuthServiceExtensions.cs (registro DI)
services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
services.AddScoped<IJwtTokenService, JwtTokenService>();
// LegalDocuments: AddOptions + Validate (versiones no vacías) + ValidateOnStart
// (vol. 1 §5.1.3). Sin TermsVersion/PrivacyVersion de entorno, la API no arranca.
// Lectura: GET /api/v1/legal/versions (RA-869epmbfm; v1 global y exento de tenant;
// Fase 3 por org: retirar la exención en TenantExemptApiPaths).
```

**Login social (OAuth 2.0 / OpenID Connect) y el mismo JWT**

El backend registra esquemas externos acordados: **`AddGoogle`**, **`AddFacebook`** (Meta / **Instagram Login** según configuración en Meta Developers), **`Apple`** (handler OAuth/OIDC para Sign in with Apple, p. ej. `AspNet.Security.OAuth.Apple`). Tras el **callback** del IdP, un controlador o manejador usa `UserManager` / `SignInManager` para **crear o enlazar** el usuario y persistir la fila en **`AspNetUserLogins`**. El login local con 2FA ya exige `POST /api/v1/auth/mfa/verify`; **el flujo social aún no**: `ExternalLoginAsync` emite tokens aunque `TwoFactorEnabled` sea true (ampliación pendiente). Cuando se emiten tokens, se usa el **mismo** `IJwtTokenService.GenerateAccessToken` (y el flujo de refresh) que en `POST /api/v1/auth/login`.

```csharp
// ReservArte.API/Extensions/AuthServiceExtensions.cs — registro JwtBearer (RA-869d7eze3 + RA-869d7ezgy)
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Imprescindible: sin esto, "sub" no se lee por su clave corta.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)), // Jwt:SecretKey (User Secrets)
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
        };

        options.Events = new JwtBearerEvents
        {
            // Ticket mfa_pending: JWT válido en firma, pero no autoriza [Authorize].
            OnTokenValidated = context =>
            {
                if (context.Principal?.FindFirst("mfa_pending") is not null)
                    context.Fail("El ticket de 2FA no autoriza esta operación.");
                return Task.CompletedTask;
            },
        };
    });

// Program.cs — orden del pipeline (coherencia tenant): UseAuthentication()
// ANTES de TenantMiddleware; luego UseAuthorization().
// Exenciones de tenant (TenantExemptApiPaths): webhook Redsys + GET /api/v1/legal/versions (v1).

// Esquemas externos (fragmento ilustrativo; registro condicional en AddExternalAuthentication)
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    // SecurePolicy = SameAsRequest: el default Secure rompe el flujo en HTTP
    // local (p. ej. Safari); en HTTPS el flag Secure vuelve solo.
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRemoteFailure = ctx =>
    {
        // Redirige a {origen permitido}/auth/callback#error=external_auth_failed
        // (cancelaciones / fallos de intercambio; sin filtrar el motivo al cliente)
        return Task.CompletedTask;
    };
})
.AddFacebook("Instagram", options => // esquema dedicado; OAuth de Meta (Instagram Login / permisos según app)
{
    options.AppId = builder.Configuration["Authentication:Meta:AppId"]!;
    options.AppSecret = builder.Configuration["Authentication:Meta:AppSecret"]!;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRemoteFailure = /* misma redirección SPA #error=external_auth_failed */;
});
// Sign in with Apple: instalar AspNet.Security.OAuth.Apple y usar la extensión .AddApple(...)
// (ClientSecret suele ser un JWT de corta duración generado con clave privada de Apple)
// Mismos endurecimientos: CorrelationCookie.SecurePolicy = SameAsRequest;
// Events.OnRemoteFailure → {origen permitido}/auth/callback#error=external_auth_failed
```

> **Nota operativa — consola Meta (desarrollo local, RA-869d7ezbm):** app en modo desarrollo con (a) «Dominios de la aplicación» = `localhost` y plataforma «Sitio web» con `http://localhost:5555/` (HTTP de `launchSettings.json`; **no usar 5000**, colisiona con AirPlay en macOS); (b) permiso **`email` añadido al caso de uso** (sin él Meta responde `Invalid Scopes: email` — el handler lo solicita por defecto y la lógica de vinculación lo exige); (c) la URI `http://localhost:5555/signin-facebook` **no** necesita registrarse (localhost permitido por defecto en desarrollo).

**Detalles de implementación (RA-869d7ez7e, 2026-07-18; Instagram RA-869d7ezbm, 2026-07-19):**
- **Registro condicional de proveedores:** sin credenciales, el handler OAuth aborta el arranque; con registro condicional la API arranca en cualquier máquina (solo se añaden los esquemas cuya configuración esté completa). `google` activo; `apple` implementado y latente; `instagram` **implementado y verificado** (esquema `"Instagram"` vía `AddFacebook`, paquete `Microsoft.AspNetCore.Authentication.Facebook` 8.0.0; `LoginProvider = "Instagram"`).
- Cookie externa `IdentityConstants.ExternalScheme` de **un solo uso** (se consume en el callback).
- **PKCE:** los handlers de Google **y Facebook** de .NET 8 emiten `code_challenge` S256 automáticamente; no requiere implementación propia.
- `CorrelationCookie.SameSite = Lax` para Google/Instagram (desarrollo HTTP; flujo redirect GET).
- **Endurecimientos transversales (los tres proveedores):** `CorrelationCookie.SecurePolicy = SameAsRequest` (el default `Secure` rompe el flujo en HTTP local con navegadores estrictos como Safari; en HTTPS el flag vuelve automáticamente) y `Events.OnRemoteFailure` → redirección a `{origen permitido}/auth/callback#error=external_auth_failed` (cancelaciones de consentimiento y fallos de intercambio aterrizan en la SPA, sin filtrar el motivo).
- Apple requiere **HTTPS** por su `form_post`; el `ClientSecret` se genera con `GenerateClientSecret` y la clave privada desde `Authentication:Apple:PrivateKey` (nunca en repositorio).

**2FA opcional (Identity) — RA-869d7eze3 + RA-869d7ezgy (2026-08-20):**
- Alta (`MfaController`): `enable` → secreto + `otpauthUri` + `manualEntryKey` (no activa); `confirm` → activa y devuelve **10 códigos de recuperación una sola vez** (hasheados en `AspNetUserTokens`; canje con `RedeemTwoFactorRecoveryCodeAsync`); `disable` → código TOTP + reset de secreto.
- Login local: si `TwoFactorEnabled`, `AuthResponse` con `MfaRequired` + `MfaTicket` (JWT 5 min, `mfa_pending`, sin `role`); canje en `POST /api/v1/auth/mfa/verify` (TOTP o recuperación) → tokens definitivos. El ticket se rechaza en `[Authorize]` (`OnTokenValidated`). SPA: `MfaVerifyPage` (§9.2.3).
- QR como URI `otpauth://` (el frontend la renderiza); secreto `AuthenticatorKey` cifrado por Data Protection.
- **Pendiente de seguridad conocido (no bloqueante):** `VerifyMfaAsync` acepta el mismo TOTP durante toda su ventana temporal (estándar; rechazado tras varias ventanas). Endurecimiento futuro: invalidar tras el primer uso. Candidato para **RA-869en8a17**. Los códigos de recuperación ya son de un solo uso.

> **Secuenciación:** el 2FA sobre **login social** (OAuth) **no** está implementado: `ExternalLoginAsync` emite el par de tokens definitivo aunque el usuario tenga `TwoFactorEnabled`. **No es el comportamiento deseado.** Ampliación: **RA-869f151x1** (emitir ticket `mfa_pending` en el fragmento). El login **local** sí atraviesa el gate. SPA del retorno: §9.2.3 (`OAuthCallbackPage`).

En la práctica (decisión RA-869d7ez7e, 2026-07-18), el flujo «challenge → IdP → callback → tokens al cliente» termina con **redirección final a la SPA**: `returnUrl` se **valida contra `Cors:AllowedOrigins`** (anti open-redirect) y los tokens viajan en el **fragmento de URL** (`#...`), de modo que no llegan al servidor ni a logs de acceso. Un **código de un solo uso** intercambiable por tokens queda documentado como endurecimiento futuro; la cookie de correlación de ASP.NET Core sigue usándose durante el round-trip con el IdP.

#### 9.2.2 Refresh Token Service

> **v2 (2026-07-17, RA-869d7ez3e):** Modelo alineado con la implementación real. Tabla `RefreshTokens`: `UserId` es `int` (FK a `AspNetUsers`, borrado en cascada), índice **único** sobre `Token` (`nvarchar(200)`), `CreatedByIp` `nvarchar(45)`. En cada uso hay **rotación**: el token consumido se marca `IsRevoked = true` y se emite un par access+refresh nuevo en la misma operación (`AuthService.RefreshTokenAsync` / `IssueTokensAsync`). **Verificado en runtime (2026-09-13):** reutilizar el refresh ya rotado se rechaza.

```csharp
// ReservArte.Domain/Entities/RefreshToken.cs
public class RefreshToken
{
    public Guid Id { get; set; }
    public int UserId { get; set; } // FK a AspNetUsers (IdentityUser<int>), cascada
    public string Token { get; set; } // nvarchar(200), índice único
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? CreatedByIp { get; set; } // nvarchar(45); cabe IPv6
    public User User { get; set; }
}

// Fragmento ilustrativo del flujo de rotación (la implementación real
// vive en AuthService y responde con el envelope §5.1.1).
public class TokenRefreshService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtTokenService _jwtTokenService;

    public async Task<(string AccessToken, string RefreshToken)> RefreshTokensAsync(
        string refreshToken,
        string ipAddress)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null || 
            storedToken.IsRevoked || 
            storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new SecurityException("Invalid refresh token");
        }

        // Rotación: revocar el token usado e emitir un par nuevo
        storedToken.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(storedToken);

        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        var accessToken = _jwtTokenService.GenerateToken(user, user.OrganizationId);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        return (accessToken, newRefreshToken);
    }
}
```

#### 9.2.3 Patrón de páginas de autenticación (SPA) — RA-869d7f7kn (2026-08-23); MFA RA-869d7f7vw (2026-08-24); registro RA-869d7fbhg (2026-08-25); recuperación RA-869d7fbmy (2026-08-27); retorno OAuth RA-869d7f7r1 (2026-09-12)

Patrón establecido en el frontend (`reservarte-web`):

- **Página contenedora** (`pages/auth/*Page.vue`, p. ej. `LoginPage.vue`, `RegisterPage.vue`): orquesta lógica, estado (carga, error, umbral CAPTCHA, hidratación de `authStore`) y navegación.
- **Componente de presentación** (`components/ui/*`, p. ej. `LoginForm.vue`, `RegisterForm.vue`): sin llamadas HTTP; emite eventos (`submit`, `oauth`, `captchaVerified`).
- **API:** `features/auth/api/auth.api.ts` desenvuelve el envelope `{ success, data, error, meta }` y traduce fallos a `AuthApiError` tipado con `code`.
- **Cliente HTTP (`src/lib/api/client.ts`, RA-869f18urw; comentarios PR #44–#45):** enumeración, no una sola regla.
  1. **401** en endpoint protegido → fin de sesión **por status**. Exceptuados `AUTH_ENDPOINTS_WITHOUT_SESSION` (`login`, `mfa/verify`, `refresh-token`, `set-password`, **`reset-password`**): 401 de negocio **con** envelope. `endSession()` redirige **aunque no haya sesión**; un 401 de reset caducado mandaba a `/login` antes de leer el mensaje (**RA-869f1m12x**, PR #52). **Advertencia:** otro 401 de negocio futuro hay que exceptuarlo, o discriminar por `error.code` como en el 403. Hoy los únicos 401 de negocio son esos cinco.
  2. **403** → fin de sesión **solo si** `error.code` ∈ `SESSION_ENDING_ERROR_CODES` (hoy `ORG_TENANT_MISMATCH`, envelope de `TenantMiddleware`).
  3. Códigos de **permiso no entran** en esa lista. **`GEN_FORBIDDEN` no cierra sesión** (RA-869f1anz3, 2026-09-14): el 403 de `[Authorize(Roles)]` y el de las reglas de `EmployeeService` van **con** envelope. El spec E2E guarda un caso «403 sin cuerpo» como robustez ante proxies/WAF; la API ya no lo emite.
- **`endSession()`:** borra `localStorage['authToken']` y navega con **`window.location.href`**, no `router.push`, **a propósito**. La recarga descarta el estado en memoria de Pinia, de modo que **no** llama a `authStore.logout()` (eso ataría `client` al store y crearía `client → store → client`). Queda en el código: si algún día se pasa a navegación SPA sin recarga, **entonces** hay que limpiar el store explícitamente. No se refactoriza hoy por un riesgo hipotético.
- **Theming:** solo variables CSS / clases Tailwind ligadas a tokens (`bg-primary`, `text-foreground`, etc.); **sin colores literales**. Tres deudas **distintas** (detalle en [`accessibility-and-i18n.md`](accessibility-and-i18n.md) §5): (1) modo **claro** parcialmente adaptado — restos shadcn en tokens secundarios de `:root` → **RA-869f0w7r2**; `--destructive` no es deuda; (2) modo **oscuro** (`.dark`) placeholder de plantilla, **no diseñado** → **RA-869f0w75h**; (3) contraste de `--primary` → **RA-869f0v6vm**. No absorber (1) en (2).

**Validación de formularios (estándar, RA-869d7fbhg + RA-869d7fbmy + RA-869f17y68):** **VeeValidate + Zod**, composition API (`useForm` / `useField`) y `toTypedSchema`. Los esquemas viven junto al feature (`features/auth/validation/`: `register.schema.ts`, `forgot-password.schema.ts`, `reset-password.schema.ts`; **`set-password.schema.ts` reexporta el del reset**). Estrenado en **RegisterPage**; Forgot/Reset/SetPassword usan el mismo patrón. La política de `register.schema.ts` y `reset-password.schema.ts` **replica** `RegisterRequestValidator` / `ResetPasswordRequestValidator` / `SetPasswordRequestValidator` y **debe mantenerse alineada** con el backend (`acceptedDataProcessing` obligatorio en registro, RA-869f1xc2n). **Pendiente (backlog):** migrar `LoginForm` a este patrón.

**Layout de páginas de auth:** `LoginPage`, `MfaVerifyPage`, `RegisterPage`, `ForgotPasswordPage`, `ResetPasswordPage` y **`SetPasswordPage`** montan el componente `Banner` directamente + contenido centrado (**no** usan `AuthLayout`). **Excepción:** `OAuthCallbackPage` (`/auth/callback`) **no** monta `Banner` (pantalla de tránsito de milisegundos, centrada en viewport completo). Ninguna monta `BottomNav` propio: la barra inferior es **global** (`App.vue`, §9.2.4).

> **Pendiente — rol de `AuthLayout`:** ninguna página de auth lo consume (Forgot/Reset también usan Banner). Deuda de retirada junto con `DashboardLayout` (reconciliación de layouts, backlog).

**Verificación 2FA en SPA (`MfaVerifyPage`, `/login/two-factor`) — RA-869d7f7vw (2026-08-24):**
- Flujo backend de verificación: vol. 1 **§4.4.1** (ticket `mfa_pending` → `POST /api/v1/auth/mfa/verify`).
- Consume el `mfaTicket` del `authStore` (dejado por el login cuando `mfaRequired`).
- Llama a `verifyMfa` → `POST /api/v1/auth/mfa/verify` con el código en un **único campo** (TOTP o código de recuperación).
- Completa la sesión con `authStore.setMfaVerified()` (persiste el par access/refresh del verify y limpia el ticket).
- **Guard de acceso directo:** sin `mfaTicket` en el store → redirección a login.

**Registro en SPA (`RegisterPage`, `/register`) — RA-869d7fbhg (2026-08-25); `acceptedDataProcessing` RA-869f1xc2n (2026-09-15):**
- Al montar pide **`GET /api/v1/legal/versions`**. Sin versiones no se registra (no hay consentimiento a ciegas).
- `RegisterForm`: **tres** checkboxes obligatorios (términos + privacidad + tratamiento de datos). Enlaces a **`/legal/terminos`** y **`/legal/privacidad`**: rutas **públicas** (nivel superior del router, **sin** `requiresAuth`, fuera de `DashboardLayout`). Motivo: se consultan en el registro **sin sesión**; el consentimiento informado exige acceso público. Siguen siendo **stubs**; el contenido real de los documentos es trabajo futuro. El tercero («Acepto el tratamiento de mis datos para gestionar mis citas.», `acceptedDataProcessing`) **no** tiene enlace.
- Envío a `POST /api/v1/auth/register` con flags de consentimiento (incl. `acceptedDataProcessing: true`) **y** las versiones vigentes cargadas. El alta crea ficha `Customer` **`new`** (mismo Id que la cuenta; RA-869d7f369; en PR #59 era `regular`). Tras el alta, **login automático** (`authStore.login` con tokens + user) y navegación a `my-appointments` (`/mis-citas`). El alta nace con rol **`Customer`** (RA-869f18116): no es personal; cuando existan guards por rol, el destino natural es la zona de cliente, no el backoffice.
- Contrato backend, RGPD y catálogo de roles: vol. 1 **§4.4.1**. `UserRole` en `auth.types.ts` (`'Admin' | 'Manager' | 'Employee' | 'Customer'`) espeja `Roles.cs`; lo usan `authStore` y el DTO de `/account/me` (el rol **deja de** tiparse como `string`).

**Recuperación de contraseña en SPA — RA-869d7fbmy (2026-08-27):** backend vol. 1 **§4.4.1** (RA-869eq5tg3).

- **`ForgotPasswordPage` (`/forgot-password`):** formulario de email (`ForgotPasswordForm` + `forgot-password.schema.ts`). Tras `POST /api/v1/auth/forgot-password` muestra un estado **«enviado»** genérico (anti-enumeración: el mismo mensaje exista o no la cuenta).
- **`ResetPasswordPage` (`/reset-password/:token?`):** el token es **opcional** en la ruta a propósito: sin param se muestra **«enlace no válido»** (no el formulario; cubierto en E2E). Con token: formulario email + nueva contraseña + confirmación (`ResetPasswordForm` + `reset-password.schema.ts`, VeeValidate+Zod; política = `ResetPasswordRequestValidator`). Éxito → mensaje y enlace a login.
- **Contrato del token (RA-869f18rp7):** enlace del email = segmento **URL-encoded**; Vue Router **decodifica** `route.params.token`; el POST lleva el token **en claro, una sola decodificación**. El comentario anterior en `auth.api.ts` («viaja tal cual, ya URL-encoded») era **falso**. Backend: `Uri.UnescapeDataString` se conserva como **tolerancia** (inocuo sobre base64). Spec `e2e/reset-password.spec.ts` (token con `+` `/` `=`; enlace sin token; **RA-869f1m12x:** enlace caducado muestra el error y **no** manda a `/login`).
- Ambas páginas: patrón Banner, no `AuthLayout`. **Runtime (2026-09-13):** forgot → `DevFileEmailService` → reset, primera vez. Ese camino **no** está en Playwright (el spec intercepta el POST). Backlog: **RA-869f18uta**. `reset-password` está en `AUTH_ENDPOINTS_WITHOUT_SESSION` (mismo motivo que `set-password`: `endSession()` redirige aunque no haya sesión).

**Invitación / primera contraseña en SPA (`SetPasswordPage`, `/set-password/:token?`) — RA-869f17y68 (2026-09-14):** backend vol. 1 **§4.4.1**. Textos de **alta** («Crea tu contraseña», «Guardar contraseña», «Contraseña creada»), no de recuperación. Sin token → «Enlace no válido» + pedir reenvío al centro. `setPassword()` en `auth.api.ts`. Mismo contrato de token que el reset. `set-password` está en `AUTH_ENDPOINTS_WITHOUT_SESSION` (el 401 de enlace caducado **no** manda a `/login`; `endSession()` redirige aunque no haya sesión). Spec `e2e/set-password.spec.ts` (3 tests × 3 navegadores). `AuthService.SetPasswordAsync` **no** tiene unitarios propios (el alta pública sí: `PublicSignupCustomerTests`, RA-869f1xc2n); cubierto en runtime.

**Retorno OAuth en SPA (`OAuthCallbackPage`, `/auth/callback`) — RA-869d7f7r1 (2026-09-12):**
- Es la `returnUrl` que la SPA envía en el challenge (`getOAuthChallengeUrl`) y la pantalla de aterrizaje del backend. Pantalla mínima: solo «Iniciando sesión…» con `role="status"`.
- Lee el **fragmento** de la URL (nunca query ni cuerpo) y distingue las dos formas que emite `ExternalAuthController.Callback`: `#access_token=…&refresh_token=…` (éxito) y `#error=<código>` (fallo; `OnRemoteFailure` ya documentado más arriba en este §9.2: `{origen permitido}/auth/callback#error=external_auth_failed`, sin filtrar el motivo).
- **Éxito:** `authStore.login({ accessToken, refreshToken, mfaRequired: false })`; como el fragmento no trae el usuario, se completa con `GET /api/v1/account/me`. Si `/me` falla **sin** código de fin de sesión —p. ej. 403 `GEN_FORBIDDEN` u otro código con envelope— la sesión **no** se aborta y se redirige a `/`. Si el código es `ORG_TENANT_MISMATCH`, el interceptor **sí** cierra la sesión. Spec `e2e/session-ending.spec.ts` (el caso «403 GEN_FORBIDDEN» es el 403 real de `[Authorize(Roles)]` desde RA-869f1anz3).
- **Fallo** (o fragmento sin tokens): redirige a **`/login?error=oauth_failed`** (criterio 5 de la tarea). Hay **dos vocabularios de error distintos y deliberados**: el backend expone su código en el fragmento (`external_auth_failed`, o el `ErrorCode` del dominio), y la SPA lo **colapsa** en un único `oauth_failed` de query, para no filtrar el motivo al usuario. `LoginPage` traduce ese único valor a un mensaje genérico al montar.
- **Seguridad / UX:** el fragmento se **consume al leerlo** (`window.history.replaceState`), de modo que los tokens no sobreviven en la barra de direcciones ni en el historial del navegador; se usa `router.replace` (no `push`) para que el botón Atrás no devuelva a un callback ya consumido.
- **Nota de contrato:** el criterio 3 original de la tarea (contemplar `mfaRequired` aquí y redirigir a `/login/two-factor`) **quedó obsoleto**: el backend no emite ese caso en el flujo externo. No se implementó esa rama (comentario en el código). Limitación conocida del contrato actual, **no** comportamiento deseado: el flujo social entrega tokens definitivos **sin** gate 2FA (vol. 1 **§4.4.1**). Ampliación **RA-869f151x1**, que añadirá un **tercer** formato de fragmento (ticket `mfa_pending`) y obligará a revisar este apartado.
- **Accesibilidad:** `role="alert"` en el mensaje de error de `LoginForm` (lo anuncian los lectores de pantalla al insertarse en el DOM tras la navegación) y `role="status"` en el callback.

#### 9.2.4 Navegación global (`BottomNav`) — RA-869ep9b52 (2026-08-24)

`BottomNav` es **navegación global y persistente**: se monta en `App.vue` (no por página ni por layout), **sticky** en la parte inferior. Tres destinos fijos, siempre visibles:

| Destino | Ruta (`name`) | Visibilidad / guard |
|---------|----------------|---------------------|
| Inicio | sin sesión → `login` (`/login`); con sesión → `my-appointments` (`/mis-citas`) | el destino del icono es **condicional** según `authStore` |
| Contacto | `contact` (`/contacto`) | **público** |
| Cuenta | `account` (`/cuenta`) | **requiere autenticación** (igual que `/mis-citas`) |

`/mis-citas`, `/contacto` y `/cuenta` son **stubs** (definidos en el router) hasta el contenido de sus módulos. Las rutas de documentos legales **`/legal/terminos`** y **`/legal/privacidad`** son **públicas** (sin `requiresAuth`; no van bajo `DashboardLayout`) porque se abren desde el registro. También son stubs. Fondo con token `bg-background` (blanco por defecto), pensado para configurarse por tenant más adelante.

**Diseño de navegación (fuente de verdad):** la aplicación **no tiene barra lateral**. El `BottomNav` de 3 destinos es la **única** navegación persistente. La gestión (Citas, Usuarios, Servicios, Empleados, Configuración, Datos de usuario, Métodos de pago, Notificaciones) se accede desde **`/cuenta`**, hub con:

- **Área de administración** + **Área de usuario** — roles `Admin` / `Manager` / `Employee`
- **Área de usuario** solamente — rol `Customer`

Las citas se gestionan desde la pantalla de Citas, no desde un menú lateral.

**Código vs diseño:** `DashboardLayout` (Sidebar + Header) **sí existe** en el repo y envuelve las rutas de `/` (dashboard, empleados, etc.). Fue una **licencia de implementación** (no el diseño). Su **retirada** está prevista como tarea de reconciliación de layouts (backlog). Hasta entonces, el código y el diseño divergen: documentar el diseño **sin sidebar**; no tratar el Sidebar como estado deseado.

**Corrección:** `LoginPage` dejó de montar su propio `BottomNav` (antes, 2 iconos). Todas las pantallas heredan la barra global de 3 destinos. **Incluye `/auth/callback`:** al montarse `BottomNav` en `App.vue`, la barra **también se pinta** en el retorno OAuth; es consecuencia esperada del patrón de navegación global, no un descuido de `OAuthCallbackPage`.

**Pendientes no bloqueantes:** (a) refinamiento visual del destino activo (el resalte `text-primary` / `isActive` actual es funcional y provisional); (b) color del `BottomNav` en preferencias de organización (theming futuro); (c) **deuda de layouts:** `AuthLayout.vue` (huérfano desde el patrón Banner) y `DashboardLayout`/`Sidebar` (no contemplados en diseño) pendientes de **retirada** en una tarea de reconciliación en backlog — no son el estado deseado.

---

### 9.3 Protección contra Ataques

#### 9.3.1 Rate Limiting

> **Implementación actual (2026-08-21, RA-869d7ezkp):** middleware nativo de **.NET 8** (`Microsoft.AspNetCore.RateLimiting`, `AddRateLimiter` + `UseRateLimiter` en `Program.cs`), no la librería `AspNetCoreRateLimit`. Políticas nombradas referenciadas con `[EnableRateLimiting]`:
> - `auth-login` — **10** peticiones / hora (`FixedWindowRateLimiter`, partición por IP) → `POST /api/v1/auth/login`
> - `auth-mfa-verify` — **20** peticiones / hora (misma ventana y partición) → `POST /api/v1/auth/mfa/verify`
>
> **Alcance real hoy:** solo esos dos endpoints. Rechazo → HTTP **429**, envelope con `error.code = GEN_RATE_LIMITED` y cabecera `Retry-After` cuando el limitador informa la espera. El contador es **in-memory por instancia**; multi-instancia requiere store distribuido o WAF (p. ej. AWS WAF, vol. 1 §4.4.3).
>
> **Pendientes conocidos** (tarea de seguimiento en backlog: **RA-869en8a17** — *«Refinamientos de auth: completar políticas de rate limiting + AUTH_MFA_INVALID en verify»*): políticas para `POST /api/v1/auth/register` (**5/día**), `GET /api/v1/auth/external/*/challenge` (**30/h**) y límite global `*` (**100/min**). Los números coinciden con el fragmento ilustrativo de `AspNetCoreRateLimit` más abajo; **aún no están implementados** en el rate limiter nativo.
```csharp
// ReservArte.API/Extensions/RateLimitingServiceExtensions.cs (RA-869d7ezkp)
services.AddRateLimiter(options =>
{
    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
            }));

    options.AddPolicy("auth-mfa-verify", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        // Retry-After si hay MetadataName.RetryAfter; cuerpo = envelope GEN_RATE_LIMITED
    };
});
// Program.cs: app.UseRateLimiter();
// AuthController: [EnableRateLimiting("auth-login")] / [EnableRateLimiting("auth-mfa-verify")]
```

**Alternativa histórica/opcional — `AspNetCoreRateLimit`** (**no** es la implementación activa del backend; se conserva como referencia de umbrales futuros y de configuración por JSON):

```csharp
// Usar AspNetCoreRateLimit — NO implementado; políticas nativas viven en código
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
app.UseIpRateLimiting();
```

```json
// Esquema ilustrativo de IpRateLimiting (alternativa AspNetCoreRateLimit; NO activo).
// login (10/h) y mfa/verify (20/h) ya están cubiertos por el middleware nativo.
// register (5/día), external/*/challenge (30/h) y * (100/min) = pendientes de la
// tarea de seguimiento «Refinamientos de auth…» (mismos números objetivo).
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "GeneralRules": [
      { "Endpoint": "*:/api/v1/auth/login", "Period": "1h", "Limit": 10 },
      { "Endpoint": "*:/api/v1/auth/mfa/verify", "Period": "1h", "Limit": 20 },
      { "Endpoint": "*:/api/v1/auth/external/*/challenge", "Period": "1h", "Limit": 30 },
      { "Endpoint": "*:/api/v1/auth/register", "Period": "1d", "Limit": 5 },
      { "Endpoint": "*", "Period": "1m", "Limit": 100 }
    ]
  }
}
```

#### 9.3.2 CAPTCHA para Login

> **Backend (2026-08-21, RA-869d7ezkp):** `ICaptchaService` / `CaptchaService` verifica el token del campo `Captcha` de `LoginRequest` dentro de `LoginAsync`. Proveedor por defecto: Cloudflare **Turnstile** (`VerifyUrl` = siteverify de Turnstile); la URL es configurable para **reCAPTCHA** u otro proveedor compatible. Sección de configuración `Captcha`: `Enabled`, `SecretKey` (User Secrets / Secrets Manager; **nunca** en repositorio), `VerifyUrl`. En desarrollo `Enabled = false` omite la verificación. Errores de red o del proveedor → **fail-closed** (se rechaza el login). Token inválido o ausente (con CAPTCHA activo) → `GEN_VALIDATION_FAILED` (400). La adopción de `AUTH_MFA_INVALID` en `/auth/mfa/verify` es un pendiente de la tarea de seguimiento de refinamientos de auth (vol. 1 §5.1.2), no de este CAPTCHA.
>
> **Reparto de responsabilidades (camino B, RA-869d7f7kn, 2026-08-23):** el **frontend** decide cuándo mostrar el widget (contador de fallos, umbral **3**) y reserva el punto de montaje; el **backend** solo verifica el token si llega. El **widget real Turnstile no está activado** (falta `VITE_TURNSTILE_SITE_KEY` y el script; el token se emitiría con `captchaVerified`). En Development `Captcha:Enabled = false` → login sin token. El ejemplo Vue siguiente (reCAPTCHA) sigue siendo referencia de UX; la implementación actual usa el hueco en `LoginForm` hasta activar Turnstile.

```csharp
// ReservArte.Infrastructure/Options/CaptchaOptions.cs — sección "Captcha"
public class CaptchaOptions
{
    public const string SectionName = "Captcha";
    public bool Enabled { get; set; }           // false en Development
    public string SecretKey { get; set; } = ""; // User Secrets / Secrets Manager
    public string VerifyUrl { get; set; } =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}

// ICaptchaService.VerifyAsync(token, remoteIp) → true/false (fail-closed)
// AuthService.LoginAsync: si !VerifyAsync(request.Captcha, ip) → GEN_VALIDATION_FAILED
```

```vue
<!-- frontend-web/src/components/auth/LoginForm.vue — referencia UX (reCAPTCHA);
     el backend por defecto espera un token Turnstile (VerifyUrl configurable). -->
<script setup lang="ts">
import { ref } from 'vue'
import VueRecaptcha from 'vue-recaptcha' // o integración equivalente con reCAPTCHA v2/v3 / Turnstile

const captchaValue = ref<string | null>(null)
const loginAttempts = ref(0)
const email = ref('')
const password = ref('')

async function handleLogin() {
  if (loginAttempts.value >= 3 && !captchaValue.value) {
    alert('Por favor, completa el CAPTCHA')
    return
  }
  try {
    await apiClient.post('/api/v1/auth/login', {
      email: email.value,
      password: password.value,
      captcha: captchaValue.value,
    })
    loginAttempts.value = 0
  } catch {
    loginAttempts.value++
  }
}
</script>

<template>
  <form @submit.prevent="handleLogin">
    <!-- Campos email y password -->
    <VueRecaptcha
      v-if="loginAttempts >= 3"
      sitekey="YOUR_RECAPTCHA_SITE_KEY"
      @verify="(v: string) => (captchaValue = v)"
    />
    <button type="submit">Iniciar Sesión</button>
  </form>
</template>
```

#### 9.3.3 Content Security Policy

```csharp
// ReservArte.API/Middleware/SecurityHeadersMiddleware.cs
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://sis.redsys.es; " + // Redsys InSite
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' https://api.reservarte.com; " +
            "frame-src 'self' https://sis.redsys.es; " + // iframes Redsys
            "frame-ancestors 'none';");

        // Otras cabeceras de seguridad
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        await _next(context);
    }
}

// Registrar middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
```

#### 9.3.4 CORS (SPA → API) — lección aprendida (2026-08-23)

`Cors:AllowedOrigins` está descrita en el volumen 1 **§5.1.3** (gestión de secretos / contrato de `appsettings`) y **ya se usaba** para validar el `returnUrl` del flujo OAuth (anti open-redirect, §9.2.1). Eso **no** equivale a un middleware CORS: hasta el bloque Layout + Auth UI (RA-869d7edpt) **no existía** `AddCors` / `UseCors` en `Program.cs`. Toda petición del SPA (`localhost:3000`) a la API en el navegador fallaba de forma **silenciosa** (bloqueo CORS; en DevTools aparece como red fallida, no como error de negocio del envelope).

**Corrección implementada:**
- `ReservArte-API/Extensions/CorsServiceExtensions.cs` — `AddCorsPolicy` lee `Cors:AllowedOrigins` y registra la política `DefaultCorsPolicy`.
- `Program.cs` — `builder.Services.AddCorsPolicy(...)` y `app.UseCors(CorsServiceExtensions.DefaultPolicy)` en el pipeline.

**Checklist para módulos futuros:** no dar por hecho que CORS «ya funciona» porque la clave está en `appsettings`. Verificar contra un frontend real (navegador, no solo Swagger/Postman, que no aplican CORS). Ítem correspondiente en volumen 3 **§12.2**.

---

### 9.4 Auditoría y Logging

```csharp
// ReservArte.Infrastructure/Services/AuditService.cs
public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task LogActionAsync(
        string action,
        string entityType,
        Guid? entityId,
        string details = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var organizationId = httpContext.User.FindFirst("organization_id")?.Value;

        var auditLog = new AuditLog
        {
            UserId = userId != null ? Guid.Parse(userId) : null,
            OrganizationId = organizationId != null ? Guid.Parse(organizationId) : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
    }
}

// Uso en controladores
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteCustomer(Guid id)
{
    var customer = await _customerRepository.GetByIdAsync(id);
    
    if (customer == null)
        return NotFound();

    await _customerRepository.DeleteAsync(id);

    // Auditar acción
    await _auditService.LogActionAsync(
        "Customer.Delete",
        "Customer",
        id,
        JsonSerializer.Serialize(new { customer.Email, customer.FirstName, customer.LastName })
    );

    return NoContent();
}
```

---

### 9.5 Referencia: estrategia de testing

La **estrategia completa de pruebas** (pirámide unitaria / integración / E2E, simulación de Redsys, CI/CD, cobertura por fase y tablas de herramientas) está recogida en el documento independiente **[`Documentation/reservarte-testing-strategy.md`](reservarte-testing-strategy.md)**. Este volumen mantiene los detalles de **seguridad y pagos**. Alinear con el roadmap del volumen 3: backend `tests/ReservArte.UnitTests` e `tests/ReservArte.IntegrationTests`; E2E y accesibilidad del **frontend** en **`reservarte-web/e2e/`** (Playwright + `@axe-core/playwright`, no Cypress ni `tests/ReservArte.E2ETests`). Convenciones de formato: vol. 2 **§9.10**.

### 9.6 Dominio y persistencia — módulo de Empleados (RA-869d7ezrr, 2026-09-12; RA-869d7ezv0 + RA-869f17myx, 2026-09-13)

Primera subtarea del bloque **RA-869d7ed2j** (CRUD Empleados): dominio. Las tres entidades ya existían desde `InitialCreate`; RA-869d7ezrr **completa y documenta** el dominio, no lo crea de cero. Persistencia: migración `AddEmployeeAvailabilityAndExceptions` (PR #36). Modelo de datos y convención de semana: vol. 1 **§3.1.2**.

**Contrato de disponibilidad:** `Employee` expone `Availabilities` y `Exceptions`. La disponibilidad **real** (horario menos ausencias) la calculará `AvailabilityService` (**RA-869d7f4rd**). Estos endpoints (RA-869d7f01b) **persisten y exponen** tramos y ausencias; no restan. Horas de horario = `TimeOnly` sin zona; ausencias = `DateTime` UTC. La conversión a la zona del centro queda para el frontend y para `AvailabilityService`.

**Helper `WeekDay`** (`ReservArte-Domain/Entities`, en el mismo fichero que `EmployeeAvailability`): constantes `Monday`…`Sunday` (`0`…`6`) y conversiones `FromDate(DateTime)`, `FromDate(DateOnly)`, `FromDayOfWeek(DayOfWeek)` y `ToDayOfWeek(int)`. El desfase de un día respecto a `System.DayOfWeek` se resuelve **en un único punto**. Cubierto por `WeekDayTests` (17 casos: semana completa, round-trip, paridad `DateOnly`/`DateTime`).

**Tipos de excepción:** `EmployeeException.Type` persistido como texto; valores de `EmployeeExceptionTypes` (`vacation`, `sick_leave`, `personal`, `training`, `other`), alineados con `CK_EmployeeExceptions_Type`.

**Esquema (RA-869d7ezv0 + RA-869f17myx, misma migración):** `EmployeeAvailabilities` y `EmployeeExceptions` están en el `DbContext` y en SQL Server. `OrganizationId` **nace con las tablas** (decisión de integrar RA-869f17myx en RA-869d7ezv0): **no hubo backfill**. CHECKs e índices: vol. 1 **§3.1.2**. `Employee` sigue mapeado con PK compartida (`EmployeeConfiguration`, `ValueGeneratedNever`).

**Scripts `data/` (RA-869f17mzg, PR #55; plantilla PR #57; última regeneración PR #58):** vía de arranque, alineada con EF. Tras **cada** migración: `bash data/schema/regenerate-create.sh` en el mismo PR; si toca tablas que siembra el demo (o cambia `DevSeeder`), actualizar `data/demo/seed_demo_ReservArteDB.sql`. **No editar** el `create` a mano. **Advertencia:** no hay CI que falle si se olvida regenerar. La plantilla `.github/PULL_REQUEST_TEMPLATE.md` **sí cubre** (PR #57) las casillas: regenerar `create` con `regenerate-create.sh`, revisar `seed_demo`, verificar sobre base de prueba creada con los scripts (**nunca** `ReservArteDB`). El demo añade **horario semanal** (`0 = lunes`); `DevSeeder` no. Detalle: vol. 1 **§5.2**, [`data/README.md`](../data/README.md).

**Patrón de repositorio (estrenado en Empleados; Clientes lo replica en RA-869d7f32r; catálogo RA-869d7f3z0 / paquetes RA-869d7f45n; citas RA-869d7f4n4):** interfaz en `ReservArte-Domain/Interfaces` (`IEmployeeRepository`, `ICustomerRepository`, `IServiceRepository`, `IServicePackageRepository`, `IAppointmentRepository`), implementación en `ReservArte-Infrastructure/Persistence/Repositories`, registro scoped vía `AddRepositories()`. ClickUp a veces pide `Application/Interfaces` y `Infrastructure/Repositories`: **no** es el sitio real. **Regla:** ningún método de repositorio recibe la organización por parámetro; sale de `ICurrentOrganizationService`. Si se pudiera pasar por argumento, una llamada podría leer datos de otro centro.

- **`PagedResult<T>`** (`ReservArte-Domain/Common`): elementos + total del filtro, que es lo que `meta.pagination` del envelope necesita; el total se cuenta **antes** de paginar.
- **`EmployeeFilter`:** búsqueda por nombre/apellidos/email, rol, y `IsActive` con semántica **`null` = solo activos** (baja lógica; la lista de gestión no arrastra bajas salvo petición explícita). Tope de tamaño de página: **100**.
- **Escritura:** `ReplaceAvailabilitiesAsync` **impone** `EmployeeId` y `OrganizationId` desde la petición, no desde el payload, para que un cliente no cuele filas en otro empleado ni en otra organización. El horario se **reemplaza entero** (no altas/bajas sueltas) para evitar estados intermedios incoherentes al editar. Efecto colateral **con consecuencia en API (RA-869d7f01b):** los Id de los tramos **cambian en cada PUT**; habrá que revisarlo cuando las citas referencien disponibilidad.
- **Ausencias (RA-869d7f01b):** `GetExceptionAsync(employeeId, exceptionId)` (acotado al empleado; tenant por query filter), `AddException`, `UpdateException` (sella `UpdatedAt`). DELETE de API = baja lógica (`IsActive = false`); la fila permanece y deja de entrar en `GetExceptionsAsync`.
- **Unicidad del email (RA-869f1xc0u, PR #57, 2026-09-15):** único **por organización**. Migración `20260915101445_ScopeEmailAndExternalLoginsToOrganization`. Índices `EmailIndex` = `(OrganizationId, NormalizedEmail)`, `UserNameIndex` = `(OrganizationId, NormalizedUserName)` (filtrados `IS NOT NULL`); `Employees`: `IX_Employees_OrganizationId_Email` (desaparece `IX_Employees_OrganizationId`). PK `AspNetUserLogins` = `(OrganizationId, LoginProvider, ProviderKey)` con `OrganizationId` NOT NULL y backfill. Entidad `UserLogin` + query filter. `OrganizationUserStore` sustituye `AddEntityFrameworkStores` para que `AddLoginAsync` selle el tenant. **Eliminado `GlobalUniqueUserValidator`.** `EmailExistsAsync` solo mira el tenant actual (**cero** `IgnoreQueryFilters()` en producción). Mismo email o mismo sujeto social en dos centros: válido. Duplicado en el mismo tenant → 409 `GEN_CONFLICT`. **Caminos sin tenant:** `FindByEmailAsync` lanza si el email existe en dos centros; hay que fijar `ICurrentOrganizationService` antes de usar Identity. `DevSeeder` solo siembra con la base vacía. `Down()` reversible **solo** mientras no haya el mismo email o sujeto de proveedor en dos orgs. `create` regenerado en el mismo PR; `seed_demo` solo cambió la cabecera.

**Aislamiento multi-tenant (RA-869f17vet, PR #54; `UserLogin` RA-869f1xc0u, PR #57; Clientes RA-869d7f32r, PR #58; catálogo RA-869d7f3z0, PR #65; Citas RA-869d7f4j8, PR #70):** `AppDbContext` recibe `ICurrentOrganizationService`; `CurrentOrganizationId` se lee en los query filters. Constructor de solo opciones: migraciones, seeders y tests (sin tenant, el filtro no restringe). Filtros: disponibilidad, **`Employee`**, **`User`**, **`UserLogin`**, **`RefreshToken`** (`User.OrganizationId`), **`Customer`**, **`CustomerNote`**, **`CustomerAllergy`**, **`CustomerConsent`**, las siete del catálogo, **`Appointment`**, **`AppointmentServiceItem`**, **`WaitingList`**. Claims y tokens de Identity **sin** filtro (justificado). **Cero** `IgnoreQueryFilters()` en producción. Filtro **manual** de `EmployeeRepository` **se mantiene**; `CustomerRepository` también filtra por el tenant del holder (sin organización resuelta no devuelve nada). Test de metadatos: entidad con `OrganizationId` sin filtro = fallo. Test `AppDbContextTenantResolutionTests`: DI debe elegir el constructor **con** tenant. **Advertencia:** un `Where(OrganizationId == …)` a mano en un repositorio **no** sustituye el filtro global; el global es la protección. El manual en empleados/clientes es defensa extra, no la fuente de verdad.

**Capa de servicio (RA-869d7ezwy, 2026-09-13; primera del proyecto tras auth):** interfaz `IEmployeeService` en `ReservArte-Application/Interfaces`, implementación `EmployeeService` en `ReservArte-Infrastructure/Services`, DTOs en `Application/DTOs/Employees`, validadores en `Application/Validators/Employees` (`CreateEmployeeRequestValidator` / `UpdateEmployeeRequestValidator`, mismas reglas), profile AutoMapper `EmployeeProfile` en `Application/Mapping`. Registro `AddApplicationServices()` (escanea el ensamblado de Application para los `Profile`). Plantilla para Clientes, Servicios y Citas.

- **`Result<T>`** (`Application/Common`): patrón general de resultado (éxito + datos, o fallo con `error.code`). **Hermano idéntico** de `AuthResult<T>` (`Application/DTOs/Auth`). Deuda de unificación: **RA-869f17y6k**.
- **Unidad de trabajo (RA-869f1811u, PR #53, 2026-09-14; alta pública RA-869f1xc2n, PR #59):** `IUnitOfWork.ExecuteInTransactionAsync` en Application (usa `Result<T>`; no Domain). `EfUnitOfWork` abre la transacción **dentro** de `CreateExecutionStrategy()` (`EnableRetryOnFailure` prohíbe una transacción abierta a mano). Confirma si el `Result` es éxito; deshace si es fallo o si lanza; al deshacer **vacía el change tracker** (sin eso, un `SaveChanges` posterior en la misma petición reescribiría lo rechazado). Scoped: mismo `AppDbContext` que repositorio y `UserManager`. La operación **puede reejecutarse** ante fallo transitorio → entidades **dentro** de la operación; correos **después** del commit. **Comprobado** (`EmployeeAtomicityTests`, SQLite + `UserManager`/`UserOnlyStore` reales): si la ficha no se guarda, la cuenta que Identity ya había persistido **desaparece** con la reversión. **`AuthService` también recibe `IUnitOfWork`** (RA-869f1xc2n): el alta pública (`RegisterAsync` y el alta de `ExternalLoginAsync`) envuelve cuenta + ficha (y el consentimiento marcado en el registro local) en la misma transacción; se comprueba cada `IdentityResult` (`CreateAsync` / `AddLoginAsync`). Los tokens se emiten **tras el commit**: si fallara la emisión, la cuenta ya existe y la persona puede iniciar sesión. El argumento del PR #37 («una transacción afectaría al camino de auth») queda **desmentido**.
- **Alta:** cuenta (`CreateAsync` sin contraseña) y ficha en la transacción; entidades construidas **dentro**. **Sin** compensación `DeleteAsync`. Invitación **después** del commit (fallo de correo ≠ rollback; un reintento no manda el correo dos veces).
- **Invitación (RA-869f17y68):** `ResendInvitationAsync` / `POST …/invitation`. El envío del alta ya no va «tras crear la ficha» a secas: va **tras commit**.
- **Advertencia — `EmailConfirmed`:** ni el alta ni el canje `set-password` ponen `EmailConfirmed = true` (el alta social sí). Tarea **RA-869f1812p**.
- **Edición:** en la transacción; **cada `IdentityResult` se comprueba** (encadenados; para en el primero que falle). Email/nombre duplicado **en el mismo tenant** (también contra cuenta que no es empleado) → **409 `GEN_CONFLICT`**. El mismo email en **otra** org es válido (RA-869f1xc0u). Otros errores Identity → 400 `GEN_VALIDATION_FAILED`. Helper `IdentityFailure`.
- **Baja lógica idempotente:** desactivar a quien ya está de baja no es error y no vuelve a sellar `UpdatedAt`. Existe reactivación.
- **Baja y reactivación (RA-869f180e5 + RA-869f1811u):** ficha y lockout en la transacción. `SyncAccountLockAsync` indica si pudo aplicar el bloqueo; si no → deshacer y **500 `GEN_INTERNAL_ERROR`**. Sin cuenta asociada: no es fallo. Auth comprueba `IsLockedOutAsync`. El access token vigente **sobrevive hasta caducar**.
- **Límites (sin tarea de unicidad):** (1) no hay token de concurrencia en `Employee`. (2) el reintento transitorio no se ejercita en tests. Unicidad de email: **por organización** (**RA-869f1xc0u** shipped).
- **Patrón para próximos módulos:** escrituras que abarquen varias tablas, y en especial Identity + tablas propias, por `IUnitOfWork` y **comprobar cada `IdentityResult`**. Toda entidad nueva con `OrganizationId` **debe** nacer con query filter (test de metadatos). No ignorar el resultado de Identity y luego `SaveChanges` sobre el mismo contexto. **Antes de mapear**, `OrganizationId` tiene que ser `Guid` (no `int`): **8** entidades aún en `Ignore` de `AppDbContext` siguen en `int` — `Payment`, `CancellationPolicy`, `Configuration`, `MessageTemplate`, `ReminderConfiguration`, `Product`, `ProductCategory`, `ProductSale`. `Appointment`, `AppointmentServiceItem` y `WaitingList` ya son `Guid` (**RA-869d7f4f1**) y están **mapeadas** (**RA-869d7f4j8**, PR #70 + #71). `AppointmentServiceItem` estrena tenant `Guid` (RA-869f17myx). El catálogo de Servicios (**RA-869d7f3wa** + **RA-869d7f3z0**, PRs #64–#65) ya es `Guid` y está **mapeado**. Las cuatro hijas (`ServiceVariation`, `ServicePricing`, `ServicePackageItem`, `EmployeeServiceAssignment`) **no tenían** `OrganizationId`; ahora lo tienen en `Guid` (RA-869f17myx). Quedan pagos/productos/recordatorios cuando existan. El test de metadatos y la FK a `Organizations` lo exigen al mapear.
- **Roles de ficha (RA-869f18116):** validadores = `Roles.AssignableToEmployee` (`Admin`, `Manager`, `Employee`). Default del DTO de alta: `Roles.Employee` (catálogo). **`Customer` no es asignable** a `Employees`.
- **Migración `NormalizeRolesToPascalCase` (PR #47):** `UPDATE` idempotente por `LOWER(Rol)` en `AspNetUsers` y `Employees`. `Down` revierte a minúsculas (`Customer`→`client`). Sin ella, el primer `[Authorize(Roles)]` habría denegado a todos los usuarios ya existentes.
- **Quién asigna cada rol (RA-869d7ezz4, 2026-09-14):** atributo `[Authorize(Roles = Admin,Manager)]` en `EmployeesController`; reglas por dato en `EmployeeService` vía `ICurrentUserService` (403 `GEN_FORBIDDEN`). Solo un Admin asigna/gestiona Admin; nadie cambia su propio rol ni se da de baja a sí mismo; fail-closed; `GEN_NOT_FOUND` antes que 403. Detalle enumerado: vol. 1 **§4.4.1**.
- **Endpoints (RA-869d7ezz4 + RA-869f17y68):** lista `data.items` + `meta.pagination` (`ApiItems<T>`); GET/PUT/DELETE por `{id:int}`; POST 201 + `Location`; **`POST …/{id}/reactivate`**; **`POST …/{id}/invitation`**. Mapeo en controlador: `GEN_VALIDATION_FAILED` / `ORG_TENANT_NOT_RESOLVED` → 400; `GEN_FORBIDDEN` → 403; `GEN_NOT_FOUND` → 404; `GEN_CONFLICT` → 409 (también email de cuenta sin ficha); fallo de lockout en baja/reactivación → **500** y operación deshecha; fallo de envío del reenvío → **500 `GEN_INTERNAL_ERROR`**; código sin mapear → **500**. Id no numérico → 404 sin cuerpo (no hay ruta). `ValidateAsync` duplicado con `AuthController` → **RA-869f17y6k**.
- **Disponibilidad (RA-869d7f01b, 2026-09-14):** mismo controlador y `[Authorize]`. GET `…/availability?from&to` (UTC; solo acota ausencias; default hoy→+90 días; rango aplicado en `exceptionsFrom`/`exceptionsTo`; `to < from` → 400 `field=to`). PUT reemplaza la semana (vacío = sin horario). POST `…/exceptions` 201, `Location` al GET de disponibilidad. DELETE ausencia: baja lógica idempotente; ausencia de otro empleado → 404. **Lectura:** Admin o Manager ven también a un Admin. **Escritura:** Manager no toca Admin (403). DTOs: `EmployeeAvailabilityResponse`, `EmployeeExceptionDto`, `UpdateAvailabilityRequest` / `AvailabilitySlotRequest`, `CreateEmployeeExceptionRequest` — sin org ni empleado en el payload. Validación (el frontend debe replicar): `dayOfWeek` 0–6; fin > inicio **solo en código** (el horario no tiene CHECK de intervalo); sin solapes el mismo día, varios tramos/día; máx. 50 tramos; ausencias fin > inicio + `type` ∈ `EmployeeExceptionTypes` + `reason` ≤ 500 (trim). `ToCamelCase` por tramo de ruta (`weeklySchedule[0].dayOfWeek`); `AuthController` conserva la versión antigua (RA-869f17y6k). Límites: rango de ausencias **sin tope**; Ids de tramo cambian en cada PUT.
- **Advertencia — pantalla Empleados (frontend, aún no hecha):** replicar la validación del horario (varios tramos/día, sin solapes, 0 = lunes) y tratar **`GEN_FORBIDDEN` como «sin permiso»**, no como fin de sesión (`SESSION_ENDING_ERROR_CODES` solo `ORG_TENANT_MISMATCH`). No usar los Id de tramo como clave estable entre guardados. **`AvailabilityService` (RA-869d7f4rd)** es quien debe restar ausencias y aplicar la zona del centro; estos endpoints no lo hacen.
- **Límite conocido (sigue vigente):** un cambio de rol o una baja **no** revoca el access token ya emitido del afectado; vale hasta caducar.

**Criterio de trabajo (2026-09-13):** contrastar cada cambio con el código ya desarrollado y verificar que no rompe lo existente. Aquí: `LoginAsync` no exige consentimiento RGPD y ya trata cuentas sin contraseña local; el alta de empleado reutiliza ese camino.

### 9.7 Dominio, persistencia y servicio — módulo de Clientes (RA-869d7f2z5 + RA-869d7f32r + RA-869f1xc2n + RA-869d7f369 + RA-869d7f3bt)

**RA-869d7f2z5 (PR #56, 2026-09-14) — solo dominio.** Primera subtarea del bloque **RA-869d7ed68**. Las clases existían en el repo y en `Ignore` de `AppDbContext` (no en `InitialCreate`). Ese PR las alineó al producto **sin** migración. Modelo: vol. 1 **§3.1.3**.

**RA-869d7f32r (PR #58, 2026-09-15) — esquema y repositorio.** Migración `20260915112149_AddCustomers` (solo crea tablas; no toca las existentes). `create_ReservArteDB.sql` regenerado en el mismo PR. Recuento del padre entonces: **2/8**. Cadena: **RA-869f1xc0u** (hecha) → **RA-869d7f32r** (hecha) → **RA-869f1xc2n**.

**PK compartida** `Customer.Id` = `User.Id` (mismo patrón que `Employee`). Navegación `User.Customer`. Todo cliente tiene cuenta Identity, aunque el centro lo dé de alta sin contraseña.

**Empleada y clienta, misma cuenta (decisión 2026-09-14; implementado RA-869d7f369, PR #60):** un `User` puede tener **ambas** fichas (`Employee` y `Customer`) con el mismo Id. `User.Rol` es el rol de **personal**. La ficha Customer no implica `Rol = Customer`. El alta de cliente sobre una cuenta del centro **sin ficha** añade la ficha (mismo Id), no otra cuenta, y **no** invita. Si ya hay ficha → 409. La baja de ficha de cliente **no** hace lockout (solo la baja de empleado). El registro público con un email ya usado en el centro sigue respondiendo **409** y no añade ficha.

**Alta pública con ficha (RA-869f1xc2n, PR #59, 2026-09-15) — shipped.** Recuento entonces: **3/8**. Cadena: **RA-869f1xc0u** (hecha) → **RA-869d7f32r** (hecha) → **RA-869f1xc2n** (hecha). Siguiente entonces: **RA-869d7f369**.
- **Registro local:** cuenta + ficha (`regular` / `email` **en ese PR**; **desde RA-869d7f369** nace `new`) + consentimiento `data_processing` (`IsGranted = 1`, `GrantedAt`) en `IUnitOfWork.ExecuteInTransactionAsync`; cada `IdentityResult` comprobado. Campo **`acceptedDataProcessing`** obligatorio; ausente o `false` → 400 `GEN_VALIDATION_FAILED` (`field = acceptedDataProcessing`); guarda en `AuthService` (nunca persiste un consentimiento no marcado). Email repetido en el centro → 409 `GEN_CONFLICT`, sin ficha duplicada. Tokens **tras el commit**.
- **Alta social, cuenta nueva:** cuenta + vínculo (`AddLoginAsync` comprobado) + ficha en una transacción. **Sin consentimientos granulares** (el OAuth no pasa por el formulario). `data_processing` pendiente de recabar en otro punto (p. ej. primera reserva; no hay tarea ClickUp).
- **Vincular un proveedor a una cuenta existente** (vínculo previo o email coincidente) **no toca fichas**.
- **Migración `20260915151444_BackfillCustomerProfiles`:** solo SQL, sin cambio de esquema. Ficha para cada `AspNetUsers` con `Rol = 'Customer'` que no la tenga (`regular`/`email`, **sin** consentimientos). Salta el email ya usado por **otra** ficha del mismo centro. Idempotente. **`Down()` vacío a propósito**. Sobre una base vacía no inserta nada. `create` regenerado; `seed_demo` solo cabecera. Cuentas `Employee` sin ficha: no se tocan.
- **SPA:** tercer checkbox en `RegisterForm` (sin enlace); `register.schema.ts` (Zod); `RegisterCredentials` y `RegisterPage`.
- **RA-869f1mqah no se cierra:** este PR solo cubre `CreateAsync`/`AddLoginAsync` del alta pública; el resto de `AuthService` sigue sin auditar.

**`OrganizationId` es `Guid`** (antes `int` en `Customer`, incompatible con `Organization.Id`). `CustomerNote`, `CustomerAllergy` y `CustomerConsent` llevan `OrganizationId` propio + navegación `Organization` (redundancia deliberada, RA-869f17myx). **`CustomerPaymentMethod` no** tiene `OrganizationId` y **sigue en `Ignore`** — **RA-869f2gnbm** lo añade al mapear (antes RA-869d7f3fw). El `organization_id` del sketch `customer_payment_methods` del vol. 1 **§5.2** es el **estado objetivo**; la entidad aún no lo tiene (el sketch no está mal). El test de metadatos lo exigirá al mapear.

**Retirado de `Customer`:** `Rol` (fuente: `User.Rol`) y `MarketingConsent` (fuente: `CustomerConsents`). **Aún no en la ficha:** `NoShowCount` / `BlockedAt` — diseño **RA-869f2gtyv** (no implementado).

**Catálogos** (constantes texto, snake_case minúsculas, como `EmployeeExceptionTypes`; `Roles` es la excepción PascalCase):
- `CustomerCategories`: `new` (default de entidad, RA-869d7f369), `regular`, `vip`. Bloqueo = `IsBlocked` + `BlockedReason` (**nvarchar(500)** en BD; el sketch original del vol. 1 decía `NVARCHAR(MAX)` — alineado en v6).
- `CustomerContactMethods`: `email` (default de entidad), `phone`, `sms`, `whatsapp`.
- `AllergySeverities`: `low`, `medium`, `high`.
- `CustomerConsentTypes`: `data_processing`, `marketing`, `photos`, `whatsapp`, `saved_cards`; `Required` = solo `data_processing`.

Los CHECK de catálogo se generan desde esas constantes (`CatalogCheck` en Infrastructure); un test fija el SQL resultante. **Sin DEFAULT en BD** (igual que `Employees`): `new`, `email` e `IsActive = 1` los pone la entidad.

**Email** obligatorio en la entidad. Unicidad por organización: **`IX_Customers_OrganizationId_Email`** (RA-869d7f32r). Identity/Employees: **RA-869f1xc0u**.

**Navegaciones** a citas, pagos y lista de espera: no en `Customer`; llegan con esos módulos (criterio `Employee`).

**Nota — `CustomerPaymentMethod.Appointments` / `Payments`:** `CustomerPaymentMethod` **sigue en `Ignore`** (`AppDbContext`) y conserva esas dos colecciones. Lo que hoy la mantiene fuera del modelo **no** es que `Appointment` esté ignorada —**ya no lo está** (RA-869d7f4j8)— sino su propio `modelBuilder.Ignore<CustomerPaymentMethod>()` y que `Appointment` **ya no tiene** navegación ni `PaymentMethodId` hacia ella (retirados en RA-869d7f4f1, PR #69). Si esa FK se hubiera conservado, mapear `Appointment` habría obligado a mapear también `CustomerPaymentMethod`. La frase «EF descarta navegaciones hacia tipos ignorados» **sigue siendo cierta para `Payment`**. Evidencia histórica: PR #56 añadió `User.Customer` con `Customer` ignorado y `has-pending-model-changes` no detectó cambios. Al mapear (**RA-869f2gnbm**) hay que añadir **`OrganizationId` Guid** (hoy no lo tiene) y query filter; las colecciones se retirarán solo por coherencia con `Employee` y `Customer`.

**Esquema generado (RA-869d7f32r):**
- **`Customers`:** PK = `AspNetUsers.Id` (cascada, `ValueGeneratedNever`); FK `OrganizationId` Restrict; longitudes FirstName/LastName 100, Email 255, Phone 20, ProfileImageUrl 500, Category 20, PreferredContactMethod 20, BlockedReason 500; CHECKs de categoría y canal de contacto.
- **`CustomerNotes`:** `Note` nvarchar(2000); FK `CustomerId` cascada; FK **`EmployeeId` Restrict** (la nota es histórico del cliente; SQL Server rechaza dos caminos CASCADE desde `AspNetUsers`); FK `OrganizationId` Restrict; índices `(CustomerId, CreatedAt)`, `EmployeeId`, `OrganizationId`.
- **`CustomerAllergies`:** descripción 500, Severity 20 + CHECK; FK Customer cascada, Org Restrict.
- **`CustomerConsents`:** ConsentType 50 + CHECK de los 5 tipos; CHECK **`GrantedAt`** (`[IsGranted] = 0 OR [GrantedAt] IS NOT NULL`); índice **único filtrado** `(CustomerId, ConsentType) WHERE [IsActive] = 1`.

**Repositorio (`ICustomerRepository` / `CustomerRepository`):** interfaz en `ReservArte-Domain/Interfaces` (ClickUp pedía `Application/Interfaces`; mismo sitio que `IEmployeeRepository`). Registrado en `AddRepositories`.
- `GetPagedAsync(CustomerFilter)`: `Search` en nombre, apellidos y email; filtros `Category`, `IsBlocked`, `IsActive` (`null` = solo activos); página con tamaño máximo 100; orden apellidos, nombre, Id.
- `GetByIdAsync`, `GetByEmailAsync` (email único en la organización), `GetProfileAsync` (solo lectura: notas más recientes primero, alergias y consentimientos **vigentes**, split query; pensado para el perfil completo de RA-869d7f3bt).
- `Add` / `Update` (sella `UpdatedAt`) **síncronos** + `SaveChangesAsync` (mismo patrón que `EmployeeRepository`).
- Filtra explícitamente por el tenant del holder: **sin organización resuelta no devuelve nada**.
- **Sin `GetHistoryAsync` en este repositorio:** el historial de citas **no** falta por mapeo — `Appointment` está mapeado desde RA-869d7f4j8 y el acceso a datos es `IAppointmentRepository` filtrando con `AppointmentFilter.CustomerId`. Lo que falta es el endpoint `GET /api/v1/customers/{id}/history` (**RA-869f2gn91**).
- **Notas (`GetNoteAsync` / `AddNote` / `UpdateNote`):** `GetNoteAsync(customerId, noteId)` acota tenant y cliente; devuelve vigentes y retiradas. `AddNote` y `UpdateNote` (sella `UpdatedAt`). Escritura HTTP: **RA-869d7f3fw** (PR #62).

**Datos demo** (`DevSeeder` y `data/demo/seed_demo_ReservArteDB.sql`, alineados; **una sola organización**):
- `carmen.lopez@example.com` / `Cliente123!`: cuenta `Customer`, ficha VIP, consentimientos `data_processing` + `marketing`, alergia «Látex» (`high`), nota de María («Prefiere citas por la tarde.»). Id 4.
- `sofia.ruiz@example.com` / `Cliente123!`: cuenta `Customer`, ficha `regular`, consentimiento `data_processing`. Id 5.

**Limpieza:** eliminados los tres `.bak` de `Configurations/` (`CustomerConfiguration`, `AppointmentConfiguration`, `EmployeeServiceAssignmentConfiguration`). `EmployeeServiceConfiguration.cs.bak` **no** era un cuarto fichero: nombre antiguo del de Assignment, renombrado en `3a3bf2d` (**RA-869f17y7n**).

**RA-869d7f369 (PR #60, 2026-09-15) — shipped.** Capa de servicio, **sin endpoints** (siguen en RA-869d7f3bt). Recuento del padre: **4/7** (RA-869d7f3q4 cancelada el 2026-09-15; el bloque pasa de 8 a 7 subtareas). Cadena: **RA-869f1xc0u** (hecha) → **RA-869d7f32r** (hecha) → **RA-869f1xc2n** (hecha) → **RA-869d7f369** (hecha). Siguiente del bloque: **RA-869d7f3bt**.
- **Sitio:** `ICustomerService` en `ReservArte-Application/Interfaces`; `CustomerService` en `ReservArte-Infrastructure/Services` (igual que `EmployeeService`: usa `UserManager`). Registro DI en `AddApplicationServices`. AutoMapper `CustomerProfile` (solo entidad → DTO). ClickUp decía `SearchAsync`; la operación se llama **`GetPagedAsync`**, como en Empleados.
- **`GetPagedAsync(CustomerFilter)`** → `PagedResult<CustomerDto>`. Sin tenant resuelto → `ORG_TENANT_NOT_RESOLVED`.
- **`GetByIdAsync`** → `CustomerDetailDto` vía `GetProfileAsync` (consentimientos, alergias y notas vigentes). Otro centro o inexistente → 404 `GEN_NOT_FOUND`.
- **`CreateAsync(CreateCustomerRequest)`:**
  - `grantedConsents` (lista de `CustomerConsentTypes`). Sin `data_processing` → 400 `GEN_VALIDATION_FAILED` (`field = grantedConsents`), en el validador **y** en el servicio. Solo se registran las finalidades marcadas (`IsGranted = 1`, `GrantedAt`).
  - Email con ficha en el centro → 409 `GEN_CONFLICT`. Email de cuenta del centro **sin ficha** (p. ej. empleada) → **se añade la ficha a esa cuenta** (mismo Id); no se toca rol/nombre/email ni se invita. Si esa cuenta ya tiene ficha, aunque el email sea otro → 409.
  - Cuenta nueva: `Rol = Customer`, **sin contraseña**. Cuenta, ficha y consentimientos en `IUnitOfWork.ExecuteInTransactionAsync`. **Tras el commit**, invitación `set-password` (proveedor `Invitation`, 7 días, mismo propósito que Empleados; texto propio para clientas). Si caduca, el correo remite a «¿Has olvidado tu contraseña?» (`ResetPasswordAsync` no exige contraseña previa). Fallo de envío **no** revierte el alta. **No hay reenvío de invitación para clientas.**
  - Categoría: la indicada o, por defecto, `new`.
- **`UpdateAsync`:** email de otra ficha del centro → 409. **Cuenta solo de cliente** (`User.Rol == Customer`): nombre, email, teléfono e imagen se sincronizan con la cuenta en la transacción, comprobando cada `IdentityResult`. `SetEmailAsync`/`SetUserNameAsync` **solo si el email cambia** (si no, Identity lo marcaría no confirmado). Email de una cuenta sin ficha (p. ej. el admin) → Identity rechaza → 409, sin dejar nada a medias. **Cuenta de personal** (`User.Rol != Customer`, falla cerrado): solo se edita la ficha; **cambiar el email → 403 `GEN_FORBIDDEN`**. La edición no toca consentimientos, bloqueo ni baja.
- **`DeactivateAsync` / `ReactivateAsync`:** baja lógica de la ficha, idempotente (no sella `UpdatedAt` si no cambia). **No hace lockout.**
- **DTOs:** `CustomerDto` (Id, FirstName, LastName, FullName, Email, Phone, ProfileImageUrl, BirthDate, Category, LoyaltyPoints, IsBlocked, BlockedReason, PreferredContactMethod, IsActive, CreatedAt, UpdatedAt; **sin** OrganizationId). `CustomerDetailDto : CustomerDto` + Consents / Allergies / Notes. `CreateCustomerRequest`: datos de ficha + `Category?` (null = `new`) + `PreferredContactMethod` (default `email`) + `GrantedConsents`. `UpdateCustomerRequest`: los mismos salvo consentimientos; Category obligatoria.
- **Validadores** `CreateCustomerRequestValidator` / `UpdateCustomerRequestValidator` (`Application/Validators/Customers`): nombre y apellidos obligatorios ≤100; email obligatorio, formato válido, ≤255; teléfono ≤20, solo dígitos y `+ ( ) . -`; fecha de nacimiento no futura; URL de imagen ≤500; categoría y canal del catálogo. En el alta: `grantedConsents` no nulo, con `data_processing`, sin repetidos y del catálogo. **La unicidad del email no está en el validador** (un email de cuenta existente sin ficha no es conflicto).
- **Categoría `new` por defecto (decisión de producto 2026-09-15; cierra el punto abierto de RA-869d7f2z5):** toda ficha nueva nace `new` (alta desde el centro, registro web y alta social / `NewCustomerProfile` de `AuthService`). Default de entidad `Customer.Category` (antes `regular`). **Sin migración:** la BD no tiene DEFAULT y el CHECK ya admitía `new`. `has-pending-model-changes`: sin cambios. `create` y `seed_demo` no cambian. Promoción `new` → `regular`: **RA-869f2g02q** (subtarea de Citas **RA-869d7edau**, backlog, prioridad normal).
- **Traslado de alcance:** `IncrementNoShowAsync` (bloqueo automático tras `MaxNoShowsBeforeBlock`) iba en ClickUp de esta tarea; **trasladado a RA-869d7f3ka**. Motivos: `Customer` no tiene contador; `CancellationPolicy` sigue en `Ignore` con `OrganizationId` int; no existe `OrganizationSettings`. RA-869d7f3ka deberá añadir `NoShowCount`, mapear la política con `OrganizationId` Guid (migración + query filter), implementar el bloqueo en `CustomerService` **y** el test «al alcanzar el umbral bloquea» (absorbido de RA-869d7f3q4). **Trasladada a RA-869f2gtyv el 2026-09-15.**
- **Deuda / puntos abiertos de esta capa:** sin reenvío de invitación para clientas; `SendInvitationEmailAsync` duplicado entre `EmployeeService` y `CustomerService`; `PreferredContactMethod = whatsapp` no exige el consentimiento `whatsapp`; una clienta dada de alta por el centro que se registra luego en la web con el mismo email sigue recibiendo 409 (camino: invitación o recuperación de contraseña). Quién edita el email de una cuenta solo de cliente: **resuelto en RA-869d7f3bt** (solo Admin y Manager).

**Tests:** `CustomerDomainTests` (12) + `CustomerRepositoryTests` + **`PublicSignupCustomerTests`** (ahora espera categoría `new`) + **`RegisterRequestValidatorTests`** + **`CustomerServiceTests`** (21, SQLite con Identity, repositorio y `EfUnitOfWork` reales sobre un único contexto) + **`CustomerValidatorTests`** (10) + **`CustomerProfileTests`** (2). `AuthServiceTenantTests` adaptado. Suite **279/279** (antes 246). E2E **57/57** (reejecutados tras PR #60; Chromium, Firefox y WebKit sobre `develop`). Mutación: forzando la regla de cuenta de personal a `false` fallan exactamente sus 2 tests. Los tests «crear sin `data_processing` falla» y «con `data_processing` persiste» de **RA-869d7f3q4** (cancelada) ya están aquí.

**Verificación en runtime (PR #58):** API en Development contra base vacía: EF migra, `DevSeeder` siembra 2 clientas, 3 consentimientos, 1 alergia y 1 nota; login de Carmen 200. Base solo con scripts `create` + `demo`: API arranca sin migrar; login 200 de Carmen, Sofía y María; mismos recuentos.

**Verificación en runtime (PR #59, SQL Server):** sobre una base creada con los scripts anteriores a la rama y cuentas antiguas sembradas: cuenta `Customer` sin ficha → ficha creada, 0 consentimientos; cuenta `Customer` con email ya usado por otra ficha → saltada; cuenta `Employee` sin ficha → no se toca; `register` sin el campo o con `false` → 400 y sin cuenta; `register` con `true` → 200, cuenta + ficha `regular` + `data_processing` fechado. **Desde RA-869d7f369** el registro nace `new`.

**Verificación en runtime (PR #60):** API en Development contra SQL Server. Arranque correcto (la validación de DI construye `CustomerService`) y `GET /legal/versions` → 200. No se creó base de prueba por scripts (no hay migración).

**RA-869d7f3bt (PR #61, 2026-09-15) — shipped.** Endpoints sobre `CustomerService`. Recuento del padre: **5/7**. Cadena: … → **RA-869d7f369** (hecha) → **RA-869d7f3bt** (hecha). Siguiente del bloque: **RA-869d7f3fw** / **RA-869d7f3ka** (sin orden impuesto).
- **Controlador:** `CustomersController`, ruta `api/v1/customers`. Autorización en dos niveles que se suman: clase `[Authorize(Roles = Admin,Manager,Employee)]`; escrituras (POST, PUT, DELETE, reactivate) además `[Authorize(Roles = Admin,Manager)]`. Rol **Customer:** 403 `GEN_FORBIDDEN` en todo el módulo, también en su propio perfil (backoffice; «mis datos» del cliente es otra funcionalidad).
- **Decisión de producto:** Employee lee y no escribe; se exponen baja y reactivación (no venían en ClickUp). Cierra quién edita el email de una cuenta solo de cliente: **Admin y Manager**.
- **Contrato HTTP:** vol. 1 **§5.1**. Envelope en todas las respuestas; `field` de validación en camelCase. Mapeo de códigos = Empleados (sin mapear → 500).
- **Helpers duplicados:** `ValidateAsync`, `FromFailure` y `ToCamelCase` se replican por tercera vez (Auth, Empleados, Clientes); unificación **RA-869f17y6k**.
- **Tests:** sin tests nuevos (el proyecto de unitarios no referencia la API; no hay tests de controladores). Las reglas siguen en `CustomerServiceTests`. Suite **279/279**. E2E **57/57** (SPA sin cambios). Hueco: **RA-869f2gh37** (backlog Backend, prioridad normal) — tests de integración HTTP con `WebApplicationFactory` (roles, envelope y contrato de Empleados y Clientes). Se cruza con **RA-869f18uta** y **RA-869eqxm7w** (API y BD en el runner).
- **Verificación en runtime (PR #61):** API Development contra SQL Server; tokens reales de Admin, Employee y una cuenta Customer registrada por la web. 401 sin token; 403 para Customer (lista y perfil propio) y para POST/PUT/DELETE de Employee; 200 lista Employee con `meta.pagination` y búsqueda `search` + `category`; POST sin `data_processing` → 400 `field=grantedConsents`; POST válido → 201 + `Location` e invitación (log); mismo email → 409; PUT categoría inválida → 400 `field=category`; PUT email del admin sin ficha → 409 sin cambios; PUT válido → 200; GET inexistente → 404; DELETE ×2 → 200 idempotente (lista por defecto la excluye; `isActive=false` la incluye); reactivate → 200. La cuenta registrada por la web tiene ficha `new` con `data_processing`.
- **Observación de entorno:** la `ReservArteDB` de dev era anterior a RA-869d7f32r y no tenía las fichas demo (Carmen y Sofía), porque `DevSeeder` solo siembra con la base vacía. Copia de seguridad hecha; se recreará con los scripts de `data/`. No es un defecto del código. **Hecho en la verificación de PR #62:** drop → create → demo; recuentos alineados a `data/README.md`; login demo y perfil de Carmen OK.
- Desde PR #62 (RA-869d7f3fw), Employee también escribe notas internas (`POST /notes`); las escrituras de ficha siguen siendo Admin|Manager. Pendientes vigentes: solo RA-869d7f3ka. **Trasladada a RA-869f2gtyv el 2026-09-15.**

**RA-869d7f3fw (PR #62, 2026-09-15) — shipped (alcance reducido a notas).** Recuento del padre: **6/7**. Cadena: … → **RA-869d7f3bt** (hecha) → **RA-869d7f3fw** (hecha). ClickUp: «Endpoints /history + /notes + /payment-methods»; el 2026-09-15 se redujo a **«Endpoints de notas internas de cliente (POST/DELETE /api/v1/customers/{id}/notes)»**. `/history` → **RA-869f2gn91** (Citas). `/payment-methods` y mapeo de `CustomerPaymentMethod` → **RA-869f2gnbm** (Redsys). **RA-869d7f3ka trasladada a RA-869f2gtyv el 2026-09-15.**
- **Autoría:** la nota la firma la ficha `Employee` **activa** de quien llama (`CustomerNotes.EmployeeId`, FK a `Employees`). El autor **nunca** va en el cuerpo. Sin cambio de esquema (decisión frente a `AuthorUserId`). Cuenta de personal **sin ficha** (p. ej. `guille@svalero.com`) o empleada **de baja** → 403. DELETE: autora (`EmployeeId` = usuario), Admin o Manager; 404 si la nota no existe, es de otro cliente o de otro centro; baja lógica idempotente.
- **Lectura:** notas vigentes en `GET /customers/{id}` (más recientes primero). No hay lista de notas.
- **Servicio:** `AddNoteAsync` / `DeleteNoteAsync`. `CustomerService` recibe `IEmployeeRepository` e `ICurrentUserService`.
- **Validador:** `CreateCustomerNoteRequestValidator` (`Application/Validators/Customers`): `note` obligatoria, no solo espacios, ≤2000, recorte.
- **Tests:** `CustomerServiceTests` +9 (SQLite + `EmployeeRepository` real): nota firmada y visible en el perfil; sin ficha → 403; de baja → 403; otro centro → 404; autora retira de forma idempotente y desaparece del perfil; otra empleada no retira nota ajena; Manager y Admin retiran; otro cliente → 404. `CustomerValidatorTests` +5. Suite **293/293**. E2E **57/57** (SPA sin cambios).
- **Mutación:** quitando la comprobación de ficha activa y dando permiso de gestión a cualquiera, fallan exactamente los 2 tests esperados.
- **Runtime (PR #62):** API Development contra `ReservArteDB` recreada con scripts `data/`; tokens reales. Customer POST → 403; admin sin ficha POST → 403; nota solo espacios → 400 `field=note`; cliente inexistente → 404; María POST con acentos → 201 + `Location`; Lucía DELETE nota ajena → 403; DELETE indicando otro cliente → 404; Admin DELETE ×2 → 200 idempotente; el perfil vuelve a la nota demo. Nota de prueba id 2 retirada en dev.
- **Observación de entorno:** un primer intento con `curl` en Git Bash pasando acentos como argumento produjo 400 `ProblemDetails` sin envelope (JSON mal codificado). Deuda conocida **RA-869f1k17q**, no un defecto de este PR.

**Cierre del bloque RA-869d7ed68 (2026-09-15, PR #63).** **Shipped 6/6.** PR #63 solo `CLAUDE.md`. Unit **293/293**, E2E **57/57**. Canceladas: RA-869d7f3q4 y **RA-869d7f3ka**. No-shows → **RA-869f2gtyv** (Citas; disparador: **RA-869d7f4xf** — «AppointmentService: máquina de estados Pending→Confirmed→InProgress→Completed/Cancelled/NoShow»). `/history` → RA-869f2gn91. Tarjetas → RA-869f2gnbm. Frontend fuera del bloque: **RA-869d7fc34**, **RA-869d7fc51** (subtareas de **RA-869d7edt7** — «Módulos Empleados, Clientes, Servicios y Dashboard (UI completa)»).

**Diseño de no-shows (decidido, no implementado) — RA-869f2gtyv:** umbral en `OrganizationSettings` (una fila por org, `OrganizationId` Guid, query filter; `MaxNoShowsBeforeBlock` default 3; sin fila se aplica 3). No fusionar aquí `Configuration` / `CancellationPolicy`. Desbloqueo Admin|Manager con motivo; `NoShowCount` a 0 (RGPD art. 22). Constancia: Serilog + `BlockedReason` + `BlockedAt` (sin `AuditLog` genérico). `IncrementNoShowAsync` desde transición a `NoShow`; `CUST_BLOCKED` 403 al reservar. Test de umbral (de RA-869d7f3q4) + desbloqueo a 0 + aislamiento tenant.

**Abiertos del bloque backend: 0.** Fuera: historial **RA-869f2gn91**; tarjetas **RA-869f2gnbm**; no-shows **RA-869f2gtyv**; promoción **RA-869f2g02q**; **RA-869f2gtz8** (AuditLog transversal, backlog Backend, prioridad baja: esquema Guid + query filter, servicios vs interceptor `SaveChanges`, qué se audita, retención/acceso RGPD). Unicidad **RA-869f1xc0u** (**shipped**, PR #57; independiente). **RA-869d7f3q4** y **RA-869d7f3ka** canceladas.

**Criterio de nombres (RA-869f17y7n):** no llamar a la entidad `CustomerService`.

### 9.8 Dominio, persistencia, servicio y API — módulo de Servicios (RA-869d7f3wa + RA-869d7f3z0 + RA-869d7f42u + RA-869f2wtrk + RA-869d7f45n)

**Nota de recuento:** el padre **RA-869d7ed7v** nació con **5** subtareas. Tras la auditoría del PR #66 se creó **RA-869f2wtrk** (las escrituras de categorías, variaciones y tarifas no tenían dueño) y el denominador pasó a **6**. Los recuentos «entonces n/5» de los PRs #64–#66 son foto de su momento, no un error. Recuento vigente: **5/6**. **Solo queda RA-869d7f4b4** (dashboard). El catálogo tiene capa de acceso a datos completa.

**RA-869d7f3wa (PR #64, merge `deb39ba`, 2026-09-16) — solo dominio.** Primera subtarea del bloque **RA-869d7ed7v** («CRUD Servicios + endpoint Dashboard»). Recuento del padre entonces: **1/5**. Las clases existían en el repo y en `Ignore` de `AppDbContext`. Ese PR las alineó al producto **sin** migración (mismo criterio que RA-869d7f2z5 / Clientes). `dotnet ef migrations has-pending-model-changes`: «No changes have been made to the model since the last migration». Scripts de `data/` **no cambian** en ese PR. **Sin verificación en runtime, a propósito:** sin mapeo no había nada que ejercitar por HTTP ni en BD. El mapeo llegó en **RA-869d7f3z0**.

**Orden:** el bloque de Servicios se **adelanta al de Citas** (RA-869d7edau). Motivo verificado en código: `AppointmentServiceItem` (`ServiceId`, `ServiceVariationId`) y `WaitingList` (`ServiceId`) tienen FK a tablas que estaban en `Ignore`, y la duración y el importe de una cita salen de `Service.DurationMinutes` / `BasePrice`. El roadmap ya ponía Servicios en Sprint 3-4 y Citas en Sprint 5-6; el bloque se había saltado.

**Alcance real: 7 entidades**, no las 4 del título de ClickUp (faltaban `ServiceCategory`, `ServicePricing` y `EmployeeServiceAssignment`).

- `OrganizationId` de `int` a **`Guid`** en `Service`, `ServiceCategory`, `ServiceVariation`, `ServicePricing`, `ServicePackage`, `ServicePackageItem` y `EmployeeServiceAssignment`.
- Las cuatro hijas (`ServiceVariation`, `ServicePricing`, `ServicePackageItem`, `EmployeeServiceAssignment`) **estrenan** `OrganizationId` + navegación `Organization`: antes no lo tenían. Redundante con el padre a propósito (RA-869f17myx), para que el query filter no dependa de un JOIN.
- Nuevo catálogo **`EmployeeLevels`** (`junior` / `senior` / `expert`) que respalda `ServicePricing.EmployeeLevel`, snake_case como el resto. No es `Roles` (PascalCase, `[Authorize]`): una Manager puede cobrar tarifa junior. Tampoco es `EmployeeServiceAssignment.ProficiencyLevel` (destreza 1-5 por servicio).
- Retiradas las navegaciones a entidades aún en `Ignore`: `Service.Products`, `Service.Promotions`, `Service.WaitingLists`, `ServiceVariation.AppointmentItems`, `ServicePackage.Promotions`. Mismo criterio que `Customer` y `Employee`.
- `Employee` gana la navegación `Services` (`ICollection<EmployeeServiceAssignment>`). La clase puente se llama `EmployeeServiceAssignment` (tabla `EmployeeServices`) para no colisionar con `Infrastructure.Services.EmployeeService` (RA-869f17y7n).

**Fuera de alcance, intactas y en `Ignore`:** `ServiceProduct` (necesita `Product`), `ServicePhoto` (alcance de módulo: fotografías / Cloudinary, no falta de tabla padre — `Appointment` y `Service` ya están mapeadas; el comentario de `AppDbContext` es «fotos (necesitan el almacenamiento de imágenes, no solo la cita)»; **tampoco tiene `OrganizationId`**: al mapearla hay que añadirlo en `Guid` con query filter, como las hijas del catálogo, RA-869f17myx; **sin subtarea ClickUp conocida**, pendiente de asignar), `ServicePromotion` (sin subtarea ClickUp).

**Dos escalas de «nivel» (decidido en RA-869d7f3z0):** se mantienen **independientes**. `ProficiencyLevel` (1-5) responde a *quién puede* prestar el servicio; `EmployeeLevel` (`junior`/`senior`/`expert`) a *cuánto cuesta*. No se deriva una de otra: son preguntas distintas. **Queda sin regla de negocio que las relacione**: si destreza 5 debiera implicar tarifa `expert`, hay que introducirla explícitamente. Está documentado en ambas entidades.

**Tests (PR #64):** `ServiceDomainTests` (21: valores del catálogo, tenant `Guid` en las siete, tenant propio en las hijas, defaults del producto y ausencia de las navegaciones retiradas). Suite entonces **314/314** (antes 293). E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet build`: 0 errores, 0 advertencias.

**Criterio de nombres (RA-869f17y7n):** no llamar a la entidad `ServiceService`. El servicio de aplicación se llama **`ServiceCatalogService`**: no tartamudea y describe el conjunto (servicios, categorías, variaciones y tarifas). No es `CatalogService` a secas porque más adelante habrá catálogo de productos (inventario). ClickUp pedía `ServiceService`; el título de **RA-869d7f3z0** ya dice `ServiceCatalogService`.

**RA-869d7f3z0 (PR #65, merge `c653d24`, 2026-09-16) — persistencia y servicio.** Recuento del padre entonces: **2/5** (denominador aún 5). Saca las siete entidades de `Ignore`. Modelo: vol. 1 **§3.1.4**. Esquema: vol. 1 **§5.2**.

**Migración `20260916084021_AddServiceCatalog`.** Solo **crea** tablas (`Services`, `ServiceCategories`, `ServiceVariations`, `ServicePricings`, `ServicePackages`, `ServicePackageItems`, `EmployeeServices`). **No toca ninguna tabla existente.** Los `DropTable` están solo en el `Down()`. Query filter global en las siete (el test de metadatos lo exige). `create_ReservArteDB.sql` regenerado (+291 líneas). Demo alineado en `DevSeeder` y `seed_demo_ReservArteDB.sql`: 2 categorías, 3 servicios, 1 variación, 3 tarifas, 5 asignaciones, **0 paquetes**.

**CHECKs** generados desde el dominio con `CatalogCheck` (mismo patrón que Clientes):

- `CK_ServicePricings_EmployeeLevel` — constantes `EmployeeLevels` (`junior` / `senior` / `expert`).
- `CK_Services_DurationAndPrice` — duración > 0 y precio ≥ 0.
- `CK_ServicePricings_Price` — precio ≥ 0.
- `CK_ServicePackages_TotalPrice` — precio ≥ 0.
- `CK_ServicePackages_DiscountPercentage` — 0–100.
- `CK_EmployeeServices_ProficiencyLevel` — destreza 1–5.

**`Restrict` (SQL Server, dos caminos en cascada):** `EmployeeServices.EmployeeId` y `ServicePackageItems.ServiceId` (mismo caso que `CustomerNotes.EmployeeId`). Las FK a `Organizations`, siempre `Restrict`. Cascada: `ServicePricings.ServiceId`, `ServiceVariations.ServiceId`, `ServicePackageItems.ServicePackageId`, `EmployeeServices.ServiceId`.

**Índice único filtrado** `IX_ServicePricings_ServiceId_EmployeeLevel` `(ServiceId, EmployeeLevel) WHERE IsActive = 1`: una sola tarifa vigente por servicio y nivel; una retirada no estorba (patrón de `CustomerConsents`).

**`EmployeeServices`:** PK compuesta `(EmployeeId, ServiceId)`.

**Sin DEFAULT en BD:** `IsActive` y el resto los pone la entidad. El SQL de `seed_demo` debe dar `IsActive` explícito (ver advertencia del INSERT de `EmployeeServices`).

**Longitudes** (estilo del proyecto; el sketch de §5.2 usaba `NVARCHAR(MAX)` y no las detallaba): nombre 200, descripción 1000 (categoría 500, variación 100), URL 500, color 20, nivel 20, importes `decimal(10,2)`, descuento `decimal(5,2)`. Cambiar cualquiera exige migración.

**`IServiceRepository`** en **`Domain/Interfaces`** (no en Application; ClickUp lo pedía ahí; mismo sitio que `ICustomerRepository`). `ServiceRepository`: lista paginada con búsqueda y filtros, detalle con variaciones y tarifas vigentes, categorías, variaciones y tarifas. **Sin organización resuelta no devuelve nada.** Expone escrituras de variaciones y tarifas; el servicio de aplicación las usa desde **RA-869f2wtrk** (el repositorio **no se tocó** en ese PR: el upsert de tarifas cabe en `GetPricingAsync`). Paquetes: **entonces** mapeados sin métodos aquí; la capa propia llega en **RA-869d7f45n** (`IServicePackageRepository` / `IServicePackageService`, no se mezclan con este repositorio). Mismo criterio que `CustomerRepository`, que llevó los métodos de notas desde RA-869d7f32r, antes de sus endpoints.

**`IServiceCatalogService` / `ServiceCatalogService`:** lista, detalle, alta, edición, baja/reactivación idempotentes y lectura de categorías. Desde **RA-869f2wtrk**, también escrituras de categorías, variaciones y tarifas. **Sin `IUnitOfWork`:** no hay cuenta de Identity de por medio; todo cabe en un `SaveChanges`. DTOs, validadores FluentValidation, `ServiceCatalogProfile` y registro DI. Una categoría que no exista en el centro al alta de un servicio → `GEN_VALIDATION_FAILED` (`field = categoryId`), no 404.

**Tests (PR #65):** `ServiceRepositoryTests` (SQLite real), `ServiceValidatorTests`, `ServiceCatalogProfileTests` (+30). Suite **344/344** (antes 314). E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet build`: 0 errores, 0 advertencias. `dotnet format --verify-no-changes`: **101** avisos, línea base de `develop`; **ninguno** en ficheros de este PR.

**Runtime (PR #65):** SQL Server, base desechable `ReservArteTestDB` (nunca `ReservArteDB`). `drop` → `create` → `demo` sin errores; recuentos 2/3/1/3/5 y 0 paquetes; los 6 CHECK existen; el de `EmployeeLevel` **rechaza** un nivel inventado (`Msg 547`); API contra esa base con `/health` **200** (`database: Healthy`), `/api/v1/legal/versions` **200**, **sin aplicar migraciones**; base eliminada al terminar. La batería unitaria **no** ejecuta los scripts SQL: un `INSERT` de `EmployeeServices` con 5 columnas y 4 valores solo apareció al sembrar.

**RA-869d7f42u (PR #66, merge `c01c566`, 2026-09-16) — API de servicios.** Recuento del padre entonces: **3/5** (denominador aún 5). `ServicesController` (`/api/v1/services`) sobre el servicio de PR #65. Contrato: vol. 1 **§5.1**. Las escrituras de categorías, variaciones y tarifas **no** iban en este PR; se creó **RA-869f2wtrk**.

**Autorización en dos niveles, que se suman.** La clase pide solo `[Authorize]` (cualquier rol autenticado, **Customer incluido**). POST/PUT/DELETE/reactivate llevan además `[Authorize(Roles = Admin,Manager)]`. Es una **diferencia deliberada** con Empleados y Clientes, donde Customer recibe 403 en todo el módulo: el catálogo no es dato personal y una clienta lo necesita para elegir servicio al reservar. **No** se ha abierto a usuarios sin autenticar: la reserva pública (vol. 1 §3.1.5) es decisión del bloque de Citas. **Reversible en una línea:** `[Authorize(Roles = StaffRoles)]` en la clase vuelve al criterio conservador.

**Categoría inexistente en el centro** al alta o edición → **400 `GEN_VALIDATION_FAILED`** (`field = categoryId`), **no 404**: el recurso que se crea o edita es el servicio.

**`GET /categories`:** sin `isActive` devuelve **todas**, activas y retiradas. El formulario de edición necesita ver la categoría retirada de un servicio ya guardado; si no, la ficha perdería su clasificación en pantalla. Distinto de la lista de servicios, que sin `isActive` devuelve solo activos.

**Escrituras de categorías, variaciones y tarifas:** el repositorio las exponía; **no hay endpoints en este PR**. Llegan en **RA-869f2wtrk**. Paquetes: llegaron en **RA-869d7f45n** (PR #68), con repositorio y servicio propios.

**Cuarta réplica** de `ValidateAsync` / `FromFailure` / `ToCamelCase` (Auth, Empleados, Clientes, Servicios). Unificación **RA-869f17y6k**.

**Tests:** **sin tests nuevos** (el proyecto de unitarios no referencia la API; no hay tests de controladores; mismo caso que RA-869d7f3bt / PR #61). Suite **344/344**. E2E **57/57** (SPA no se toca; **no reejecutados**). Hueco: **RA-869f2gh37**. `dotnet format --verify-no-changes`: **101** avisos, línea base de `develop`; **ninguno** en ficheros de este PR.

**Test frágil (cierra la Constancia del vol. 3):** `ValidateToken_rechaza_un_token_manipulado` alteraba el último carácter de la firma HMAC-SHA256. 32 bytes → 43 caracteres base64url (258 bits); los 2 últimos bits del último carácter son relleno y se descartan. Solo `Y` colisiona con la `a` del test (grupo `YZab`): 1/16 = 6,25 %, coherente con el 1 de 15 medido. Ahora altera el **payload**. **20 de 20** ejecuciones correctas. **No era un fallo de producción**: la validación del JWT siempre fue correcta; el test daba por inválido un token que seguía siéndolo.

**Runtime (PR #66):** SQL Server, base desechable `ReservArteTestDB`, eliminada al terminar.

- Autorización: sin token **401**; `GET` lista, categorías y detalle con Customer y con Employee **200**; `POST`, `PUT`, `DELETE` y `reactivate` con Customer y con Employee **403**.
- Validación: `categoryId: 999` → **400** `field=categoryId`; `durationMinutes: 0` → **400** `field=durationMinutes`; `basePrice: -1` → **400** `field=basePrice`.
- Negocio: alta **201** con `Location`; detalle con `categoryName` resuelto; edición **200**; `GET`/`PUT` de id inexistente **404**; `DELETE` dos veces **200** e idempotente; la lista por defecto excluye el dado de baja y `isActive=false` lo devuelve; `reactivate` **200**.
- Filtros: `search=tinte` → id 2; `search=medición` (busca en descripción) → id 1; `categoryId=2` → id 3; `pageSize=5000` se acota a **100**.
- Detalle: variación `Con hilo`; tarifas `junior 22`, `senior 25`, `expert 30`.
- Multi-tenant: organización inexistente → **400 `ORG_TENANT_NOT_RESOLVED`**; organización existente ≠ claim → **403 `ORG_TENANT_MISMATCH`** (hubo que crear una organización temporal, luego borrada: el 403 exige que la organización exista); organización propia → **200**.

**Nota de método — un 400 que no era del servidor:** la primera pasada devolvió 400 en todos los endpoints con tokens válidos. Causa: el arnés (`${t:+-H "Authorization: Bearer $t"}` en bash se parte en palabras y curl recibía la cabecera rota). Con las cabeceras bien formadas, todo respondía. Un 400 con token válido invita a buscar el fallo en el servidor, y no estaba ahí.

**RA-869f2wtrk (PR #67, merge `9abae79`, 2026-09-16) — escrituras del catálogo.** Recuento del padre entonces: **4/6**. El denominador pasa de 5 a 6: esta subtarea se creó al detectar, en la auditoría del PR #66, que categorías/variaciones/tarifas no tenían dueño (un prompt anterior las daba por incluidas en RA-869d7f42u). Completó esas escrituras: **antes, una categoría nueva solo se podía crear con un `INSERT` a mano.** Quedaban entonces **RA-869d7f45n** (paquetes) y **RA-869d7f4b4** (dashboard).

**Nueve endpoints, todos `[Authorize(Roles = Admin,Manager)]`:**

| Verbo | Ruta | Notas |
|---|---|---|
| POST | `/api/v1/services/categories` | 201; `Location` a `GET /categories` (no hay GET por id: se consumen como conjunto) |
| PUT | `/api/v1/services/categories/{categoryId}` | no toca la baja |
| DELETE | `/api/v1/services/categories/{categoryId}` | baja lógica idempotente; **se permite con servicios** |
| POST | `/api/v1/services/categories/{categoryId}/reactivate` | idempotente |
| POST | `/api/v1/services/{id}/variations` | 201; `Location` al detalle del servicio |
| PUT | `/api/v1/services/{id}/variations/{variationId}` | variación de otro servicio → 404 |
| DELETE | `/api/v1/services/{id}/variations/{variationId}` | baja lógica **idempotente** |
| PUT | `/api/v1/services/{id}/pricings/{employeeLevel}` | **upsert** por nivel; `SENIOR` se normaliza |
| DELETE | `/api/v1/services/{id}/pricings/{employeeLevel}` | **no** idempotente: 404 sin vigente |

`IServiceCatalogService` crece con las nueve operaciones; DTOs y validadores nuevos. **El repositorio no se ha tocado.**

**Decisiones:**

1. **Baja de categoría con servicios: se permite.** Es lógica: la fila no se borra, `Restrict` no interviene, ningún servicio queda sin clasificar, y `GET /categories` sin filtro sigue devolviendo la retirada (el formulario de edición conserva la clasificación). Runtime: categoría 1 dada de baja; servicios 1 y 2 conservan `categoryId`; `GET /categories` → `[1,2,3]`; `isActive=true` → `[2,3]`.
2. **Tarifas = upsert por nivel, no CRUD por id.** El nivel es la clave natural; el índice único filtrado solo admite una vigente. `PUT` es idempotente y no choca con el índice (no hay 409 de duplicado). Por eso basta `GetPricingAsync(serviceId, level)`; un CRUD por id habría exigido `GetPricingByIdAsync`.
3. **Asimetría de idempotencia.** `DELETE` de variación: idempotente (la consulta ve también las retiradas). `DELETE` de tarifa: 404 sin vigente, porque el recurso *es* la vigente y una retirada ya no se direcciona por nivel. La interfaz documentó «idempotente» para tarifas por error; se corrigió **antes** de implementarla.

**Duración resultante:** un `durationModifier` negativo es legítimo; no puede dejar la duración total ≤ 0. FluentValidation no conoce el servicio padre; lo comprueba `ServiceCatalogService` (`field = durationModifier`). Runtime: `-45` sobre un servicio de 45 min → 400.

**Nivel:** se normaliza a minúsculas **antes** de comparar. Desconocido → 400 `field=employeeLevel` sin tocar la BD (el CHECK lo pararía igual, como error de infraestructura y sin campo).

**Tests (PR #67):** `ServiceCatalogWriteValidatorTests` (+8: categoría, variación, tarifa). Sin tests de controlador (**RA-869f2gh37**). Suite **352/352** (antes 344). E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet format --verify-no-changes`: **101** avisos; **ninguno** en ficheros de este PR.

**Runtime (PR #67):** base desechable `ReservArteTestDB`, eliminada al terminar.

- Autorización (seis escrituras): sin token **401**; Customer **403**; Employee **403**.
- Categorías: alta **201**; nombre en blanco **400** `field=name`; `displayOrder: -1` **400** `field=displayOrder`; edición **200**; `PUT` id 9999 **404**; baja de la categoría 1 (servicios 1 y 2) **200**; `DELETE` repetido **200**; `reactivate` **200**.
- Variaciones: alta **201**; `durationModifier: -45` **400** `field=durationModifier`; nombre vacío **400** `field=name`; servicio 9999 **404**; edición **200**; variación del servicio 1 pedida desde el 2 **404**; `DELETE` ×2 **200**.
- Tarifas: `PUT senior` en servicio sin tarifas **200** (19,50); repetido con 21,00 **200** y **una sola fila vigente**; `PUT SENIOR` **200**; nivel `maestro` **400** `field=employeeLevel`; `price: -1` **400** `field=price`; servicio 9999 **404**; `DELETE senior` **200** y repetido **404**; `DELETE maestro` **400** `field=employeeLevel`.

**RA-869d7f45n (PR #68, merge `d8c23af`, 2026-09-16) — paquetes del catálogo.** Recuento del padre: **5/6**. **Solo queda RA-869d7f4b4** (dashboard). **El catálogo queda completo** (capa de acceso a datos). `ServicePackagesController` (`/api/v1/service-packages`) con `IServicePackageRepository` (`Domain/Interfaces`) e `IServicePackageService` (`Application/Interfaces`) **separados** de `IServiceRepository` / `ServiceCatalogService`: aquel servicio ya iba por quince operaciones y los paquetes son un recurso HTTP distinto. Contrato: vol. 1 **§5.1**. Seed demo: **0 paquetes**.

**Seis endpoints** (lista, detalle, alta, PUT, DELETE, reactivate):

| Verbo | Ruta | Notas |
|---|---|---|
| GET | `/api/v1/service-packages` | `search`, `isActive`, `page`, `pageSize` (acotado a 100) |
| GET | `/api/v1/service-packages/{id}` | líneas en orden + desglose calculado |
| POST | `/api/v1/service-packages` | 201 + `Location` |
| PUT | `/api/v1/service-packages/{id}` | **reemplaza la composición entera** |
| DELETE | `/api/v1/service-packages/{id}` | baja lógica idempotente |
| POST | `/api/v1/service-packages/{id}/reactivate` | idempotente |

Misma autorización que el resto del catálogo: **lee cualquier rol autenticado** (Customer incluido); escriben **Admin o Manager**. Un `serviceId` desconocido → **400** `field=items[n].serviceId` (`n` = índice de la línea), no 404: el recurso que se crea o edita es el paquete.

**Decisiones:**

1. **Al reemplazar la composición, las líneas anteriores se borran físicamente** (`RemoveRange`). Única excepción a la baja lógica del módulo. Precedente: `ReplaceAvailabilitiesAsync` (Empleados). Una línea de composición no es histórico de negocio (ninguna cita apunta a ella); dejarla con `IsActive = false` acumularía filas muertas que reaparecerían en cualquier lectura mal filtrada.
2. **El repositorio impone el paquete y el tenant a cada línea entrante.** Una petición no puede colar líneas en otro paquete ni en otra organización (test de entrada maliciosa, mismo criterio que Empleados).
3. **El desglose se calcula al leer y no se guarda.** `totalPrice` es el importe pactado; `discountPercentage` es informativo; `itemsTotalPrice`, `savings` y `totalDurationMinutes` salen de los servicios incluidos en el momento de la consulta. Si cambia el precio de un servicio, el desglose se mueve solo. **`savings` no se recorta a cero**: un paquete más caro que sus partes muestra un negativo.

**Quinta réplica** de `ValidateAsync` / `FromFailure` / `ToCamelCase` (Auth, Empleados, Clientes, Servicios, Paquetes). Unificación **RA-869f17y6k**.

**Tests (PR #68):** `ServicePackageRepositoryTests` (SQLite real) replica el contrato de `ReplaceAvailabilitiesAsync` (reemplazo total, imposición de paquete y tenant, no tocar lo ajeno). `ServicePackageValidatorTests` cubre la composición. +18. Sin tests de controlador (**RA-869f2gh37**). Suite **370/370** (antes 352). E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet format --verify-no-changes`: tras escribir los tests subió de 101 a **111**; se formatearon **solo esos dos ficheros** (no el proyecto entero) y volvió a 101. Primera vez en el bloque que la herramienta aporta algo útil pese a **RA-869f2pjf8**.

**Runtime (PR #68):** base desechable `ReservArteTestDB`, eliminada al terminar.

- Autorización: sin token **401**; Customer y Employee **200** en lectura y **403** en escritura.
- Validación: paquete sin servicios **400**; servicio repetido **400**; `discountPercentage: 150` **400** `field=discountPercentage`; `serviceId` inexistente en la **segunda** línea → **400** `field=items[1].serviceId`.
- Desglose (servicios de 25,00/45 min y 18,00/30 min, `totalPrice` 38,00): **201** con `itemsTotalPrice = 43,0`, `savings = 5,0`, `totalDurationMinutes = 75`; líneas en orden 0 y 1 con nombre y precio.
- Paquete más caro que la suma (20,00 sobre 18,00): `savings = -2,0`, **no se recorta a cero**.
- `PUT` de 2 líneas a 1: **200**, y **una sola fila en base de datos**, sin huérfanas.
- `GET`/`PUT` id 9999 **404**; `DELETE` ×2 **200** (idempotente); la lista por defecto excluye el dado de baja e `isActive=false` lo devuelve; `reactivate` **200**; `search` filtra; `pageSize=5000` se acota a **100**.

**Dashboard (RA-869d7f4b4):** único pendiente del bloque de Servicios, que queda **parado en 5/6**, no cerrado. Pide citas de hoy por estado, ingresos del mes y próximas citas. `Appointment` **ya está mapeado** (RA-869d7f4j8); el dashboard sigue parado porque **no hay servicio de citas ni datos que medir** (`Payment` sigue en `Ignore`, Redsys pendiente). Hacerlo ahora serían ceros o métricas provisionales; se retomará cuando Citas dé datos. La decisión es del usuario.

### 9.9 Dominio, mapeo y repositorio — módulo de Citas (RA-869d7f4f1 + RA-869d7f4j8 + RA-869d7f4n4)

**RA-869d7f4f1 (PR #69, merge `55feccd`, 2026-09-16) — solo dominio.** Primera subtarea del bloque **RA-869d7edau** («Sistema de Citas: API completa, disponibilidad, máquina de estados y tests»). Recuento del padre: **1/11**. El padre nació con **10** subtareas; al alinear `WaitingList` se creó **RA-869f2yh9b** (repositorio, servicio y endpoints de lista de espera) y el denominador pasó a **11**. Padre en `in development`, fechas 2026-09-16 → 2026-09-25.

Las clases existían en el repo y en `Ignore` de `AppDbContext`. Ese PR las alineó al producto **sin** migración (mismo criterio que RA-869d7f2z5 / Clientes y RA-869d7f3wa / catálogo). `dotnet ef migrations has-pending-model-changes`: «No changes have been made to the model since the last migration». Scripts de `data/` **no cambian**. **Sin verificación en runtime, a propósito:** sin mapeo no hay nada que ejercitar por HTTP ni en BD. El mapeo es **RA-869d7f4j8**.

Lo desbloqueó el catálogo: `AppointmentServiceItem` (`ServiceId`, `ServiceVariationId`) y `WaitingList` (`ServiceId`) tienen FK a `Services`, que salió de `Ignore` en el PR #65. La duración y el importe de una cita se **congelan** en la línea al crear: no se releen del catálogo.

**Alcance: 3 entidades.**

- `OrganizationId` de `int` a **`Guid`** en `Appointment` y `WaitingList`.
- `AppointmentServiceItem` **estrena** `OrganizationId` + navegación `Organization`. Redundante con el padre a propósito (RA-869f17myx), para que el query filter no dependa de un JOIN.
- Nuevo catálogo **`AppointmentStatuses`** con los **ocho** valores del CHECK de diseño de vol. 1 §5.2.2 (`pending`, `confirmed`, `in_progress`, `completed`, `cancelled`, `cancelled_by_customer`, `cancelled_by_business`, `no_show`), snake_case como el resto. No es un enum `AppointmentStatus`. Colecciones derivadas: **`Cancellations`** (los tres que significan «cancelada») y **`Terminal`** (completed + las tres cancelaciones + no_show).
- Catálogo **`AppointmentCancelledByTypes`** (`customer`, `business`).
- Retiradas de `Appointment` las navegaciones a módulos aún en `Ignore`: `PaymentMethod` **y su `PaymentMethodId`**, `Payments`, `Photos`, `ReminderLogs`, `ConfirmationTokens`. Mismo criterio que `Customer` y `Service`.
- Se conservan **`RedsysOrderNumber`** y **`RedsysPreAuthToken`**: son escalares, no FK; RA-869d7f4j8 indexa el primero.
- `WaitingList.Priority` default **1000** (menor va antes; deja hueco sin renumerar). Baja lógica `IsActive`. El aviso de hueco libre queda en `NotifiedAt`; el envío es del sistema de recordatorios.

**Decisión del usuario — ocho valores en `Status` Y se mantiene `CancelledByType`:** el mismo dato vive en dos columnas y pueden contradecirse. **`Status` es la fuente de verdad** (documentado en la entidad). Imponer la coherencia al cancelar es trabajo de **RA-869d7f4xf**: hoy nada impide `Status = cancelled_by_customer` con `CancelledByType = business`.

**`PaymentMethodId` se retiró con la navegación.** Es FK a `CustomerPaymentMethod` (`Ignore`, **RA-869f2gnbm**). El sketch de vol. 1 §5.2 sí conserva `payment_method_id`: diseño objetivo, no el estado actual.

**Advertencia abierta — campos del sketch de `appointments` sin dueño:** `redsys_auth_code`, `redsys_transaction_type` y `created_by` **no están** en la tabla EF y **no hay tarea** que los incorpore. No se asignan por cuenta propia. `payment_method_id` sí tiene dueño (**RA-869f2gnbm**). Los otros tres quedan pendientes de que el usuario decida si entran con Redsys (**RA-869d7eden**) o si se retiran del sketch.

**Subtarea nueva RA-869f2yh9b:** ninguna de las 10 subtareas originales daba a `WaitingList` repositorio, servicio ni endpoints. Sin ella habría quedado mapeada sin capa de datos, como `ServicePackages` antes de RA-869d7f45n.

**Aviso para RA-869d7f4j8 (RESUELTO, PR #70 + #71):** las dos FK de `Appointments` a `Customers` y `Employees` son **Restrict**. `WaitingList` entra en la misma migración `AddAppointments`; **RA-869f2yh9b no necesitará migración propia**. `regenerate-create.sh` usa `--no-build` (hay que compilar antes). **Fix Windows (PR #70):** el script usaba una variable `TMP`, que en Windows **ya es variable de entorno exportada**; se la pasaba a `dotnet ef`, que resolvía su directorio temporal contra un fichero y moría con `DirectoryNotFoundException`. Renombrada a `SCRIPT_TMP`. En macOS no se veía (`TMPDIR`). Misma precaución con `TEMP` en cualquier script nuevo. `data/README.md` corrige «servicios y citas no están aquí»: el demo sí siembra el catálogo; citas, líneas y lista de espera están en el esquema **sin** datos demo (RA-869d7f519).

**Tests (PR #69):** `AppointmentDomainTests` (18: ocho valores del CHECK, `Cancellations`, `Terminal` sin abiertos, tipos de cancelación, snake_case, default `pending`/`IsActive`, tenant `Guid` en las tres, tenant propio en la línea, ausencia de las navegaciones retiradas, conservación de los escalares Redsys, default de lista de espera). Suite entonces **388/388** (antes 370). E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet build`: 0 errores, 0 advertencias. `dotnet format --verify-no-changes`: **101** avisos, línea base de `develop` **entonces**; **ninguno** en ficheros de ese PR.

**RA-869d7f4j8 (PR #70 `fe6bf60` + PR #71 `de94fa8`, 2026-09-16) — mapeo.** Recuento del padre: **2/11**. Las tres salen de `Ignore` (`DbSet`, configuración propia, query filter por `OrganizationId`). Tablas `Appointments`, `AppointmentServiceItems`, `WaitingLists`. Migraciones `20260916161457_AddAppointments` y `20260916171801_RenameWaitingListToWaitingLists` (`Down()` completo: PK, 4 FK, 4 índices, CHECK). Entidad de dominio **`WaitingList`**; solo cambia el nombre de tabla. **Regla:** ninguna tabla del esquema va en singular, aunque el ERD de diseño la nombre así.

**Decisiones de mapeo (el porqué):**

1. **Lista de espera en la misma migración** (decisión del usuario). ClickUp de RA-869d7f4j8 ya pedía el índice `(OrganizationId, ServiceId, Priority)`.
2. **Índice único filtrado** `idx_appointments_redsys_order` (`WHERE [RedsysOrderNumber] IS NOT NULL`). En SQL Server un único admite un solo NULL; la mayoría de las citas no pasan por Redsys. El sketch §5.2 declara `UNIQUE` en columna y en la lista de índices lo da como no único: la implementación es única y filtrada.
3. **FK de cita a clienta y empleada: las dos Restrict.** Histórico de negocio (no puede irse con una ficha) y SQL Server rechaza los dos CASCADE desde `AspNetUsers`. El sketch dice `ON DELETE SET NULL`; `CustomerId`/`EmployeeId` son NOT NULL, SET NULL no aplica. FK a Organizations Restrict (el sketch dice CASCADE).
4. **Líneas:** Cascade desde la cita; Restrict a `Services` y `ServiceVariations` (por eso la baja de servicio es lógica).
5. **WaitingLists:** Cascade desde `Customers`; Restrict en `Services`, `PreferredEmployee` y `Organizations`.
6. **Longitudes:** `CancellationReason` 500, `Notes` 2000 (el sketch deja MAX).
7. **`CancelledByType` nullable:** un CHECK solo rechaza FALSE; los NULL pasan. Coherencia con `Status`: RA-869d7f4xf.

**Índices y CHECK:** `idx_appointments_org_date`; `idx_appointments_redsys_order` (único filtrado); `IX_Appointments_EmployeeId_AppointmentDate`; `IX_Appointments_CustomerId`. `CK_Appointments_Status` (ocho valores, `CatalogCheck`), `CK_Appointments_CancelledByType`, `CK_Appointments_EndTime`, `CK_Appointments_Amounts`. Líneas: `(AppointmentId, Order)`, `(ServiceId)`, `(OrganizationId)`, `CK_AppointmentServiceItems_PriceAndDuration`. Lista de espera: `idx_waiting_lists_org_service_priority`, `(CustomerId)`, `CK_WaitingLists_DateRange`.

**Runtime (base desechable, nunca `ReservArteDB`):** `create` completo OK (SQL Server habría rechazado los dos CASCADE). Dos citas sin `RedsysOrderNumber` conviven; dos con el mismo → `Msg 2601` (SQLite no lo habría cazado: allí un único admite varios NULL). Estado `reprogramada` → `CK_Appointments_Status`. `EndTime` < `StartTime` → `CK_Appointments_EndTime`. `CancelledByType = 'empleada'` → `CK_Appointments_CancelledByType`. Borrar ficha de clienta con citas → `FK_Appointments_Customers_CustomerId`. Rango invertido en espera → `CK_WaitingLists_DateRange`. Renombrado por los dos caminos: `database update` sobre base que ya tenía `WaitingList` (queda sin residuos) y base desde cero con scripts (12 migraciones; nace `WaitingLists`). API contra esa base: 0 migraciones reaplicadas, login demo 200, `GET /api/v1/services` 200. `seed_demo` no se toca.

**Tests (PR #70 + #71):** `AppointmentMappingTests` (22: 21 en #70 + `Las_tablas_del_modulo_van_en_plural` en #71). Contra SQLite real, no dobles. Suite **410/410**. E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet build`: 0 errores, 0 advertencias. `dotnet format --verify-no-changes`: **113** avisos (antes 101). 12 son de `AppointmentMappingTests.cs` (varias asignaciones en una línea en inicializadores): el **mismo patrón de estilo** que ya usan `CustomerRepositoryTests` (17 avisos) y `TenantQueryFilterTests` (8). Es el estilo real del repo; la regla de `format` y el estilo del proyecto **no coinciden**. Deuda de **RA-869f2pjf8**, no una regresión de estos PR. **La línea base de `develop` ya no es 101.**

**RA-869d7f4n4 (PR #74, commit `3def77c`, merge `a1d7931`, 2026-09-16) — repositorio.** Recuento del padre: **3/11**. `IAppointmentRepository` + `AppointmentFilter` en `ReservArte-Domain/Interfaces/`; `AppointmentRepository` en `ReservArte-Infrastructure/Persistence/Repositories/`; scoped en `AddRepositories()` (cinco repositorios). **Sin migración ni cambios en `data/`.** Sin endpoints (RA-869d7f519): el ejercicio funcional son los tests (SQL real).

**Métodos:** `GetPagedAsync(AppointmentFilter)`, `GetByIdAsync`, `GetDetailAsync`, `GetByDateRangeAsync(from, to, employeeId?)`, `GetByRedsysOrderAsync`, `Add`, `Update` (sella `UpdatedAt`), `SaveChangesAsync`.

**`AppointmentFilter`:** `From`/`To` (`DateOnly`, rango inclusivo), `EmployeeId`, `CustomerId`, `Status` (un valor; filtrar cancelaciones exige los tres de `AppointmentStatuses.Cancellations` — avisado en el XML; se ampliará a colección si el servicio lo necesita), `IsActive` (`null` = solo activas), paginación con `PageSize` acotado a 100. La lista ordena de la cita **más reciente** a la más antigua e incluye `Customer` y `Employee` (nombres sin consulta por fila).

**Decisiones:**

1. **Rutas.** ClickUp pedía `Application/Interfaces` y `Infrastructure/Repositories`. Se usó Domain + `Persistence/Repositories`, como los otros cuatro. La descripción de ClickUp era la desalineada.
2. **Ningún método recibe `orgId`.** El tenant sale de `ICurrentOrganizationService`. Si se pudiera pasar por argumento, una llamada podría leer la agenda de otro centro. Misma regla que el resto de repositorios.
3. **Seguimiento de EF, con test que lo fija:** `GetByIdAsync` y `GetByRedsysOrderAsync` **no** llevan `AsNoTracking` (lecturas para escribir: con `AsNoTracking`, un `SaveChanges` posterior no guardaría nada y no daría error). `GetPagedAsync` y `GetDetailAsync` sí son `AsNoTracking`, con `AsSplitQuery`.
4. **`GetByDateRangeAsync` no filtra por estado:** si una cita cancelada libera el hueco es regla de RA-869d7f4rd, no del acceso a datos. Sí excluye las de baja lógica (`IsActive`).
5. **`IsActive` no es cancelada.** Cancelar es una transición de `Status` que la clienta ve; esas citas **siguen activas**. `IsActive` es la baja lógica de gestión. Hay un test dedicado.
6. **Sin organización resuelta el repositorio no devuelve nada.** El query filter global sí deja pasar todo sin tenant (migraciones y seeders); el repositorio, no. Patrón `TenantAppointments`, igual que en paquetes.

**Tests (PR #74):** `AppointmentRepositoryTests` (22, SQLite real): aislamiento por tenant (incluido «sin tenant no devuelve nada» y «el número de pedido de Redsys de otro centro no se encuentra»), filtros, rango inclusivo, orden, paginación con total del filtro y `pageSize` acotado, detalle con líneas ordenadas, escritura (incluido que `GetByIdAsync` viene con seguimiento). Suite **432/432** (antes 410). E2E **57/57** (SPA no se toca; **no reejecutados**). `dotnet build` 0/0. `dotnet format --verify-no-changes`: **EXIT 0** (línea base cero, RA-869f2pjf8). Runtime: API contra `ReservArteDB` sin errores de DI (`Development` valida el grafo al construir) y `GET /api/v1/services` 200.

**Siguiente:** **RA-869d7f4rd** (disponibilidad).

**El bloque de Servicios queda parado en 5/6**, no cerrado: solo le falta el dashboard (**RA-869d7f4b4**), que se retomará cuando Citas dé datos.

### 9.10 Convenciones de formato (`.editorconfig`, RA-869f2pjf8)

Fuente de verdad: **`.editorconfig` en la raíz** del monorepo (PR #73). Fija el estilo que ya tenía el código para que editores, `dotnet format` y Prettier coincidan.

**Puerta (PR #72, camino 1 — alinear el espaciado, no retirar la herramienta):** `dotnet format --verify-no-changes` debe salir **código 0** y **cero avisos**. Cualquier aviso lo introduce el PR en revisión. Al medirlo, **no** encadenar con `| tail` (se leería el código de salida de `tail`).

**Qué fija el fichero:** C# sangrado de 4 espacios; frontend 2 espacios y 100 columnas (alineado con `reservarte-web/.prettierrc`); namespaces de ámbito de fichero; llaves Allman; `using` fuera del namespace con System primero; salto de línea final; campos privados `_camelCase`. Las reglas de **nombres** van en severidad `suggestion` a propósito: `format` no debe fallar por un nombre.

**`end_of_line` no se fija para el código.** Con `core.autocrlf=true` el índice guarda LF y el árbol de trabajo de Windows tiene CRLF; fijarlo haría fallar el formateo en uno de los dos equipos. De los finales de línea se encarga git. Control explícito, si alguna vez se quiere: **`.gitattributes`**, no el `.editorconfig`. **`*.sh` sí** lleva `end_of_line = lf` (con CRLF, Git Bash puede romper con `$'\r': command not found`).

**Excluidos:** migraciones (`generated_code = true`) y `data/schema/create_ReservArteDB.sql` (generado). Verificado: `dotnet format` no las toca; `regenerate-create.sh` sigue generando un `create` idéntico.

**Frontend:** no se ve afectado. `npx prettier --check src/` da el mismo resultado con y sin `.editorconfig` (Prettier 3 lo lee; manda su propia config). `eslint` pasa.

**Efecto del PR #72:** expandió los inicializadores de objeto compactos a una asignación por línea (regla por defecto de C#). Volver al estilo compacto se decide en el `.editorconfig`, no revirtiendo aquel PR.

**Métricas (ambos PR):** build 0/0; unit **410/410** (no cambia: solo espacios / BOM / usings); E2E **57/57** no reejecutados; `dotnet format` **113 → 0**.

---

**Fin del volumen 2 de 3**

---

**Continúa en el volumen 3: Planificación y gestión**

El volumen 3 incluye:
- Plan de Desarrollo - Roadmap
- Estimación de Costos
- Próximos Pasos
- Anexos

---