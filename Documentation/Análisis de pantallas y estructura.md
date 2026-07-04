# ANÁLISIS DE PANTALLAS Y ESTRUCTURA DEL PROYECTO RESERVARTE

**Documento:** Análisis de Pantallas Web y Estructura de Archivos  
**Versión:** 1.0  
**Fecha:** Octubre 2025  
**Proyecto:** ReservArte - Sistema Multi-Tenant de Gestión para Centros de Diseño de Cejas

---

## ÍNDICE

1. [Pantallas Requeridas para la Web](#pantallas-requeridas-para-la-web)
2. [Estructura de Archivos Completa](#estructura-de-archivos-completa)
3. [Resumen de Pantallas por Prioridad](#resumen-de-pantallas-por-prioridad)
4. [Notas Adicionales](#notas-adicionales)

---

## PANTALLAS REQUERIDAS PARA LA WEB

### 1. MÓDULO DE AUTENTICACIÓN

#### 1.1 Pantallas Públicas

**Login** (`/login`)
- Formulario de email/password
- Botones **Google**, **Apple** y **Instagram** (este último vía **OAuth de Meta** / Instagram Login según app configurada en Meta Developers)
- Opción "Recordar sesión"
- Link "Olvidé mi contraseña"
- CAPTCHA (después de 3 intentos fallidos)
- Si el usuario tiene **2FA activada**, tras contraseña o tras login social correcto se redirige al flujo de verificación (no se emite JWT hasta introducir TOTP o código de recuperación)

**Verificación 2FA** (`/login/two-factor` o ruta equivalente)
- Campo para código del autenticador (TOTP) o código de recuperación de un solo uso
- Enlace de ayuda («He perdido el dispositivo») acorde a la política de soporte

**Retorno OAuth** (`/auth/callback` o ruta equivalente)
- Pantalla o componente mínimo que procesa el retorno desde el backend (p. ej. lectura de tokens de un solo uso, fragmento o cuerpo acordado) y actualiza el store de sesión antes de redirigir al dashboard; si aplica **2FA**, continuar hacia la pantalla de verificación antes del dashboard

**Registro** (`/register`)
- Formulario de registro de usuario
- Opción de registro con **Google**, **Apple** o **Instagram (Meta)** (misma emisión de JWT que el login una vez completado el flujo)
- Términos y condiciones
- Política de privacidad

**Recuperar Contraseña** (`/forgot-password`)
- Solicitud de reset por email
- Página de confirmación

**Restablecer Contraseña** (`/reset-password/:token`)
- Formulario de nueva contraseña

---

### 2. MÓDULO DE DASHBOARD (Post-Login)

#### 2.1 Dashboard Principal

**Dashboard Home** (`/dashboard`)
- Resumen de métricas del día
- Citas de hoy
- Ingresos del mes
- Gráficos de tendencias
- Acciones rápidas

---

### 3. MÓDULO DE EMPLEADOS

#### 3.1 Gestión de Empleados

**Lista de Empleados** (`/employees`)
- Tabla con búsqueda y filtros
- Botón "Nuevo Empleado"
- Acciones: Editar, Desactivar, Ver detalles

**Crear Empleado** (`/employees/new`)
- Formulario completo
- Datos personales
- Asignación de rol
- Servicios que puede realizar
- Horarios

**Editar Empleado** (`/employees/:id/edit`)
- Formulario prellenado
- Mismos campos que creación

**Detalle de Empleado** (`/employees/:id`)
- Información completa
- Historial de servicios
- Calendario de disponibilidad
- Estadísticas

**Gestión de Horarios** (`/employees/:id/schedule`)
- Calendario visual
- Horarios recurrentes semanales
- Excepciones (vacaciones, bajas)
- Bloques no disponibles

---

### 4. MÓDULO DE CLIENTES

#### 4.1 Gestión de Clientes

**Lista de Clientes** (`/customers`)
- Tabla con búsqueda avanzada
- Filtros por categoría, estado
- Botón "Nuevo Cliente"
- Acciones: Editar, Ver perfil, Historial

**Crear Cliente** (`/customers/new`)
- Formulario de datos personales
- Preferencias de contacto
- Consentimientos RGPD
- Alergias/notas médicas

**Editar Cliente** (`/customers/:id/edit`)
- Formulario prellenado

**Perfil de Cliente** (`/customers/:id`)
- Información completa
- Historial de citas
- Historial de pagos
- Tarjetas guardadas
- Fotografías antes/después
- Notas internas
- Programa de fidelización (puntos)

**Gestión de Tarjetas** (`/customers/:id/payment-methods`)
- Lista de tarjetas guardadas (enmascaradas)
- Marcar como predeterminada
- Eliminar tarjeta
- Añadir nueva tarjeta (modal con Redsys InSite)

**Historial de Citas** (`/customers/:id/appointments`)
- Timeline de citas pasadas y futuras
- Filtros por fecha, estado, servicio

---

### 5. MÓDULO DE SERVICIOS

#### 5.1 Catálogo de Servicios

**Lista de Servicios** (`/services`)
- Cards/Grid de servicios
- Filtros por categoría
- Botón "Nuevo Servicio"
- Acciones: Editar, Desactivar

**Crear Servicio** (`/services/new`)
- Nombre y descripción
- Duración y precio base
- Categoría
- Imagen
- Variaciones (opcional)
- Requisitos previos

**Editar Servicio** (`/services/:id/edit`)
- Formulario prellenado

**Paquetes de Servicios** (`/service-packages`)
- Crear combos de servicios
- Precio con descuento
- Orden de servicios

---

### 6. MÓDULO DE AGENDA Y CITAS (CORE)

#### 6.1 Sistema de Agenda

**Vista de Calendario** (`/calendar`)
- Vista diaria/semanal/mensual (tabs)
- Vista por empleado o todos
- Drag & drop de citas
- Código de colores
- Click para ver detalles
- Botón "Nueva Cita"

**Crear Cita (Interno)** (`/appointments/new`)
- **Wizard paso a paso:**
  1. Seleccionar cliente (búsqueda o nuevo)
  2. Seleccionar servicio(s)
  3. Seleccionar empleado (o automático)
  4. Seleccionar fecha y hora (calendario)
  5. Método de pago (si aplica pre-autorización)
  6. Confirmación
- Validaciones en tiempo real
- Sugerencias de disponibilidad

**Detalle de Cita** (`/appointments/:id`)
- Información completa
- Cliente, empleado, servicio
- Estado de la cita
- Historial de cambios
- Notas internas
- Acciones: Confirmar, Cancelar, Reagendar, Marcar completada

**Editar/Reagendar Cita** (`/appointments/:id/reschedule`)
- Formulario con nueva fecha/hora
- Validaciones

**Cancelar Cita** (Modal)
- Motivo de cancelación
- Cálculo automático de penalización
- Confirmación de cargo

**Lista de Espera** (`/waiting-list`)
- Clientes en espera
- Criterios de búsqueda
- Prioridad
- Notificar cuando hay disponibilidad

---

### 7. MÓDULO DE PAGOS

#### 7.1 Gestión de Pagos

**Lista de Pagos** (`/payments`)
- Historial de transacciones
- Filtros por fecha, estado, método
- Detalles de Redsys
- Exportar

**Detalle de Pago** (`/payments/:id`)
- Información completa de transacción
- Logs de Redsys
- Estado actual
- Acciones: Reembolsar (si aplica)

**Formulario de Pago (Redsys InSite)** (Modal/Componente)
- Iframe de número de tarjeta (Redsys)
- Iframe de fecha de expiración (Redsys)
- Iframe de CVV (Redsys)
- Checkbox "Guardar tarjeta"
- Indicador de seguridad

---

### 8. MÓDULO DE RECORDATORIOS

#### 8.1 Configuración de Recordatorios

**Configuración de Recordatorios** (`/settings/reminders`)
- Cantidad de recordatorios
- Tiempos antes de la cita
- Canales (Email, WhatsApp, Ambos)
- Plantillas personalizables
- Horarios de envío
- Vista previa

**Historial de Recordatorios** (`/reminders/logs`)
- Recordatorios enviados
- Estado (enviado, entregado, abierto)
- Filtros por fecha, canal

---

### 9. MÓDULO DE CONFIGURACIÓN

#### 9.1 Configuración de Organización

**Datos de la Organización** (`/settings/organization`)
- Información fiscal y comercial
- Logo, colores, branding
- Datos de contacto
- Dirección

**Configuración General** (`/settings/general`)
- Horarios de operación
- Zona horaria
- Moneda
- Idioma

**Políticas de Cancelación** (`/settings/cancellation-policy`)
- Tiempo mínimo de anticipación
- Porcentaje de penalización
- Máximo de no-shows antes de bloqueo
- Excepciones para VIPs

**Configuración de Reservas** (`/settings/booking`)
- Modo público/privado
- Requiere aprobación
- Lista blanca
- Restricciones por categoría

**Configuración de Redsys** (`/settings/redsys`)
- Código de comercio (FUC)
- Terminal
- Entorno (test/producción)
- Guardar clave secreta (****)

**Usuarios del Sistema** (`/settings/users`)
- Lista de usuarios con acceso
- Roles y permisos
- Crear/editar usuarios
- Desactivar accesos

#### 9.2 Seguridad de la cuenta (2FA opcional)

**Seguridad de la cuenta** (`/settings/account-security` o `/account/security`)
- Activar / desactivar **doble factor (TOTP)**; no es obligatorio para ningún usuario
- Mostrar QR / secreto para registro en autenticador
- Confirmación con primer código TOTP
- Códigos de recuperación (generar, mostrar una vez, regenerar)
- Puede exigir reintroducir contraseña o sesión reciente para desactivar 2FA

---

### 10. MÓDULO DE REPORTES Y ANALÍTICAS (FASE 2/FUTURO)

#### 10.1 Dashboards y Reportes

**Dashboard Ejecutivo** (`/reports/executive`)
- KPIs principales
- Gráficos de tendencias

**Reportes Financieros** (`/reports/financial`)
- Ingresos por período
- Ingresos por servicio
- Ingresos por empleado
- Exportar a Excel/PDF

**Reportes Operativos** (`/reports/operational`)
- Citas por estado
- Tasa de no-shows
- Tiempo promedio por servicio
- Ocupación de empleados

**Reportes de Marketing** (`/reports/marketing`)
- Tasa de conversión
- Clientes nuevos vs. recurrentes
- Programas de fidelización
- Cupones más usados

---

### 11. MÓDULO DE FOTOGRAFÍAS (FASE 2)

#### 11.1 Gestión de Fotografías

**Subir Fotografías** (Modal en cita)
- Antes/Después
- Asociar a cita
- Consentimiento del cliente

**Galería de Cliente** (`/customers/:id/photos`)
- Grid de fotos
- Comparación lado a lado
- Marca de agua
- Opción de compartir (con permiso)

---

### 12. MÓDULO MULTI-TENANT (FASE 3 - SaaS)

#### 12.1 Landing y Registro Público

**Landing Page Pública** (`/`)
- Descripción del producto
- Planes y precios
- Testimonios
- CTA "Comenzar prueba gratis"

**Registro de Organización** (`/signup`)
- Wizard de onboarding
- Paso 1: Datos de negocio
- Paso 2: Configuración inicial
- Paso 3: Método de pago (suscripción)
- Paso 4: ¡Listo!

**Dashboard de Super Admin** (`/admin`)
- Gestión de todas las organizaciones
- Métricas globales (MRR, churn, etc.)
- Planes y facturación
- Soporte y logs

---

### 13. MÓDULO DE RESERVA PÚBLICA (Cliente Final)

#### 13.1 Flujo de Reserva Pública

**Landing de Reserva** (`/book`)
- Logo y nombre del centro
- Catálogo de servicios
- Botón "Reservar ahora"

**Wizard de Reserva (Cliente)** (`/book/new`)
- Paso 1: Seleccionar servicio(s)
- Paso 2: Seleccionar empleado (opcional)
- Paso 3: Seleccionar fecha y hora
- Paso 4: Datos del cliente (login/registro)
- Paso 5: Pago con Redsys InSite
- Paso 6: Confirmación + Email

**Perfil del Cliente** (`/my-profile`)
- Mis datos personales
- Mis citas (pasadas y futuras)
- Mis tarjetas guardadas
- Mis fotografías
- Mis puntos de fidelización

**Mis Citas** (`/my-appointments`)
- Ver próximas citas
- Ver historial
- Cancelar cita
- Reagendar cita

---

## ESTRUCTURA DE ARCHIVOS COMPLETA

### Frontend Web (Vue 3 + Vite)

```
reservarte-web/
├── public/
│   ├── logo.svg
│   ├── favicon.ico
│   └── robots.txt
│
├── src/
│   ├── app/                           # Configuración de la app
│   │   ├── App.vue
│   │   ├── main.ts
│   │   └── router/index.ts
│   │
│   ├── assets/                        # Assets estáticos
│   │   ├── images/
│   │   ├── icons/
│   │   └── fonts/
│   │
│   ├── components/                    # Componentes reutilizables
│   │   ├── ui/                        # Componentes básicos (kit UI compatible con Vue, p. ej. Reka UI)
│   │   │   ├── button.vue
│   │   │   ├── input.vue
│   │   │   ├── dialog.vue
│   │   │   ├── dropdown-menu.vue
│   │   │   ├── calendar.vue
│   │   │   ├── card.vue
│   │   │   ├── badge.vue
│   │   │   ├── avatar.vue
│   │   │   ├── table.vue
│   │   │   ├── tabs.vue
│   │   │   ├── toast.vue
│   │   │   ├── select.vue
│   │   │   ├── checkbox.vue
│   │   │   ├── switch.vue
│   │   │   ├── separator.vue
│   │   │   ├── label.vue
│   │   │   ├── alert.vue
│   │   │   └── popover.vue
│   │   │
│   │   ├── layouts/                   # Layouts
│   │   │   ├── DashboardLayout.vue
│   │   │   ├── AuthLayout.vue
│   │   │   ├── PublicLayout.vue
│   │   │   ├── Header.vue
│   │   │   ├── Sidebar.vue
│   │   │   └── Footer.vue
│   │   │
│   │   ├── forms/                     # Componentes de formularios
│   │   │   ├── FormField.vue
│   │   │   ├── FormError.vue
│   │   │   ├── FormSelect.vue
│   │   │   ├── FormTextarea.vue
│   │   │   ├── FormDatePicker.vue
│   │   │   └── FormFileUpload.vue
│   │   │
│   │   └── common/                    # Componentes comunes
│   │       ├── LoadingSpinner.vue
│   │       ├── ErrorBoundary.vue
│   │       ├── ConfirmDialog.vue
│   │       ├── SearchInput.vue
│   │       ├── Pagination.vue
│   │       ├── EmptyState.vue
│   │       ├── DataTable.vue
│   │       └── StatusBadge.vue
│   │
│   ├── features/                      # Features por módulo
│   │   │
│   │   ├── auth/                      # Autenticación
│   │   │   ├── components/
│   │   │   │   ├── LoginForm.vue
│   │   │   │   ├── SocialLoginButtons.vue
│   │   │   │   ├── OAuthCallbackHandler.vue
│   │   │   │   ├── MfaChallengeForm.vue
│   │   │   │   ├── RegisterForm.vue
│   │   │   │   ├── ForgotPasswordForm.vue
│   │   │   │   └── ResetPasswordForm.vue
│   │   │   ├── composables/
│   │   │   │   ├── useAuth.ts
│   │   │   │   └── useLogin.ts
│   │   │   ├── services/
│   │   │   │   └── auth.service.ts
│   │   │   └── types/
│   │   │       └── auth.types.ts
│   │   │
│   │   ├── dashboard/                 # Dashboard
│   │   │   ├── components/
│   │   │   │   ├── MetricCard.vue
│   │   │   │   ├── RevenueChart.vue
│   │   │   │   ├── AppointmentsList.vue
│   │   │   │   └── QuickActions.vue
│   │   │   ├── composables/
│   │   │   │   └── useDashboardData.ts
│   │   │   └── services/
│   │   │       └── dashboard.service.ts
│   │   │
│   │   ├── employees/                 # Empleados
│   │   │   ├── components/
│   │   │   │   ├── EmployeeList.vue
│   │   │   │   ├── EmployeeForm.vue
│   │   │   │   ├── EmployeeCard.vue
│   │   │   │   ├── EmployeeSchedule.vue
│   │   │   │   ├── ScheduleEditor.vue
│   │   │   │   └── EmployeeStats.vue
│   │   │   ├── composables/
│   │   │   │   ├── useEmployees.ts
│   │   │   │   ├── useEmployeeSchedule.ts
│   │   │   │   └── useEmployeeServices.ts
│   │   │   ├── services/
│   │   │   │   └── employees.service.ts
│   │   │   ├── types/
│   │   │   │   └── employee.types.ts
│   │   │   └── validations/
│   │   │       └── employee.schema.ts
│   │   │
│   │   ├── customers/                 # Clientes
│   │   │   ├── components/
│   │   │   │   ├── CustomerList.vue
│   │   │   │   ├── CustomerForm.vue
│   │   │   │   ├── CustomerProfile.vue
│   │   │   │   ├── CustomerHistory.vue
│   │   │   │   ├── PaymentMethodsList.vue
│   │   │   │   ├── AddPaymentMethodModal.vue
│   │   │   │   ├── CustomerNotes.vue
│   │   │   │   ├── CustomerPhotos.vue
│   │   │   │   └── LoyaltyPoints.vue
│   │   │   ├── composables/
│   │   │   │   ├── useCustomers.ts
│   │   │   │   ├── useCustomerPaymentMethods.ts
│   │   │   │   └── useCustomerHistory.ts
│   │   │   ├── services/
│   │   │   │   └── customers.service.ts
│   │   │   ├── types/
│   │   │   │   └── customer.types.ts
│   │   │   └── validations/
│   │   │       └── customer.schema.ts
│   │   │
│   │   ├── services/                  # Servicios del negocio
│   │   │   ├── components/
│   │   │   │   ├── ServiceList.vue
│   │   │   │   ├── ServiceForm.vue
│   │   │   │   ├── ServiceCard.vue
│   │   │   │   ├── ServiceVariations.vue
│   │   │   │   └── ServicePackages.vue
│   │   │   ├── composables/
│   │   │   │   └── useServices.ts
│   │   │   ├── services/
│   │   │   │   └── services.service.ts
│   │   │   ├── types/
│   │   │   │   └── service.types.ts
│   │   │   └── validations/
│   │   │       └── service.schema.ts
│   │   │
│   │   ├── appointments/              # Citas (CORE)
│   │   │   ├── components/
│   │   │   │   ├── Calendar/
│   │   │   │   │   ├── CalendarView.vue
│   │   │   │   │   ├── DayView.vue
│   │   │   │   │   ├── WeekView.vue
│   │   │   │   │   ├── MonthView.vue
│   │   │   │   │   └── AppointmentCard.vue
│   │   │   │   ├── AppointmentList.vue
│   │   │   │   ├── AppointmentDetail.vue
│   │   │   │   ├── CreateAppointment/
│   │   │   │   │   ├── AppointmentWizard.vue
│   │   │   │   │   ├── Step1SelectCustomer.vue
│   │   │   │   │   ├── Step2SelectService.vue
│   │   │   │   │   ├── Step3SelectEmployee.vue
│   │   │   │   │   ├── Step4SelectDateTime.vue
│   │   │   │   │   ├── Step5PaymentMethod.vue
│   │   │   │   │   └── Step6Confirmation.vue
│   │   │   │   ├── RescheduleModal.vue
│   │   │   │   ├── CancelModal.vue
│   │   │   │   ├── AvailabilityChecker.vue
│   │   │   │   └── WaitingList.vue
│   │   │   ├── composables/
│   │   │   │   ├── useAppointments.ts
│   │   │   │   ├── useCalendar.ts
│   │   │   │   ├── useAvailability.ts
│   │   │   │   └── useWaitingList.ts
│   │   │   ├── services/
│   │   │   │   └── appointments.service.ts
│   │   │   ├── types/
│   │   │   │   └── appointment.types.ts
│   │   │   ├── validations/
│   │   │   │   └── appointment.schema.ts
│   │   │   └── utils/
│   │   │       ├── availability.utils.ts
│   │   │       └── calendar.utils.ts
│   │   │
│   │   ├── payments/                  # Pagos
│   │   │   ├── components/
│   │   │   │   ├── PaymentList.vue
│   │   │   │   ├── PaymentDetail.vue
│   │   │   │   ├── RedsysPaymentForm.vue    # ⭐ Redsys InSite
│   │   │   │   ├── RedsysCardFields.vue     # iframes Redsys
│   │   │   │   ├── SavedCardSelector.vue
│   │   │   │   └── RefundModal.vue
│   │   │   ├── composables/
│   │   │   │   ├── usePayments.ts
│   │   │   │   └── useRedsysPayment.ts
│   │   │   ├── services/
│   │   │   │   ├── payments.service.ts
│   │   │   │   └── redsys.service.ts        # ⭐ Servicio Redsys
│   │   │   ├── types/
│   │   │   │   ├── payment.types.ts
│   │   │   │   └── redsys.types.ts
│   │   │   └── utils/
│   │   │       └── redsys.utils.ts
│   │   │
│   │   ├── reminders/                 # Recordatorios
│   │   │   ├── components/
│   │   │   │   ├── ReminderConfig.vue
│   │   │   │   ├── ReminderTemplates.vue
│   │   │   │   ├── ReminderLogs.vue
│   │   │   │   └── TemplateEditor.vue
│   │   │   ├── composables/
│   │   │   │   └── useReminders.ts
│   │   │   └── services/
│   │   │       └── reminders.service.ts
│   │   │
│   │   ├── photos/                    # Fotografías (FASE 2)
│   │   │   ├── components/
│   │   │   │   ├── PhotoGallery.vue
│   │   │   │   ├── PhotoUpload.vue
│   │   │   │   ├── BeforeAfterComparison.vue
│   │   │   │   └── PhotoViewer.vue
│   │   │   ├── composables/
│   │   │   │   └── usePhotos.ts
│   │   │   └── services/
│   │   │       └── photos.service.ts
│   │   │
│   │   ├── reports/                   # Reportes (FUTURO)
│   │   │   ├── components/
│   │   │   │   ├── ExecutiveDashboard.vue
│   │   │   │   ├── FinancialReports.vue
│   │   │   │   ├── OperationalReports.vue
│   │   │   │   └── ChartsWrapper.vue
│   │   │   ├── composables/
│   │   │   │   └── useReports.ts
│   │   │   └── services/
│   │   │       └── reports.service.ts
│   │   │
│   │   ├── settings/                  # Configuración
│   │   │   ├── components/
│   │   │   │   ├── OrganizationSettings.vue
│   │   │   │   ├── GeneralSettings.vue
│   │   │   │   ├── CancellationPolicy.vue
│   │   │   │   ├── BookingSettings.vue
│   │   │   │   ├── RedsysSettings.vue
│   │   │   │   ├── UserManagement.vue
│   │   │   │   ├── ReminderSettings.vue
│   │   │   │   └── AccountSecuritySettings.vue   # 2FA opcional (TOTP)
│   │   │   ├── composables/
│   │   │   │   └── useSettings.ts
│   │   │   └── services/
│   │   │       └── settings.service.ts
│   │   │
│   │   ├── public-booking/            # Reserva pública (Cliente final)
│   │   │   ├── components/
│   │   │   │   ├── BookingLanding.vue
│   │   │   │   ├── BookingWizard.vue
│   │   │   │   ├── ServiceCatalog.vue
│   │   │   │   ├── DateTimePicker.vue
│   │   │   │   └── BookingConfirmation.vue
│   │   │   ├── composables/
│   │   │   │   └── usePublicBooking.ts
│   │   │   └── services/
│   │   │       └── public-booking.service.ts
│   │   │
│   │   ├── profile/                   # Perfil del cliente
│   │   │   ├── components/
│   │   │   │   ├── MyProfile.vue
│   │   │   │   ├── MyAppointments.vue
│   │   │   │   ├── MyPaymentMethods.vue
│   │   │   │   └── MyLoyaltyPoints.vue
│   │   │   ├── composables/
│   │   │   │   └── useProfile.ts
│   │   │   └── services/
│   │   │       └── profile.service.ts
│   │   │
│   │   └── admin/                     # Super Admin (FASE 3 - SaaS)
│   │       ├── components/
│   │       │   ├── AdminDashboard.vue
│   │       │   ├── OrganizationsList.vue
│   │       │   ├── SubscriptionManagement.vue
│   │       │   └── GlobalMetrics.vue
│   │       ├── composables/
│   │       │   └── useAdmin.ts
│   │       └── services/
│   │           └── admin.service.ts
│   │
│   ├── composables/                         # Custom hooks globales
│   │   ├── useDebounce.ts
│   │   ├── useLocalStorage.ts
│   │   ├── useMediaQuery.ts
│   │   ├── useOnClickOutside.ts
│   │   └── useToast.ts
│   │
│   ├── locales/                     # Traducciones vue-i18n (es base en MVP)
│   │   └── es/
│   │       └── index.ts
│   ├── i18n/                        # Bootstrap createI18n
│   │   └── index.ts
│   │
│   ├── lib/                           # Librerías y utilidades
│   │   ├── api/
│   │   │   ├── client.ts              # Cliente Axios; cuerpo JSON con envelope §5.1.1 (volumen 1)
│   │   │   └── interceptors.ts
│   │   ├── utils/
│   │   │   ├── cn.ts                  # classnames helper
│   │   │   ├── date.utils.ts
│   │   │   ├── format.utils.ts
│   │   │   ├── validation.utils.ts
│   │   │   └── currency.utils.ts
│   │   └── constants/
│   │       ├── api.constants.ts
│   │       ├── app.constants.ts
│   │       └── routes.constants.ts
│   │
│   ├── pages/                         # Páginas principales
│   │   ├── auth/
│   │   │   ├── LoginPage.vue
│   │   │   ├── OAuthCallbackPage.vue
│   │   │   ├── MfaVerifyPage.vue
│   │   │   ├── RegisterPage.vue
│   │   │   ├── ForgotPasswordPage.vue
│   │   │   └── ResetPasswordPage.vue
│   │   │
│   │   ├── dashboard/
│   │   │   └── DashboardPage.vue
│   │   │
│   │   ├── employees/
│   │   │   ├── EmployeesPage.vue
│   │   │   ├── EmployeeDetailPage.vue
│   │   │   ├── CreateEmployeePage.vue
│   │   │   └── EditEmployeePage.vue
│   │   │
│   │   ├── customers/
│   │   │   ├── CustomersPage.vue
│   │   │   ├── CustomerDetailPage.vue
│   │   │   ├── CreateCustomerPage.vue
│   │   │   └── EditCustomerPage.vue
│   │   │
│   │   ├── services/
│   │   │   ├── ServicesPage.vue
│   │   │   ├── CreateServicePage.vue
│   │   │   └── EditServicePage.vue
│   │   │
│   │   ├── appointments/
│   │   │   ├── CalendarPage.vue
│   │   │   ├── CreateAppointmentPage.vue
│   │   │   ├── AppointmentDetailPage.vue
│   │   │   └── WaitingListPage.vue
│   │   │
│   │   ├── payments/
│   │   │   ├── PaymentsPage.vue
│   │   │   └── PaymentDetailPage.vue
│   │   │
│   │   ├── reminders/
│   │   │   └── RemindersPage.vue
│   │   │
│   │   ├── reports/
│   │   │   └── ReportsPage.vue
│   │   │
│   │   ├── settings/
│   │   │   └── SettingsPage.vue
│   │   │
│   │   ├── public/
│   │   │   ├── BookingPage.vue
│   │   │   └── MyProfilePage.vue
│   │   │
│   │   ├── admin/
│   │   │   └── AdminPage.vue
│   │   │
│   │   └── errors/
│   │       ├── NotFoundPage.vue
│   │       └── ErrorPage.vue
│   │
│   ├── stores/                        # Estado global (Pinia)
│   │   ├── authStore.ts
│   │   ├── organizationStore.ts
│   │   ├── appointmentStore.ts
│   │   ├── calendarStore.ts
│   │   └── uiStore.ts
│   │
│   ├── types/                         # TypeScript types globales
│   │   ├── index.ts
│   │   ├── api.types.ts               # ApiEnvelope, error codes, DTOs API
│   │   ├── models.types.ts
│   │   └── enums.ts
│   │
│   ├── styles/                        # Estilos globales
│   │   └── globals.css
│   │
│   └── config/                        # Configuración
│       └── env.ts
│
├── .env.development
├── .env.production
├── .env.example
├── .eslintrc.cjs
├── .prettierrc
├── .gitignore
├── components.json                    # Config del kit de componentes (si aplica)
├── index.html
├── package.json
├── postcss.config.js
├── tailwind.config.js
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── vite.config.ts
└── README.md
```

---

### Backend (ASP.NET Core 8.0 / C#)

```
reservarte-api/
├── src/
│   ├── ReservArte.API/                      # API Layer
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── ExternalAuthController.cs   # challenge/callback OAuth/OIDC → emisión JWT
│   │   │   ├── AccountMfaController.cs     # alta / baja 2FA opcional (autenticado)
│   │   │   ├── OrganizationsController.cs
│   │   │   ├── EmployeesController.cs
│   │   │   ├── CustomersController.cs
│   │   │   ├── ServicesController.cs
│   │   │   ├── AppointmentsController.cs
│   │   │   ├── PaymentsController.cs        # ⭐ Redsys integration
│   │   │   ├── RemindersController.cs
│   │   │   ├── PhotosController.cs
│   │   │   ├── ReportsController.cs
│   │   │   └── SettingsController.cs
│   │   │
│   │   ├── Middleware/
│   │   │   ├── TenantMiddleware.cs          # Multi-tenant resolution
│   │   │   ├── ErrorHandlerMiddleware.cs
│   │   │   ├── SecurityHeadersMiddleware.cs
│   │   │   └── RateLimitingMiddleware.cs
│   │   │
│   │   ├── Filters/
│   │   │   ├── ValidateModelAttribute.cs
│   │   │   └── AuthorizeOrganizationAttribute.cs
│   │   │
│   │   ├── Extensions/
│   │   │   ├── ServiceCollectionExtensions.cs
│   │   │   └── ApplicationBuilderExtensions.cs
│   │   │
│   │   ├── appsettings.json              # contrato: todas las claves; valores vacíos (volumen 1 §5.1.3)
│   │   ├── appsettings.Development.json  # dev sin secretos; tenant por cabecera en local (§5.1.3)
│   │   ├── appsettings.Production.json   # prod sin secretos (§5.1.3)
│   │   ├── Program.cs
│   │   └── Startup.cs
│   │
│   ├── ReservArte.Application/              # Application Layer
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   ├── RegisterRequest.cs
│   │   │   │   ├── TokenResponse.cs
│   │   │   │   ├── RefreshTokenRequest.cs
│   │   │   │   ├── MfaVerifyRequest.cs
│   │   │   │   └── MfaSetupResponse.cs
│   │   │   ├── Employees/
│   │   │   │   ├── EmployeeDto.cs
│   │   │   │   ├── CreateEmployeeRequest.cs
│   │   │   │   └── UpdateEmployeeRequest.cs
│   │   │   ├── Customers/
│   │   │   │   ├── CustomerDto.cs
│   │   │   │   ├── CreateCustomerRequest.cs
│   │   │   │   └── UpdateCustomerRequest.cs
│   │   │   ├── Services/
│   │   │   │   ├── ServiceDto.cs
│   │   │   │   ├── CreateServiceRequest.cs
│   │   │   │   └── UpdateServiceRequest.cs
│   │   │   ├── Appointments/
│   │   │   │   ├── AppointmentDto.cs
│   │   │   │   ├── CreateAppointmentRequest.cs
│   │   │   │   ├── UpdateAppointmentRequest.cs
│   │   │   │   └── AvailabilityRequest.cs
│   │   │   ├── Payments/
│   │   │   │   ├── PaymentDto.cs
│   │   │   │   ├── RedsysPaymentRequest.cs  # ⭐ Redsys DTOs
│   │   │   │   ├── RedsysPaymentResponse.cs
│   │   │   │   └── SavedCardDto.cs
│   │   │   └── Common/
│   │   │       ├── PagedResult.cs
│   │   │       └── Result.cs
│   │   │
│   │   ├── Services/                        # Application Services
│   │   │   ├── Auth/
│   │   │   │   ├── IAuthService.cs
│   │   │   │   └── AuthService.cs
│   │   │   ├── Employees/
│   │   │   │   ├── IEmployeeService.cs
│   │   │   │   └── EmployeeService.cs
│   │   │   ├── Customers/
│   │   │   │   ├── ICustomerService.cs
│   │   │   │   └── CustomerService.cs
│   │   │   ├── Services/
│   │   │   │   ├── IServiceService.cs
│   │   │   │   └── ServiceService.cs
│   │   │   ├── Appointments/
│   │   │   │   ├── IAppointmentService.cs
│   │   │   │   ├── AppointmentService.cs
│   │   │   │   ├── IAvailabilityService.cs
│   │   │   │   └── AvailabilityService.cs
│   │   │   ├── Payments/
│   │   │   │   ├── IPaymentService.cs
│   │   │   │   ├── PaymentService.cs
│   │   │   │   ├── IRedsysPaymentService.cs  # ⭐ Redsys Service
│   │   │   │   └── RedsysPaymentService.cs
│   │   │   ├── Reminders/
│   │   │   │   ├── IReminderService.cs
│   │   │   │   └── ReminderService.cs
│   │   │   └── Photos/
│   │   │       ├── IPhotoService.cs
│   │   │       └── PhotoService.cs
│   │   │
│   │   ├── Interfaces/                      # Repository Interfaces
│   │   │   ├── IOrganizationRepository.cs
│   │   │   ├── IEmployeeRepository.cs
│   │   │   ├── ICustomerRepository.cs
│   │   │   ├── IServiceRepository.cs
│   │   │   ├── IAppointmentRepository.cs
│   │   │   ├── IPaymentRepository.cs
│   │   │   ├── IPaymentMethodRepository.cs  # ⭐ Tarjetas guardadas
│   │   │   └── IRedsysLogRepository.cs      # ⭐ Logs Redsys
│   │   │
│   │   ├── Validators/                      # FluentValidation
│   │   │   ├── EmployeeValidator.cs
│   │   │   ├── CustomerValidator.cs
│   │   │   ├── ServiceValidator.cs
│   │   │   ├── AppointmentValidator.cs
│   │   │   └── PaymentValidator.cs
│   │   │
│   │   ├── Mappings/                        # AutoMapper profiles
│   │   │   ├── EmployeeProfile.cs
│   │   │   ├── CustomerProfile.cs
│   │   │   ├── ServiceProfile.cs
│   │   │   └── AppointmentProfile.cs
│   │   │
│   │   └── Exceptions/
│   │       ├── NotFoundException.cs
│   │       ├── ValidationException.cs
│   │       ├── BusinessException.cs
│   │       └── UnauthorizedException.cs
│   │
│   ├── ReservArte.Domain/                   # Domain Layer
│   │   ├── Entities/
│   │   │   ├── Organization.cs
│   │   │   ├── OrganizationSettings.cs
│   │   │   ├── User.cs
│   │   │   ├── Employee.cs
│   │   │   ├── EmployeeAvailability.cs
│   │   │   ├── EmployeeException.cs
│   │   │   ├── EmployeeService.cs
│   │   │   ├── Customer.cs
│   │   │   ├── CustomerNote.cs
│   │   │   ├── CustomerAllergy.cs
│   │   │   ├── CustomerConsent.cs
│   │   │   ├── CustomerPaymentMethod.cs     # ⭐ Tarjetas guardadas
│   │   │   ├── Service.cs
│   │   │   ├── ServiceVariation.cs
│   │   │   ├── ServicePackage.cs
│   │   │   ├── ServicePackageItem.cs
│   │   │   ├── Appointment.cs
│   │   │   ├── AppointmentService.cs
│   │   │   ├── WaitingList.cs
│   │   │   ├── Payment.cs
│   │   │   ├── RedsysTransactionLog.cs      # ⭐ Logs Redsys
│   │   │   ├── ReminderConfiguration.cs
│   │   │   ├── MessageTemplate.cs
│   │   │   ├── ReminderLog.cs
│   │   │   ├── ServicePhoto.cs
│   │   │   ├── RefreshToken.cs
│   │   │   └── AuditLog.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── AppointmentStatus.cs
│   │   │   ├── UserRole.cs
│   │   │   ├── PaymentMethod.cs
│   │   │   ├── PaymentStatus.cs
│   │   │   └── ReminderChannel.cs
│   │   │
│   │   └── ValueObjects/
│   │       ├── Address.cs
│   │       ├── TimeSlot.cs
│   │       └── Money.cs
│   │
│   ├── ReservArte.Infrastructure/           # Infrastructure Layer
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/              # EF Core configurations
│   │   │   │   ├── OrganizationConfiguration.cs
│   │   │   │   ├── EmployeeConfiguration.cs
│   │   │   │   ├── CustomerConfiguration.cs
│   │   │   │   ├── AppointmentConfiguration.cs
│   │   │   │   └── PaymentConfiguration.cs
│   │   │   │
│   │   │   ├── Migrations/                  # EF Migrations
│   │   │   │   └── ...
│   │   │   │
│   │   │   └── Seeders/
│   │   │       ├── OrganizationSeeder.cs
│   │   │       └── UserSeeder.cs
│   │   │
│   │   ├── Repositories/                    # Repository implementations
│   │   │   ├── OrganizationRepository.cs
│   │   │   ├── EmployeeRepository.cs
│   │   │   ├── CustomerRepository.cs
│   │   │   ├── ServiceRepository.cs
│   │   │   ├── AppointmentRepository.cs
│   │   │   ├── PaymentRepository.cs
│   │   │   ├── PaymentMethodRepository.cs   # ⭐
│   │   │   └── RedsysLogRepository.cs       # ⭐
│   │   │
│   │   ├── Services/                        # Infrastructure Services
│   │   │   ├── Email/
│   │   │   │   ├── IEmailService.cs
│   │   │   │   └── AmazonSESEmailService.cs
│   │   │   ├── WhatsApp/
│   │   │   │   ├── IWhatsAppService.cs
│   │   │   │   └── WhatsAppService.cs
│   │   │   ├── FileStorage/
│   │   │   │   ├── IFileStorageService.cs
│   │   │   │   └── CloudinaryMediaService.cs
│   │   │   ├── Payments/
│   │   │   │   ├── RedsysPaymentService.cs  # ⭐ Implementación Redsys
│   │   │   │   └── RedsysSignatureHelper.cs # ⭐ HMAC SHA-256
│   │   │   ├── Security/
│   │   │   │   ├── JwtTokenService.cs
│   │   │   │   ├── PasswordHashingService.cs
│   │   │   │   └── EncryptionService.cs
│   │   │   └── Tenant/
│   │   │       ├── ITenantService.cs
│   │   │       └── TenantService.cs
│   │   │
│   │   └── BackgroundJobs/                  # Hangfire jobs
│   │       ├── ReminderJob.cs
│   │       └── CleanupJob.cs
│   │
│   └── ReservArte.Shared/                   # Shared utilities
│       ├── Constants/
│       │   ├── AppConstants.cs
│       │   ├── ErrorMessages.cs
│       │   └── RedsysConstants.cs           # ⭐
│       │
│       └── Extensions/
│           ├── DateTimeExtensions.cs
│           ├── StringExtensions.cs
│           └── EnumExtensions.cs
│
├── tests/
│   ├── ReservArte.UnitTests/
│   │   ├── Services/
│   │   ├── Validators/
│   │   └── Helpers/
│   │
│   ├── ReservArte.IntegrationTests/
│   │   ├── Controllers/
│   │   ├── Repositories/
│   │   └── Services/
│   │
│   └── ReservArte.E2ETests/
│       └── Scenarios/
│
├── .gitignore
├── .editorconfig
├── ReservArte.sln
└── README.md
```

---

### Mobile (React Native)

```
reservarte-mobile/
├── android/                                 # Android native code
├── ios/                                     # iOS native code
│
├── src/
│   ├── app/
│   │   ├── App.tsx
│   │   └── Navigation.tsx
│   │
│   ├── assets/
│   │   ├── images/
│   │   ├── icons/
│   │   └── fonts/
│   │
│   ├── components/
│   │   ├── common/
│   │   │   ├── Button.tsx
│   │   │   ├── Input.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── Loading.tsx
│   │   │   └── EmptyState.tsx
│   │   │
│   │   └── navigation/
│   │       ├── TabNavigator.tsx
│   │       ├── StackNavigator.tsx
│   │       └── DrawerNavigator.tsx
│   │
│   ├── features/
│   │   ├── auth/
│   │   │   ├── screens/
│   │   │   │   ├── LoginScreen.tsx
│   │   │   │   ├── MfaVerifyScreen.tsx
│   │   │   │   └── RegisterScreen.tsx
│   │   │   ├── components/
│   │   │   │   └── SocialLoginButtons.tsx
│   │   │   └── hooks/
│   │   │
│   │   ├── appointments/
│   │   │   ├── screens/
│   │   │   │   ├── AppointmentsListScreen.tsx
│   │   │   │   ├── CreateAppointmentScreen.tsx
│   │   │   │   └── AppointmentDetailScreen.tsx
│   │   │   ├── components/
│   │   │   │   ├── AppointmentCard.tsx
│   │   │   │   └── BookingWizard.tsx
│   │   │   └── hooks/
│   │   │
│   │   ├── services/
│   │   │   ├── screens/
│   │   │   │   ├── ServiceListScreen.tsx
│   │   │   │   └── ServiceDetailScreen.tsx
│   │   │   └── components/
│   │   │       └── ServiceCard.tsx
│   │   │
│   │   ├── profile/
│   │   │   ├── screens/
│   │   │   │   ├── ProfileScreen.tsx
│   │   │   │   ├── MyAppointmentsScreen.tsx
│   │   │   │   └── PaymentMethodsScreen.tsx
│   │   │   └── components/
│   │   │
│   │   ├── payments/
│   │   │   ├── components/
│   │   │   │   ├── RedsysWebView.tsx        # ⭐ WebView for Redsys InSite
│   │   │   │   └── SavedCardsList.tsx
│   │   │   └── hooks/
│   │   │
│   │   └── employee/                        # Para personal
│   │       ├── screens/
│   │       │   ├── EmployeeHomeScreen.tsx
│   │       │   ├── TodayAgendaScreen.tsx
│   │       │   └── CustomerDetailScreen.tsx
│   │       └── components/
│   │
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   ├── useNetworkStatus.ts
│   │   └── usePushNotifications.ts
│   │
│   ├── lib/
│   │   ├── api/
│   │   │   ├── client.ts
│   │   │   └── interceptors.ts
│   │   ├── utils/
│   │   │   ├── date.utils.ts
│   │   │   ├── format.utils.ts
│   │   │   └── storage.utils.ts
│   │   └── constants/
│   │
│   ├── services/
│   │   ├── auth.service.ts
│   │   ├── appointments.service.ts
│   │   ├── payments.service.ts
│   │   └── notifications.service.ts
│   │
│   ├── stores/                              # Zustand
│   │   ├── authStore.ts
│   │   ├── appointmentStore.ts
│   │   └── userStore.ts
│   │
│   ├── types/
│   │   ├── navigation.types.ts
│   │   ├── api.types.ts               # ApiEnvelope, error codes, DTOs API
│   │   └── models.types.ts
│   │
│   └── config/
│       ├── env.ts
│       └── firebase.config.ts               # Push notifications
│
├── .env.development
├── .env.production
├── .gitignore
├── app.json
├── babel.config.js
├── metro.config.js
├── package.json
├── tsconfig.json
└── README.md
```

---

## RESUMEN DE PANTALLAS POR PRIORIDAD

### MVP (FASE 1) - Imprescindibles

| Módulo | Pantallas | Cantidad |
|--------|-----------|----------|
| Autenticación | Login, OAuth callback, 2FA verify, Register, Forgot, Reset | 6 |
| Dashboard | Home | 1 |
| Empleados | List, Create, Edit, Detail, Schedule | 5 |
| Clientes | List, Create, Edit, Profile, Payment Methods | 5 |
| Servicios | List, Create, Edit | 3 |
| Citas | Calendar, Create Wizard (6 steps), Detail, Cancel Modal | 9 |
| Pagos | List, Detail, Redsys Payment Form | 3 |
| Recordatorios | Configuration, Logs | 2 |
| Configuración | Organization, General, Cancellation, Redsys, Account security (2FA) | 5 |
| **TOTAL MVP** | | **39 pantallas** |

---

### FASE 2 - Mejoras y Móvil

| Módulo | Pantallas | Cantidad |
|--------|-----------|----------|
| Reserva Pública | Landing, Booking Wizard, Confirmation | 3 |
| Perfil Cliente | My Profile, My Appointments, My Cards | 3 |
| Fotografías | Gallery, Upload, Comparison | 3 |
| Lista de Espera | Waiting List Management | 1 |
| **TOTAL FASE 2** | | **10 pantallas** |

---

### FASE 3 - SaaS

| Módulo | Pantallas | Cantidad |
|--------|-----------|----------|
| SaaS Landing | Public Landing, Signup Wizard, Pricing | 3 |
| Super Admin | Dashboard, Organizations List, Metrics | 3 |
| **TOTAL FASE 3** | | **6 pantallas** |

---

### FUTURO - Opcional

| Módulo | Pantallas | Cantidad |
|--------|-----------|----------|
| Reportes | Executive, Financial, Operational, Marketing | 4 |
| Productos | List, Create, Edit, Inventory (FUTURO) | 4 |
| **TOTAL FUTURO** | | **8 pantallas** |

---

## TOTAL DE PANTALLAS: **63 pantallas completas**

- **MVP**: 39 pantallas
- **Fase 2**: 10 pantallas
- **Fase 3**: 6 pantallas
- **Futuro**: 8 pantallas

---

## NOTAS ADICIONALES

### 1. Componentes Reutilizables
Muchas pantallas compartirán componentes comunes (formularios, tablas, modales), reduciendo significativamente el tiempo de desarrollo.

### 2. Redsys InSite
El formulario de pago con Redsys será un componente crítico reutilizado en múltiples flujos (crear cita, reserva pública, guardar tarjeta).

### 3. Responsive Design
Todas las pantallas web deben ser responsive (móvil, tablet, desktop) usando Tailwind CSS.

### 4. Arquitectura Modular
La estructura propuesta facilita la escalabilidad y el mantenimiento del código.

### 5. Multi-Tenant
El middleware de tenant resolution en el backend garantiza el aislamiento de datos desde el primer momento.

### 6. Convenciones de Código

#### Frontend (Vue 3 / TypeScript)
- Nombres de componentes: PascalCase (`EmployeeList.vue`)
- Nombres de composables: camelCase con prefijo `use` (`useEmployees.ts` en `composables/`)
- Nombres de servicios: camelCase con sufijo `.service` (`employees.service.ts`)
- Nombres de stores: camelCase con sufijo `Store` (`authStore.ts`)

#### Backend (C#)
- Nombres de controladores: PascalCase con sufijo `Controller` (`EmployeesController.cs`)
- Nombres de servicios: PascalCase con sufijo `Service` (`EmployeeService.cs`)
- Interfaces: PascalCase con prefijo `I` (`IEmployeeService.cs`)
- Entidades: PascalCase sin sufijos (`Employee.cs`)

### 7. Testing
Cada módulo debe incluir:
- **Frontend**: Tests unitarios con Vitest + Vue Test Utils
- **Backend**: Tests unitarios con xUnit, tests de integración
- **E2E**: Tests end-to-end con Playwright o Cypress

### 8. Documentación
Cada feature debe incluir:
- README.md explicando el módulo
- Comentarios JSDoc/XML en código crítico
- Swagger/OpenAPI para endpoints de API

### 9. Configuración del backend (secretos y `appsettings`)
- **Contrato y jerarquía:** volumen 1 **§5.1.3** (`appsettings.json` → `Development` / `Production` → User Secrets → variables de entorno / AWS Secrets Manager).
- **Onboarding de desarrolladores:** cuando exista, `Documentation/Project-Init/user-secrets-guide.md` (comandos `dotnet user-secrets`, Redsys de prueba, ngrok para webhook, FAQ); el repositorio no debe contener secretos del API.

### 10. Accesibilidad e internacionalización (web)
- **WCAG 2.1 AA y vue-i18n v9:** [`accessibility-and-i18n.md`](accessibility-and-i18n.md); alineado con el script **`Documentation/Project-Init/Scripts de instalación.md`** (Pasos 2–5) y el roadmap del volumen 3 **§10.2**.

---

**Documento creado por:** Gabriel Sánchez-Vallejo Millán y Guillermo Algárate del Arco  
**Fecha:** Octubre 2025  
**Versión:** 1.0

---

**FIN DEL DOCUMENTO DE ANÁLISIS DE PANTALLAS Y ESTRUCTURA**
