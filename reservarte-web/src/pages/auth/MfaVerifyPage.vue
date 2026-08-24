<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { useAuthStore } from '@stores/authStore';
import { verifyMfa, AuthApiError } from '@features/auth/api/auth.api';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

const router = useRouter();
const authStore = useAuthStore();

const code = ref('');
const loading = ref(false);
const errorMessage = ref('');
// Alterna la ayuda entre "código de la app" y "código de recuperación".
// El backend acepta ambos en el mismo campo, así que es solo textual.
const usingRecoveryCode = ref(false);

// Guard de acceso directo: sin ticket en el store (p. ej. alguien navega a
// la URL sin pasar por el login, o recarga la página), no hay nada que
// verificar → vuelta al login.
onMounted(() => {
  if (!authStore.mfaTicket) {
    router.replace({ name: 'login' });
  }
});

const inputClasses =
  'w-full border border-input bg-background px-4 py-3 text-center font-sans text-2xl tracking-[0.5em] text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]';

async function handleSubmit() {
  if (loading.value) return;
  errorMessage.value = '';

  const ticket = authStore.mfaTicket;
  if (!ticket) {
    router.replace({ name: 'login' });
    return;
  }

  loading.value = true;
  try {
    const result = await verifyMfa({ mfaTicket: ticket, code: code.value.trim() });

    // El verify devuelve el par definitivo + el usuario: se completa la
    // sesión pendiente (aquí es donde los tokens entran por fin al store).
    authStore.setMfaVerified({
      user: result.user,
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
    });

    router.push({ name: 'dashboard' });
  } catch (err) {
    errorMessage.value =
      err instanceof AuthApiError
        ? err.message
        : 'No se pudo verificar el código. Inténtalo de nuevo.';
    code.value = '';
  } finally {
    loading.value = false;
  }
}

function toggleRecoveryCode() {
  usingRecoveryCode.value = !usingRecoveryCode.value;
  code.value = '';
  errorMessage.value = '';
}

function cancel() {
  // Descarta la sesión pendiente y vuelve al login limpio
  authStore.logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />

    <main class="flex flex-1 items-center justify-center px-4 py-8">
      <form
        class="mx-auto flex w-full max-w-[600px] flex-col gap-4 md:gap-6"
        @submit.prevent="handleSubmit"
      >
        <div class="flex flex-col gap-2 text-center">
          <Text size="h3" class="text-foreground">Verificación en dos pasos</Text>
          <Text size="notes" class="text-muted-foreground">
            <template v-if="!usingRecoveryCode">
              Introduce el código de 6 dígitos de tu aplicación de autenticación.
            </template>
            <template v-else>
              Introduce uno de tus códigos de recuperación de un solo uso.
            </template>
          </Text>
        </div>

        <div class="flex flex-col gap-2">
          <label for="mfa-code" class="sr-only">
            {{ usingRecoveryCode ? 'Código de recuperación' : 'Código de verificación' }}
          </label>
          <input
            id="mfa-code"
            v-model="code"
            type="text"
            inputmode="text"
            autocomplete="one-time-code"
            :maxlength="usingRecoveryCode ? 11 : 6"
            :placeholder="usingRecoveryCode ? 'xxxxx-xxxxx' : '••••••'"
            required
            autofocus
            :class="inputClasses"
          />
        </div>

        <Text v-if="errorMessage" size="notes" class="text-center text-destructive">
          {{ errorMessage }}
        </Text>

        <Button type="submit" variant="primary" class="w-full" :disabled="loading || !code">
          {{ loading ? 'Verificando…' : 'Verificar' }}
        </Button>

        <div class="flex flex-col items-center gap-2">
          <button
            type="button"
            class="font-sans text-sm text-muted-foreground underline-offset-2 hover:text-primary hover:underline"
            @click="toggleRecoveryCode"
          >
            {{ usingRecoveryCode ? 'Usar el código de la aplicación' : '¿Has perdido el acceso? Usa un código de recuperación' }}
          </button>
          <button
            type="button"
            class="font-sans text-sm text-muted-foreground underline-offset-2 hover:text-primary hover:underline"
            @click="cancel"
          >
            Cancelar y volver al inicio de sesión
          </button>
        </div>
      </form>
    </main>
  </div>
</template>