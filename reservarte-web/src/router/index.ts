import { defineComponent, h } from 'vue';
import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/authStore';

// ── Páginas stub (patrón del Paso 5 del script): cada módulo las
//    sustituirá por sus páginas reales en su tarea ──────────────────────
const DashboardPage = defineComponent({
  name: 'DashboardPage',
  setup() {
    return () => h('div', 'Dashboard');
  },
});
const LoginPage = defineComponent({
  name: 'LoginPage',
  setup() {
    return () => h('div', 'Login');
  },
});
const MfaVerifyPage = defineComponent({
  name: 'MfaVerifyPage',
  setup() {
    return () => h('div', '2FA verify');
  },
});
const OAuthCallbackPage = defineComponent({
  name: 'OAuthCallbackPage',
  setup() {
    return () => h('div', 'OAuth callback');
  },
});
const RegisterPage = defineComponent({
  name: 'RegisterPage',
  setup() {
    return () => h('div', 'Registro');
  },
});
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

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: DashboardPage, meta: { requiresAuth: true } },
    { path: '/login', name: 'login', component: LoginPage },
    { path: '/login/two-factor', name: 'mfa-verify', component: MfaVerifyPage },
    { path: '/auth/callback', name: 'oauth-callback', component: OAuthCallbackPage },
    { path: '/register', name: 'register', component: RegisterPage },
    { path: '/forgot-password', name: 'forgot-password', component: ForgotPasswordPage },
    { path: '/reset-password/:token', name: 'reset-password', component: ResetPasswordPage },
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
    return { name: 'login' };
  }

  if (to.meta.requiresAuth && authStore.mfaRequired) {
    return { name: 'mfa-verify' };
  }

  return true;
});
