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
9. [SEGURIDAD Y PROTECCIÓN DE DATOS](#9-seguridad-y-protecciÃ³n-de-datos) (incl. **§9.5** referencia a estrategia de testing en [`reservarte-testing-strategy.md`](reservarte-testing-strategy.md))

---

## 7. PASARELAS DE PAGO Y SISTEMA FINANCIERO

> **Contrato de respuestas JSON:** Antes de estabilizar controladores de pago (y cualquier otro endpoint de negocio), aplicar el **envelope** `{ success, data, error, meta }` y los códigos `error.code` definidos en el volumen 1 (**§5.1.1** y **§5.1.2**). Los ejemplos de este capítulo que usan `BadRequest(new { success = false, error = "..." })` deben evolucionar a ese contrato (incl. `meta.requestId`).

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

```csharp
// ReservArte.Infrastructure/Services/AmazonSESEmailService.cs
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace ReservArte.Infrastructure.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string textBody = null);
        Task<bool> SendTemplatedEmailAsync(string to, string templateName, object templateData);
    }

    public class AmazonSESEmailService : IEmailService
    {
        private readonly IAmazonSimpleEmailService _sesClient;
        private readonly ILogger<AmazonSESEmailService> _logger;
        private readonly string _fromEmail;

        public AmazonSESEmailService(
            IAmazonSimpleEmailService sesClient,
            ILogger<AmazonSESEmailService> logger,
            IConfiguration configuration)
        {
            _sesClient = sesClient;
            _logger = logger;
            _fromEmail = configuration["Email:FromAddress"];
        }

        public async Task<bool> SendEmailAsync(
            string to, 
            string subject, 
            string htmlBody, 
            string textBody = null)
        {
            try
            {
                var request = new SendEmailRequest
                {
                    Source = _fromEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { to }
                    },
                    Message = new Message
                    {
                        Subject = new Content(subject),
                        Body = new Body
                        {
                            Html = new Content(htmlBody),
                            Text = new Content(textBody ?? StripHtml(htmlBody))
                        }
                    }
                };

                var response = await _sesClient.SendEmailAsync(request);
                
                _logger.LogInformation($"Email enviado a {to}. MessageId: {response.MessageId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando email a {to}");
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(
            string to, 
            string templateName, 
            object templateData)
        {
            try
            {
                var request = new SendTemplatedEmailRequest
                {
                    Source = _fromEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { to }
                    },
                    Template = templateName,
                    TemplateData = System.Text.Json.JsonSerializer.Serialize(templateData)
                };

                var response = await _sesClient.SendTemplatedEmailAsync(request);
                
                _logger.LogInformation($"Email templated enviado a {to}. MessageId: {response.MessageId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enviando email templated a {to}");
                return false;
            }
        }

        private string StripHtml(string html)
        {
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
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

El hashing lo realiza el **hasher oficial de ASP.NET Core Identity (PBKDF2)** registrado con `AddIdentityCore<User>()` — no BCrypt ni un `PasswordHashingService` propio. Alta y verificación vía `UserManager<User>`:

```csharp
// reservarte-api/Extensions/IdentityServiceExtensions.cs (fragmento)
services
    .AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Alta de usuario con contraseña (Application / Auth)
var result = await _userManager.CreateAsync(user, password);
// CreateAsync hashea con IPasswordHasher<User> y persiste en AspNetUsers.PasswordHash
```

---

### 9.2 Autenticación y Autorización

#### 9.2.1 JWT Service

> **v2 (2026-07-07, RA-869d7eyze):** Adaptado a `User : IdentityUser<int>` (`Id` int, `Rol`, email nullable), `IOptions<JwtOptions>`, interfaz `IJwtTokenService` en Application y `GenerateAccessToken` con expiración `AccessTokenMinutes` (no hardcodeada).
>
> **v3 (2026-07-17, RA-869d7ez3e):** El claim de rol se emite con el nombre literal `"role"` (no `ClaimTypes.Role`). `JwtSecurityToken` escribe los claims sin mapeo corto: si se usara `ClaimTypes.Role`, el payload llevaría la URI larga `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. El registro de `AddJwtBearer` debe declarar `TokenValidationParameters.RoleClaimType = "role"` para que `[Authorize(Roles = ...)]` resuelva correctamente. Ese registro (junto con la resolución de tenant desde el claim `organization_id`) se realiza en la tarea de endpoints `/account/mfa/*` (RA-869d7eze3), primer consumidor real de `[Authorize]`.

**Dependencias NuGet (auth JWT):** `Microsoft.IdentityModel.Tokens` y `System.IdentityModel.Tokens.Jwt` se fijan en **8.14.0** (no 8.0.0). La familia `Microsoft.IdentityModel.*` tiene numeración independiente del target .NET; las dependencias transitivas de AutoMapper/MediatR ya resuelven esa versión, y fijarla en 8.0.0 provoca **NU1605**. Los paquetes de **EF Core** sí permanecen en **8.0.0**, alineados con el target framework .NET 8.

```csharp
// ReservArte.Application/Interfaces/IJwtTokenService.cs
public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Guid organizationId);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}

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

    public string GenerateRefreshToken()
    {
        // Token opaco de 64 bytes (no JWT); se persistirá en §9.2.2
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
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
```

**Login social (OAuth 2.0 / OpenID Connect) y el mismo JWT**

El backend registra esquemas externos acordados: **`AddGoogle`**, **`AddFacebook`** (Meta / **Instagram Login** según configuración en Meta Developers), **`Apple`** (handler OAuth/OIDC para Sign in with Apple, p. ej. `AspNet.Security.OAuth.Apple`). Tras el **callback** del IdP, un controlador o manejador usa `UserManager` / `SignInManager` para **crear o enlazar** el usuario y persistir la fila en **`AspNetUserLogins`**. Si el usuario tiene **2FA activada** (`TwoFactorEnabled`), no se emite JWT hasta completar `POST /api/v1/auth/mfa/verify`. En caso contrario, inmediatamente después se llama al **mismo** `IJwtTokenService.GenerateAccessToken` (y al flujo de refresh descrito más abajo) que en `POST /api/v1/auth/login`, de forma que el cliente recibe **access JWT + refresh** idénticos en estructura y uso.

```csharp
// Program.cs — fragmento ilustrativo (esquemas y nombres según el proyecto)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
// AddJwtBearer se registra en RA-869d7eze3 (/account/mfa/*): RoleClaimType = "role"
// y resolución de tenant desde claim organization_id. Hasta entonces el emisor
// JwtTokenService ya produce tokens con esos claims.
.AddJwtBearer(/* validación del access token; RoleClaimType = "role" */)
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

> **Nota operativa — consola Meta (desarrollo local, RA-869d7ezbm):** app en modo desarrollo con (a) «Dominios de la aplicación» = `localhost` y plataforma «Sitio web» con `http://localhost:5218/`; (b) permiso **`email` añadido al caso de uso** (sin él Meta responde `Invalid Scopes: email` — el handler lo solicita por defecto y la lógica de vinculación lo exige); (c) la URI `http://localhost:5218/signin-facebook` **no** necesita registrarse (localhost permitido por defecto en desarrollo).

**Detalles de implementación (RA-869d7ez7e, 2026-07-18; Instagram RA-869d7ezbm, 2026-07-19):**
- **Registro condicional de proveedores:** sin credenciales, el handler OAuth aborta el arranque; con registro condicional la API arranca en cualquier máquina (solo se añaden los esquemas cuya configuración esté completa). `google` activo; `apple` implementado y latente; `instagram` **implementado y verificado** (esquema `"Instagram"` vía `AddFacebook`, paquete `Microsoft.AspNetCore.Authentication.Facebook` 8.0.0; `LoginProvider = "Instagram"`).
- Cookie externa `IdentityConstants.ExternalScheme` de **un solo uso** (se consume en el callback).
- **PKCE:** los handlers de Google **y Facebook** de .NET 8 emiten `code_challenge` S256 automáticamente; no requiere implementación propia.
- `CorrelationCookie.SameSite = Lax` para Google/Instagram (desarrollo HTTP; flujo redirect GET).
- **Endurecimientos transversales (los tres proveedores):** `CorrelationCookie.SecurePolicy = SameAsRequest` (el default `Secure` rompe el flujo en HTTP local con navegadores estrictos como Safari; en HTTPS el flag vuelve automáticamente) y `Events.OnRemoteFailure` → redirección a `{origen permitido}/auth/callback#error=external_auth_failed` (cancelaciones de consentimiento y fallos de intercambio aterrizan en la SPA, sin filtrar el motivo).
- Apple requiere **HTTPS** por su `form_post`; el `ClientSecret` se genera con `GenerateClientSecret` y la clave privada desde `Authentication:Apple:PrivateKey` (nunca en repositorio).

**2FA opcional (Identity):** Usar `UserManager` para `ResetAuthenticatorKeyAsync`, `SetTwoFactorEnabledAsync`, y el flujo de verificación con `VerifyTwoFactorTokenAsync` (proveedor `Authenticator`). Los endpoints bajo `/api/v1/account/mfa/*` encapsulan alta, confirmación, baja y regeneración de códigos de recuperación. El login local con 2FA activa devuelve primero un **ticket de un solo uso** (o flujo equivalente) validable solo en `mfa/verify`.

En la práctica (decisión RA-869d7ez7e, 2026-07-18), el flujo «challenge → IdP → callback → tokens al cliente» termina con **redirección final a la SPA**: `returnUrl` se **valida contra `Cors:AllowedOrigins`** (anti open-redirect) y los tokens viajan en el **fragmento de URL** (`#...`), de modo que no llegan al servidor ni a logs de acceso. Un **código de un solo uso** intercambiable por tokens queda documentado como endurecimiento futuro; la cookie de correlación de ASP.NET Core sigue usándose durante el round-trip con el IdP.

#### 9.2.2 Refresh Token Service

> **v2 (2026-07-17, RA-869d7ez3e):** Modelo alineado con la implementación real. Tabla `RefreshTokens`: `UserId` es `int` (FK a `AspNetUsers`, borrado en cascada), índice **único** sobre `Token` (`nvarchar(200)`), `CreatedByIp` `nvarchar(45)`. En cada uso hay **rotación**: el token consumido se marca `IsRevoked = true` y se emite un par access+refresh nuevo en la misma operación (`AuthService.RefreshTokenAsync` / `IssueTokensAsync`).

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

---

### 9.3 Protección contra Ataques

#### 9.3.1 Rate Limiting

```csharp
// Usar AspNetCoreRateLimit
// Startup.cs / Program.cs
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

var app = builder.Build();
app.UseIpRateLimiting();
```

```json
// appsettings.json — fragmento; esquema completo de secciones (JWT, OAuth, Redsys, Cloudinary, SES, Hangfire, multi-tenant, etc.) y jerarquía con User Secrets / producción: volumen 1 §5.1.3
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*:/api/v1/auth/login",
        "Period": "1h",
        "Limit": 10
      },
      {
        "Endpoint": "*:/api/v1/auth/external/*/challenge",
        "Period": "1h",
        "Limit": 30
      },
      {
        "Endpoint": "*:/api/v1/auth/mfa/verify",
        "Period": "1h",
        "Limit": 20
      },
      {
        "Endpoint": "*:/api/v1/auth/register",
        "Period": "1d",
        "Limit": 5
      },
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

#### 9.3.2 CAPTCHA para Login

```vue
<!-- frontend-web/src/components/auth/LoginForm.vue -->
<script setup lang="ts">
import { ref } from 'vue'
import VueRecaptcha from 'vue-recaptcha' // o integración equivalente con reCAPTCHA v2/v3

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

La **estrategia completa de pruebas** (pirámide unitaria / integración / E2E, simulación de Redsys, CI/CD, cobertura por fase y tablas de herramientas) está recogida en el documento independiente **[`Documentation/reservarte-testing-strategy.md`](reservarte-testing-strategy.md)**. Este volumen mantiene los detalles de **seguridad y pagos**; el lector debe usar el documento de testing para alinear `tests/ReservArte.UnitTests`, `tests/ReservArte.IntegrationTests` y `tests/ReservArte.E2ETests` con el roadmap del volumen 3.

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