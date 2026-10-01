import { defineComponent, h } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@stores/authStore';
import LoginPage from '@pages/auth/LoginPage.vue';
import OAuthCallbackPage from '@pages/auth/OAuthCallbackPage.vue';
import MfaVerifyPage from '@pages/auth/MfaVerifyPage.vue';
import RegisterPage from '@pages/auth/RegisterPage.vue';
import ForgotPasswordPage from '@pages/auth/ForgotPasswordPage.vue';
import ResetPasswordPage from '@pages/auth/ResetPasswordPage.vue';
import SetPasswordPage from '@pages/auth/SetPasswordPage.vue';
import AccountPage from '@pages/account/AccountPage.vue';
import MyAppointmentsPage from '@pages/appointments/MyAppointmentsPage.vue';
import ContactPage from '@pages/contact/ContactPage.vue';
import BookingPage from '@pages/booking/BookingPage.vue';
import AppointmentsPage from '@pages/appointments/AppointmentsPage.vue';

// ── Páginas stub (patrón del Paso 5 del script): cada módulo las
//    sustituirá por sus páginas reales en su tarea ──────────────────────
function stubPage(name: string, label: string) {
  return defineComponent({
    name,
    setup() {
      return () => h('div', label);
    },
  });
}

const EmployeesPage = stubPage('EmployeesPage', 'Empleados');
const CustomersPage = stubPage('CustomersPage', 'Clientes');
const ServicesPage = stubPage('ServicesPage', 'Servicios');
const PaymentsPage = stubPage('PaymentsPage', 'Pagos');
const RemindersPage = stubPage('RemindersPage', 'Recordatorios');
const SettingsPage = stubPage('SettingsPage', 'Configuración');
const LegalTermsPage = stubPage('LegalTermsPage', 'Términos y condiciones');
const LegalPrivacyPage = stubPage('LegalPrivacyPage', 'Política de privacidad');
// Área de usuario de la pantalla de Usuario (RA-869ep9p36)
const ProfilePage = stubPage('ProfilePage', 'Datos de usuario');
const PaymentMethodsPage = stubPage('PaymentMethodsPage', 'Métodos de pago');
const NotificationsPage = stubPage('NotificationsPage', 'Notificaciones');
const AccountSettingsPage = stubPage('AccountSettingsPage', 'Configuración de la cuenta');
const PrivacyPage = stubPage('PrivacyPage', 'Privacidad');
const AboutPage = stubPage('AboutPage', 'Acerca de More Than Brows');

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    // Área privada: pantallas planas bajo el BottomNav global de App.vue, sin
    // Sidebar ni Header (RA-869ep9p36). La gestión se abre desde la pantalla
    // de Usuario (/cuenta); no hay otro menú.
    // La raíz lleva a Mis citas, el inicio con sesión (RA-869faaunu). El panel
    // de métricas no tiene ruta hasta su tarea (`869d7fc7e`).
    { path: '/', redirect: { name: 'my-appointments' } },
    {
      path: '/empleados',
      name: 'employees',
      component: EmployeesPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/clientes',
      name: 'customers',
      component: CustomersPage,
      meta: { requiresAuth: true },
    },
    { path: '/servicios', name: 'services', component: ServicesPage, meta: { requiresAuth: true } },
    { path: '/pagos', name: 'payments', component: PaymentsPage, meta: { requiresAuth: true } },
    {
      path: '/recordatorios',
      name: 'reminders',
      component: RemindersPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/configuracion',
      name: 'settings',
      component: SettingsPage,
      meta: { requiresAuth: true },
    },
    { path: '/login', name: 'login', component: LoginPage },
    { path: '/login/two-factor', name: 'mfa-verify', component: MfaVerifyPage },
    { path: '/auth/callback', name: 'oauth-callback', component: OAuthCallbackPage },
    { path: '/register', name: 'register', component: RegisterPage },
    // Documentos legales: PÚBLICOS (se consultan en el registro, sin sesión).
    // Contenido real = trabajo futuro; hoy son stubs.
    { path: '/legal/terminos', name: 'legal-terms', component: LegalTermsPage },
    { path: '/legal/privacidad', name: 'legal-privacy', component: LegalPrivacyPage },
    { path: '/forgot-password', name: 'forgot-password', component: ForgotPasswordPage },
    { path: '/reset-password/:token?', name: 'reset-password', component: ResetPasswordPage },
    // Invitación de alta de empleado (RA-869f17y68): flujo distinto del
    // restablecimiento, con su propio token (7 días) y su propio endpoint.
    { path: '/set-password/:token?', name: 'set-password', component: SetPasswordPage },
    // Destinos del BottomNav (RA-869faaunu)
    {
      path: '/mis-citas',
      name: 'my-appointments',
      component: MyAppointmentsPage,
      meta: { requiresAuth: true },
    },
    { path: '/contacto', name: 'contact', component: ContactPage },
    // Listado de citas del personal (RA-869fajn7g): día, semana y mes, con detalle.
    {
      path: '/citas',
      name: 'appointments',
      component: AppointmentsPage,
      meta: { requiresAuth: true },
    },
    // Reserva y modificación de citas (RA-869fagpyg, H-45).
    { path: '/reservar', name: 'booking', component: BookingPage, meta: { requiresAuth: true } },
    { path: '/cuenta', name: 'account', component: AccountPage, meta: { requiresAuth: true } },
    // Área de usuario (stubs; su contenido real es tarea de cada módulo)
    {
      path: '/cuenta/datos',
      name: 'account-profile',
      component: ProfilePage,
      meta: { requiresAuth: true },
    },
    {
      path: '/cuenta/metodos-pago',
      name: 'account-payment-methods',
      component: PaymentMethodsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/cuenta/notificaciones',
      name: 'account-notifications',
      component: NotificationsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/cuenta/configuracion',
      name: 'account-settings',
      component: AccountSettingsPage,
      meta: { requiresAuth: true },
    },
    {
      path: '/cuenta/privacidad',
      name: 'account-privacy',
      component: PrivacyPage,
      meta: { requiresAuth: true },
    },
    // Información del centro: pública, como Contacto.
    { path: '/acerca-de', name: 'about', component: AboutPage },
  ],
});

// ── Guards (RA-869d7f7ce) ─────────────────────────────────────────────
// requiresAuth: sin sesión → /login.
// requiresMfa: sesión con verificación TOTP pendiente → /login/two-factor
// (no se entra al área privada hasta superar el 2FA).
router.beforeEach((to) => {
  // El store se resuelve AQUÍ dentro, no en el import del módulo: cuando
  // este archivo se carga, Pinia todavía no está instalada (main.ts la
  // registra antes que el router, pero los imports se evalúan antes)
  const authStore = useAuthStore();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login', query: to.fullPath !== '/' ? { redirect: to.fullPath } : undefined };
  }

  if (to.meta.requiresAuth && authStore.mfaRequired) {
    return { name: 'mfa-verify' };
  }

  return true;
});
