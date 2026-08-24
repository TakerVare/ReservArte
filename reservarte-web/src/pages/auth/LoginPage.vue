<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { LoginForm } from '@components/ui/login-form';
import { useAuthStore } from '@stores/authStore';
import { login, getOAuthChallengeUrl, AuthApiError } from '@features/auth/api/auth.api';
import type { OAuthProvider } from '@features/auth/types/auth.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const loading = ref(false);
const errorMessage = ref('');

// CAPTCHA (vol. 1 §4.4.3): el frontend decide CUÁNDO mostrarlo; el backend
// lo verifica cuando llega (hoy desactivado en dev: Captcha:Enabled = false).
// Tras 3 intentos fallidos se exige. El token del widget se adjunta al login.
const FAILED_ATTEMPTS_THRESHOLD = 3;
const failedAttempts = ref(0);
const captchaToken = ref<string | null>(null);
const captchaRequired = computed(() => failedAttempts.value >= FAILED_ATTEMPTS_THRESHOLD);

function redirectAfterLogin() {
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
  router.push(redirect);
}

function handleCaptchaVerified(token: string) {
  captchaToken.value = token;
}

async function handleSubmit(payload: { email: string; password: string }) {
  errorMessage.value = '';
  loading.value = true;
  try {
    const result = await login({
      ...payload,
      // Solo se envía si el usuario ya superó el umbral y resolvió el widget
      captcha: captchaRequired.value ? (captchaToken.value ?? undefined) : undefined,
    });

    // Login correcto: se reinicia el contador de fallos
    failedAttempts.value = 0;
    captchaToken.value = null;

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
    // Cada fallo incrementa el contador; al alcanzar el umbral, la próxima
    // vez el formulario mostrará el CAPTCHA
    failedAttempts.value += 1;
    captchaToken.value = null;
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
        :captcha-required="captchaRequired"
        :forgot-password-to="{ name: 'forgot-password' }"
        @submit="handleSubmit"
        @oauth="handleOAuth"
        @captcha-verified="handleCaptchaVerified"
      />
    </main>
  </div>
</template>