# ADR-032: Plataforma de producción del piloto: AWS simplificado

**Fecha:** 2026-09-29

**Estado:** aceptada

## Contexto

El piloto es un centro, con datos de salud (EIPD) y un solo desarrollador. El presupuesto de infraestructura del volumen 3 estaba pensado para SQL Server: unos 133 € al mes con ECS Fargate y un balanceador. PostgreSQL 18 ya está decidido (D-28). [ADR-021](ADR-021-plataforma-produccion.md) obligaba a decidir la plataforma antes de montarla; este ADR es esa decisión y no lo sustituye.

## Decisión

Producción del piloto (D-29, opción B): AWS simplificado en `eu-south-2` (España). Una EC2 `t4g.small` con Docker Compose —la API y Caddy, que sirve la SPA, hace de proxy inverso y obtiene el certificado comodín de Let's Encrypt por DNS en Route 53— y RDS PostgreSQL 18 `db.t4g.micro` en subred privada, con copias automáticas y restauración a un punto en el tiempo. SES y CloudWatch se quedan como en el diseño. La arquitectura del volumen 1 (ECS Fargate, ALB y CloudFront) es la vía de escalado para varios centros: cambia el cómputo; la base, el DNS y los servicios externos se quedan. El coste medido está en el volumen 3 §11.2.

## Alternativas descartadas

- La arquitectura del volumen 1 desde el principio (opción A): unos 65-80 € al mes y unas 30 h de montaje para un solo centro.
- Base autoalojada en la EC2 o en un VPS europeo: las copias y la restauración quedan en manos de un desarrollador solo, con datos de salud.
- PostgreSQL con los datos en S3: S3 no es un disco de bloques y corrompe la base.
- Azure Container Apps: más barata, pero fuera del diseño y del código (CloudWatch, SES).
- Scaleway: sin PostgreSQL 18.

## Consecuencias

Hay un sistema operativo que mantener, con actualizaciones automáticas. La API del piloto no tiene alta disponibilidad. La base es gestionada, con copias y restauración a un punto en el tiempo. Staging puede usar PostgreSQL en contenedor, sin datos reales.

## Tareas relacionadas

D-29. Tarea `869f6r4ww`. Desarrolla el ADR-021; no lo sustituye.
