# User Secrets — Guía rápida (ReservArte API)

**Complemento de:** volumen 1 **§5.1.3**, [`Documentation/redsys-development-guide.md`](../redsys-development-guide.md)

Todos los comandos se ejecutan desde el proyecto **`ReservArte.API`** (ajusta la ruta).

```bash
cd src/ReservArte.API
dotnet user-secrets init
```

## Formato de claves

Usar la notación jerárquica de ASP.NET Core con `dotnet user-secrets set "Clave" "Valor"`:

```bash
dotnet user-secrets set "Jwt:SecretKey" "..."
dotnet user-secrets set "Redsys:WebhookBaseUrl" "https://xxxx.ngrok-free.app"
dotnet user-secrets set "Redsys:{ORGANIZATION_GUID}:SecretKey" "sq7HjrUOBfKmC576ILgskD5srU870gJ7"
```

Listar y comprobar:

```bash
dotnet user-secrets list
```

## Redsys (sandbox) — resumen

La **tabla exhaustiva de tarjetas, CVV especiales, 3DS, COF y tipos de transacción** vive en **[`Documentation/redsys-development-guide.md`](../redsys-development-guide.md) §2**. Mantener una sola fuente de verdad: actualizar primero esa guía y, si hace falta, copiar aquí solo el resumen para onboarding.

### Credenciales genéricas de prueba (documentación Redsys pública)

| Uso | Valor |
|-----|--------|
| FUC | `999008881` |
| Terminal | `001` |
| Clave de firma (ejemplo sandbox) | `sq7HjrUOBfKmC576ILgskD5srU870gJ7` |

### Tarjeta VISA genérica EMV3DS (la más usada en desarrollo)

| Campo | Valor |
|-------|--------|
| PAN | `4548810000000003` |
| Caducidad | `12/49` (u otra futura, según Redsys) |
| CVV | `123` |

Para **denegación simulada con CVV**, **saldo**, **3DS challenge**, **COF** y **importes especiales**, ver **§2** de `redsys-development-guide.md`.

### URL pública del webhook (ngrok)

Tras `ngrok http http://localhost:5000`:

```bash
dotnet user-secrets set "AppUrl" "https://TU-SUBDOMINIO.ngrok-free.app"
dotnet user-secrets set "Redsys:WebhookBaseUrl" "https://TU-SUBDOMINIO.ngrok-free.app"
```

Detalle: `redsys-development-guide.md` §3.

---

**Nota:** No commitear secretos. El `UserSecretsId` solo en el `.csproj` local o de equipo, nunca valores sensibles en el repositorio.
