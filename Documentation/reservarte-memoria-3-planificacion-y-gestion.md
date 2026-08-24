# RESERVARTE — Documentación técnica

## Sistema multi-tenant de gestión para centros de diseño de cejas

**Volumen 3 de 3: Planificación y gestión**

---

**Versión:** 1.0  
**Fecha:** Octubre 2025  
**Cliente:** More Than Brows  
**Ubicación:** España  
**Equipo de desarrollo:** Gabriel Sánchez-Vallejo Millán y Guillermo Algárate del Arco

---

## Índice (volumen 3)

1. [PLAN DE DESARROLLO - ROADMAP](#10-plan-de-desarrollo-roadmap)
2. [ESTIMACIÓN DE COSTOS](#11-estimaciÃ³n-de-costos)
3. [PRÓXIMOS PASOS](#12-prÃ³ximos-pasos)
4. [ANEXOS](#anexos)

> **Documentación complementaria:** [Estrategia de testing](reservarte-testing-strategy.md) — pirámide de pruebas, herramientas (xUnit, Testcontainers, Vitest, Playwright), CI/CD y cobertura por fase; enlazada desde **§12** y la subsección **Testing** del checklist **§12.2**. [Accesibilidad e i18n](accessibility-and-i18n.md) — WCAG 2.1 AA, vue-i18n v9, contraste y axe; coherente con **§10.2** y `Documentation/Project-Init/Scripts de instalación.md`.

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

- **Gestión de proyecto:** **ClickUp** (workspace, espacios y listas definidos en §10.1.1)
- **Comunicación:** Slack
- **Control de versiones:** Git en **GitHub** — estrategia de ramas **Git Flow**, mensajes **Conventional Commits** y revisión mediante **Pull Requests** con plantilla (§10.1.2)
- **CI/CD:** GitHub Actions
- **Documentación técnica:** repositorio Git (`Documentation/`, volúmenes de análisis, implementación y planificación); seguimiento de tareas de documentación en ClickUp — Space **Documentation**, listas **Technical Specs** y **Architecture Decisions**



#### 10.1.1 ClickUp — Workspace y espacios

La planificación del trabajo, el backlog, los sprints y el seguimiento transversal se centralizan en **ClickUp** con la siguiente estructura:

**Workspace:** `ReservArte`


| Space                     | Listas                                  |
| ------------------------- | --------------------------------------- |
| **Backend (.NET)**        | Sprint Activo; Backlog; Bugs            |
| **Frontend (Vue 3)**      | Sprint Activo; Backlog                  |
| **Mobile (React Native)** | Backlog                                 |
| **Infrastructure**        | Tareas AWS / Docker / CI-CD             |
| **Documentation**         | Technical Specs; Architecture Decisions |


- **Sprint Activo:** tareas comprometidas para el sprint en curso (donde exista lista homónima).
- **Backlog:** trabajo priorizado pendiente de asignar a un sprint.
- **Bugs:** incidencias y regresiones del backend (Space Backend).
- **Tareas AWS / Docker / CI-CD:** despliegue, contenedores, pipelines y operación (Space Infrastructure).
- **Technical Specs:** especificaciones y entregables técnicos alineados con el repositorio `Documentation/`.
- **Architecture Decisions:** decisiones de arquitectura (p. ej. ADR), debates y cierres de diseño.



#### 10.1.2 Git Flow, Conventional Commits y Pull Requests

**Modelo de ramas (Git Flow)** — referencia clásica [nvie Git Flow](https://nvie.com/posts/a-successful-git-branching-model/):


| Rama                | Propósito                                                                                                                     |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `main`              | Código **en producción**; solo recibe merges desde `release/`* o `hotfix/*` (o etiquetas de versión).                         |
| `develop`           | Rama de **integración** continua del siguiente release; destino habitual de `feature/`* y origen de `release/*`.              |
| `feature/<nombre>`  | Nuevo desarrollo o mejora (p. ej. `feature/appointments-calendar`); se abre desde `develop` y se fusiona en `develop` vía PR. |
| `release/<versión>` | Preparación de un despliegue (congelar versión, ajustes finos); merge a `main` y de vuelta a `develop`.                       |
| `hotfix/<nombre>`   | Corrección urgente en producción; parte de `main`, merge a `main` y a `develop`.                                              |


**Reglas operativas:**

- No pushear directamente a `main` ni a `develop` sin política explícita; usar **Pull Requests** y **branch protection** (revisiones obligatorias, CI en verde).
- Los workflows de **GitHub Actions** deben dispararse en PR hacia `develop` / `main` y en push según política del equipo (documentar en cada workflow).
- Si el código vive en **varios repositorios** (API, web, móvil), replicar la misma convención en todos para no fragmentar el flujo.

**Conventional Commits** — especificación [conventionalcommits.org](https://www.conventionalcommits.org/):

- Formato: `<tipo>[ámbito opcional]: <descripción breve>`  
Ejemplos: `feat(auth): add Google OAuth challenge`, `fix(appointments): validate slot overlap`, `docs: update API envelope §5.1.1`
- Tipos habituales: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
- Cuerpo y pie opcionales; para cambios rupturistas: pie con `BREAKING CHANGE:` o `!` tras el tipo (`feat(api)!: ...`).
- Permite generar **changelog** y versionado semántico de forma coherente con **release/**.

**Plantilla de Pull Request**

- Ubicación en el repositorio: `.github/PULL_REQUEST_TEMPLATE.md` (GitHub la aplica al abrir un PR).
- Si hay monorepo único, un solo fichero basta; si hay varios repos, copiar la misma plantilla a cada uno o adaptarla.
- El contenido debe guiar: descripción del cambio, tipo (feature/fix/docs…), checklist (tests, documentación, breaking changes), enlace a tarea ClickUp, capturas si aplica UI.

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
  - Aprovisionar SQL Server en Docker (entorno dev, p. ej. `docker-compose`)
  - Crear cuenta **Cloudinary** y carpetas / upload presets (dev/staging/prod)
  - Configurar variables o secrets con `CloudName`, `ApiKey`, `ApiSecret`
  - Configurar Amazon SES (verificar dominio)
- ✅ Configuración de proyecto .NET
  - Crear solución con Clean Architecture
  - Configurar Entity Framework Core
  - Setup de migraciones de BD
  - Serilog: pipeline en dos fases + sink consola + enriquecimiento por petición — **hecho**; sink CloudWatch — **pendiente** (infra)
- ✅ Configuración de proyecto Vite
  - Crear proyecto Vue 3 + TypeScript + Vite
  - Configurar Tailwind CSS + componentes UI alineados con Vue (p. ej. Reka UI / Radix-Vue)
  - **Arquitectura i18n (Sprint 1):** instalar **vue-i18n v9**, carpetas `src/locales/` y `src/i18n/`, mensajes base en **español** y registro en `main.ts` según `Documentation/Project-Init/Scripts de instalación.md` (Pasos 2–5)
  - Utilidades de formato **es-ES** generadas en el mismo script (Paso 5): `src/lib/utils/date.utils.ts`, `currency.utils.ts` (dd/MM/yyyy, moneda EUR)
  - Setup de Pinia para estado global
  - Configurar Vue Router
  - **Accesibilidad (linea base):** criterios WCAG 2.1 AA, contraste y pruebas con **axe** según `[accessibility-and-i18n.md](accessibility-and-i18n.md)`
- ✅ Base de datos inicial
  - Migración: tablas core (organizations, users, employees)
  - Seed data para desarrollo
  - Índices iniciales
- ✅ Autenticación básica
  - Login/Registro con JWT (access + refresh)
  - Login social: **Google**, **Apple**, **Instagram** (OAuth **Meta**; permisos y revisión en Meta Developers) con **mismo** par de tokens que el login local
  - **2FA opcional** (TOTP + códigos de recuperación): flujo `mfa/verify` tras login; ajustes en cuenta
  - Middleware de autenticación y validación `JwtBearer`
  - Tabla / entidad de logins externos (`AspNetUserLogins`) y reglas de vinculación por email
  - Rate limiting nativo + CAPTCHA (Turnstile) en login
  - Tests unitarios del `JwtTokenService` (`tests/ReservArte.UnitTests`)
  - **Andamiaje auth frontend** (router con guards, authStore, rutas, interceptor Axios) — **hecho**
  - **UI de login funcional (`LoginPage`, RA-869d7f7kn, 2026-08-23)** — **shipped.** Verificado en runtime: login local E2E (200 + hidratación de `authStore` + redirect). Formulario de credenciales, botones OAuth (Google / Apple / Instagram) **cableados**, enlace «¿has olvidado tu contraseña?», estados de carga y error. CAPTCHA: contador de fallos (umbral 3) y hueco de montaje verificados; **widget Turnstile real pendiente**. **Pendientes no bloqueantes de LoginPage:** (a) widget Turnstile; (b) OAuth en runtime (credenciales de proveedor por entorno); (c) migración completa de tokens en `globals.css` (restos shadcn en `.dark` y tokens genéricos → fase de theming).
  - **UI de verificación 2FA (`MfaVerifyPage`, RA-869d7f7vw, 2026-08-24)** — **shipped.** Verificado en runtime el flujo 2FA de extremo a extremo (login → `mfaRequired` → verificación TOTP/recuperación → dashboard), incluidos código de recuperación y rechazo de código incorrecto. Bloque padre **RA-869d7edpt:** **3/7** (layouts **RA-869d7f7h0** + LoginPage + MfaVerifyPage). **Pendiente del bloque (no shipped):** OAuthCallback, Register, Forgot/Reset, test axe.

> **Cierre de módulo Auth — RA-869d7ed03 (2026-08-21):** **completo (9/9 subtareas)** — Identity; `JwtTokenService`; endpoints locales; Google/Apple; Instagram/Meta; 2FA TOTP (enable/confirm/disable); 2FA verify + códigos de recuperación; rate limiting + CAPTCHA; tests del `JwtTokenService` (RA-869d7ezp3). Pendiente **no bloqueante** en backlog: **RA-869en8a17** (*Refinamientos de auth: completar políticas de rate limiting +* `AUTH_MFA_INVALID` *en verify*). El 2FA sobre login social sigue como ampliación documentada (no bloquea el cierre del módulo).

- ✅ Panel de administración — estructura de layout (**RA-869d7edpt**, 2026-08-23)
  - **DashboardLayout** (Sidebar + Header) implementado, no solo checklist
  - **Sidebar:** 8 módulos (Dashboard, Empleados, Clientes, Servicios, Citas, Pagos, Recordatorios, Configuración), colapso, resaltado de sección activa (`router-link-exact-active`; criterio en vol. 1 §4.1.2.1)
  - **Header:** nombre de usuario y logout; toggle de sidebar en móvil
  - **AuthLayout** (centrado) creado pero **aún sin usar** por ninguna página real: se aplicará a Register / Forgot-Password / Reset-Password / MFA-verify cuando se implementen. Decisión explícita: **LoginPage no usa AuthLayout**; mantiene el diseño de Figma (Banner + BottomNav)
  - Dashboard placeholder (contenido de negocio pendiente)

**Entregables Sprint 1-2:**

- ✅ Infraestructura AWS configurada y funcional
- ✅ Repositorios Git con CI/CD básico y convenciones **Git Flow** + **Conventional Commits** (§10.1.2)
- ✅ Login **backend** funcional (API Auth completa; módulo RA-869d7ed03 cerrado 9/9)
- ✅ Login **frontend** local (`LoginPage`, RA-869d7f7kn) + verificación 2FA (`MfaVerifyPage`, RA-869d7f7vw): UI shipped; login y flujo 2FA E2E verificados en runtime. Bloque RA-869d7edpt **3/7**. **Pendiente:** OAuthCallback, Register, Forgot/Reset, Turnstile real, test axe
- ✅ Panel de administración con estructura base (DashboardLayout, Sidebar, Header)
- ✅ **i18n operativo en español** (vue-i18n, estructura de claves y ficheros de traducción base) y **utilidades** `date.utils.ts` / `currency.utils.ts` según script de instalación
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
  - Subida a **Cloudinary**
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
  - Login/Registro (local y **Sign in with Apple** / **Google Sign-In** / **Instagram (Meta SDK o web OAuth)** según plataforma, mismo backend emisor de JWT)
  - **2FA** con misma semántica que web (TOTP tras login parcial si está activo)
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
- ✅ Multi-idioma (fase de contenidos e idiomas adicionales)
  - **Ya en Sprint 1:** arquitectura **vue-i18n v9**, convención de claves, **español** como único locale activo en MVP, ficheros bajo `src/locales/` (véase `Documentation/Project-Init/Scripts de instalación.md` y `[accessibility-and-i18n.md](accessibility-and-i18n.md)`)
  - **Fase 4 (esta entrega):** ficheros de traducción para **inglés, francés y portugués**, contenidos de UI y mensajes de negocio migrados o ampliados, e **implementación de detección automática de idioma** (cabecera HTTP, `Accept-Language`, preferencia de usuario o equivalente acordado)

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
  - OAuth2 para **apps de terceros** (clientes de API / integradores; distinto del **login social** de usuarios —Google, Apple, Instagram/Meta— con JWT descrito en el **volumen de análisis**)

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
├─ Sprint 1-2: Setup + Auth + Infraestructura + i18n (vue-i18n, ES) + utilidades fecha/moneda
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
└─ WhatsApp + IA + Integraciones + locales EN/FR/PT + detección automática de idioma
```

---



### 10.4 Equipo Requerido



#### Para MVP (Fase 1 - 4 meses)


| Rol                                 | Dedicación | Responsabilidades                                 |
| ----------------------------------- | ---------- | ------------------------------------------------- |
| **Backend Developer (.NET/C#)**     | 100%       | API, BD, Integración Redsys, Servicios            |
| **Frontend Developer (Vue 3/Vite)** | 100%       | UI/UX web, Integración Redsys InSite, Componentes |
| **Full-Stack Developer**            | 50%        | Apoyo backend y frontend, Code review             |
| **DevOps/Infra (AWS)**              | 25%        | Infraestructura, CI/CD, Monitoring                |
| **UI/UX Designer**                  | 25%        | Diseños, Wireframes, Prototipos                   |


**Total personas equivalentes:** ~3.5 FTE

---



#### Para Fase 2 (Apps Móviles - 3 meses)


| Rol                                 | Dedicación | Responsabilidades                   |
| ----------------------------------- | ---------- | ----------------------------------- |
| **Backend Developer**               | 75%        | APIs para móvil, Push notifications |
| **Frontend Web Developer**          | 50%        | Mantenimiento y bugs                |
| **Mobile Developer (React Native)** | 100%       | Apps iOS/Android                    |
| **Full-Stack Developer**            | 50%        | Apoyo general                       |
| **QA/Tester**                       | 50%        | Testing manual y automatizado       |
| **DevOps**                          | 25%        | Infraestructura y despliegues       |


**Total personas equivalentes:** ~3.5 FTE

---



#### Para Fase 3 (SaaS - 2 meses)


| Rol                        | Dedicación | Responsabilidades            |
| -------------------------- | ---------- | ---------------------------- |
| **Backend Developer**      | 100%       | Multi-tenancy, Suscripciones |
| **Frontend Web Developer** | 75%        | Dashboard admin, Onboarding  |
| **Mobile Developer**       | 25%        | Actualizaciones necesarias   |
| **Full-Stack Developer**   | 50%        | Apoyo y testing              |
| **DevOps**                 | 50%        | Escalabilidad, Subdominios   |


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


| Rol                             | Horas                 | Tarifa/h | Subtotal    |
| ------------------------------- | --------------------- | -------- | ----------- |
| Backend Developer (.NET)        | 640h (4 meses × 160h) | €40/h    | €25,600     |
| Frontend Developer (Vue 3/Vite) | 640h                  | €40/h    | €25,600     |
| Full-Stack Developer            | 320h (50% × 4 meses)  | €40/h    | €12,800     |
| DevOps (AWS)                    | 160h (25% × 4 meses)  | €50/h    | €8,000      |
| UI/UX Designer                  | 160h (25% × 4 meses)  | €35/h    | €5,600      |
| **SUBTOTAL FASE 1**             |                       |          | **€77,600** |


**Con margen de contingencia (+15%):** **€89,240**

---



#### Fase 2: Mejoras + App Móvil (3 meses)


| Rol                             | Horas                 | Tarifa/h | Subtotal    |
| ------------------------------- | --------------------- | -------- | ----------- |
| Backend Developer               | 360h (75% × 3 meses)  | €40/h    | €14,400     |
| Frontend Web Developer          | 240h (50% × 3 meses)  | €40/h    | €9,600      |
| Mobile Developer (React Native) | 480h (100% × 3 meses) | €40/h    | €19,200     |
| Full-Stack Developer            | 240h (50% × 3 meses)  | €40/h    | €9,600      |
| QA/Tester                       | 240h (50% × 3 meses)  | €30/h    | €7,200      |
| DevOps                          | 120h (25% × 3 meses)  | €50/h    | €6,000      |
| **SUBTOTAL FASE 2**             |                       |          | **€66,000** |


**Con margen de contingencia (+15%):** **€75,900**

---



#### Fase 3: Multi-Tenant SaaS (2 meses)


| Rol                    | Horas                 | Tarifa/h | Subtotal    |
| ---------------------- | --------------------- | -------- | ----------- |
| Backend Developer      | 320h (100% × 2 meses) | €40/h    | €12,800     |
| Frontend Web Developer | 240h (75% × 2 meses)  | €40/h    | €9,600      |
| Mobile Developer       | 80h (25% × 2 meses)   | €40/h    | €3,200      |
| Full-Stack Developer   | 160h (50% × 2 meses)  | €40/h    | €6,400      |
| DevOps                 | 160h (50% × 2 meses)  | €50/h    | €8,000      |
| **SUBTOTAL FASE 3**    |                       |          | **€40,000** |


**Con margen de contingencia (+15%):** **€46,000**

---

**TOTAL DESARROLLO (9 meses):** **€211,140**

**Notas sobre costos de desarrollo:**

- Estos son costos estimados para un equipo en España/Europa
- Pueden reducirse significativamente:
  - **Equipo remoto de Latinoamérica:** -40% a -60% (~€85k-€125k total)
  - **Freelancers vs. Empresa:** -20% a -40% (~€125k-€170k total)
  - **Equipo interno o founders** que asuman el desarrollo sin facturación externa: el coste principalmente es **tiempo propio** (coste de oportunidad), no una tarifa de mercado imputada

---



### 11.2 Costos de Infraestructura AWS (Mensual)



#### Configuración Inicial (1 organización, ~500 citas/mes)


| Servicio                            | Especificación                                                   | Costo Mensual |
| ----------------------------------- | ---------------------------------------------------------------- | ------------- |
| **Compute (ECS Fargate)**           | 0.5 vCPU, 1GB RAM × 730h                                         | ~€30          |
| **SQL Server (Docker + host/EBS)**  | Contenedor con volumen; host tipo t3.medium (2 vCPU, 4GB RAM)    | ~€55          |
|                                     | 50GB storage SSD                                                 | Incluido      |
| **Cloudinary**                      | Imágenes y CDN (plan según volumen; free tier posible al inicio) | ~€8           |
| **ALB (Load Balancer)**             | Fijo + data processing                                           | ~€22          |
| **CloudFront CDN**                  | 50GB transfer out                                                | ~€5           |
| **SES (Email)**                     | 2,000 emails/mes                                                 | €0.20         |
| **Route 53**                        | 1 hosted zone                                                    | €0.50         |
| **CloudWatch**                      | Logs + métricas                                                  | ~€5           |
| **Secrets Manager**                 | 5 secrets                                                        | €2            |
| **Backups SQL / snapshots volumen** | 50GB                                                             | ~€5           |
| **Certificate Manager**             | SSL/TLS certificates                                             | Gratis        |
| **TOTAL INICIAL**                   |                                                                  | **~€133/mes** |


---



#### Escalado (5 organizaciones, 2,500 citas/mes)


| Servicio                | Cambios                                      | Costo Mensual |
| ----------------------- | -------------------------------------------- | ------------- |
| **Compute**             | t3.medium (más potencia)                     | ~€60          |
| **SQL Server (Docker)** | Host + contenedor ampliados (2 vCPU, 8GB)    | ~€115         |
| **Cloudinary**          | Mayor volumen de imágenes / transformaciones | ~€18          |
| **ALB**                 | Mayor tráfico                                | ~€30          |
| **CloudFront**          | 200GB transfer                               | ~€15          |
| **Otros**               | Similar                                      | ~€15          |
| **TOTAL (5 ORGS)**      |                                              | **~€250/mes** |


---



#### Escalado (50 organizaciones, 25,000 citas/mes)


| Servicio                           | Cambios                                                           | Costo Mensual |
| ---------------------------------- | ----------------------------------------------------------------- | ------------- |
| **Compute**                        | Múltiples instancias + autoscaling                                | ~€300         |
| **SQL Server (Docker / dedicado)** | Clúster o instancia potente (4 vCPU, 32GB) + réplica según diseño | ~€480         |
| **Cloudinary**                     | Alto volumen multimedia                                           | ~€55          |
| **CloudFront**                     | 1TB transfer                                                      | ~€60          |
| **SES**                            | 100,000 emails                                                    | ~€10          |
| **WAF**                            | Protección DDoS                                                   | ~€25          |
| **Otros**                          | Monitoring avanzado                                               | ~€50          |
| **TOTAL (50 ORGS)**                |                                                                   | **~€980/mes** |


---



### 11.3 Costos de Servicios Externos (Mensual)



#### Cloudinary (imágenes y medios)

- **Uso:** fotografías antes/después, logos de organización, transformaciones y CDN.
- **Coste:** plan gratuito con límites; planes de pago según almacenamiento, ancho de banda y transformaciones — ver [precios Cloudinary](https://cloudinary.com/pricing).
- Las estimaciones de la tabla **11.2** son orientativas; conviene revisar la calculadora oficial según volumen real.

---



#### Redsys (Pasarela de Pago)

**Estructura de costos:**

- Redsys es procesador contratado a través del banco
- Costos varían por entidad bancaria y volumen

**Estimación típica 2025:**


| Transacciones/mes | Importe promedio | Comisión | Costo Mensual |
| ----------------- | ---------------- | -------- | ------------- |
| 500               | €25              | 1.2%     | €150          |
| 2,500             | €25              | 1.1%     | €687.50       |
| 10,000            | €25              | 1.0%     | €2,500        |


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
| -------------- | --------- | ------------- | ------------- |
| 1              | 500       | 1,000         | €10           |
| 5              | 2,500     | 5,000         | €50           |
| 50             | 25,000    | 50,000        | €500          |


---



#### Otros Servicios


| Servicio               | Plan              | Costo Mensual |
| ---------------------- | ----------------- | ------------- |
| **Dominio (.com/.es)** | Anual             | €1/mes        |
| **GitHub**             | Team (5 usuarios) | €20           |
| **Sentry**             | Error monitoring  | €26           |
| **Google Analytics**   | Free tier         | Gratis        |
| **Figma**              | Professional      | €12           |
| **TOTAL OTROS**        |                   | **~€60/mes**  |


---

**SUBTOTAL Servicios Externos (inicial):** **~€70/mes** (sin WhatsApp)  
**SUBTOTAL Servicios Externos (con WhatsApp):** **~€80/mes** (1 org)

---



### 11.4 Costos Legales y Compliance


| Concepto                              | Costo               | Frecuencia |
| ------------------------------------- | ------------------- | ---------- |
| **Asesoría RGPD inicial**             | €800 - €1,500       | Una vez    |
| **Elaboración de Políticas**          | €600 - €1,200       | Una vez    |
| (Privacidad, Cookies, T&C)            |                     |            |
| **DPO externo** (si requerido)        | €80 - €200          | Mensual    |
| **Revisión anual de compliance**      | €500                | Anual      |
| **Auditoría PCI-DSS** (SAQ A-EP)      | €2,000 - €5,000     | Anual      |
| **TOTAL INICIAL**                     | **€2,000 - €3,500** | Una vez    |
| **TOTAL ANUAL** (después del inicial) | **€2,500 - €5,000** | Anual      |


---



### 11.5 Costos de Publicación App Móvil


| Concepto                    | Costo       | Frecuencia |
| --------------------------- | ----------- | ---------- |
| **Apple Developer Program** | $99 (€95)   | Anual      |
| **Google Play Console**     | $25 (€24)   | Una vez    |
| **TOTAL AÑO 1**             | **€119**    |            |
| **TOTAL AÑOS SIGUIENTES**   | **€95/año** | Anual      |


---



### 11.6 Resumen de Costos Totales



#### Inversión Inicial (Fase 1 - MVP)


| Concepto                      | Costo         |
| ----------------------------- | ------------- |
| Desarrollo (4 meses)          | €89,240       |
| Infraestructura AWS (4 meses) | €532 (€133×4) |
| Servicios externos (4 meses)  | €280 (€70×4)  |
| Legal y compliance            | €2,500        |
| **TOTAL INVERSIÓN MVP**       | **€92,552**   |


---



#### Costos Operativos Mensuales (Después de MVP)


| Concepto            | 1 Org         | 5 Orgs   | 50 Orgs    |
| ------------------- | ------------- | -------- | ---------- |
| AWS Infraestructura | €133          | €250     | €980       |
| WhatsApp (Fase 3+)  | €10           | €50      | €500       |
| Otros servicios     | €60           | €80      | €120       |
| DPO (si aplica)     | €0-200        | €150     | €200       |
| **TOTAL MENSUAL**   | **€203-€403** | **€528** | **€1,800** |


---



#### Inversión Total (Fases 1-3)


| Concepto                                 | Costo        |
| ---------------------------------------- | ------------ |
| Desarrollo completo (9 meses)            | €211,140     |
| Infraestructura AWS (9 meses desarrollo) | €1,197       |
| Servicios externos (9 meses)             | €630         |
| Legal y compliance inicial               | €2,500       |
| Publicación apps móviles                 | €119         |
| **TOTAL PROYECTO COMPLETO**              | **€215,586** |


---



### 11.7 Modelo de Monetización SaaS (Fase 3)



#### Planes Propuestos


| Plan            | Precio/mes | Citas/mes  | Empleados  | Características                        |
| --------------- | ---------- | ---------- | ---------- | -------------------------------------- |
| **Básico**      | €49        | 200        | 3          | Email, 1 local, Web + Móvil            |
| **Profesional** | €99        | 1,000      | 10         | + WhatsApp, Reportes básicos           |
| **Premium**     | €199       | Ilimitadas | Ilimitados | + IA, Multi-local, Soporte prioritario |
| **Enterprise**  | €399+      | Ilimitadas | Ilimitados | + Personalización, Onboarding dedicado |


**Notas:**

- Período de prueba: 14 días gratis (todos los planes)
- Descuento anual: 20% (2 meses gratis)
- Costos de transacción Redsys: pagados por cada organización
- Setup fee: €0 (incluido en todos los planes)

---



#### Análisis Break-Even

**Costos fijos mensuales (50 clientes):**

- Infraestructura AWS: €980
- Servicios externos: €120
- DPO: €200
- Soporte/Mantenimiento: €500 (estimado)
- **TOTAL FIJOS:** €1,800/mes

**Ingresos mensuales objetivo:**


| Escenario       | Distribución                                           | MRR        |
| --------------- | ------------------------------------------------------ | ---------- |
| **Conservador** | 20 Básico + 5 Profesional + 2 Premium                  | €1,675/mes |
| **Moderado**    | 25 Básico + 15 Profesional + 8 Premium + 2 Enterprise  | €4,418/mes |
| **Optimista**   | 15 Básico + 25 Profesional + 15 Premium + 5 Enterprise | €7,215/mes |


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

La **estrategia de pruebas automatizadas** (unitarios, integración, E2E, simulación Redsys y qué ejecutar en cada pipeline) está descrita en `[reservarte-testing-strategy.md](reservarte-testing-strategy.md)`. El checklist **§12.2** (incluida la subsección **Testing**) debe implementarse de forma coherente con ese documento y con el volumen 2 **§9.5**. La **accesibilidad (WCAG 2.1 AA)** y la **internacionalización (vue-i18n)** están recogidas en `[accessibility-and-i18n.md](accessibility-and-i18n.md)` y en el script `Documentation/Project-Init/Scripts de instalación.md`.

### 12.1 Pasos Inmediatos (Semana 1-2)



#### 1. Validación y Aprobación del Cliente

**Acciones:**

- [ ] Entregar al cliente el **conjunto de documentación técnica** (los tres volúmenes) para revisión y aprobación
- [ ] Revisar todas las funcionalidades propuestas
- [ ] Confirmar prioridades y alcance del MVP
- [ ] Discutir presupuesto y timeline
- [ ] Definir criterios de éxito
- [ ] Firmar contrato o acuerdo de desarrollo

**Entregables:**

- Documentación técnica revisada y **aprobada por el cliente** (acta de conformidad o firma en el contrato / SOW)
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
- [ ] Crear workspace **ReservArte** en ClickUp replicando la estructura del §10.1.1 (Spaces **Backend (.NET)**, **Frontend (Vue 3)**, **Mobile (React Native)**, **Infrastructure**, **Documentation** y todas sus listas)
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
- [ ] Aprovisionar SQL Server en Docker (entorno dev, `docker-compose`)
- [ ] Configurar **Cloudinary** (clouds o carpetas por entorno; API keys en Secrets Manager)
- [ ] Verificar dominio en Amazon SES

**Repositorios:**

- [ ] Crear organización en GitHub
- [ ] Crear repositorio backend (reservarte-api)
- [ ] Crear repositorio frontend web (reservarte-web)
- [ ] Crear repositorio móvil (reservarte-mobile)
- [ ] Aplicar **Git Flow** (`main`, `develop`, `feature/`*, `release/*`, `hotfix/*`) — §10.1.2
- [ ] Exigir **Conventional Commits** en mensajes (hooks opcionales: commitlint)
- [ ] Añadir `.github/PULL_REQUEST_TEMPLATE.md` en cada repositorio (o monorepo)
- [ ] Configurar branch protection rules (`main`, `develop`: PR obligatorio, revisores, CI)
- [ ] Configurar GitHub Actions para CI

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

Detalle de herramientas, umbrales de cobertura y jobs de CI: `[reservarte-testing-strategy.md](reservarte-testing-strategy.md)`.

#### Backend (.NET Core)

- [x] Crear solución con Clean Architecture
- [x] Instalar paquetes NuGet necesarios
- [x] Configurar Entity Framework Core
- [x] Crear primera migración (tablas core)
- [x] Configurar ASP.NET Core Identity
- [x] Implementar JWT authentication (Bearer como autorización API)
- [x] Registrar proveedores: **Google** (OAuth), **Apple** (Sign in with Apple), **Meta/Instagram** (OAuth; app y permisos en Meta Developers)
- [x] Implementar **2FA opcional** (TOTP Identity, códigos de recuperación, endpoints `mfa` / `account/mfa`)
- [x] Persistir logins externos (`AspNetUserLogins`) y política de cuentas duplicadas por email
- [x] Rate limiting nativo (login / mfa-verify) + CAPTCHA (`ICaptchaService`)
- [x] Proyecto `tests/ReservArte.UnitTests` + batería `JwtTokenServiceTests` (RA-869d7ezp3)
- [x] **Serilog — pipeline + sink consola:** patrón en dos fases (bootstrap logger + configuración definitiva desde `appsettings`), sink de consola y enriquecimiento por petición (`RequestId`, `OrganizationId` vía middleware) — hecho (Setup Backend)
- [ ] **Serilog — sink CloudWatch:** envío de logs a AWS — **pendiente** (tareas de infraestructura; mismo criterio que SES, key ring de Data Protection en prod, etc.)
- [x] Configurar Swagger/OpenAPI con esquema reutilizable del **envelope** `{ success, data, error, meta }` y códigos `error.code` (volumen 1 §5.1.1–5.1.2)
- [x] Definir `appsettings.json` **como contrato** (volumen 1 §5.1.3): todas las secciones y claves con valores vacíos o placeholders; **sin secretos** en el repositorio
- [x] Completar `appsettings.Development.json` y `appsettings.Production.json` en el repo solo con valores **no sensibles** (localhost, CORS, flags, `MultiTenant:ResolutionStrategy = Header` en dev, URLs públicas en prod)
- [x] **CORS conectado al pipeline HTTP:** `Cors:AllowedOrigins` no basta por sí solo. Registrar `AddCorsPolicy` (`ReservArte-API/Extensions/CorsServiceExtensions.cs`) y `app.UseCors(...)` — hallazgo 2026-08-23: la clave existía y se usaba para validar `returnUrl` OAuth, pero **no había middleware CORS**; el navegador bloqueaba en silencio toda petición del SPA a la API. Lección: no dar por hecho CORS en un módulo nuevo sin comprobarlo contra un frontend real (vol. 1 §5.1.3, vol. 2 §9.3.4)
- [x] Redactar `Documentation/Project-Init/user-secrets-guide.md`: comandos `dotnet user-secrets set` por secreto, tarjetas de prueba Redsys, **ngrok** para webhook local, FAQ
- [ ] Producción: **variables de entorno** y **AWS Secrets Manager** según la jerarquía del volumen 1 §5.1.3
- [x] Escribir primer endpoint de health check (`GET /health` + smoke test de BD vía `AddDbContextCheck`)
- [x] Configurar cadena de conexión a SQL Server (contenedor Docker `reservarte-sql`, base `ReservArteDB`)

> **Módulo Auth (RA-869d7ed03):** cerrado **9/9** (2026-08-21). Backlog no bloqueante: **RA-869en8a17** (refinamientos rate limiting + `AUTH_MFA_INVALID`).



#### Frontend Web (Vue 3 + Vite)

- [x] Crear proyecto con Vite + Vue 3 + TypeScript (proxy Vite `/api` → backend; URL de la API vía `VITE_API_BASE_URL`; puerto de la API en `launchSettings.json` — sin puerto literal de máquina; build de producción verificado) — andamiaje Setup Frontend, 2026-08-21
- [x] Configurar Tailwind CSS (**3.4.17**, PostCSS/Vite) — andamiaje Setup Frontend
- [x] Instalar librería de componentes compatible con Vue (**Reka UI** / paquete `reka-ui`; primitivos headless sobre los que se asienta shadcn-vue) — andamiaje Setup Frontend
- [x] Configurar Pinia para estado global (`authStore`, `uiStore`; registrado en `main.ts`)
- [x] Configurar Vue Router (7 rutas; guards `requiresAuth` / `requiresMfa`)
- [x] Crear estructura de carpetas (`src/`: stores, router, i18n, locales, lib, styles; backend: Clean Architecture de 5 proyectos + `tests/`) — Setup Frontend/Backend
- [x] Implementar axios client con interceptors (`client.ts`: Bearer + manejo 401)
- [x] Crear layout principal (`DashboardLayout` + Sidebar 8 módulos + Header con usuario/logout + toggle móvil; `AuthLayout` creado, aún sin páginas consumidoras) — **RA-869d7f7h0** / bloque RA-869d7edpt, 2026-08-23
- [x] Implementar página de login (`LoginPage` + `LoginForm`: credenciales, botones OAuth cableados, hueco CAPTCHA tras 3 fallos) — **RA-869d7f7kn shipped** (2026-08-23); login local verificado en runtime
- [ ] Vista de retorno OAuth (`OAuthCallback`) — cableado correcto; **pendiente de credenciales de proveedor por entorno** (no shipped)
- [x] Vista **Verificación 2FA** (`MfaVerifyPage`, RA-869d7f7vw) — **shipped** (2026-08-24); flujo E2E verificado (TOTP, recuperación, rechazo de código incorrecto). Ajustes **Seguridad de cuenta** (activar/desactivar TOTP) siguen pendientes
- [ ] Register, Forgot-Password, Reset-Password (usarán `AuthLayout`)
- [ ] Widget **Turnstile** real en login (camino B: contador + hueco hechos; site key `VITE_TURNSTILE_SITE_KEY` + script pendientes)
- [ ] Configurar variables de entorno
- [x] Instalar y configurar **vue-i18n v9** (registrado en `main.ts`; locale **`es`** cargado desde `src/locales/es/`) — andamiaje Setup Frontend; uso en pantallas de auth funcionales pendiente
- [ ] Definir convención de claves y documentación operativa en `[Documentation/accessibility-and-i18n.md](accessibility-and-i18n.md)` (Bloque B)
- [ ] Instalar **axe-core** y **vitest-axe** como `devDependencies`; añadir al menos un test de accesibilidad de humo en componente crítico; revisión manual con **axe DevTools** antes de merge de UI sensible
- [ ] Objetivo de contraste y patrones ARIA según `[Documentation/accessibility-and-i18n.md](accessibility-and-i18n.md)` (Bloque A); verificar pares de color con WebAIM / axe antes del primer deploy a staging

> **Andamiaje vs UI (2026-08-21, 3ª pasada post RA-869d7ezp3):** Vite + Tailwind 3.4.17 + Reka UI (`reka-ui`) + Pinia + Vue Router + axios + vue-i18n (`es`) + estructura de carpetas = **hecho**. En esa fecha la UI de auth (login/OAuth/2FA/CAPTCHA) estaba **pendiente**.
>
> **Superado el 2026-08-23 — LoginPage:** la **UI de login (`LoginPage`) está implementada y verificada** (login local end-to-end + CAPTCHA tras 3 fallos).
>
> **Superado el 2026-08-24 — MfaVerifyPage (RA-869d7f7vw):** verificación 2FA en SPA **shipped** y verificada E2E (login → `mfaRequired` → TOTP/recuperación → dashboard; rechazo de código incorrecto). Cierre parcial del bloque RA-869d7edpt: **3/7** (layouts + LoginPage + MfaVerifyPage). **Pendiente del bloque:** OAuthCallback, Register, Forgot/Reset, test axe. Widget Turnstile real pendiente (hueco y umbral 3 hechos). Patrón de páginas de auth + flujo MFA SPA: vol. 2 **§9.2.3**. CAPTCHA camino B: vol. 1 **§4.4.3**. CORS: vol. 2 **§9.3.4**.



#### Testing (unitarios, integración y E2E)

- [x] **Backend unitario:** proyecto `tests/ReservArte.UnitTests` con xUnit + Moq + FluentAssertions; suite inicial `JwtTokenServiceTests` (17) — RA-869d7ezp3 / `[reservarte-testing-strategy.md](reservarte-testing-strategy.md)` §3.1
- [ ] **Backend integración:** `tests/ReservArte.IntegrationTests` + Testcontainers (SQL Server) + `WebApplicationFactory`; migraciones EF Core; semilla multi-tenant
- [ ] **Frontend:** instalar y configurar **Vitest** + **Vue Test Utils**; scripts `test` / `test:watch` en `package.json`; carpetas `tests/unit` o convención alineada con el monorepo
- [ ] **E2E:** instalar y configurar **Playwright** (TypeScript), proyecto bajo `tests/ReservArte.E2ETests` con subcarpeta `Scenarios/` como en `Análisis de pantallas y estructura.md`
- [ ] **CI:** jobs acordados con la estrategia (unitarios + integración en PR; E2E en merge a `develop`; humo Redsys pre-deploy) — detalle en `reservarte-testing-strategy.md` §9



#### DevOps

- [ ] Dockerfile para backend
- [ ] Dockerfile para frontend
- [ ] docker-compose.yml para desarrollo local
- [ ] GitHub Actions workflow para backend (disparo en PR a `develop` / `main` según §10.1.2)
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


| Riesgo                               | Probabilidad | Impacto | Mitigación                                                                  |
| ------------------------------------ | ------------ | ------- | --------------------------------------------------------------------------- |
| **Retrasos en desarrollo**           | Alta         | Alto    | Planificación realista con buffer del 15%, revisiones semanales             |
| **Problemas con aprobación Redsys**  | Media        | Alto    | Iniciar proceso bancario cuanto antes, tener entorno de test funcionando    |
| **Costos AWS más altos**             | Media        | Medio   | Monitorización constante, alertas de billing, optimización continua         |
| **Cambios en regulación RGPD**       | Baja         | Alto    | Asesor legal continuo, revisión trimestral de compliance                    |
| **Baja adopción SaaS**               | Media        | Alto    | Marketing pre-lanzamiento, precio competitivo, periodo prueba, UX excelente |
| **Problemas de escalabilidad**       | Baja         | Alto    | Arquitectura escalable desde inicio, load testing antes de producción       |
| **Brecha de seguridad**              | Baja         | Crítico | Auditorías de seguridad, penetration testing, seguro cibernético            |
| **Dependencia de terceros (Redsys)** | Media        | Medio   | Documentación exhaustiva, fallbacks, monitoreo 24/7                         |
| **Rotación del equipo**              | Media        | Alto    | Documentación detallada, code reviews, knowledge sharing                    |
| **Competencia en mercado**           | Alta         | Medio   | Diferenciación por UX, soporte local, precio competitivo                    |


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
- **Conventional Commits:** convención de mensajes de commit (`feat:`, `fix:`, `docs:`, …) alineada con [conventionalcommits.org](https://www.conventionalcommits.org/)
- **COF:** Credential On File (Redsys)
- **CRUD:** Create, Read, Update, Delete
- **DPA:** Data Processing Agreement
- **DPO:** Data Protection Officer (Delegado de Protección de Datos)
- **EIPD:** Evaluación de Impacto en Protección de Datos

**F-M:**

- **FUC:** Número de comercio en Redsys
- **Git Flow:** modelo de ramas Git (`main`, `develop`, `feature/`*, `release/*`, `hotfix/*`) para integración y releases
- **HMAC:** Hash-based Message Authentication Code
- **HMR:** Hot Module Replacement (Vite)
- **JWT:** JSON Web Token (access token emitido por la API de ReservArte; la autorización en controladores se basa en este Bearer token)
- **MFA / 2FA:** Autenticación multifactor / doble factor; en el producto es **opcional** por usuario (TOTP)
- **OIDC:** OpenID Connect (p. ej. Google y Apple; Meta/Instagram usa principalmente OAuth 2.0). Distinto del OAuth2 «para apps de terceros» del marketplace
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
- **TOTP:** Time-based One-Time Password (autenticadores tipo Google Authenticator; base de la 2FA opcional)
- **TPV:** Terminal Punto de Venta
- **VPC:** Virtual Private Cloud

---



### Anexo B: Referencias y Recursos



#### Documentación Técnica

**Frameworks y Librerías:**

- ASP.NET Core: [https://docs.microsoft.com/aspnet/core](https://docs.microsoft.com/aspnet/core)
- Vue 3: [https://vuejs.org](https://vuejs.org)
- Vite: [https://vitejs.dev](https://vitejs.dev)
- React Native: [https://reactnative.dev](https://reactnative.dev)
- Entity Framework Core: [https://docs.microsoft.com/ef/core](https://docs.microsoft.com/ef/core)
- Pinia: [https://pinia.vuejs.org](https://pinia.vuejs.org)
- Vue Router: [https://router.vuejs.org](https://router.vuejs.org)
- Tailwind CSS: [https://tailwindcss.com/docs](https://tailwindcss.com/docs)
- Reka UI (ejemplo headless Vue): [https://reka-ui.com](https://reka-ui.com)

**Git y flujo de entrega:**

- Conventional Commits: [https://www.conventionalcommits.org](https://www.conventionalcommits.org)
- Git Flow (modelo de ramas): [https://nvie.com/posts/a-successful-git-branching-model/](https://nvie.com/posts/a-successful-git-branching-model/)
- GitHub — Pull Request templates: [https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/creating-a-pull-request-template-for-your-repository](https://docs.github.com/en/communities/using-templates-to-encourage-useful-issues-and-pull-requests/creating-a-pull-request-template-for-your-repository)

**Infraestructura:**

- AWS Documentation: [https://docs.aws.amazon.com](https://docs.aws.amazon.com)
- SQL Server en Linux (contenedor): [https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-configure](https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-configure)
- Amazon SES: [https://docs.aws.amazon.com/ses/](https://docs.aws.amazon.com/ses/)
- Cloudinary (imágenes / DAM): [https://cloudinary.com/documentation](https://cloudinary.com/documentation)

**Redsys:**

- Portal desarrolladores Redsys: [https://pagosonline.redsys.es](https://pagosonline.redsys.es)
- Documentación técnica: [https://pagosonline.redsys.es/desarrolladores.html](https://pagosonline.redsys.es/desarrolladores.html)
- Manual de integración InSite: Solicitar a banco adquirente
- Códigos de respuesta: Consultar documentación oficial

---



#### RGPD y Legal

**Recursos oficiales:**

- AEPD (Agencia Española de Protección de Datos): [https://www.aepd.es](https://www.aepd.es)
- RGPD Texto completo: [https://gdpr.eu](https://gdpr.eu)
- Guía de Cookies AEPD: [https://www.aepd.es/guias/guia-cookies.pdf](https://www.aepd.es/guias/guia-cookies.pdf)
- Guía de Análisis de Riesgos: [https://www.aepd.es/sites/default/files/2019-09/guia-analisis-de-riesgos.pdf](https://www.aepd.es/sites/default/files/2019-09/guia-analisis-de-riesgos.pdf)

**Plantillas útiles:**

- Política de Privacidad template: Solicitar a asesor legal
- Registro de Actividades de Tratamiento: Template AEPD
- Modelo de consentimiento RGPD: Template AEPD

---



#### WhatsApp Business

**Documentación oficial:**

- WhatsApp Business API: [https://business.whatsapp.com](https://business.whatsapp.com)
- Meta for Developers: [https://developers.facebook.com/docs/whatsapp](https://developers.facebook.com/docs/whatsapp)
- Precios WhatsApp: [https://business.whatsapp.com/products/platform-pricing](https://business.whatsapp.com/products/platform-pricing)
- 360dialog Docs: [https://docs.360dialog.com](https://docs.360dialog.com)

**Categorías de mensajes:**

- Utility: Recordatorios, confirmaciones
- Marketing: Promociones, ofertas
- Authentication: Códigos OTP
- Service: Respuestas a consultas

---



#### Herramientas y Servicios

**Desarrollo:**

- GitHub: [https://github.com](https://github.com)
- Vite documentation: [https://vitejs.dev/guide/](https://vitejs.dev/guide/)
- Figma: [https://www.figma.com](https://www.figma.com)
- Postman: [https://www.postman.com](https://www.postman.com)

**Monitoreo:**

- Sentry: [https://sentry.io](https://sentry.io)
- AWS CloudWatch: [https://aws.amazon.com/cloudwatch/](https://aws.amazon.com/cloudwatch/)
- Google Analytics: [https://analytics.google.com](https://analytics.google.com)

**Testing:**

- TestFlight (iOS): [https://testflight.apple.com](https://testflight.apple.com)
- Google Play Console (Android): [https://play.google.com/console](https://play.google.com/console)

**Calculadoras:**

- AWS Pricing Calculator: [https://calculator.aws](https://calculator.aws)
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

- 360dialog: [https://www.360dialog.com](https://www.360dialog.com)
- Twilio: [https://www.twilio.com/whatsapp](https://www.twilio.com/whatsapp)
- Vonage (ex-Nexmo): [https://www.vonage.com](https://www.vonage.com)

**Asesoría Legal RGPD:**

- Buscar despacho local especializado en RGPD y tech
- Verificar experiencia con startups SaaS
- Solicitar referencias

**Hosting Alternativo:**

- DigitalOcean: [https://www.digitalocean.com](https://www.digitalocean.com)
- Vultr: [https://www.vultr.com](https://www.vultr.com)
- Hetzner Cloud: [https://www.hetzner.com/cloud](https://www.hetzner.com/cloud)

---



#### Comunidades y Soporte

**Desarrollo:**

- Stack Overflow: Para dudas técnicas
- Reddit r/dotnet: Comunidad .NET
- Reddit r/vuejs: Comunidad Vue.js
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

Esta documentación describe un plan completo, detallado y viable para el desarrollo de **ReservArte**, una aplicación multi-tenant de gestión para centros de diseño de cejas en España.

### Puntos Clave del Proyecto

**✅ Tecnología Moderna y Robusta:**

- Backend: ASP.NET Core 8.0
- Autenticación y autorización API: ASP.NET Core Identity (local + **Google, Apple, Instagram/Meta**) y JWT (Bearer); **2FA opcional** (TOTP)
- Frontend Web: Vue 3 + Vite (HMR ultra-rápido)
- Frontend Móvil: React Native
- Base de Datos: Microsoft SQL Server en contenedor Docker
- Infraestructura: AWS con alta disponibilidad

**✅ Gestión de proyecto (ClickUp):**

- Workspace **ReservArte** con Spaces **Backend (.NET)**, **Frontend (Vue 3)**, **Mobile (React Native)**, **Infrastructure** y **Documentation**; listas según §10.1.1 (Sprint Activo, Backlog, Bugs, tareas de infra, **Technical Specs**, **Architecture Decisions**)

**✅ Git, revisiones y CI/CD:**

- **Git Flow** en GitHub, mensajes **Conventional Commits**, plantilla de PR en `.github/PULL_REQUEST_TEMPLATE.md`, branch protection y **GitHub Actions** (§10.1.2)

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

**Documento elaborado por el equipo de producto e ingeniería de ReservArte**  
**Fecha:** Octubre 2025  
**Versión:** 1.0  
**Confidencialidad:** Este documento puede contener información confidencial. Su reproducción o distribución requiere autorización por escrito de las partes.

---



## FIRMAS DE CONFORMIDAD



### Por parte del Cliente

**Nombre:** Sofía Fatás Ounka___________  
**Cargo:** CEO y propietaria___________  
**Empresa:** More Than Brows__________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

---



### Por parte del proveedor / equipo de desarrollo

**Nombre:** Gabriel Sánchez-Vallejo Millán  
**Cargo:** Desarrollador de software________________  
**Organización:** ________________________________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

## **Nombre:** Guillermo Algárate del Arco  
**Cargo:** Desarrollador de software________________  
**Organización:** ________________________________  
**Fecha:** 08/10/2025__________________  
**Firma:** ____________________________

**Fin de la documentación técnica (volumen 3 de 3)**

**Conjunto documental:**

1. Análisis y especificaciones técnicas
2. Implementación y desarrollo
3. Planificación y gestión

---

