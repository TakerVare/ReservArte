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
dotnet user-secrets set "Redsys:WebhookBaseUrl" "https://xxxx.ngrok-free.app"
dotnet user-secrets set "Redsys:{ORGANIZATION_GUID}:SecretKey" "sq7HjrUOBfKmC576ILgskD5srU870gJ7"
```

## JWT — clave de firma (`Jwt:SecretKey`)

La clave simétrica del emisor JWT (**vol. 2 §9.2.1**, tarea RA-869d7eyze) **no** va en `appsettings` ni en el repositorio. Generarla en cada máquina de desarrollo (Windows y macOS):

```bash
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 48)" --project ReservArte-API
```

En Windows sin `openssl` en PATH, usar Git Bash, WSL o generar 48 bytes aleatorios en Base64 con otro método equivalente y asignarlos manualmente con `dotnet user-secrets set "Jwt:SecretKey" "<valor>" --project ReservArte-API`.

Completar también `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes` y `Jwt:RefreshTokenDays` según el contrato del vol. 1 **§5.1.3** (valores no secretos pueden ir en `appsettings.Development.json`).

## Notas de operación — MFA / SQL en Docker (RA-869d7ezgy)

- Escrituras vía `docker exec ... sqlcmd` contra SQL Server en contenedor: empezar el batch con `SET QUOTED_IDENTIFIER ON;` (sin eso fallan sentencias que tocan objetos con índices filtrados / Identity).
- Cada `POST /api/v1/account/mfa/enable` **regenera** el secreto TOTP. Tras el último `enable` exitoso, escanear el QR (o teclear `manualEntryKey`) y pasar a `confirm` **sin** volver a llamar a `enable`; si no, la app autenticadora queda desincronizada respecto al secreto almacenado.
## Google OAuth (`Authentication:Google`)

Credenciales de la consola Google Cloud (OAuth 2.0). Configurarlas en **cada máquina** de desarrollo; no van en el repositorio:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project ReservArte-API
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project ReservArte-API
```

En GCP, registrar el redirect URI del handler de ASP.NET Core: `http://localhost:5218/signin-google` (ajusta host/puerto si el perfil de lanzamiento local difiere).

## Meta / Instagram OAuth (`Authentication:Meta`)

Credenciales de [Meta Developers](https://developers.facebook.com/) (Facebook Login / Instagram). Configurarlas en **cada máquina** de desarrollo; no van en el repositorio:

```bash
dotnet user-secrets set "Authentication:Meta:AppId" "<app-id>" --project ReservArte-API
dotnet user-secrets set "Authentication:Meta:AppSecret" "<app-secret>" --project ReservArte-API
```

En modo desarrollo: dominios de la app = `localhost`, plataforma «Sitio web» = `http://localhost:5218/`, permiso **`email`** en el caso de uso. La URI `http://localhost:5218/signin-facebook` no requiere registro explícito en localhost.

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
