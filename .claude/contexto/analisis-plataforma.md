# Análisis: plataforma de producción del piloto (DP-01, `869f6r4ww`)

> Preparado el 2026-09-29 para la sesión de decisión con Guillermo. El motor ya está decidido
> (PostgreSQL 18, D-28); aquí se decide el **hosting** de la API, la SPA y la base de datos para
> **un centro** (More Than Brows), con la puerta abierta a varios. Precios en orden de magnitud: las
> fuentes dan casi siempre tarifas de EE. UU. y en la UE suelen ser un 10-20 % más altas; la cifra
> final sale de la calculadora oficial de la región elegida.

## 1. Requisitos que condicionan la elección

| Requisito | Por qué | Consecuencia |
|---|---|---|
| PostgreSQL 18 gestionado o con copias fiables | D-28; datos de salud (alergias, art. 9 RGPD), EIPD en `869f6r7b3` | Copias automáticas con restauración a un punto en el tiempo, o un plan de copias probado |
| Datos en la UE, con contrato de encargo (DPA) | RGPD; la EIPD lo revisará | Región UE (España si es posible: AWS `eu-south-2`, Azure `Spain Central`) |
| Proceso siempre encendido | Hangfire en la Fase 5 (recordatorios); Data Protection con claves persistentes | Nada de escalar a cero sin un worker aparte |
| Subdominio por centro (`morethanbrows.<dominio>`) | Estrategia `Subdomain` en producción | DNS comodín y certificado TLS comodín |
| Un solo desarrollador, 25 h/semana | Operación y parches corren a cargo de Guillermo | Cuanto menos servidor que mantener, mejor |
| Coste bajo con un centro | Piloto sin ingresos | El vol. 3 §11.2 presupuesta unos 133 €/mes (con SQL Server) |

El código ya toca AWS en dos sitios: el sink de Serilog a CloudWatch (`AWS.Logger.SeriLog`, que trae
`AWSSDK.Core` con el aviso NU1901, deuda `869f8hpfj`) y el correo previsto con SES (`869d7f65a`). SES
se puede usar desde cualquier nube; el sink de CloudWatch solo tiene sentido en AWS.

## 2. PostgreSQL 18 en los gestionados (septiembre de 2026)

- **AWS RDS:** PostgreSQL 18 desde noviembre de 2025; minor 18.6 desde agosto de 2026.
- **Azure Database for PostgreSQL (Flexible Server):** PostgreSQL 18 en GA en todas las regiones
  públicas, con actualización de versión mayor en el sitio.
- **Scaleway:** sin PostgreSQL 18 (16/17 según su documentación y una petición de febrero de 2026).
  Descartado por la condición de la tarea.

## 3. Opciones

### A. Arquitectura AWS del vol. 1 (ECS Fargate + ALB + CloudFront + RDS)

- Cómputo en Fargate (0,5 vCPU, 1 GB), balanceador ALB, SPA en S3 + CloudFront, RDS PostgreSQL 18
  (`db.t4g.micro`), Route 53, ACM (certificado comodín gratis), Secrets Manager, CloudWatch.
- **Coste:** ≈ 65-80 €/mes (el ALB solo ya ronda los 20-25 €; Fargate ≈ 20 €; RDS ≈ 15-18 €).
  Cuidado con el NAT Gateway (≈ 35 €/mes más) si las tareas van en subredes privadas.
- **Pros:** es lo documentado; escala a muchos centros sin rediseño; sin servidores que parchear.
- **Contras:** la más cara y la de más montaje (VPC, subredes, ALB, IAM, ECS, despliegue); mucha
  pieza para un centro.
- **Montaje estimado:** ≈ 30 h (el paso 6.10 del plan ya las prevé).

### B. AWS simplificado (EC2 pequeña con Docker + RDS)

- Una EC2 (`t4g.small`, 2 vCPU y 2 GB) con Docker Compose: la API y Caddy, que sirve la SPA, hace de
  proxy inverso y saca el certificado comodín con Let's Encrypt (reto DNS en Route 53). La base en
  RDS PostgreSQL 18 (`db.t4g.micro`) en subred privada, con copias automáticas y restauración a un
  punto en el tiempo. SES y CloudWatch como en el diseño.
- **Coste:** ≈ 35-45 €/mes (EC2 ≈ 15 €, IP pública ≈ 3,5 €, RDS con almacenamiento ≈ 17-20 €, resto
  unos euros).
- **Pros:** misma nube que el diseño, el código (SES, CloudWatch) y las tareas de Infra; la pieza
  crítica, la base, gestionada; modelo mental sencillo; el salto a la opción A con varios centros
  es cambiar solo el cómputo (la base y el DNS se quedan).
- **Contras:** hay un sistema operativo que parchear (actualizaciones automáticas de Amazon Linux) y
  un único servidor para la API (sin alta disponibilidad; aceptable en el piloto).
- **Montaje estimado:** ≈ 18-20 h.

### C. Azure (Container Apps + PostgreSQL Flexible Server)

- API en Azure Container Apps (plan de consumo, una réplica mínima), SPA en Static Web Apps, base en
  PostgreSQL Flexible Server Burstable B1ms, región Spain Central.
- **Coste:** ≈ 25-40 €/mes (B1ms ≈ 13-15 € más almacenamiento; la réplica siempre encendida cobra la
  tarifa reducida de inactividad y parte la cubre la cuota gratuita).
- **Pros:** la más barata de las gestionadas; sin sistema operativo que mantener; ecosistema natural
  de .NET; región en España.
- **Contras:** se sale del diseño AWS del vol. 1 (documentación y tareas de Infra a rehacer); los logs
  pasan de CloudWatch a Azure Monitor (cambio de sink); el certificado comodín hay que traerlo
  (Container Apps no gestiona comodines); curva de aprendizaje si no se conoce Azure.
- **Montaje estimado:** ≈ 20-25 h, más reescribir la parte de infraestructura de los volúmenes.

### D. VPS europeo autoalojado (p. ej. Hetzner) — descartada

- Todo en un servidor (API, PostgreSQL 18 en Docker y Caddy), copias propias a almacenamiento de
  objetos. **Coste:** ≈ 10-20 €/mes.
- **Por qué no:** con datos de salud, las copias, la restauración probada, los parches de la base y la
  monitorización recaen enteros en un desarrollador solo; un fallo de copias es irrecuperable. Además,
  Hetzner marca sus planes compartidos como sin stock desde el 4 de septiembre de 2026. Se puede
  reconsiderar si el coste pasa a ser el problema principal.

## 4. Recomendación

**B, AWS simplificado.** Mantiene la nube del diseño y del código, deja gestionada la pieza que no
se puede perder (la base, con copias y restauración a un punto en el tiempo) y cuesta menos de la
mitad de la opción A. Cuando haya varios centros, se pasa a Fargate + ALB sin tocar la base. C es
una alternativa real si Guillermo prefiere no mantener un servidor, o si ya conoce Azure: es más
barata y más gestionada, pero obliga a rehacer la parte de infraestructura de la documentación.

## 5. Qué cambia según la opción

- **A o B:** se mantienen SES y CloudWatch; `869f8hpfj` (AWSSDK.Core) se resuelve subiendo
  `AWS.Logger.SeriLog`. B necesita tareas de Infra nuevas (EC2 + Compose + Caddy) y deja las de
  ECS/ALB para cuando haya varios centros.
- **C:** fuera `AWS.Logger.SeriLog` (desaparece el NU1901), sink de Azure Monitor u OpenTelemetry;
  SES se puede mantener; hay que reescribir las tareas de Infra y la parte de infraestructura de los
  vol. 1 y 3.
- **Todas:** presupuesto del vol. 3 §11.2 corregido; `869d7ewnz` (copias de seguridad) pasa a
  «configurar y probar la restauración» del gestionado; el endurecimiento `869f74u8w` (IP real tras
  el proxy) se ajusta al proxy elegido (ALB, Caddy o el ingress de Container Apps).

## Fuentes

- [Amazon RDS for PostgreSQL now supports major version 18](https://aws.amazon.com/about-aws/whats-new/2025/11/amazon-rds-postgresql-major-version-18)
- [Amazon RDS for PostgreSQL supports minor versions 18.6, 17.11…](https://aws.amazon.com/about-aws/whats-new/2026/08/amazon-rds-postgresql-18-6-17-11-16-15-15-19-14-24/)
- [PostgreSQL 18 Now GA on Azure Postgres Flexible Server](https://techcommunity.microsoft.com/blog/adforpostgresql/postgresql-18-now-ga-on-azure-postgres-flexible-server/4469802)
- [Scaleway: Databases SQL Serverless postgresql 18 (feature request)](https://feature-request.scaleway.com/posts/1280/databases-sql-serverless-postgresql-18)
- [Azure Container Apps pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/) y [Understanding Idle Usage in Azure Container Apps](https://techcommunity.microsoft.com/blog/appsonazureblog/understanding-idle-usage-in-azure-container-apps/4419197)
- [Azure Flexible Server B1ms pricing (Bytebase)](https://www.bytebase.com/dbcost/azure-flexible/instance/B1ms/)
- [AWS Fargate pricing](https://aws.amazon.com/fargate/pricing/) y [AWS Fargate Pricing: Real Costs Per Environment (fortem.dev)](https://fortem.dev/blog/aws-fargate-pricing-real-costs/)
- [db.t4g.micro pricing (Vantage)](https://instances.vantage.sh/aws/rds/db.t4g.micro)
- [Hetzner CX23 pricing 2026 (cloudhim)](https://www.cloudhim.com/cloud-costs/hetzner-cx22-pricing-2026)
