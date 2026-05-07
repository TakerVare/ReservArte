# MEMORIA TÉCNICA DEL PROYECTO RESERVARTE
## Sistema Multi-Tenant de Gestión para Centros de Diseño de Cejas en España

**DOCUMENTO 3 DE 3: PLANIFICACIÓN Y GESTIÓN**

---

**Versión:** 1.0  
**Fecha:** Octubre 2025  
**Cliente:** More Than Brows  
**Ubicación:** España  
**Desarrollador:** Gabriel Sánchez-Vallejo Millán y Guillermo Algárate del Arco

---

## ÍNDICE DEL DOCUMENTO 3

10. [PLAN DE DESARROLLO - ROADMAP](#10-plan-de-desarrollo-roadmap)
11. [ESTIMACIÓN DE COSTOS](#11-estimaciÃ³n-de-costos)
12. [PRÓXIMOS PASOS](#12-prÃ³ximos-pasos)
13. [ANEXOS](#anexos)

---

## 10. PLAN DE DESARROLLO - ROADMAP

### 10.1 Metodología

**Enfoque:** Agile Scrum
- Sprints de 2 semanas
- Daily standups (15 minutos)
- Sprint review y retrospective
- Continuous Integration/Continuous Deployment (CI/CD)

**Roles:**
- **Product Owner:** Cliente (centro de cejas)
- **Scrum Master:** Líder técnico del equipo
- **Development Team:** Desarrolladores Full-Stack
- **QA Engineer:** Testing y calidad

**Herramientas:**
- **Gestión de proyecto:** Jira o Linear
- **Comunicación:** Slack
- **Control de versiones:** GitHub
- **CI/CD:** GitHub Actions
- **Documentación:** Notion o Confluence

---

### 10.2 Fases del Proyecto

#### FASE 1: MVP - Funcionalidades Esenciales (3-4 meses)

**Objetivo:** Aplicación web funcional con lo mínimo indispensable para gestionar un centro

---

**Sprints 1-2 (Mes 1): Fundación**

**Semana 1-2:**
- ✅ Setup de infraestructura AWS
  - Crear cuenta AWS
  - Configurar VPC, subnets, security groups
  - Crear RDS PostgreSQL (entorno dev)
  - Crear S3 buckets (fotos, backups)
  - Configurar Amazon SES (verificar dominio)
- ✅ Configuración de proyecto .NET
  - Crear solución con Clean Architecture
  - Configurar Entity Framework Core
  - Setup de migraciones de BD
  - Configurar Serilog + CloudWatch
- ✅ Configuración de proyecto Vite
  - Crear proyecto React + TypeScript + Vite
  - Configurar Tailwind CSS + shadcn/ui
  - Setup de Zustand para estado global
  - Configurar React Router
  - Setup de Axios para API calls

**Semana 3-4:**
- ✅ Base de datos inicial
  - Migración: tablas core (organizations, users, employees)
  - Seed data para desarrollo
  - Índices iniciales
- ✅ Autenticación básica
  - Login/Registro con JWT
  - Middleware de autenticación
  - Refresh tokens
  - Página de login en frontend
- ✅ Panel de administración vacío
  - Layout principal
  - Sidebar con navegación
  - Header con usuario
  - Dashboard placeholder

**Entregables Sprint 1-2:**
- ✅ Infraestructura AWS configurada y funcional
- ✅ Repositorios Git con CI/CD básico
- ✅ Login funcional en frontend y backend
- ✅ Panel de administración con estructura base
- ✅ Documentación de setup para nuevos desarrolladores

---

**Sprints 3-4 (Mes 2): Gestión Básica**

**Semana 5-6:**
- ✅ CRUD de empleados
  - API endpoints completos
  - Formularios de creación/edición
  - Lista con búsqueda y paginación
  - Gestión de roles
- ✅ CRUD de clientes
  - API endpoints completos
  - Formularios de creación/edición
  - Lista con búsqueda y filtros
  - Sistema de categorías (VIP, Regular, etc.)
- ✅ Validaciones y manejo de errores
  - FluentValidation en backend
  - Zod en frontend
  - Mensajes de error consistentes

**Semana 7-8:**
- ✅ CRUD de servicios
  - API endpoints completos
  - Formularios con precios y duración
  - Categorías de servicios
  - Gestión de variaciones
- ✅ Horarios de empleados
  - Disponibilidad semanal recurrente
  - Excepciones (vacaciones, bajas)
  - Validación de solapamientos
- ✅ Dashboard con métricas básicas
  - Citas del día
  - Ingresos del mes
  - Clientes totales
  - Servicios más solicitados

**Entregables Sprint 3-4:**
- ✅ Gestión completa de maestros (empleados, clientes, servicios)
- ✅ Posibilidad de configurar el centro completamente
- ✅ Dashboard operativo con datos en tiempo real
- ✅ Testing unitario de endpoints críticos

---

**Sprints 5-6 (Mes 3): Sistema de Citas (Core del Sistema)**

**Semana 9-10:**
- ✅ Modelo de datos de citas
  - Migraciones BD
  - Entidades y relaciones
  - Repositorios
- ✅ API de citas
  - CRUD completo
  - Validación de disponibilidad
  - Asignación de empleado y servicio
  - Estados de cita
- ✅ Calendario visual (FullCalendar)
  - Vista diaria/semanal/mensual
  - Drag & drop para reorganizar
  - Código de colores
  - Modal de detalles de cita

**Semana 11-12:**
- ✅ Crear cita (modo interno - personal)
  - Wizard paso a paso
  - Selección de cliente
  - Selección de servicio(s)
  - Selección de empleado (o auto)
  - Selección de fecha/hora
  - Confirmación
- ✅ Validaciones de disponibilidad
  - Horarios de empleado
  - Solapamiento de citas
  - Horarios de operación
  - Tiempo suficiente para servicio
- ✅ Notificaciones básicas por email
  - Confirmación de cita creada
  - Template HTML responsive
  - Integración con Amazon SES

**Entregables Sprint 5-6:**
- ✅ Sistema de citas funcional
- ✅ Agenda visual interactiva y profesional
- ✅ Personal puede crear y gestionar citas
- ✅ Emails transaccionales funcionando
- ✅ Testing de flujos críticos

---

**Sprints 7-8 (Mes 4): Pagos y Finalización MVP**

**Semana 13-14:**
- ✅ Integración con Redsys InSite
  - Configuración de cuenta Redsys (test)
  - SDK JavaScript en frontend
  - Servicio de pagos en backend
  - Pre-autorizaciones
  - Captura de pagos
  - Cancelación de pre-autorizaciones
- ✅ Guardado de tarjetas (tokenización)
  - Flujo de primera transacción con COF
  - Almacenamiento de tokens
  - Gestión de tarjetas guardadas
  - Pago con tarjeta guardada
- ✅ Gestión de cancelaciones
  - Política de penalización configurable
  - Cálculo automático de penalización
  - Captura parcial en cancelación tardía
  - Liberación en cancelación a tiempo

**Semana 15-16:**
- ✅ Sistema de recordatorios
  - Configuración de recordatorios
  - Jobs programados con Hangfire
  - Recordatorios por email
  - Template de recordatorio HTML
  - Enlaces de confirmación/cancelación
- ✅ Testing end-to-end
  - Flujo completo de reserva
  - Flujo de pago con Redsys (test)
  - Flujo de cancelación con penalización
  - Recordatorios automáticos
- ✅ Documentación
  - Manual de usuario (personal del centro)
  - Documentación técnica (API)
  - Guía de despliegue
  - Troubleshooting común

**Entregables Sprint 7-8:**
- ✅ MVP completo y funcional
- ✅ Sistema de pagos con Redsys operativo
- ✅ Pre-autorizaciones y penalizaciones funcionando
- ✅ Recordatorios automáticos por email
- ✅ Aplicación desplegada en producción (cliente piloto)
- ✅ Documentación completa para uso y mantenimiento

---

**🎯 HITO 1: MVP EN PRODUCCIÓN**
- **Fecha objetivo:** Fin de Mes 4
- **Criterio de éxito:**
  - ✅ Centro piloto usando la aplicación diariamente
  - ✅ 50+ citas gestionadas sin incidencias críticas
  - ✅ Sistema de pagos Redsys funcionando correctamente
  - ✅ 0 violaciones de seguridad
  - ✅ Uptime > 99%
  - ✅ NPS (Net Promoter Score) > 7/10 del cliente piloto

---

#### FASE 2: Mejoras y Aplicación Móvil (2-3 meses)

**Objetivo:** Añadir funcionalidades avanzadas y crear apps móviles

---

**Sprints 9-10 (Mes 5): Funcionalidades Avanzadas Web**

**Semana 17-18:**
- ✅ Reserva pública (clientes)
  - Landing page de reserva
  - Wizard de reserva simplificado
  - Registro/login de cliente
  - Pago con Redsys InSite
  - Confirmación por email
- ✅ Configuración avanzada
  - Modo público/privado
  - Restricciones de clientes
  - Aprobación manual
  - Lista blanca
- ✅ Lista de espera
  - Apuntarse a lista de espera
  - Notificación cuando se libera hueco
  - Prioridad por categoría de cliente

**Semana 19-20:**
- ✅ Cupones y descuentos
  - Creación de cupones
  - Código promocional
  - Validez temporal
  - Límite de usos
  - Aplicación en reserva
- ✅ Programa de fidelización
  - Acumulación de puntos
  - Reglas de puntos por servicio
  - Canje de puntos por descuentos
  - Historial de puntos
- ✅ Fotografías antes/después
  - Subida a S3
  - Asociación a cita
  - Galería privada del cliente
  - Marca de agua
  - Expiración automática (RGPD)

**Entregables Sprint 9-10:**
- ✅ Booking público funcional
- ✅ Sistema de fidelización operativo
- ✅ Gestión de fotografías implementada
- ✅ Clientes pueden reservar por su cuenta
- ✅ Configuración avanzada para cada organización

---

**Sprints 11-14 (Mes 6-7): Aplicación Móvil**

**Semana 21-22: Setup y Pantallas Cliente (Parte 1)**
- ✅ Setup React Native
  - Crear proyecto con TypeScript
  - Configurar React Navigation
  - Setup de Zustand
  - Integrar React Native Paper
  - Configurar API client
- ✅ Autenticación móvil
  - Login/Registro
  - JWT handling
  - Refresh tokens
  - Biometría (FaceID/TouchID)

**Semana 23-24: Pantallas Cliente (Parte 2)**
- ✅ Pantallas principales
  - Home con servicios destacados
  - Catálogo completo de servicios
  - Detalle de servicio
  - Wizard de reserva
  - Pago (WebView Redsys InSite)
- ✅ Gestión de perfil
  - Ver/editar datos personales
  - Gestionar tarjetas guardadas
  - Preferencias de notificaciones
  - Historial de citas

**Semana 25-26: Pantallas Personal**
- ✅ App para empleados
  - Agenda del día
  - Detalle de cita
  - Check-in de cliente
  - Marcar como completado
  - Ver perfil de cliente
  - Registrar pago en efectivo
- ✅ Notificaciones push
  - Integración Firebase Cloud Messaging
  - Notificaciones de nuevas citas
  - Recordatorios personalizados
  - Deep linking a pantallas

**Semana 27-28: Testing y Publicación**
- ✅ Testing en dispositivos
  - iOS (iPhone 12+, iPad)
  - Android (varios fabricantes)
  - Diferentes tamaños de pantalla
- ✅ Beta testing
  - TestFlight (iOS)
  - Google Play Console (Android Beta)
  - Feedback de 10-20 usuarios beta
- ✅ Publicación en stores
  - App Store (iOS)
  - Google Play (Android)
  - Screenshots y descripción
  - Video preview

**Entregables Sprint 11-14:**
- ✅ Apps móviles iOS y Android publicadas
- ✅ Paridad de funcionalidades con web
- ✅ Push notifications funcionando
- ✅ Integración con Redsys en WebView
- ✅ 50+ descargas y valoración > 4.0/5 en stores

---

**🎯 HITO 2: APLICACIÓN COMPLETA**
- **Fecha objetivo:** Fin de Mes 7
- **Criterio de éxito:**
  - ✅ Apps móviles publicadas y disponibles
  - ✅ 100+ clientes del centro usando la app
  - ✅ 40%+ de citas reservadas vía app móvil
  - ✅ Valoración promedio > 4.0/5 en stores
  - ✅ < 1% crash rate

---

#### FASE 3: Multi-Tenant y SaaS (2 meses)

**Objetivo:** Convertir en plataforma SaaS lista para reventa

---

**Sprints 15-16 (Mes 8): Multi-Tenant**

**Semana 29-30:**
- ✅ Arquitectura multi-tenant
  - Aislamiento de datos por OrganizationId
  - Query filters globales en EF Core
  - Tenant resolution middleware
  - Testing de aislamiento exhaustivo
- ✅ Página de registro de organizaciones
  - Landing page pública
  - Formulario de registro
  - Verificación de email
  - Configuración de Redsys por organización
  - Subdominio personalizado

**Semana 31-32:**
- ✅ Onboarding wizard
  - Paso 1: Datos de negocio
  - Paso 2: Configuración de horarios
  - Paso 3: Primer empleado
  - Paso 4: Primer servicio
  - Paso 5: Configuración de pagos (Redsys)
  - Paso 6: ¡Listo para usar!
- ✅ Gestión de subdominios
  - DNS wildcard en Route 53
  - Certificados SSL dinámicos
  - Resolución de tenant por subdomain
- ✅ Testing de aislamiento
  - Unit tests
  - Integration tests
  - Penetration testing básico
  - Verificar que Org A no puede acceder a datos de Org B

**Entregables Sprint 15-16:**
- ✅ Sistema multi-tenant operativo
- ✅ Proceso de onboarding fluido y profesional
- ✅ 5-10 organizaciones de prueba activas
- ✅ Aislamiento de datos verificado
- ✅ Subdominios personalizados funcionando

---

**Sprints 17-18 (Mes 9): Monetización y Facturación**

**Semana 33-34:**
- ✅ Planes de suscripción
  - Definir 4 planes (Básico/Pro/Premium/Enterprise)
  - Límites por plan
  - Features por plan
  - Página de pricing
- ✅ Gestión de suscripciones
  - Crear suscripción al registrarse
  - Pagos recurrentes con Redsys
  - Upgrade/downgrade de plan
  - Cancelación de suscripción
  - Período de prueba (14 días)

**Semana 35-36:**
- ✅ Dashboard de administrador SaaS
  - Métricas de negocio
    - MRR (Monthly Recurring Revenue)
    - Churn rate
    - New signups
    - Active organizations
  - Gestión de organizaciones
    - Lista de todas las orgs
    - Cambiar plan manualmente
    - Suspender/reactivar
    - Ver logs y actividad
- ✅ Facturación automática
  - Generación de facturas (FUTURO - marcado)
  - Envío automático por email (FUTURO - marcado)
  - Descarga en PDF (FUTURO - marcado)
- ✅ Análisis y reportes
  - Cohort analysis (FUTURO - marcado)
  - Customer lifetime value (FUTURO - marcado)
  - Funnel de conversión (FUTURO - marcado)

**Entregables Sprint 17-18:**
- ✅ Modelo SaaS completamente funcional
- ✅ Sistema de suscripciones operativo con Redsys
- ✅ Dashboard de administración SaaS
- ✅ Proceso de pago recurrente automatizado
- ✅ 15+ organizaciones de pago activas

---

**🎯 HITO 3: LANZAMIENTO SAAS**
- **Fecha objetivo:** Fin de Mes 9
- **Criterio de éxito:**
  - ✅ 20+ organizaciones de pago usando la plataforma
  - ✅ MRR > €1,500/mes
  - ✅ Churn < 10%/mes
  - ✅ Tiempo de onboarding < 20 minutos
  - ✅ Satisfacción del cliente (NPS) > 8/10

---

#### FASE 4: Optimización y Escalado (Continuo)

**Objetivo:** Mejorar, escalar y añadir features avanzados

**Sprints 19+ (Mes 10 en adelante):**

**Prioridad Alta:**
- ✅ WhatsApp Business API
  - Integración con 360dialog o Twilio
  - Recordatorios por WhatsApp
  - Templates aprobados por Meta
  - Opt-in/opt-out management
- ✅ Integraciones externas
  - Google Calendar (sincronización bidireccional)
  - Apple Calendar
  - Outlook Calendar
  - Zapier webhooks
- ✅ Multi-idioma
  - Español (por defecto)
  - Inglés
  - Francés
  - Portugués
  - Detección automática de idioma

**Prioridad Media:**
- ✅ Gestión de múltiples locales
  - Una organización puede tener varios locales
  - Empleados por local
  - Transferencia de citas entre locales
- ✅ Analytics avanzado (marcar como FUTURO inicialmente)
  - Dashboard de BI
  - Reportes personalizables
  - Exportación a Excel/PDF
  - Gráficos interactivos
- ✅ Marketplace de integraciones
  - SDK para desarrolladores externos
  - Documentación de API pública
  - OAuth2 para apps de terceros

**Prioridad Baja / Experimental:**
- ✅ Inteligencia artificial
  - Recomendación de horarios óptimos (ML)
  - Predicción de no-shows
  - Chatbot de atención al cliente (GPT)
  - Análisis de sentimiento en comentarios
- ✅ Funcionalidades avanzadas
  - Video llamadas para consultas virtuales
  - Programa de referidos
  - Sistema de reseñas y valoraciones público
  - Integración con redes sociales

**Entregables continuos:**
- ✅ Mejoras de rendimiento
- ✅ Nuevas features basadas en feedback
- ✅ Escalado de infraestructura según necesidad
- ✅ Optimización de costos AWS

---

### 10.3 Cronograma Visual

```
MES 1-2: FUNDACIÓN + GESTIÓN BÁSICA
├─ Sprint 1-2: Setup + Auth + Infraestructura
└─ Sprint 3-4: CRUD Maestros (Empleados, Clientes, Servicios)

MES 3: SISTEMA DE CITAS (CORE)
└─ Sprint 5-6: Agenda + Crear Citas + Validaciones

MES 4: PAGOS + MVP
└─ Sprint 7-8: Redsys + Pre-auth + Recordatorios Email
   └─ 🎯 HITO 1: MVP EN PRODUCCIÓN

MES 5: FUNCIONALIDADES AVANZADAS
└─ Sprint 9-10: Booking Público + Fidelización + Fotos

MES 6-7: APLICACIÓN MÓVIL
├─ Sprint 11-12: App React Native - Cliente
└─ Sprint 13-14: App React Native - Personal + Testing + Publicación
   └─ 🎯 HITO 2: APP MÓVIL PUBLICADA

MES 8: MULTI-TENANT
└─ Sprint 15-16: Onboarding + Subdominios + Aislamiento

MES 9: MONETIZACIÓN SAAS
└─ Sprint 17-18: Suscripciones + Facturación Automática con Redsys
   └─ 🎯 HITO 3: LANZAMIENTO SAAS

MES 10+: OPTIMIZACIÓN CONTINUA
└─ WhatsApp + IA + Integraciones + Multi-idioma
```

---

### 10.4 Equipo Requerido

#### Para MVP (Fase 1 - 4 meses)

| Rol | Dedicación | Responsabilidades |
|-----|-----------|-------------------|
| **Backend Developer (.NET/C#)** | 100% | API, BD, Integración Redsys, Servicios |
| **Frontend Developer (React/Vite)** | 100% | UI/UX web, Integración Redsys InSite, Componentes |
| **Full-Stack Developer** | 50% | Apoyo backend y frontend, Code review |
| **DevOps/Infra (AWS)** | 25% | Infraestructura, CI/CD, Monitoring |
| **UI/UX Designer** | 25% | Diseños, Wireframes, Prototipos |

**Total personas equivalentes:** ~3.5 FTE

---

#### Para Fase 2 (Apps Móviles - 3 meses)

| Rol | Dedicación | Responsabilidades |
|-----|-----------|-------------------|
| **Backend Developer** | 75% | APIs para móvil, Push notifications |
| **Frontend Web Developer** | 50% | Mantenimiento y bugs |
| **Mobile Developer (React Native)** | 100% | Apps iOS/Android |
| **Full-Stack Developer** | 50% | Apoyo general |
| **QA/Tester** | 50% | Testing manual y automatizado |
| **DevOps** | 25% | Infraestructura y despliegues |

**Total personas equivalentes:** ~3.5 FTE

---

#### Para Fase 3 (SaaS - 2 meses)

| Rol | Dedicación | Responsabilidades |
|-----|-----------|-------------------|
| **Backend Developer** | 100% | Multi-tenancy, Suscripciones |
| **Frontend Web Developer** | 75% | Dashboard admin, Onboarding |
| **Mobile Developer** | 25% | Actualizaciones necesarias |
| **Full-Stack Developer** | 50% | Apoyo y testing |
| **DevOps** | 50% | Escalabilidad, Subdominios |

**Total personas equivalentes:** ~3.0 FTE

---

#### Roles Adicionales (Externo/Consultivo)

- **Asesor Legal RGPD/LOPD:** Consultoría puntual
- **Contador/Fiscalista:** Para facturación y fiscalidad
- **Product Manager:** El cliente puede asumir este rol
- **Marketing/Growth:** Para lanzamiento SaaS (Fase 3+)

---

## 11. ESTIMACIÓN DE COSTOS

### 11.1 Costos de Desarrollo (Recursos Humanos)

#### Fase 1: MVP (4 meses)

| Rol | Horas | Tarifa/h | Subtotal |
|-----|-------|----------|----------|
| Backend Developer (.NET) | 640h (4 meses × 160h) | €40/h | €25,600 |
| Frontend Developer (React/Vite) | 640h | €40/h | €25,600 |
| Full-Stack Developer | 320h (50% × 4 meses) | €40/h | €12,800 |
| DevOps (AWS) | 160h (25% × 4 meses) | €50/h | €8,000 |
| UI/UX Designer | 160h (25% × 4 meses) | €35/h | €5,600 |
| **SUBTOTAL FASE 1** | | | **€77,600** |

**Con margen de contingencia (+15%):** **€89,240**

---

#### Fase 2: Mejoras + App Móvil (3 meses)

| Rol | Horas | Tarifa/h | Subtotal |
|-----|-------|----------|----------|
| Backend Developer | 360h (75% × 3 meses) | €40/h | €14,400 |
| Frontend Web Developer | 240h (50% × 3 meses) | €40/h | €9,600 |
| Mobile Developer (React Native) | 480h (100% × 3 meses) | €40/h | €19,200 |
| Full-Stack Developer | 240h (50% × 3 meses) | €40/h | €9,600 |
| QA/Tester | 240h (50% × 3 meses) | €30/h | €7,200 |
| DevOps | 120h (25% × 3 meses) | €50/h | €6,000 |
| **SUBTOTAL FASE 2** | | | **€66,000** |

**Con margen de contingencia (+15%):** **€75,900**

---

#### Fase 3: Multi-Tenant SaaS (2 meses)

| Rol | Horas | Tarifa/h | Subtotal |
|-----|-------|----------|----------|
| Backend Developer | 320h (100% × 2 meses) | €40/h | €12,800 |
| Frontend Web Developer | 240h (75% × 2 meses) | €40/h | €9,600 |
| Mobile Developer | 80h (25% × 2 meses) | €40/h | €3,200 |
| Full-Stack Developer | 160h (50% × 2 meses) | €40/h | €6,400 |
| DevOps | 160h (50% × 2 meses) | €50/h | €8,000 |
| **SUBTOTAL FASE 3** | | | **€40,000** |

**Con margen de contingencia (+15%):** **€46,000**

---

**TOTAL DESARROLLO (9 meses):** **€211,140**

**Notas sobre costos de desarrollo:**
- Estos son costos estimados para un equipo en España/Europa
- Pueden reducirse significativamente:
  - **Equipo remoto de Latinoamérica:** -40% a -60% (~€85k-€125k total)
  - **Freelancers vs. Empresa:** -20% a -40% (~€125k-€170k total)
  - **Si el desarrollador es el mismo que presenta esta memoria:** costo = tiempo propio

---

### 11.2 Costos de Infraestructura AWS (Mensual)

#### Configuración Inicial (1 organización, ~500 citas/mes)

| Servicio | Especificación | Costo Mensual |
|----------|----------------|---------------|
| **Compute (ECS Fargate)** | 0.5 vCPU, 1GB RAM × 730h | ~€30 |
| **RDS PostgreSQL** | db.t3.medium (2 vCPU, 4GB RAM) | ~€60 |
| | 50GB storage SSD | Incluido |
| **S3** | 10GB storage | €0.23 |
| | 10,000 PUT requests | €0.05 |
| | 100,000 GET requests | €0.04 |
| | 50GB transfer out | €4.50 |
| **ALB (Load Balancer)** | Fijo + data processing | ~€22 |
| **CloudFront CDN** | 50GB transfer out | ~€5 |
| **SES (Email)** | 2,000 emails/mes | €0.20 |
| **Route 53** | 1 hosted zone | €0.50 |
| **CloudWatch** | Logs + métricas | ~€5 |
| **Secrets Manager** | 5 secrets | €2 |
| **Backups RDS** | 50GB | ~€5 |
| **Certificate Manager** | SSL/TLS certificates | Gratis |
| **TOTAL INICIAL** | | **~€135/mes** |

---

#### Escalado (5 organizaciones, 2,500 citas/mes)

| Servicio | Cambios | Costo Mensual |
|----------|---------|---------------|
| **Compute** | t3.medium (más potencia) | ~€60 |
| **RDS** | db.t3.large (2 vCPU, 8GB) | ~€120 |
| **S3** | 50GB + más requests | ~€10 |
| **ALB** | Mayor tráfico | ~€30 |
| **CloudFront** | 200GB transfer | ~€15 |
| **Otros** | Similar | ~€15 |
| **TOTAL (5 ORGS)** | | **~€250/mes** |

---

#### Escalado (50 organizaciones, 25,000 citas/mes)

| Servicio | Cambios | Costo Mensual |
|----------|---------|---------------|
| **Compute** | Múltiples instancias + autoscaling | ~€300 |
| **RDS** | db.r5.xlarge (4 vCPU, 32GB) + réplica | ~€500 |
| **S3** | 200GB + requests | ~€40 |
| **CloudFront** | 1TB transfer | ~€60 |
| **SES** | 100,000 emails | ~€10 |
| **WAF** | Protección DDoS | ~€25 |
| **Otros** | Monitoring avanzado | ~€50 |
| **TOTAL (50 ORGS)** | | **~€985/mes** |

---

### 11.3 Costos de Servicios Externos (Mensual)

#### Redsys (Pasarela de Pago)

**Estructura de costos:**
- Redsys es procesador contratado a través del banco
- Costos varían por entidad bancaria y volumen

**Estimación típica 2025:**

| Transacciones/mes | Importe promedio | Comisión | Costo Mensual |
|-------------------|------------------|----------|---------------|
| 500 | €25 | 1.2% | €150 |
| 2,500 | €25 | 1.1% | €687.50 |
| 10,000 | €25 | 1.0% | €2,500 |

**Costos adicionales Redsys:**
- **Cuota mensual:** €0 - €50 (según banco)
- **Setup:** €0 (puede haber costos del banco)
- **Bizum:** ~€0.50 por transacción

**Nota:** Estos costos los absorbe cada organización cliente, no el desarrollador de la plataforma SaaS.

---

#### WhatsApp Business API (Fase 3+)

**Proveedor recomendado:** 360dialog

**Costos de mensajes en España (2025):**
- **Categoría Utility:** €0.0095 por mensaje
- **Categoría Marketing:** €0.0436 por mensaje
- **Conversaciones de Servicio:** Gratis (cuando el cliente escribe primero)

**Estimación para recordatorios (Utility):**

| Organizaciones | Citas/mes | Recordatorios | Costo Mensual |
|----------------|-----------|---------------|---------------|
| 1 | 500 | 1,000 | €10 |
| 5 | 2,500 | 5,000 | €50 |
| 50 | 25,000 | 50,000 | €500 |

---

#### Otros Servicios

| Servicio | Plan | Costo Mensual |
|----------|------|---------------|
| **Dominio (.com/.es)** | Anual | €1/mes |
| **GitHub** | Team (5 usuarios) | €20 |
| **Sentry** | Error monitoring | €26 |
| **Google Analytics** | Free tier | Gratis |
| **Figma** | Professional | €12 |
| **TOTAL OTROS** | | **~€60/mes** |

---

**SUBTOTAL Servicios Externos (inicial):** **~€70/mes** (sin WhatsApp)  
**SUBTOTAL Servicios Externos (con WhatsApp):** **~€80/mes** (1 org)

---

### 11.4 Costos Legales y Compliance

| Concepto | Costo | Frecuencia |
|----------|-------|------------|
| **Asesoría RGPD inicial** | €800 - €1,500 | Una vez |
| **Elaboración de Políticas** | €600 - €1,200 | Una vez |
| (Privacidad, Cookies, T&C) | | |
| **DPO externo** (si requerido) | €80 - €200 | Mensual |
| **Revisión anual de compliance** | €500 | Anual |
| **Auditoría PCI-DSS** (SAQ A-EP) | €2,000 - €5,000 | Anual |
| **TOTAL INICIAL** | **€2,000 - €3,500** | Una vez |
| **TOTAL ANUAL** (después del inicial) | **€2,500 - €5,000** | Anual |

---

### 11.5 Costos de Publicación App Móvil

| Concepto | Costo | Frecuencia |
|----------|-------|------------|
| **Apple Developer Program** | $99 (€95) | Anual |
| **Google Play Console** | $25 (€24) | Una vez |
| **TOTAL AÑO 1** | **€119** | |
| **TOTAL AÑOS SIGUIENTES** | **€95/año** | Anual |

---

### 11.6 Resumen de Costos Totales

#### Inversión Inicial (Fase 1 - MVP)

| Concepto | Costo |
|----------|-------|
| Desarrollo (4 meses) | €89,240 |
| Infraestructura AWS (4 meses) | €540 (€135×4) |
| Servicios externos (4 meses) | €280 (€70×4) |
| Legal y compliance | €2,500 |
| **TOTAL INVERSIÓN MVP** | **€92,560** |

---

#### Costos Operativos Mensuales (Después de MVP)

| Concepto | 1 Org | 5 Orgs | 50 Orgs |
|----------|-------|--------|---------|
| AWS Infraestructura | €135 | €250 | €985 |
| WhatsApp (Fase 3+) | €10 | €50 | €500 |
| Otros servicios | €60 | €80 | €120 |
| DPO (si aplica) | €0-200 | €150 | €200 |
| **TOTAL MENSUAL** | **€205-€405** | **€530** | **€1,805** |

---

#### Inversión Total (Fases 1-3)

| Concepto | Costo |
|----------|-------|
| Desarrollo completo (9 meses) | €211,140 |
| Infraestructura AWS (9 meses desarrollo) | €1,350 |
| Servicios externos (9 meses) | €630 |
| Legal y compliance inicial | €2,500 |
| Publicación apps móviles | €119 |
| **TOTAL PROYECTO COMPLETO** | **€215,739** |

---

### 11.7 Modelo de Monetización SaaS (Fase 3)

#### Planes Propuestos

| Plan | Precio/mes | Citas/mes | Empleados | Características |
|------|-----------|-----------|-----------|-----------------|
| **Básico** | €49 | 200 | 3 | Email, 1 local, Web + Móvil |
| **Profesional** | €99 | 1,000 | 10 | + WhatsApp, Reportes básicos |
| **Premium** | €199 | Ilimitadas | Ilimitados | + IA, Multi-local, Soporte prioritario |
| **Enterprise** | €399+ | Ilimitadas | Ilimitados | + Personalización, Onboarding dedicado |

**Notas:**
- Período de prueba: 14 días gratis (todos los planes)
- Descuento anual: 20% (2 meses gratis)
- Costos de transacción Redsys: pagados por cada organización
- Setup fee: €0 (incluido en todos los planes)

---

#### Análisis Break-Even

**Costos fijos mensuales (50 clientes):**
- Infraestructura AWS: €985
- Servicios externos: €120
- DPO: €200
- Soporte/Mantenimiento: €500 (estimado)
- **TOTAL FIJOS:** €1,805/mes

**Ingresos mensuales objetivo:**

| Escenario | Distribución | MRR |
|-----------|-------------|-----|
| **Conservador** | 20 Básico + 5 Profesional + 2 Premium | €1,675/mes |
| **Moderado** | 25 Básico + 15 Profesional + 8 Premium + 2 Enterprise | €4,418/mes |
| **Optimista** | 15 Básico + 25 Profesional + 15 Premium + 5 Enterprise | €7,215/mes |

**Break-even:** ~**15-20 clientes** (mix de planes) = €1,800-€2,000/mes

**Objetivos:**
- **Mes 12:** 30 clientes = €2,500/mes MRR
- **Mes 18:** 50 clientes = €4,500/mes MRR
- **Mes 24:** 100 clientes = €10,000/mes MRR

---

#### Análisis de ROI

**Inversión total:** €215,739

**Escenario conservador:**
- Año 1: MRR promedio €2,000/mes × 12 = €24,000
- Año 2: MRR promedio €6,000/mes × 12 = €72,000
- Año 3: MRR promedio €10,000/mes × 12 = €120,000
- **Total 3 años:** €216,000
- **Recuperación inversión:** 24-30 meses

**Escenario optimista:**
- Año 1: MRR promedio €3,500/mes × 12 = €42,000
- Año 2: MRR promedio €9,000/mes × 12 = €108,000
- Año 3: MRR promedio €15,000/mes × 12 = €180,000
- **Total 3 años:** €330,000
- **Recuperación inversión:** 18-20 meses

**Conclusión:** ROI positivo esperado entre 18-30 meses según tasa de adquisición.

---

## 12. PRÓXIMOS PASOS

### 12.1 Pasos Inmediatos (Semana 1-2)

#### 1. Validación y Aprobación del Cliente

**Acciones:**
- [ ] Presentar esta memoria técnica completa al cliente
- [ ] Revisar todas las funcionalidades propuestas
- [ ] Confirmar prioridades y alcance del MVP
- [ ] Discutir presupuesto y timeline
- [ ] Definir criterios de éxito
- [ ] Firmar contrato o acuerdo de desarrollo

**Entregables:**
- Memoria técnica aprobada con firma del cliente
- Statement of Work (SOW) detallado
- Cronograma acordado
- Presupuesto aprobado

---

#### 2. Planificación Detallada

**Acciones:**
- [ ] Definir equipo de desarrollo
  - Identificar desarrolladores disponibles
  - Asignar roles y responsabilidades
  - Establecer dedicación por persona
- [ ] Crear backlog en herramienta de gestión (Jira/Linear)
  - Crear épicas por módulo
  - Desglosar en user stories
  - Asignar story points
  - Priorizar backlog
- [ ] Planificar Sprint 1 en detalle
  - Seleccionar user stories
  - Crear tareas técnicas
  - Asignar responsables
  - Definir Definition of Done
- [ ] Definir métricas de éxito (KPIs)
  - Velocity del equipo
  - Quality metrics (bugs, coverage)
  - Performance metrics (response time)
  - Business metrics (conversión, satisfacción)

**Entregables:**
- Backlog completo y priorizado
- Sprint 1 planificado
- KPIs definidos y acordados
- Calendario de ceremonias Scrum

---

#### 3. Setup Técnico Inicial

**Acciones:**

**AWS:**
- [ ] Crear cuenta AWS (o usar existente)
- [ ] Configurar AWS Organizations si multi-cuenta
- [ ] Configurar billing alerts
- [ ] Crear usuarios IAM con MFA
- [ ] Configurar VPC en región eu-west-1 (Irlanda)
- [ ] Crear subnets públicas y privadas
- [ ] Configurar Security Groups
- [ ] Crear RDS PostgreSQL (entorno dev)
- [ ] Crear buckets S3 (dev/staging/prod)
- [ ] Verificar dominio en Amazon SES

**Repositorios:**
- [ ] Crear organización en GitHub
- [ ] Crear repositorio backend (reservarte-api)
- [ ] Crear repositorio frontend web (reservarte-web)
- [ ] Crear repositorio móvil (reservarte-mobile)
- [ ] Configurar branch protection rules
- [ ] Configurar GitHub Actions para CI
- [ ] Definir estrategia de branching (Git Flow)

**Entornos:**
- [ ] Configurar 3 entornos: Dev / Staging / Production
- [ ] Crear bases de datos por entorno
- [ ] Configurar subdominios:
  - dev.reservarte.com
  - staging.reservarte.com
  - app.reservarte.com
- [ ] Configurar certificados SSL/TLS

**CI/CD:**
- [ ] Pipeline de build para backend
- [ ] Pipeline de build para frontend
- [ ] Pipeline de tests automatizados
- [ ] Pipeline de deployment a staging
- [ ] Pipeline de deployment a production (manual approval)

**Entregables:**
- Infraestructura AWS funcional
- Repositorios Git configurados
- CI/CD pipelines operativos
- Entornos de desarrollo, staging y producción listos

---

#### 4. Gestión de Cuentas de Servicios Externos

**Redsys:**
- [ ] Contactar con banco para cuenta de comercio
- [ ] Solicitar credenciales de entorno de pruebas
- [ ] Obtener FUC (código de comercio)
- [ ] Obtener Terminal
- [ ] Obtener clave secreta (256 bits)
- [ ] Documentar configuración en Secrets Manager
- [ ] Realizar transacción de prueba exitosa

**Otros:**
- [ ] Crear cuenta Amazon SES y verificar dominio
- [ ] Configurar SPF, DKIM, DMARC
- [ ] Solicitar salir de sandbox de SES
- [ ] Crear cuenta GitHub (si no existe)
- [ ] Crear cuenta Sentry para error tracking

**Entregables:**
- Credenciales de Redsys (test) funcionando
- Amazon SES operativo y fuera de sandbox
- Documentación de todas las credenciales en lugar seguro

---

#### 5. Legal y Compliance

**Acciones:**
- [ ] Contactar asesor legal especializado en RGPD
- [ ] Iniciar elaboración de políticas:
  - Política de Privacidad
  - Política de Cookies
  - Términos y Condiciones
  - Política de Cancelación
- [ ] Planificar EIPD (Evaluación de Impacto en Protección de Datos)
- [ ] Definir proceso de gestión de consentimientos
- [ ] Revisar necesidad de DPO

**Entregables:**
- Asesor legal contratado
- Borradores de políticas en revisión
- Plan para EIPD
- Documentación de compliance

---

### 12.2 Checklist de Arranque (Semana 3-4)

#### Backend (.NET Core)

- [ ] Crear solución con Clean Architecture
- [ ] Instalar paquetes NuGet necesarios
- [ ] Configurar Entity Framework Core
- [ ] Crear primera migración (tablas core)
- [ ] Configurar ASP.NET Core Identity
- [ ] Implementar JWT authentication
- [ ] Configurar Serilog + CloudWatch
- [ ] Configurar Swagger/OpenAPI
- [ ] Escribir primer endpoint de health check
- [ ] Configurar conexión a RDS PostgreSQL

#### Frontend Web (React + Vite)

- [ ] Crear proyecto con Vite + React + TypeScript
- [ ] Configurar Tailwind CSS
- [ ] Instalar y configurar shadcn/ui
- [ ] Configurar Zustand para estado global
- [ ] Configurar React Router
- [ ] Crear estructura de carpetas
- [ ] Implementar axios client con interceptors
- [ ] Crear layout principal
- [ ] Implementar página de login
- [ ] Configurar variables de entorno

#### DevOps

- [ ] Dockerfile para backend
- [ ] Dockerfile para frontend
- [ ] docker-compose.yml para desarrollo local
- [ ] GitHub Actions workflow para backend
- [ ] GitHub Actions workflow para frontend
- [ ] Script de deployment a staging
- [ ] Script de deployment a production
- [ ] Configurar Secrets en GitHub Actions
- [ ] Configurar CloudWatch Dashboards

#### Base de Datos

- [ ] Crear esquema inicial
- [ ] Tablas: organizations, users, employees
- [ ] Índices iniciales
- [ ] Seed data para desarrollo
- [ ] Procedimientos almacenados (si necesarios)
- [ ] Backup schedule configurado

---

### 12.3 Riesgos y Mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|--------------|---------|------------|
| **Retrasos en desarrollo** | Alta | Alto | Planificación realista con buffer del 15%, revisiones semanales |
| **Problemas con aprobación Redsys** | Media | Alto | Iniciar proceso bancario cuanto antes, tener entorno de test funcionando |
| **Costos AWS más altos** | Media | Medio | Monitorización constante, alertas de billing, optimización continua |
| **Cambios en regulación RGPD** | Baja | Alto | Asesor legal continuo, revisión trimestral de compliance |
| **Baja adopción SaaS** | Media | Alto | Marketing pre-lanzamiento, precio competitivo, periodo prueba, UX excelente |
| **Problemas de escalabilidad** | Baja | Alto | Arquitectura escalable desde inicio, load testing antes de producción |
| **Brecha de seguridad** | Baja | Crítico | Auditorías de seguridad, penetration testing, seguro cibernético |
| **Dependencia de terceros (Redsys)** | Media | Medio | Documentación exhaustiva, fallbacks, monitoreo 24/7 |
| **Rotación del equipo** | Media | Alto | Documentación detallada, code reviews, knowledge sharing |
| **Competencia en mercado** | Alta | Medio | Diferenciación por UX, soporte local, precio competitivo |

---

### 12.4 Criterios de Éxito

#### MVP (Fin Mes 4)

**Técnicos:**
- ✅ Aplicación desplegada en producción
- ✅ Tiempo de respuesta API < 300ms (p95)
- ✅ Uptime > 99.5%
- ✅ 0 vulnerabilidades de seguridad críticas
- ✅ Test coverage > 70% en backend
- ✅ Lighthouse score > 85 en frontend

**Negocio:**
- ✅ 1 centro piloto usando el sistema diariamente
- ✅ 100+ citas gestionadas exitosamente
- ✅ 50+ transacciones con Redsys sin errores
- ✅ NPS (Net Promoter Score) > 7/10 del cliente piloto
- ✅ < 5 bugs críticos reportados
- ✅ Tiempo promedio de creación de cita < 3 minutos

---

#### App Móvil (Fin Mes 7)

**Técnicos:**
- ✅ Apps publicadas en App Store y Google Play
- ✅ Crash-free rate > 99%
- ✅ App startup time < 3 segundos
- ✅ API calls < 2 segundos

**Negocio:**
- ✅ 100+ instalaciones totales
- ✅ 50+ usuarios activos mensuales
- ✅ 30% de citas reservadas vía app
- ✅ Valoración > 4.0/5 en stores
- ✅ Tasa de retención (Day 7) > 40%

---

#### Lanzamiento SaaS (Fin Mes 12)

**Técnicos:**
- ✅ Multi-tenancy funcionando sin problemas
- ✅ Aislamiento de datos verificado
- ✅ Onboarding completo < 20 minutos
- ✅ 0 downtime en últimos 30 días

**Negocio:**
- ✅ 20+ organizaciones de pago activas
- ✅ MRR (Monthly Recurring Revenue) > €1,500
- ✅ Churn rate < 10%/mes
- ✅ CAC (Customer Acquisition Cost) < €300
- ✅ NPS promedio > 8/10
- ✅ LTV/CAC ratio > 3:1

---

## ANEXOS

### Anexo A: Glosario de Términos

**A-E:**
- **API:** Application Programming Interface
- **AWS:** Amazon Web Services
- **BSP:** Business Solution Provider (WhatsApp)
- **CAC:** Customer Acquisition Cost
- **CAPTCHA:** Completely Automated Public Turing test
- **CDN:** Content Delivery Network
- **CI/CD:** Continuous Integration / Continuous Deployment
- **COF:** Credential On File (Redsys)
- **CRUD:** Create, Read, Update, Delete
- **DPA:** Data Processing Agreement
- **DPO:** Data Protection Officer (Delegado de Protección de Datos)
- **EIPD:** Evaluación de Impacto en Protección de Datos

**F-M:**
- **FUC:** Número de comercio en Redsys
- **HMAC:** Hash-based Message Authentication Code
- **HMR:** Hot Module Replacement (Vite)
- **JWT:** JSON Web Token
- **KPI:** Key Performance Indicator
- **LOPD:** Ley Orgánica de Protección de Datos
- **LSSI-CE:** Ley de Servicios de la Sociedad de la Información
- **MRR:** Monthly Recurring Revenue

**N-Z:**
- **MVP:** Minimum Viable Product
- **NPS:** Net Promoter Score
- **ORM:** Object-Relational Mapping
- **PAN:** Primary Account Number (número de tarjeta)
- **PCI-DSS:** Payment Card Industry Data Security Standard
- **RGPD:** Reglamento General de Protección de Datos (GDPR en inglés)
- **ROI:** Return On Investment
- **SaaS:** Software as a Service
- **SAQ:** Self-Assessment Questionnaire (PCI-DSS)
- **SCA:** Strong Customer Authentication
- **SDK:** Software Development Kit
- **SES:** Simple Email Service (AWS)
- **TLS:** Transport Layer Security
- **TPV:** Terminal Punto de Venta
- **VPC:** Virtual Private Cloud

---

### Anexo B: Referencias y Recursos

#### Documentación Técnica

**Frameworks y Librerías:**
- ASP.NET Core: https://docs.microsoft.com/aspnet/core
- React: https://react.dev
- Vite: https://vitejs.dev
- React Native: https://reactnative.dev
- Entity Framework Core: https://docs.microsoft.com/ef/core
- Tailwind CSS: https://tailwindcss.com/docs
- shadcn/ui: https://ui.shadcn.com

**Infraestructura:**
- AWS Documentation: https://docs.aws.amazon.com
- AWS RDS PostgreSQL: https://docs.aws.amazon.com/rds/
- Amazon SES: https://docs.aws.amazon.com/ses/
- Amazon S3: https://docs.aws.amazon.com/s3/

**Redsys:**
- Portal desarrolladores Redsys: https://pagosonline.redsys.es
- Documentación técnica: https://pagosonline.redsys.es/desarrolladores.html
- Manual de integración InSite: Solicitar a banco adquirente
- Códigos de respuesta: Consultar documentación oficial

---

#### RGPD y Legal

**Recursos oficiales:**
- AEPD (Agencia Española de Protección de Datos): https://www.aepd.es
- RGPD Texto completo: https://gdpr.eu
- Guía de Cookies AEPD: https://www.aepd.es/guias/guia-cookies.pdf
- Guía de Análisis de Riesgos: https://www.aepd.es/sites/default/files/2019-09/guia-analisis-de-riesgos.pdf

**Plantillas útiles:**
- Política de Privacidad template: Solicitar a asesor legal
- Registro de Actividades de Tratamiento: Template AEPD
- Modelo de consentimiento RGPD: Template AEPD

---

#### WhatsApp Business

**Documentación oficial:**
- WhatsApp Business API: https://business.whatsapp.com
- Meta for Developers: https://developers.facebook.com/docs/whatsapp
- Precios WhatsApp: https://business.whatsapp.com/products/platform-pricing
- 360dialog Docs: https://docs.360dialog.com

**Categorías de mensajes:**
- Utility: Recordatorios, confirmaciones
- Marketing: Promociones, ofertas
- Authentication: Códigos OTP
- Service: Respuestas a consultas

---

#### Herramientas y Servicios

**Desarrollo:**
- GitHub: https://github.com
- Vite documentation: https://vitejs.dev/guide/
- Figma: https://www.figma.com
- Postman: https://www.postman.com

**Monitoreo:**
- Sentry: https://sentry.io
- AWS CloudWatch: https://aws.amazon.com/cloudwatch/
- Google Analytics: https://analytics.google.com

**Testing:**
- TestFlight (iOS): https://testflight.apple.com
- Google Play Console (Android): https://play.google.com/console

**Calculadoras:**
- AWS Pricing Calculator: https://calculator.aws
- Redsys Simulator: Solicitar acceso a banco

---

### Anexo C: Contactos Recomendados

#### Proveedores de Servicios

**Redsys:**
- Contratar a través de tu banco comercial
- Bancos recomendados con Redsys:
  - BBVA
  - Santander
  - CaixaBank
  - Banco Sabadell

**WhatsApp BSP:**
- 360dialog: https://www.360dialog.com
- Twilio: https://www.twilio.com/whatsapp
- Vonage (ex-Nexmo): https://www.vonage.com

**Asesoría Legal RGPD:**
- Buscar despacho local especializado en RGPD y tech
- Verificar experiencia con startups SaaS
- Solicitar referencias

**Hosting Alternativo:**
- DigitalOcean: https://www.digitalocean.com
- Vultr: https://www.vultr.com
- Hetzner Cloud: https://www.hetzner.com/cloud

---

#### Comunidades y Soporte

**Desarrollo:**
- Stack Overflow: Para dudas técnicas
- Reddit r/dotnet: Comunidad .NET
- Reddit r/reactjs: Comunidad React
- Dev.to: Artículos y tutoriales

**AWS:**
- AWS Support: Plan Developer (€29/mes) o Business (€100/mes)
- AWS re:Post: Comunidad de preguntas y respuestas

**Redsys:**
- Soporte técnico: A través de tu banco adquirente
- Comunidad de desarrolladores: Foros bancarios

**WhatsApp:**
- Meta for Business Help Center
- WhatsApp Business Developers Facebook Group

---

### Anexo D: Checklist de Go-Live

#### Pre-producción (1 semana antes)

**Técnico:**
- [ ] Todos los tests pasan (unit, integration, e2e)
- [ ] Performance testing completado
- [ ] Security audit realizado
- [ ] Backup strategy configurada y probada
- [ ] Disaster recovery plan documentado
- [ ] Monitoring y alertas configurados
- [ ] SSL certificates instalados y verificados
- [ ] DNS configurado correctamente
- [ ] Redsys en modo producción configurado

**Legal:**
- [ ] Políticas de Privacidad, Cookies, T&C publicadas
- [ ] EIPD completada y aprobada
- [ ] Consentimientos implementados
- [ ] Banner de cookies funcionando

**Negocio:**
- [ ] Cliente piloto formado
- [ ] Documentación de usuario finalizada
- [ ] Videos tutoriales grabados
- [ ] Plan de soporte definido
- [ ] Pricing final confirmado

---

#### Día del Go-Live

**Mañana:**
- [ ] 09:00 - Backup completo de BD de staging
- [ ] 09:30 - Deployment a producción
- [ ] 10:00 - Smoke tests en producción
- [ ] 10:30 - Verificar Redsys en producción
- [ ] 11:00 - Activar DNS hacia producción
- [ ] 11:30 - Verificar email (SES) funcionando
- [ ] 12:00 - Meeting con cliente piloto

**Tarde:**
- [ ] 14:00 - Monitoreo activo de métricas
- [ ] 15:00 - Primera cita de prueba real
- [ ] 16:00 - Verificar logs sin errores
- [ ] 17:00 - Cliente piloto crea primera cita
- [ ] 18:00 - Retrospectiva del día

---

#### Post Go-Live (Primera semana)

**Diario:**
- [ ] Revisar logs de errores
- [ ] Revisar métricas de performance
- [ ] Revisar transacciones Redsys
- [ ] Recolectar feedback del cliente
- [ ] Actualizar documentación según sea necesario

**Semanal:**
- [ ] Meeting de retrospectiva
- [ ] Planificar fixes urgentes
- [ ] Actualizar roadmap basado en feedback
- [ ] Comunicar progreso al cliente

---

### Anexo E: Templates de Documentos

#### Template: User Story

```
Como [rol]
Quiero [funcionalidad]
Para [beneficio]

Criterios de Aceptación:
- [ ] Dado [contexto]
  Cuando [acción]
  Entonces [resultado esperado]

Notas Técnicas:
- [Consideraciones de implementación]

Definition of Done:
- [ ] Código implementado
- [ ] Tests unitarios escritos y pasando
- [ ] Code review completado
- [ ] Documentación actualizada
- [ ] Desplegado a staging y verificado
```

---

#### Template: Bug Report

```
**Título:** [Descripción breve del bug]

**Severidad:** [Crítico / Alto / Medio / Bajo]

**Entorno:** [Producción / Staging / Desarrollo]

**Pasos para Reproducir:**
1. [Paso 1]
2. [Paso 2]
3. [...]

**Comportamiento Esperado:**
[Qué debería pasar]

**Comportamiento Actual:**
[Qué está pasando]

**Screenshots/Videos:**
[Adjuntar si es posible]

**Información Adicional:**
- Navegador: [Chrome 120, Safari 17, etc.]
- SO: [Windows 11, macOS 14, etc.]
- Versión de la app: [1.0.5]
- Logs relevantes: [Adjuntar]
```

---

#### Template: Sprint Retrospective

```
**Sprint:** [Número]
**Fecha:** [DD/MM/YYYY]
**Participantes:** [Lista]

**Qué Fue Bien ✅**
- [Item 1]
- [Item 2]

**Qué Puede Mejorar 🔧**
- [Item 1]
- [Item 2]

**Acciones para el Próximo Sprint 🎯**
- [ ] [Acción 1] - Responsable: [Nombre]
- [ ] [Acción 2] - Responsable: [Nombre]

**Métricas del Sprint:**
- Velocity: [Story points completados]
- Bugs encontrados: [Número]
- Bugs resueltos: [Número]
- Test coverage: [Porcentaje]
```

---

## CONCLUSIÓN

Esta memoria técnica presenta un plan completo, detallado y viable para el desarrollo de **ReservArte**, una aplicación multi-tenant de gestión para centros de diseño de cejas en España.

### Puntos Clave del Proyecto

**✅ Tecnología Moderna y Robusta:**
- Backend: ASP.NET Core 8.0
- Frontend Web: React 18 + Vite (HMR ultra-rápido)
- Frontend Móvil: React Native
- Base de Datos: PostgreSQL en AWS RDS
- Infraestructura: AWS con alta disponibilidad

**✅ Cumplimiento Legal Estricto:**
- RGPD y LOPD compliant desde el diseño
- PCI-DSS SAQ A-EP con Redsys InSite
- Políticas de privacidad, cookies y términos
- EIPD para datos sensibles
- Datos permanecen en España/UE

**✅ Sistema de Pagos Robusto:**
- Redsys InSite como método principal (PCI simplificado)
- Pre-autorizaciones para reducir no-shows
- Tokenización nativa para guardar tarjetas
- Penalizaciones automáticas por cancelaciones tardías
- Soporte para Bizum

**✅ Arquitectura Escalable:**
- Multi-tenant desde el inicio
- Escalado horizontal posible
- Optimización de costos por etapas
- Preparado para 100+ organizaciones

**✅ Notificaciones Multi-Canal:**
- Email con Amazon SES
- WhatsApp Business API (Fase 3+)
- Push notifications en apps móviles
- Recordatorios configurables

### Viabilidad Económica

**Inversión:**
- MVP (4 meses): €92,560
- Proyecto completo (9 meses): €215,739

**Costos operativos:**
- Inicio (1 org): ~€200/mes
- Escalado (50 orgs): ~€1,800/mes

**Ingresos potenciales (SaaS):**
- Mes 12: €2,500/mes (30 clientes)
- Mes 24: €10,000/mes (100 clientes)

**ROI esperado:** 18-30 meses

### Riesgos Mitigados

- ✅ Seguridad de pagos garantizada por Redsys
- ✅ Cumplimiento legal desde el diseño
- ✅ Arquitectura probada y escalable
- ✅ Stack tecnológico maduro y bien soportado
- ✅ Roadmap realista con hitos claros

### Próximos Pasos Inmediatos

1. **Aprobación del cliente** y firma de contrato
2. **Setup de infraestructura** AWS y repositorios
3. **Inicio del Sprint 1** de desarrollo
4. **Contacto con banco** para credenciales Redsys
5. **Contratación de asesor legal** RGPD

---

**El proyecto ReservArte está técnicamente bien fundamentado, es viable económicamente, cumple con toda la normativa legal española y europea, y tiene un camino claro hacia la rentabilidad como plataforma SaaS.**

---

**Documento preparado por:** [Tu nombre/empresa]  
**Fecha:** Octubre 2025  
**Versión:** 1.0  
**Confidencialidad:** Este documento contiene información confidencial y es propiedad de [Tu empresa]. No debe ser reproducido ni compartido sin autorización expresa.

---

## FIRMAS DE CONFORMIDAD

### Por parte del Cliente

**Nombre:** Sofía Fatás Ounka___________  
**Cargo:** CEO y propietaria___________  
**Empresa:** More Than Brows__________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

---

### Por parte del Proveedor

**Nombre:** Gabriel Sánchez-Vallejo Millán
**Cargo:** Co-director________________  
**Empresa:** 2º DAW 2025-2026__________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

**Nombre:** Guillermo Algárate del Arco  
**Cargo:** Co-director_________________  
**Empresa:** 2º DAW 2025-2026__________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________
---

**FIN DE LA MEMORIA TÉCNICA DE RESERVARTE**

**3 DOCUMENTOS COMPLETADOS:**
1. ✅ Análisis y Especificaciones Técnicas
2. ✅ Implementación y Desarrollo
3. ✅ Planificación y Gestión

---