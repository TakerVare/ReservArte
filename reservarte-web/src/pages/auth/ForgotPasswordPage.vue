<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { ForgotPasswordForm } from '@components/ui/forgot-password-form';
import { Text } from '@components/ui/text';
import { forgotPassword, AuthApiError } from '@features/auth/api/auth.api';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

const loading = ref(false);
const errorMessage = ref('');
const submitted = ref(false);

async function handleSubmit(payload: { email: string }) {
  errorMessage.value = '';
  loading.value = true;
  try {
    await forgotPassword(payload.email);
    // Anti-enumeración: se muestra el mismo mensaje exista o no el email.
    submitted.value = true;
  } catch (err) {
    errorMessage.value =
      err instanceof AuthApiError
        ? err.message
        : 'No se pudo procesar la solicitud. Inténtalo de nuevo.';
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex flex-1 items-center justify-center px-4 py-8">
      <div class="w-full max-w-[600px]">
        <template v-if="submitted">
          <Text size="h3" class="mb-4">Revisa tu correo</Text>
          <Text size="paragraph" class="text-muted-foreground">
            Si ese email está registrado, recibirás un enlace para restablecer tu
            contraseña. Revisa tu bandeja de entrada (y la carpeta de spam).
          </Text>
        </template>
        <ForgotPasswordForm
          v-else
          :loading="loading"
          :error-message="errorMessage"
          @submit="handleSubmit"
        />
        <div class="mt-6 text-center">
          <RouterLink
            :to="{ name: 'login' }"
            class="font-sans text-sm text-muted-foreground underline-offset-2 hover:text-primary hover:underline"
          >
            Volver a iniciar sesión
          </RouterLink>
        </div>
      </div>
    </main>
  </div>
</template>