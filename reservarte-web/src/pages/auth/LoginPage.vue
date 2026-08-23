<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { LoginForm } from '@components/ui/login-form';
import { BottomNav, type BottomNavItem } from '@components/ui/bottom-nav';
import { useAuthStore } from '@stores/authStore';
import { login, getOAuthChallengeUrl, AuthApiError } from '@features/auth/api/auth.api';
import type { OAuthProvider } from '@features/auth/types/auth.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';
import HomeIcon from '@assets/icons/nav-home.svg';
import UserIcon from '@assets/icons/nav-user.svg';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const loading = ref(false);
const errorMessage = ref('');

// Nota: la barra inferior de Figma trae 3 iconos (Home/Map pin/User), pero
// solo hay páginas reales para "Inicio" y "Cuenta" por ahora — no se incluye
// un tercer destino inventado. Ver resumen de componentes pendientes.
const navItems: BottomNavItem[] = [
  { label: 'Inicio', to: '/', icon: HomeIcon },
  { label: 'Cuenta', to: '/login', icon: UserIcon },
];

function redirectAfterLogin() {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
  router.push(redirect);
}

async function handleSubmit(payload: { email: string; password: string }) {
  errorMessage.value = '';
  loading.value = true;

  try {
    const result = await login(payload);

    authStore.login({
      user: result.user,
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      mfaRequired: result.mfaRequired,
      mfaTicket: result.mfaTicket,
    });

    if (result.mfaRequired) {
      router.push({ name: 'mfa-verify' });
      return;
    }

    redirectAfterLogin();
  } catch (err) {
    errorMessage.value =
      err instanceof AuthApiError ? err.message : 'No se pudo iniciar sesión. Inténtalo de nuevo.';
  } finally {
    loading.value = false;
  }
}

function handleOAuth(provider: OAuthProvider) {
  window.location.href = getOAuthChallengeUrl(provider);
}
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />

    <main class="flex flex-1 items-center justify-center px-4 py-8">
      <LoginForm
        social-login
        :loading="loading"
        :error-message="errorMessage"
        :forgot-password-to="{ name: 'forgot-password' }"
        @submit="handleSubmit"
        @oauth="handleOAuth"
      />
    </main>

    <BottomNav :items="navItems" />
  </div>
</template>
