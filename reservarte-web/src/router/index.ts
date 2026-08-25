import { defineComponent, h } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@stores/authStore';
import { DashboardLayout } from '@components/layouts';
import LoginPage from '@pages/auth/LoginPage.vue';
import OAuthCallbackPage from '@pages/auth/OAuthCallbackPage.vue';
import MfaVerifyPage from '@pages/auth/MfaVerifyPage.vue';
import RegisterPage from '@pages/auth/RegisterPage.vue';

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

const DashboardPage = stubPage('DashboardPage', 'Dashboard');
const EmployeesPage = stubPage('EmployeesPage', 'Empleados');
const CustomersPage = stubPage('CustomersPage', 'Clientes');
const ServicesPage = stubPage('ServicesPage', 'Servicios');
const AppointmentsPage = stubPage('AppointmentsPage', 'Citas');
const PaymentsPage = stubPage('PaymentsPage', 'Pagos');
const RemindersPage = stubPage('RemindersPage', 'Recordatorios');
const SettingsPage = stubPage('SettingsPage', 'Configuración');
const LegalTermsPage = stubPage('LegalTermsPage', 'Términos y condiciones');
const LegalPrivacyPage = stubPage('LegalPrivacyPage', 'Política de privacidad');

const ForgotPasswordPage = defineComponent({
  name: 'ForgotPasswordPage',
  setup() {
    return () => h('div', 'Recuperar contraseña');
  },
});
const ResetPasswordPage = defineComponent({
  name: 'ResetPasswordPage',
  setup() {
    return () => h('div', 'Restablecer contraseña');
  },
});
const MyAppointmentsPage = defineComponent({
  name: 'MyAppointmentsPage',
  setup() {
    return () => h('div', 'Mis citas');
  },
});
const ContactPage = defineComponent({
  name: 'ContactPage',
  setup() {
    return () => h('div', 'Contacto');
  },
});
const AccountPage = defineComponent({
  name: 'AccountPage',
  setup() {
    return () => h('div', 'Cuenta');
  },
});

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      component: DashboardLayout,
      meta: { requiresAuth: true },
      children: [
        { path: '', name: 'dashboard', component: DashboardPage },
        { path: 'empleados', name: 'employees', component: EmployeesPage },
        { path: 'clientes', name: 'customers', component: CustomersPage },
        { path: 'servicios', name: 'services', component: ServicesPage },
        { path: 'citas', name: 'appointments', component: AppointmentsPage },
        { path: 'pagos', name: 'payments', component: PaymentsPage },
        { path: 'recordatorios', name: 'reminders', component: RemindersPage },
        { path: 'configuracion', name: 'settings', component: SettingsPage },
      ],
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
    { path: '/reset-password/:token', name: 'reset-password', component: ResetPasswordPage },
    // Destinos del BottomNav (stubs; su contenido real es tarea de cada módulo)
    { path: '/mis-citas', name: 'my-appointments', component: MyAppointmentsPage, meta: { requiresAuth: true } },
    { path: '/contacto', name: 'contact', component: ContactPage },
    { path: '/cuenta', name: 'account', component: AccountPage, meta: { requiresAuth: true } },
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
