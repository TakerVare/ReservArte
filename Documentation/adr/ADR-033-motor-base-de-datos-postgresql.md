# ADR-033: Motor de base de datos: PostgreSQL

**Fecha:** 2026-09-29

**Estado:** aceptada

## Contexto

El presupuesto de infraestructura no incluía licencia de SQL Server ([ADR-021](ADR-021-plataforma-produccion.md)). La edición Express limita cada base a 10 GB y la Developer no vale para producción. El motor era lo más caro de la plataforma y aún no había datos de producción que migrar: el cambio es de esquema y de código, no de datos.

## Decisión

El motor pasa a PostgreSQL 18, con el proveedor Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3, antes del despliegue (D-28). Las decisiones de H-37 van con el cambio:

- Mayúsculas resueltas en la aplicación (A2): los emails se normalizan al guardar y al buscar (`EmailNormalizer`: recorte y minúsculas), con CHECK `CK_Customers_EmailLowercase` y `CK_Employees_EmailLowercase`. Las búsquedas de lista usan `ToLower().Contains()`. Sin citext ni collation ICU.
- Nombres en PascalCase (B1). El SQL escrito a mano lleva comillas dobles en cada identificador; el que genera EF ya las lleva.
- PostgreSQL 18 en todos los entornos (C): imagen `postgres:18` en desarrollo y la misma versión mayor en el piloto.
- `Hangfire.SqlServer` fuera ya. El almacenamiento de Hangfire se decide en la Fase 5 (D).

Las fechas con hora cruzan la frontera de la API en UTC. El detalle de tipos, scripts y entorno está en el volumen 1 §4.1.4 y §5.2; el de hosting, en [ADR-032](ADR-032-plataforma-piloto-aws.md).

## Alternativas descartadas

- Seguir con SQL Server Express (límite de 10 GB por base) o con una edición gestionada (licencia y coste).
- MySQL o MariaDB: no tienen índices parciales, y el esquema los usa.
- `citext` (A1): exige la extensión en todos los entornos.
- Collation ICU no determinista (A3): el `LIKE` con ella solo existe desde PostgreSQL 18 y rompe los tests de SQLite.
- snake_case (B2): renombra todo el esquema sin ganancia funcional.

## Consecuencias

No hay licencias ni límites de edición. Hay más opciones de hosting; la del piloto está en [ADR-032](ADR-032-plataforma-piloto-aws.md). La imagen oficial es ligera para el CI y para Testcontainers ([ADR-031](ADR-031-tests-integracion-postgres.md)). El historial de migraciones se reinició: queda una migración inicial en PostgreSQL. Las fechas de la API van siempre en UTC. La comparación de texto distingue mayúsculas y eso se resuelve en la aplicación. Más adelante, una restricción `EXCLUDE USING gist` puede impedir en la base que dos citas de la misma empleada se solapen.

## Tareas relacionadas

D-28 y H-37. Tareas `869f8pm99`, `869f8pmnm`, `869f8pmpa`, `869f8pmpn`. Este ADR es la parte de motor de [ADR-021](ADR-021-plataforma-produccion.md); [ADR-032](ADR-032-plataforma-piloto-aws.md) es la de hosting. ADR-021 sigue aceptada.
