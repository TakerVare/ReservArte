# Prompt para la IA de documentación — cierre del bloque «Migración a PostgreSQL» (2026-09-29)

> Preparado por Claude Code con `/cerrar-bloque` (`869f8pmq4`). Guillermo lo pega entero, en **modo
> Agent**, en un chat nuevo de Cursor, **después** del de cimientos de la API (ya aplicado en
> `61ed752`). Copia solo lo que va entre las dos líneas `~~~`.

~~~text
# Documentación del bloque «Migración del motor de base de datos a PostgreSQL» (ClickUp 869f8pm99:
# 869f8pmnm, 869f8pmpa, 869f8pmpn; tarea de documentación 869f8pmq4)

## 0. Auditoría de coherencia (obligatoria, antes de cambiar nada)
Comprueba que está aplicado el prompt anterior (cimientos de la API y plataforma del piloto,
2026-09-29). Señales: existen Documentation/adr/ADR-031-tests-integracion-postgres.md y
ADR-032-plataforma-piloto-aws.md, ADR-016 figura como «sustituida por ADR-031» y el vol. 1 §5.1.2
incluye GEN_METHOD_NOT_ALLOWED. Si no, detente y repórtalo.

Fuentes de contexto que puedes LEER (no editar):
- .claude/contexto/decisiones.md: texto exacto de D-28 y H-37;
- .claude/contexto/analisis-postgresql.md: versiones, inventario, cambios de comportamiento,
  alternativas A-D;
- .claude/rules/datos.md y .claude/rules/backend.md: reglas vigentes de base de datos, SQL y
  emails;
- data/README.md: scripts de psql;
- data/schema/create_ReservArteDB.sql: tipos reales.
Si algo de este prompt contradice esas fuentes, repórtalo sin corregirlo.

Alcance: TODO lo que describe el sistema tal como es hoy pasa a PostgreSQL. NO reescribas las notas
históricas con fecha y PR del tipo «Runtime (PR #65): SQL Server, base desechable…» o «v6
(2026-09-15)…»: son registros de estado que se retiran en la auditoría mensual de octubre. Si una
nota histórica se lee como vigente, añade solo «(entonces con SQL Server)».

## 1. Qué se ha hecho (contexto; no lo copies como registro de estado)
- 869f8pmnm (PR #91), fechas en UTC en la frontera de la API:
  - Toda fecha con hora de entrada, en el cuerpo JSON o en la query, va en ISO 8601 con zona (Z o
    desplazamiento) y se convierte al instante UTC. Sin zona → 400 GEN_VALIDATION_FAILED, con el
    campo y el código MissingTimeZone.
  - De salida, siempre UTC con Z (UtcDateTimeJsonConverter).
  - Corrige que las fechas con desplazamiento se guardaran en la hora local del servidor. Era
    requisito del cambio: Npgsql rechaza en timestamptz los DateTime que no son Kind=Utc.
- 869f8pmpa (PR #92), cambio del motor:
  - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 (licencia PostgreSQL) sustituye a
    Microsoft.EntityFrameworkCore.SqlServer, con reintentos (EnableRetryOnFailure: 3 intentos,
    5 s) y CommandTimeout de 30 s.
  - Salen Hangfire.SqlServer (sin uso) y Microsoft.Data.SqlClient, y con él el pin de
    Microsoft.IdentityModel.Protocols.OpenIdConnect.
  - Historial de migraciones reiniciado: las 12 de SQL Server se retiran y queda una,
    20260929073713_InitialCreate (23 tablas del modelo más __EFMigrationsHistory, 46 índices,
    22 CHECK y 3 índices filtrados).
  - Nombres en PascalCase como antes. El SQL escrito a mano lleva comillas dobles en cada
    identificador ("Customers"."Email") y booleanos TRUE/FALSE.
  - Emails (H-37, decisión A2): EmailNormalizer (Trim + minúsculas) en altas, ediciones,
    registro, alta social y búsquedas por email. CHECK CK_Customers_EmailLowercase y
    CK_Employees_EmailLowercase. PostgreSQL compara texto distinguiendo mayúsculas; la
    aplicación ya no depende de la collation CI_AS de SQL Server.
  - Búsquedas: x.Campo.ToLower().Contains(término en minúsculas). No distinguen mayúsculas; «%» y
    «_» se buscan como texto. No ignoran acentos.
  - Scripts de data/ reescritos para psql:
    - create_ReservArteDB.sql, generado e idempotente, crea la base con \gexec;
    - drop_ReservArteDB.sql usa DROP DATABASE … WITH (FORCE);
    - seed_demo_ReservArteDB.sql, con transacción, avanza las secuencias con setval;
    - los tres aceptan -v db=<nombre> (por defecto, reservarte).
- 869f8pmpn (Windows, sin PR propio; fix del PR #98): los dos equipos de desarrollo corren
  PostgreSQL 18.6 en el contenedor reservarte-pg:
  - imagen postgres:18, publicado solo en 127.0.0.1:5432, volumen reservarte_pgdata, base
    reservarte;
  - cadena de conexión en User Secrets:
    Host=localhost;Port=5432;Database=reservarte;Username=reservarte;Password=…
  - SQL Server retirado de los dos equipos (contenedor reservarte-sql, volumen y cadenas).
  - Requisitos: SDK de .NET 10 (banda de global.json), dotnet-ef 10.0.12 y Docker.
  - Los E2E arrancan la SPA por su cuenta (webServer de Playwright), pero NO la API: tiene que
    estar en marcha en 5555.
  - PR #98: el .gitignore de reservarte-web tenía mal las rutas de Playwright y el informe estaba
    versionado.

## 2. Cambios por documento

Equivalencias de tipos (del create generado, úsalas en todo el documento):
- nvarchar(n) → character varying(n) (varchar(n));
- nvarchar(max) → text;
- datetime2 → timestamp with time zone (timestamptz, siempre UTC);
- date → date;
- time → time without time zone;
- bit → boolean;
- uniqueidentifier → uuid;
- decimal(p,s) → numeric(p,s);
- int IDENTITY → integer GENERATED BY DEFAULT AS IDENTITY;
- NVARCHAR(MAX) con ISJSON → jsonb.

### Vol. 1 — Análisis
- §4.1 (stack) y §4.1.4 «Base de datos»: reescribe con PostgreSQL 18.
  - Imagen postgres:18 en desarrollo; RDS PostgreSQL 18 en el piloto (enlaza ADR-032).
  - Proveedor Npgsql 10.0.3.
  - Multi-tenancy por query filters con OrganizationId (sin cambios).
  - JSON con jsonb si se necesita.
  - Copias: las automáticas de RDS con restauración a un punto en el tiempo (enlaza ADR-032).
  - Fuera las características de SQL Server (ISJSON, full-text de SQL Server, TDE).
  - Enlaza el ADR del motor (sección 3).
  - En §4.1, fuente del stack: pasa aquí las versiones de Testcontainers.PostgreSql 4.15.0 y
    Microsoft.AspNetCore.Mvc.Testing 10.0.12 (MIT). La estrategia de testing §10 las enlaza en
    vez de repetirlas (lo propusiste en tu informe anterior).
  - Donde §4.1 diga que tests/ReservArte.IntegrationTests no existe o que la integración va
    contra SQL Server: ya existe, con PostgreSQL (ADR-031).
- §4.2.1-4.2.4: donde la arquitectura del piloto o la objetivo digan SQL Server en Docker, pasa a
  RDS PostgreSQL 18 (el piloto ya lo dice; revisa la objetivo, §4.2.3, y los diagramas).
- §4.4.1: Microsoft.AspNetCore.Authentication.Facebook es 10.0.12, no 8.0.0. Revisa si hay más
  versiones 8.0.x de paquetes de ASP.NET Core: todos van en 10.0.12.
- §4.4.3 o donde se hable de cifrado en reposo: fuera TDE y cifrado de volumen de SQL Server; en el
  piloto, cifrado en reposo de RDS con KMS.
- §3.x (descripciones del dominio) y §5.2 (esquema): tipos con las equivalencias de arriba.
  - «Esquema autoritativo (SQL Server)» → PostgreSQL.
  - La nota del DDL orientativo en dialecto T-SQL: NO traduzcas el bloque entero. Di que es la
    visión de producto escrita en T-SQL antes del cambio de motor, que la fuente de verdad es el
    create generado (PostgreSQL) y que los tipos se leen con la tabla de equivalencias.
  - Los comentarios del DDL que citan tipos reales de EF (nvarchar(20) NULL, datetime2…) sí pasan
    a su equivalente de PostgreSQL.
  - Añade una nota breve sobre identificadores: PascalCase con comillas dobles en SQL escrito a
    mano; lo generado por EF ya las lleva.
- §5.1.1 (contrato): añade el caso de fecha con hora sin zona (400 GEN_VALIDATION_FAILED, código
  de detalle MissingTimeZone) y que las fechas de salida van en UTC con Z.
- §5.1.3 (configuración):
  - ConnectionStrings:DefaultConnection es la cadena de Npgsql (formato de la sección 1), en User
    Secrets en desarrollo y en secretos de AWS en producción.
  - Hangfire: sin almacenamiento decidido hasta la Fase 5 (Hangfire.SqlServer retirado).
  - Añade la sección Email al esqueleto JSON (la tabla ya la documenta).
- Nota de AUTH_MFA_INVALID: sigue siendo correcta para POST /auth/mfa/verify (401
  AUTH_INVALID_CREDENTIALS). Añade el segundo caso: POST /account/mfa/confirm y
  /account/mfa/disable responden 400 con AUTH_INVALID_CREDENTIALS al TOTP incorrecto. Los dos se
  resuelven en 869en8a17.
- §3.1.5, §5.1 y donde se atribuya la zona horaria a 869f2gtyv: la zona es 869f74u7y (869f2gtyv
  es hoy la tarea de no-shows).

### Vol. 2 — Implementación y desarrollo
- Figura de arquitectura (hacia la línea 105) y §9.1.2 «Cifrado en reposo»: PostgreSQL. En
  desarrollo, volumen del contenedor; en el piloto, RDS con cifrado KMS. Fuera TDE y el ejemplo de
  MSSQL_SA_PASSWORD y /var/opt/mssql.
- Persistencia y migraciones (§9.6 y donde se describa el flujo):
  - migración inicial única InitialCreate en PostgreSQL;
  - la lección de EF 10 sobre lotes y EXEC deja de aplicar (PostgreSQL usa un bloque DO por
    migración);
  - regla vigente: cada migración regenera el create y se verifica sobre una base desechable
    (-v db=…), nunca sobre la base de desarrollo reservarte.
- §9.6: AvailabilityService ya convierte las ausencias a Europe/Madrid; la zona sigue fija hasta
  869f74u7y, no 869f2gtyv. AuthResult<T> y el ValidateAsync duplicado (869f17y6k) ya no existen:
  hay un solo Result<T> y ApiControllerBase. El id no numérico ya no es un 404 sin cuerpo
  (ApiStatusCodePages).
- §9.7 y §9.8 (Clientes y Servicios): la normalización de emails (EmailNormalizer + CHECK) y las
  búsquedas con ToLower().Contains(), en lugar de la collation de SQL Server; RefreshTokens y el
  resto de longitudes, con sus tipos de PostgreSQL. Quita la mención al 400 ProblemDetails de
  869f1k17q como deuda abierta: se cerró.
- §9.8 y §9.9, las FK Restrict: la razón vigente es de negocio (histórico: la cita no puede
  perder a su clienta ni a su empleada). La otra razón, que SQL Server rechazaba dos caminos en
  cascada, ya no aplica; quítala sin cambiar la decisión.
- §9.9, índice único filtrado idx_appointments_redsys_order: PostgreSQL admite varios NULL en un
  único; el filtro se mantiene por intención explícita e índice más pequeño.

### Vol. 3 — Planificación y gestión
- §11.2: tablas de 5 y 50 organizaciones (arquitectura objetivo) con RDS PostgreSQL en vez de SQL
  Server. No inventes precios: si no puedes recalcularlos con una fuente, marca las filas de base
  de datos «por recalcular con la calculadora de AWS (eu-south-2)» y avísalo en advertencias.
- §11.6: deja de multiplicar los 133 €: usa el coste del piloto de §11.2 (≈ 35 €/mes de AWS sin
  IVA, más Cloudinary), enlazado, no copiado.
- §12.2 (checklist de arranque del entorno de desarrollo):
  - contenedor reservarte-pg (el comando docker run de data/README.md o de los scripts de
    instalación, enlazado);
  - cadena de conexión de Npgsql en User Secrets;
  - dotnet ef database update o arrancar la API en Development;
  - Docker en marcha para los tests de integración;
  - los E2E necesitan la API en 5555.
  - Fuera sqlcmd, QUOTED_IDENTIFIER, reservarte-sql, ReservArteDB y el puerto 1433.
- Quita los registros de estado que den la integración como pendiente con SQL Server o dejen
  869f2gh37 en backlog (líneas del tipo «[ ] Backend integración … Testcontainers (SQL Server)»).
  Lo hecho está en ClickUp.
- Fuera la referencia a «SQL Server en Linux (contenedor)» de la bibliografía, o cámbiala por la
  imagen oficial de PostgreSQL.

### Otros
- Project-Init/Scripts de instalación.md, paso 1b: pasa a «PostgreSQL en Docker (desarrollo)».
  - El docker run de reservarte-pg: sin contraseñas reales en el documento, con marcador <pwd>.
  - La cadena de conexión de Npgsql.
  - Los scripts de data/ con psql, enlazando data/README.md.
  - Nota para Git Bash: MSYS_NO_PATHCONV=1 delante de docker exec con rutas del contenedor.
  - Requisitos: SDK .NET 10 (banda de global.json), dotnet-ef 10.0.12 y Docker Desktop.
- Project-Init/user-secrets-guide.md: la nota de sqlcmd y QUOTED_IDENTIFIER pasa a psql dentro del
  contenedor (docker exec -i reservarte-pg psql -U reservarte -d reservarte), con identificadores
  entre comillas dobles. Documenta el secreto ConnectionStrings:DefaultConnection con el formato de
  Npgsql.
- reservarte-testing-strategy.md:
  - §3.1: SQLite no reproduce PostgreSQL (comparación de texto, timestamptz y Kind de DateTime,
    CHECK); lo que dependa del motor va a integración (ADR-031).
  - §10: enlaza las versiones que pasan al vol. 1 §4.1.
- Documentation/Análisis de pantallas y estructura.md: donde diga que el proyecto de integración no
  existe o enlace ADR-016 como pendiente → existe; ADR-031.

## 3. ADR
- Nuevo ADR (siguiente número libre del índice, será el 033) «Motor de base de datos: PostgreSQL»
  (D-28 y H-37).
  - Contexto:
    - el presupuesto no incluía licencia de SQL Server (ADR-021);
    - Express limita cada base a 10 GB y Developer no vale para producción;
    - el motor era lo más caro de la plataforma y aún no había datos de producción que migrar.
  - Decisión: PostgreSQL 18 (Npgsql 10.0.3), antes del despliegue, con las decisiones de H-37:
    - A2: mayúsculas resueltas en la aplicación (emails normalizados con CHECK; búsquedas con
      ToLower);
    - B1: PascalCase con comillas en el SQL a mano;
    - C: la versión 18 en todos los entornos;
    - D: Hangfire.SqlServer fuera y almacenamiento de Hangfire en la Fase 5.
  - Alternativas descartadas:
    - seguir con SQL Server Express (límite de 10 GB por base) o gestionado (licencia y coste);
    - MySQL/MariaDB (sin índices parciales, que el esquema usa);
    - citext (A1): exige la extensión en todos los entornos;
    - collation ICU no determinista (A3): el LIKE con ella solo existe desde PostgreSQL 18 y rompe
      los tests de SQLite;
    - snake_case (B2): renombra todo el esquema sin ganancia funcional.
  - Consecuencias:
    - sin licencias ni límites de edición;
    - más opciones de hosting (resuelto en ADR-032);
    - imagen ligera para el CI y Testcontainers (ADR-031);
    - historial de migraciones reiniciado;
    - fechas siempre en UTC en la frontera;
    - comparación de texto sensible a mayúsculas resuelta en la aplicación;
    - posibilidad futura de EXCLUDE USING gist contra solapes de citas.
  - Relación con otros ADR:
    - ADR-021 obligaba a decidir motor y hosting antes de montar: este ADR es la parte del motor y
      ADR-032 la del hosting;
    - ADR-021 sigue aceptada (no la toques).
- Enlázalo desde el README de adr y, en el vol. 1 §4.1.4, en lugar de repetir su razonamiento.
  decisiones.md lo enlaza Claude Code después: no lo toques.

## 4. Restricciones
- No añadas registros de estado, PRs ni recuentos a los volúmenes (los de la sección 1 son contexto).
- Un dato, una fuente: enlaza en vez de copiar (costes en vol. 3 §11.2, versiones en vol. 1 §4.1,
  comandos del entorno en data/README.md o en los scripts de instalación).
- Ni contraseñas ni cadenas de conexión reales: marcadores como <pwd>.
- No verifiques IDs de ClickUp.
- No toques .claude/ ni .cursor/.
- Si algo contradice otro documento o una decisión, repórtalo sin corregirlo.

## 5. Advertencias
Señala todo lo que veas dudoso o desfasado, aunque no esté en este prompt. En particular:
- cualquier mención a SQL Server, sqlcmd, ReservArteDB, 1433, nvarchar o datetime2 que quede
  describiendo el sistema actual (distingue en tu informe las que dejaste por ser notas históricas);
- precios que no hayas podido recalcular.
~~~
