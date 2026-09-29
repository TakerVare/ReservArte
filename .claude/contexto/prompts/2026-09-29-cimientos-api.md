# Prompt para la IA de documentación — cierre del bloque «Cimientos de la API» (2026-09-29)

> Preparado por Claude Code con `/cerrar-bloque`. Guillermo lo pega entero, en **modo Agent**, en un
> chat nuevo de Cursor. La migración a PostgreSQL (`869f8pmnm`, `869f8pmpa`, ADR-031) **no** va aquí:
> tiene su propio prompt en `869f8pmq4`, cuando el Windows esté migrado.

~~~text
# Documentación del bloque «Cimientos de la API antes de los endpoints de Citas» (ClickUp 869f6r5r2:
# 869f6r81n, 869f74u70, 869f1k17q) + trabajo de la misma fase (869f6r5jf, 869f6r5ng, 869f2gh37)

## 0. Auditoría de coherencia (obligatoria, antes de cambiar nada)
Comprueba que está aplicado el prompt anterior (cierre de la Fase 1, 2026-09-28). Señales: existen
Documentation/adr/ADR-029-awesomeassertions.md y ADR-030-mapeo-mapperly.md, y el README de adr los
lista en su índice. Si no, detente y repórtalo.

Fuentes de contexto que puedes LEER (no editar): .claude/contexto/decisiones.md (texto exacto de
H-38 y H-39), .claude/rules/contrato-api.md y .claude/rules/backend.md (reglas vigentes del
contrato y de los tests). Si algo de este prompt contradice esas fuentes, repórtalo sin corregirlo.

Aviso: el motor de base de datos ya es PostgreSQL 18 (D-28), pero su documentación llega en un
prompt aparte. NO cambies aún las referencias a SQL Server de los volúmenes; si alguna choca con
este prompt (p. ej. los tests de integración usan PostgreSQL), anótala en las advertencias.

## 1. Qué se ha hecho (contexto; no lo copies como registro de estado)
- 869f6r5jf (PR #90), correcciones menores de la auditoría:
  - Email:Provider elige el proveedor de correo (File | Ses); sin un valor válido la API no arranca.
  - MultiTenant se valida al arrancar: estrategia Header o Subdomain; BaseDomain obligatorio con
    Subdomain; DefaultOrganizationId vacío o GUID; Header y DefaultOrganizationId solo en Development.
  - El 400 ORG_TENANT_NOT_RESOLVED ya no dice el motivo ni la estrategia al cliente (van al log).
  - Fuera la sección IpRateLimiting (no se usaba). Si el host falla al arrancar, la API sale con
    código 1.
- 869f6r5ng (PR #93), tests de integración:
  - Proyecto tests/ReservArte.IntegrationTests: la API entera en memoria (WebApplicationFactory) contra
    PostgreSQL 18 real (Testcontainers.PostgreSql 4.15.0 y Microsoft.AspNetCore.Mvc.Testing 10.0.12,
    ambos MIT). Necesitan Docker.
  - En el CI, un paso para los unitarios y otro para los de integración, cada uno con su fichero TRX.
  - Estos tests destaparon un fallo, arreglado en el mismo PR: la disponibilidad recortaba las
    ausencias (guardadas en UTC) contra la medianoche local sin convertir. Ahora el día se calcula en
    Europe/Madrid, se consulta en UTC y cada ausencia se pasa a hora local antes de recortarla.
- 869f2gh37 (PR #94), contrato HTTP de Empleados y Clientes cubierto por tests (roles, envelope
  también en 401/403, 201 con Location, 400 con campos en camelCase, 404, 409):
  - Límite conocido y aceptado: el token de acceso de una cuenta dada de baja sigue siendo válido
    hasta que caduca (60 min). La baja bloquea el login, el refresco, la 2FA y el OAuth.
- 869f6r81n (PR #95), mapa único de códigos de error:
  - ErrorStatusCodes (ReservArte-Shared/Api) asigna un status a cada código del catálogo; un test
    falla si un código no lo tiene; un código fuera del catálogo sale como 500.
  - ApiControllerBase, base de los 10 controladores con envelope: Meta, FromFailure, Failure y
    ValidateAsync (camelCase en cada tramo de la ruta del campo).
  - Result<T> es el único tipo de resultado; AuthResult<T>, idéntico, se retiró.
  - PAY_REDSYS_DECLINED pasa a 402 (el catálogo decía «402/422»).
  - Excepción pendiente de 869en8a17: el TOTP incorrecto en /account/mfa responde aún 400 con
    AUTH_INVALID_CREDENTIALS.
- 869f74u70 (PR #96), manejador global de excepciones:
  - Cualquier excepción no controlada responde 500 GEN_INTERNAL_ERROR con envelope y meta.requestId,
    y se registra en el log con ese RequestId.
  - error.details lleva el tipo y el mensaje de la excepción SOLO en Development; la traza no sale
    nunca. Si el cliente corta la petición, se registra 499 sin cuerpo.
  - El middleware de tenant, el rate limiter y los eventos de JwtBearer escriben sus errores con
    ApiErrorWriter, que toma el status del mismo mapa.
- 869f1k17q (PR #97), errores anteriores a la acción:
  - Los 400 de model binding (JSON mal formado, cuerpo vacío, valores no convertibles) salen con
    envelope GEN_VALIDATION_FAILED y un detalle por campo, en vez de ProblemDetails. Códigos de
    detalle y mensajes fijos en español:
    - InvalidJson: el campo es la ruta JSON en camelCase, p. ej. weeklySchedule[0].dayOfWeek.
    - MissingBody: con field "body".
    - InvalidFormat: parámetro de ruta o consulta, p. ej. page=abc.
  - El 404 de una ruta inexistente y el 405 salen con envelope, solo bajo /api y solo si la
    respuesta iba vacía. Código nuevo GEN_METHOD_NOT_ALLOWED (405), con la cabecera Allow.

## 2. Cambios por documento
### Vol. 1 — Análisis
- §5.1.1 (contrato de respuesta):
  - Retira el aviso de hueco de RA-869f1k17q y añade a la tabla de cobertura del envelope:
    - 500 no controlado → GlobalExceptionHandler, sí (GEN_INTERNAL_ERROR);
    - 400 de model binding → InvalidModelStateResponse, sí (GEN_VALIDATION_FAILED);
    - 404 de ruta inexistente y 405 → ApiStatusCodePages, sí (GEN_NOT_FOUND / GEN_METHOD_NOT_ALLOWED);
    - 429 → rate limiter, sí (GEN_RATE_LIMITED).
  - Actualiza el emisor del 400/403 de tenant (ahora ApiErrorWriter).
  - Las únicas excepciones sin envelope siguen siendo los webhooks de Redsys, los health checks y
    las rutas fuera de /api.
  - En «Reglas», fija la convención de details que hoy dice «a fijar en OpenAPI»:
    { field, code, message }, field en camelCase con la ruta completa, y los tres códigos de model
    binding (InvalidJson, MissingBody, InvalidFormat) junto a los de FluentValidation.
  - Documenta que en Development error.details del 500 lleva { exception, message } y fuera de
    Development es null.
- §5.1.2 (catálogo):
  - Añade GEN_METHOD_NOT_ALLOWED (405).
  - Fija PAY_REDSYS_DECLINED en 402.
  - Añade una nota: la tabla es la de ErrorStatusCodes y un test exige que cada código tenga status
    (enlaza al ADR-032, no copies el código).
  - Mantén la nota de AUTH_MFA_INVALID: el comportamiento no ha cambiado.
- §5.1.3 (configuración): Email:Provider (File | Ses; sin valor válido la API no arranca) y la
  validación de MultiTenant al arrancar, con sus reglas (ver 869f6r5jf arriba).
- Nota de ORG_TENANT_NOT_RESOLVED (junto a §5.1.3): el 400 ya no incluye motivo ni estrategia; el
  requestId enlaza con la línea de log que los tiene.
- Si hay una sección de seguridad de sesiones o de bajas de empleadas: añade el límite de
  869f2gh37 (el JWT vivo de una cuenta dada de baja vale hasta caducar).

### Vol. 2 — Implementación y desarrollo
- §9.3.1 (rate limiting): el 429 se escribe con ApiErrorWriter y lleva Retry-After. Quita
  IpRateLimiting si aún aparece.
- §9.4 (auditoría y logging): una excepción no controlada se registra como error con su RequestId;
  el 499 de un cliente que corta se registra como información.
- §9.9 (citas, disponibilidad): las ausencias están en UTC y el horario y las citas en hora local
  del centro; la disponibilidad convierte la ventana del día a UTC y cada ausencia a Europe/Madrid
  antes de recortarla. La zona sigue fija hasta 869f74u7y.
- Nueva subsección §9.11 «Contrato de errores en código»:
  - ErrorStatusCodes, ApiControllerBase (FromFailure, Failure, ValidateAsync), ApiErrorWriter,
    GlobalExceptionHandler, InvalidModelStateResponse y ApiStatusCodePages;
  - el orden en el pipeline: UseExceptionHandler y UseStatusCodePages justo detrás de
    UseSerilogRequestLogging;
  - Result<T> como único tipo de resultado.
  - Enlaza ADR-032 y vol. 1 §5.1.1-5.1.2 en vez de repetir las tablas.

### Vol. 3 — Planificación y gestión
- Nada de estado. Solo si el plan cita los tests de integración como pendientes o con SQL Server:
  márcalo en advertencias.

### Otros
- reservarte-testing-strategy.md:
  - §4 «Capa de integración»: reescríbela con lo vigente (H-39):
    - WebApplicationFactory<Program> en Development, contra postgres:18 con Testcontainers;
    - una colección con un contenedor compartido y un centro B sembrado por la fixture;
    - cada test crea sus datos, sin depender de recuentos;
    - configuración propia que se impone a los User Secrets;
    - tokens de rol con IJwtTokenService (el login admite 10/h);
    - variantes con WithWebHostBuilder para sustituir servicios o aislar el rate limiter;
    - Docker como requisito.
    - Qué va aquí y qué en unitarios: todo lo que dependa del motor (mayúsculas, CHECK, fechas,
      filtros y orden en SQL, aislamiento por HTTP) y el contrato HTTP (roles, envelope, status).
  - §3.1 Backend (advertencia del prompt anterior): depura el bloque histórico de suites (recuentos,
    PRs, AutoMapper y *ProfileTests). Deja solo lo vigente: xUnit + Moq + AwesomeAssertions,
    repositorios contra SQLite en memoria y lo que SQLite no reproduce, que va a integración.
  - §9 «Integración con CI/CD»: dos pasos de test en el job build-test-format (unitarios e
    integración), cada uno con su TRX; los de integración se ejecutan aunque fallen los unitarios si
    el build fue bien. El nombre del job, check obligatorio en main, no cambia.
  - §10: añade las dos librerías nuevas con su licencia (MIT).
- Documentation/Project-Init (guía de user secrets): los tests de integración no leen los User
  Secrets del equipo (la fixture fija su configuración). Solo si la guía habla de tests.

## 3. ADR
- ADR-031 queda RESERVADO para la migración a PostgreSQL (D-28, H-37); llega en otro prompt. Usa 032
  y 033.
- Nuevo ADR-032 «Contrato de errores de la API: mapa único y envelope en todo el pipeline» (H-38).
  - Contexto: cada controlador tenía su copia del mapa código → status y ya divergían (Auth mandaba
    a 400 los códigos desconocidos; solo Disponibilidad conocía APT_SLOT_UNAVAILABLE). Había
    respuestas sin envelope: excepciones no controladas, model binding, 404 de ruta y 405.
  - Decisión: lo que dice H-38.
  - Alternativas descartadas:
    - ProblemDetails (RFC 9457) como formato de error: rompería el contrato del envelope que ya
      consume la SPA.
    - Mantener el mapa por controlador con una regla de revisión: ya había divergido.
    - Mandar a 400 los códigos desconocidos: disfraza un fallo del servidor de error del cliente.
    - Exponer la traza en Development: basta con el tipo y el mensaje; la traza va al log.
  - Consecuencias:
    - un código nuevo exige su status (lo vigila un test);
    - la SPA recibe siempre el mismo formato;
    - quedan dos 400 escritos a mano en MFA hasta 869en8a17.
- Nuevo ADR-033 «Tests de integración contra PostgreSQL real con Testcontainers» (H-39).
  - Contexto: SQLite no reproduce la comparación de texto, timestamptz, los CHECK ni los Kind de
    DateTime que exige Npgsql. Nada probaba el contrato HTTP. En su primera ejecución destaparon un
    500 en la consulta de huecos que no habían visto ni las pruebas manuales ni los E2E.
  - Decisión: lo que dice H-39.
  - Alternativas descartadas:
    - SQLite o un proveedor en memoria para integración: no reproduce el motor.
    - Una base compartida fuera de Docker: estado entre ejecuciones y riesgo de tocar la de
      desarrollo.
    - Un contenedor por clase: arranque lento sin ganancia, porque los datos ya se aíslan por test.
  - Consecuencias:
    - Docker es requisito para dotnet test de la solución (en los equipos y en el CI);
    - los tests comparten base y no pueden contar filas globales;
    - el login está limitado, así que los tokens de rol se emiten sin él.
- Enlaza los dos desde el README de adr. decisiones.md lo enlaza Claude Code después: no lo toques.

## 4. Restricciones
- No añadas registros de estado, PRs ni recuentos a los volúmenes (los de la sección 1 son contexto).
- Un dato, una fuente: enlaza en vez de copiar (la tabla de status vive en vol. 1 §5.1.2 y en
  ErrorStatusCodes; el resto la enlaza).
- No verifiques IDs de ClickUp.
- No toques .claude/ ni .cursor/.
- Si algo contradice otro documento o una decisión, repórtalo sin corregirlo.

## 5. Advertencias
Señala todo lo que veas dudoso o desfasado, aunque no esté en este prompt. En particular:
- referencias a SQL Server que vayan a chocar con los tests de integración (no las cambies);
- cualquier sitio que aún diga que el 500, el model binding o el 404/405 salen sin envelope.
~~~
