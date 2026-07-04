# RESERVARTE — Guía de desarrollo local con Redsys (entorno test)

**Documento:** Desarrollo y pruebas de pagos InSite / REST sin bloquear el sprint  
**Versión:** 1.0  
**Fecha:** mayo 2026  
**Proyecto:** ReservArte — Sistema multi-tenant de gestión para centros de diseño de cejas  
**Referencias:** volumen 1 (**§5.2**, **§5.3.1**, **§5.3.2**), volumen 2 (**§7.3**, **§7.4**), [`user-secrets-guide.md`](Project-Init/user-secrets-guide.md) (onboarding secretos), [Redsys — Tarjetas y entornos de prueba](https://pagosonline.redsys.es/desarrolladores-inicio/integrate-con-nosotros/tarjetas-y-entornos-de-prueba/), [Redsys — Parámetros y códigos de respuesta](https://pagosonline.redsys.es/desarrolladores-inicio/integrate-con-nosotros/parametros-de-entrada-y-salida/)

---

## Índice

1. [Configuración inicial del entorno Redsys test](#1-configuración-inicial-del-entorno-redsys-test)
2. [Tarjetas de prueba y escenarios](#2-tarjetas-de-prueba-y-escenarios)
3. [Simulación del webhook en local](#3-simulación-del-webhook-en-local)
4. [Flujo de prueba completo paso a paso](#4-flujo-de-prueba-completo-paso-a-paso)
5. [Errores frecuentes y soluciones](#5-errores-frecuentes-y-soluciones)
6. [Logs útiles para depuración](#6-logs-útiles-para-depuración)

---

## 1. Configuración inicial del entorno Redsys test

### 1.1 Qué credenciales necesitas y dónde pedirlas

| Dato | Descripción | Origen |
|------|-------------|--------|
| **FUC** (`DS_MERCHANT_MERCHANTCODE`) | Código de comercio de 9 dígitos | Entidad **adquirente** / banco; o cuenta de prueba en el [Portal TPV Virtual sandbox](https://sis-t.redsys.es:25443/admincanales-web/index.jsp) |
| **Terminal** (`DS_MERCHANT_TERMINAL`) | Normalmente `001` en pruebas | Mismo correo / portal |
| **Clave de firma** (Base64, operaciones **SHA-256** según implementación del volumen 2) | Firma HMAC de peticiones REST | Email del banco o portal; **no** commitear en el repositorio |
| **Entorno** | `test` vs `production` | Solo **test** (`https://sis-t.redsys.es:25443/sis/rest/trataPeticionREST`) en desarrollo local |

**Datos genéricos públicos de sandbox Redsys** (válidos para comprobar conectividad sin TPV propio; el banco te dará credenciales definitivas de comercio de prueba):

| FUC | Terminal | Clave de firma (ejemplo documentación Redsys) |
|-----|----------|-----------------------------------------------|
| `999008881` | `001` | `sq7HjrUOBfKmC576ILgskD5srU870gJ7` |

> En ReservArte el FUC y el terminal suelen persistirse por **organización** (modelo en volumen 1 **§5.2**). La **clave de firma** por organización se resuelve como en el volumen 2 (`Redsys:{OrganizationId}:SecretKey` vía configuración / Secrets Manager).

### 1.2 Configuración vía User Secrets

Desde el directorio del proyecto API (`ReservArte.API`):

```bash
cd src/ReservArte.API   # ajustar ruta real de la solución

dotnet user-secrets init   # si el .csproj aún no tiene UserSecretsId

# Clave de firma del comercio de prueba (GUID de la organización sembrada en BD local)
dotnet user-secrets set "Redsys:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee:SecretKey" "sq7HjrUOBfKmC576ILgskD5srU870gJ7"

# URL pública base para construir DS_MERCHANT_MERCHANTURL (tras ngrok / Cloudflare — ver §3)
dotnet user-secrets set "AppUrl" "https://tu-subdominio.ngrok-free.app"

# Opcional: duplicar como base explícita de webhooks si el código la lee
dotnet user-secrets set "Redsys:WebhookBaseUrl" "https://tu-subdominio.ngrok-free.app"
```

**Formato:** mismas rutas jerárquicas que en [`user-secrets-guide.md`](Project-Init/user-secrets-guide.md) y volumen 1 **§5.1.3** (`Section:SubKey` con `dotnet user-secrets set "Clave" "Valor"`). Si el GUID de la organización de desarrollo es otro, sustituye `aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee`.

Comprueba que los secretos están cargados:

```bash
dotnet user-secrets list
```

### 1.3 Verificación antes de arrancar

1. **BD:** la organización de prueba tiene `RedsysMerchantCode`, `RedsysTerminal`, `RedsysEnvironment = 'test'` (o equivalente) alineado con el volumen 1.
2. **SecretKey:** existe entrada `Redsys:{OrganizationId}:SecretKey` para ese `OrganizationId`.
3. **AppUrl / WebhookBaseUrl:** URL **HTTPS** alcanzable desde internet (no `https://localhost` a menos que uses túnel — §3).
4. **REST:** el volumen 2 apunta a `REDSYS_TEST_URL` = `https://sis-t.redsys.es:25443/sis/rest/trataPeticionREST` para peticiones `trataPeticion`.

**Prueba mínima:** arrancar la API y llamar al endpoint que expone configuración pública InSite (p. ej. `GET .../payments/redsys/config` según implementación) y comprobar que devuelve FUC/terminal de test sin errores de configuración.

---

## 2. Tarjetas de prueba y escenarios

### 2.1 Convenciones generales (Redsys sandbox)

Según la [documentación oficial](https://pagosonline.redsys.es/desarrolladores-inicio/integrate-con-nosotros/tarjetas-y-entornos-de-prueba/):

- Las tarjetas **solo funcionan en entorno de pruebas**.
- Para la mayoría de PAN de prueba, la **caducidad puede ser cualquier fecha futura**; lo habitual es **`12/49`**.
- El **CVV** suele ser **`123`**, salvo que se indique otro valor para simular un error (**`999`**, **`172`**, etc.).

Los valores de **`Ds_Response`** en la tabla siguiente provienen del glosario oficial ([parámetros y códigos de respuesta](https://pagosonline.redsys.es/desarrolladores-inicio/integrate-con-nosotros/parametros-de-entrada-y-salida/)): éxito de autorización/pre-autorización en el rango **`0000`–`0099`**; confirmación/devoluciones pueden devolver **`900`** según tipo de operación.

> **Importante:** En sandbox, el simulador **EMV 3DS** puede pedir elegir el resultado (éxito / fallo). El **`Ds_Response` final** debe tomarse siempre de la **notificación** (`Ds_MerchantParameters` decodificado) o de la respuesta REST, no solo de la pantalla del navegador.

### 2.2 Tabla de escenarios para ReservArte

PAN sin espacios como en integración; marcas y PAN salvo **documentación Redsys** (mayo 2026).

| # | Escenario | PAN | Caducidad | CVV | `Ds_Response` esperado (orientativo) | Comportamiento esperado en la aplicación |
|---|-----------|-----|-----------|-----|--------------------------------------|------------------------------------------|
| 1 | **Pago / pre-autorización autorizada** | `4548810000000003` | 12/49 | 123 | `0000`–`0099` | `RedsysPaymentResult.IsSuccess = true`; fila en `payments` con `RedsysResponseCode` en rango éxito; cita puede pasar a confirmada según reglas; log en `redsys_transaction_log` con `is_success = true`. |
| 2 | **Denegación genérica (CVV especial)** | `4548810000000003` | 12/49 | **999** | `172` / `173` / `174` / `190` (según tabla CVV Redsys) | Tras autenticación simulada «correcta», denegación emisor; API devuelve error negocio p. ej. `PAY_REDSYS_DECLINED`; sin captura; mensaje al usuario. |
| 3 | **Tarjeta caducada** | `4548810000000003` | **12/20** (pasada) o fecha inválida | 123 | `101` o `191` | Rechazo; mensaje tipo caducidad (`GetErrorMessage` volumen 2); no crear pago autorizado. |
| 4 | **Saldo insuficiente** | `4548810000000003` | 12/49 | 123 | **`116`** (disponible insuficiente) | Redsys puede devolver `116` en sandbox según emisor simulado; mismo manejo que declinación (`IsSuccess = false`). *Alternativa documentada Redsys:* importes con céntimos **`,96` / `,72` / `,73` / `,74`** disparan denegaciones análogas a las de CVV especiales — útil para pruebas repetibles. |
| 5 | **Tarjeta bloqueada / no operativa** | `4548810000000003` o PAN de prueba alternativo | 12/49 | 123 | **`102`**, **`106`**, **`163`** | Declinación sin completar operación; UI muestra error genérico; log con `response_code` correspondiente. |
| 6 | **CVV incorrecto** | `4548810000000003` | 12/49 | distinto de `123` y no especial | **`129`** | Validación CVV fallida; no autorizar. |
| 7 | **3DS: autenticación superada** | `4548814479727229` (VISA Frictionless 2.1) | 12/49 | 123 | `0000`–`0099` | Flujo EMV3DS frictionless o challenge completado en simulador «OK»; pre-auth o pago OK. |
| 8 | **3DS: autenticación fallida** | `4548817212493017` (VISA Challenge 2.1) | 12/49 | 123 | **`184`**, **`123`**, **`9915`** (cancelación usuario) | En simulador elegir **fallo** o cancelar; operación no autorizada; mensaje acorde. |
| 9 | **Pre-autorización correcta (tipo 1)** | `4548810000000003` | 12/49 | 123 | `0000`–`0099` | Petición REST con `DS_MERCHANT_TRANSACTIONTYPE` = **`1`** (volumen 1 **§5.3** y volumen 2 **§7.3**); `appointments.RedsysOrderNumber` / token preauth actualizados. |
| 10 | **Confirmación de pre-autorización (tipo 2)** | Mismo pedido que #9 | — | — | **`900`** (confirmación aceptada, según tabla Redsys) + verificar doc actual | `CaptureAsync`: `DS_MERCHANT_TRANSACTIONTYPE` = **`2`**, importe en céntimos; actualizar pago y estado de cita. |
| 11 | **Cancelación de pre-autorización (tipo 9)** | Mismo pedido que #9 | — | — | **`900`** o rango anulación (`400` según tabla) | `CancelAsync`: `DS_MERCHANT_TRANSACTIONTYPE` = **`9`**; liberar importe; estado coherente con volumen 2 **§7.6**. |
| 12 | **Tokenización (COF inicio + `DS_MERCHANT_IDENTIFIER`)** | `4548810000000003` | 12/49 | 123 | `0000`–`0099` + datos COF | Petición con `DS_MERCHANT_IDENTIFIER` = **`REQUIRED`**, `DS_MERCHANT_COF_INI` = **`S`**, `DS_MERCHANT_COF_TYPE` = **`R`** (volumen 2); en respuesta, `Ds_Merchant_Identifier` / `Ds_Merchant_Cof_Txnid`; persistir `CustomerPaymentMethod`. |
| 13 | **Pago con tarjeta guardada (COF subsiguiente)** | N/A (usa token) | — | — | `0000`–`0099` | `DS_MERCHANT_TRANSACTIONTYPE` = **`1`** (u operación acordada), `DS_MERCHANT_IDENTIFIER` = **token almacenado**, `DS_MERCHANT_COF_INI` = **`N`**, `DS_MERCHANT_COF_TYPE` = **`R`**, `DS_MERCHANT_COF_TXNID` = **`RedsysCofTxnid`** (volumen 2 **§7.4.2**). |

**Mastercard genérica EMV3DS 2.1:** `5576441563045037`, 12/49, 123 — mismo uso que VISA para comprobar marca alternativa.

### 2.3 Ampliación de `user-secrets-guide.md`

La tabla anterior **sustituye y amplía** la tabla básica de tarjetas que debe mantenerse sincronizada en [`Documentation/Project-Init/user-secrets-guide.md`](Project-Init/user-secrets-guide.md): copiar o enlazar esta sección para que el onboarding y esta guía de sprint no diverjan.

---

## 3. Simulación del webhook en local

### 3.1 El problema

En la petición al TPV, el comercio envía **`DS_MERCHANT_MERCHANTURL`** con la URL del webhook (`POST /api/v1/payments/redsys/webhook`, volumen 2). Los servidores de **Redsys están en internet**; **`http://localhost:5xxx` no es enrutable** desde Redsys. Sin una URL pública HTTPS, la **notificación HTTP POST** del resultado **no llegará** al desarrollador, aunque la respuesta síncrona del REST (`CompleteInsitePaymentAsync`) pueda ser correcta.

### 3.2 Comparativa de opciones

| Opción | Ventajas | Inconvenientes | Cuándo usarla |
|--------|----------|-----------------|---------------|
| **ngrok** | Arranque rápido, HTTPS automático, muy usado en equipos | Plan gratuito: dominio **cambia** al reiniciar (hay que actualizar `AppUrl` y secretos) | **Recomendado** para el sprint de pagos día a día |
| **Cloudflare Tunnel** | URL más **estable** en planes gratuitos razonables; sin exponer IP | Más pasos (cuenta Cloudflare, `cloudflared`, config YAML) | Cuando el equipo quiere **menos rotación** de URL |
| **Sin webhook (solo respuesta REST / cliente)** | Cero túnel | **No** replica notificaciones asíncronas ni reintentos de Redsys; riesgo de desincronizar si el usuario cierra el navegador antes del POST | Solo smoke muy temprano; **no** sustituye validar webhook antes de pre-producción |

### 3.3 ngrok (recomendado)

1. **Instalación** (macOS con Homebrew):

```bash
brew install ngrok/ngrok/ngrok
```

2. **Autenticación** (token desde [dashboard ngrok](https://dashboard.ngrok.com/)):

```bash
ngrok config add-authtoken TU_TOKEN
```

3. **Arranque** hacia el puerto Kestrel local (ejemplos):

```bash
# Si la API escucha en HTTP 5000
ngrok http http://localhost:5000

# Si usa HTTPS local con dev-cert
ngrok http https://localhost:7001
```

4. Copia la URL **HTTPS** mostrada (p. ej. `https://abc123.ngrok-free.app`).

5. **User Secrets** (mismo formato que §1.2):

```bash
dotnet user-secrets set "AppUrl" "https://abc123.ngrok-free.app"
dotnet user-secrets set "Redsys:WebhookBaseUrl" "https://abc123.ngrok-free.app"
```

6. Reinicia la API. Las nuevas operaciones enviarán  
   `DS_MERCHANT_MERCHANTURL` = `{AppUrl}/api/v1/payments/redsys/webhook`  
   (como en volumen 2 **§7.3**).

**Limitación plan gratuito:** al reiniciar ngrok cambia el subdominio → actualizar secretos y volver a probar. Opciones de pago fijan dominio reservado.

### 3.4 Cloudflare Tunnel (alternativa)

1. Instalar `cloudflared` ([documentación Cloudflare](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/install-and-setup/installation/)).
2. Autenticar y crear un túnel con hostname bajo tu zona (p. ej. `pay-dev.tudominio.es`).
3. Enrutar el servicio local:

```bash
cloudflared tunnel run --url http://localhost:5000
```

4. Asignar el hostname estable a `AppUrl` / `Redsys:WebhookBaseUrl` en User Secrets.

### 3.5 Modo sin webhook

Válido solo si aceptas **no** probar:

- validación de firma en el **POST** de notificación,
- idempotencia ante reenvíos,
- cierre de navegador antes de procesar respuesta en cliente.

El volumen 2 implementa **`[HttpPost("redsys/webhook")]`** con lectura de `Ds_MerchantParameters` / `Ds_Signature`; en local **debe** probarse con túnel antes del despliegue.

---

## 4. Flujo de prueba completo paso a paso

Checklist para un desarrollador en **&lt; 1 h** (asume BD sembrada, User Secrets y ngrok):

1. **API y Swagger**  
   - Arranca `ReservArte.API`.  
   - Abre Swagger (`/swagger`) y verifica health / un GET público si existe.

2. **Pre-autorización OK**  
   - Crea cita de prueba y flujo InSite hasta obtener `idOper`.  
   - Llama a `POST /api/v1/payments/redsys/insite/init` + complete según volumen 2.  
   - Usa PAN `4548810000000003`, cad. `12/49`, CVV `123`.  
   - Completa 3DS en simulador si aparece.

3. **Webhook por túnel**  
   - Con ngrok activo, confirma en logs de la API la entrada **`POST /api/v1/payments/redsys/webhook`** (Serilog) y código `200` tras validación.

4. **Registro en BD**  
   - Comprueba `payments` y `appointments` (campos `redsys_*` del volumen 1 **§5.2**).  
   - Comprueba fila en **`redsys_transaction_log`** con `transaction_type` coherente (p. ej. preauth).

5. **Confirmación (captura)**  
   - Ejecuta captura parcial o total vía servicio / endpoint (`DS_MERCHANT_TRANSACTIONTYPE` = **`2`**, volumen 2).  
   - Verifica `Ds_Response` acorde (p. ej. `900` para confirmación según tabla Redsys).

6. **Cancelación de pre-autorización**  
   - Sobre otro pedido pre-autorizado sin capturar, llama a cancelación (`DS_MERCHANT_TRANSACTIONTYPE` = **`9`**).

7. **Guardar tarjeta y pago COF**  
   - Primera operación con `DS_MERCHANT_IDENTIFIER` = **`REQUIRED`**, `DS_MERCHANT_COF_INI` = **`S`**.  
   - Verifica `customer_payment_methods` (entidad `CustomerPaymentMethod`).  
   - Segunda operación con token + `DS_MERCHANT_COF_INI` = **`N`** (**§7.4.2**).

8. **Cancelación tardía con penalización**  
   - Cita con pre-auth, ajustar `OrganizationSettings.CancellationHoursThreshold` y hora de cita para forzar `CaptureAsync` de penalización (volumen 2 **§7.6**).  
   - Verificar importe capturado y estado de cita.

---

## 5. Errores frecuentes y soluciones

| Síntoma | Causa probable | Qué hacer |
|---------|----------------|-----------|
| **Firma HMAC inválida** (log / respuesta Redsys) | Clave Base64 incorrecta; mezcla entorno test/real; orden de parámetros distinto al usado al firmar | Verificar `Redsys:{OrganizationId}:SecretKey` con `dotnet user-secrets list`; misma clave que en portal sandbox; revisar `GenerateSignature` en `RedsysPaymentService` (volumen 2) y que se firme el mismo JSON Base64 que se envía en `Ds_MerchantParameters`. |
| **Número de pedido duplicado** | Reutilizar `DS_MERCHANT_ORDER` | `Ds_Response` **`913`** (pedido repetido). Generar **pedido único** por operación (12 caracteres alfanuméricos según restricciones Redsys). |
| **Webhook no recibido** | `AppUrl` localhost; ngrok parado; firewall | Túnel activo; `AppUrl` HTTPS público; probar `curl` externo al webhook; revisar que `DS_MERCHANT_MERCHANTURL` en JSON enviado sea el esperado. |
| **Timeout en pre-autorización** | Redsys lento o red; `HttpClient` sin timeout adecuado | Aumentar timeout de cliente HTTP en dev; repetir; revisar conectividad a `sis-t.redsys.es`. |
| **Error al decodificar `Ds_MerchantParameters`** | Cadena truncada; no Base64 válido; encoding | Loguear cuerpo crudo **sin PAN**; validar `Convert.FromBase64String` y UTF-8; comprobar que el webhook lee los mismos nombres de campo que el volumen 2 (`Ds_MerchantParameters`, `Ds_Signature`). |

---

## 6. Logs útiles para depuración

### 6.1 Serilog (API)

Buscar en logs estructurados (consola / archivo / CloudWatch según entorno):

- **`RedsysPaymentService`** — errores HTTP, cuerpo de error Redsys, «Firma de Redsys inválida».  
- **Webhook** — advertencias de firma, excepciones al parsear notificación.  
- **Prefijos** — `Pago rechazado. Código: {responseCode}` (volumen 2) para correlacionar con `Ds_Response`.

Añade temporalmente en desarrollo enriquecimiento con `OrganizationId` y `RedsysOrderNumber` (nunca PAN ni CVV).

### 6.2 Tabla `redsys_transaction_log` (volumen 1 **§5.2**)

| Columna | Uso en depuración |
|---------|-------------------|
| `redsys_order_number` | Correlación con `appointments.redsys_order_number` y pedido Redsys |
| `transaction_type` | PreAuth / Capture / Cancel / texto libre según implementación |
| `request_params` | JSON enviado (revisar `DS_MERCHANT_*` — coherente con volumen 2) |
| `response_params` | JSON devuelto; contiene `Ds_Response` tras decodificar |
| `response_code` | Copia rápida del código numérico |
| `is_success` | Filtro para ver solo fallos |
| `error_message` | Mensaje interno o de pasarela |
| `created_at` | Orden cronológico del flujo |

### 6.3 Códigos `Ds_Response` frecuentes

| Código | Significado (resumen) |
|--------|------------------------|
| `0000`–`0099` | Autorización / pre-autorización aceptada |
| `900` | Aceptada en confirmaciones / devoluciones (ver tipo transacción) |
| `101` | Tarjeta caducada |
| `116` | Disponible insuficiente |
| `129` | CVV incorrecto |
| `184` | Error autenticación titular (3DS) |
| `190` | Denegación sin especificar |
| `913` | Pedido repetido |
| `9915` | Cancelada por usuario |

Lista completa: [parámetros y códigos de respuesta — Ds_Response](https://pagosonline.redsys.es/desarrolladores-inicio/integrate-con-nosotros/parametros-de-entrada-y-salida/).

---

## Coherencia técnica con el repositorio documentado

- **Parámetros de entrada** al TPV en operaciones REST: mismos nombres que en el volumen 2 **§7.3** (`DS_MERCHANT_ORDER`, `DS_MERCHANT_MERCHANTCODE`, `DS_MERCHANT_TERMINAL`, `DS_MERCHANT_TRANSACTIONTYPE`, `DS_MERCHANT_AMOUNT`, `DS_MERCHANT_CURRENCY`, `DS_MERCHANT_IDOPER`, `DS_MERCHANT_MERCHANTURL`, `DS_MERCHANT_IDENTIFIER`, `DS_MERCHANT_COF_INI`, `DS_MERCHANT_COF_TYPE`, `DS_MERCHANT_COF_TXNID`).
- **Tipos de transacción:** **`1`** pre-autorización, **`2`** confirmación, **`9`** cancelación (**volumen 1 §5.3**; comentarios en entidad `Payment` / `appointments` con mapeo `0=Auth, 1=PreAuth, 2=Confirm, 9=Cancel`).
- **Implementación:** clase **`RedsysPaymentService`** en **`ReservArte.Infrastructure/Services/Payments/RedsysPaymentService.cs`** (estructura en `Análisis de pantallas y estructura.md`); interfaz **`IRedsysPaymentService`** en **`ReservArte.Application/Services/Payments/`**. Los fragmentos del volumen 2 muestran namespaces `ReservArte.Application.Services` por contexto histórico: al codificar, unificar con esta ruta física y registrar el servicio en DI desde Infrastructure.
- **Webhook y DTOs:** mismo contrato que volumen 2 **§7.3** (`Ds_SignatureVersion`, `Ds_MerchantParameters`, `Ds_Signature`).

---

**Fin de la guía de desarrollo Redsys local**
