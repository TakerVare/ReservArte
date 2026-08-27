<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter, RouterLink } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { ResetPasswordForm } from '@components/ui/reset-password-form';
import { Text } from '@components/ui/text';
import { resetPassword, AuthApiError } from '@features/auth/api/auth.api';
import { Button } from '@components/ui/button';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

const route = useRoute();
const router = useRouter();

const loading = ref(false);
const errorMessage = ref('');
const success = ref(false);

// El token llega en la ruta (/reset-password/:token). Sin token válido, no se
// muestra el formulario: el enlace es inservible.
const token = computed(() => {
  const raw = route.params.token;
  return typeof raw === 'string' ? raw : '';
});

async function handleSubmit(payload: { email: string; newPassword: string }) {
  errorMessage.value = '';
  loading.value = true;
  try {
    await resetPassword({
      email: payload.email,
      token: token.value,
      newPassword: payload.newPassword,
    });
    success.value = true;
  } catch (err) {
    errorMessage.value =
      err instanceof AuthApiError
        ? err.message
        : 'No se pudo restablecer la contraseña. Inténtalo de nuevo.';
  } finally {
    loading.value = false;
  }
}

function goToLogin() {
  router.push({ name: 'login' });
}
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex flex-1 items-center justify-center px-4 py-8">
      <div class="w-full max-w-[600px]">
        <!-- Sin token: enlace no válido -->
        <template v-if="!token">
          <Text size="h3" class="mb-4">Enlace no válido</Text>
          <Text size="paragraph" class="text-muted-foreground">
            Este enlace de restablecimiento no es válido o está incompleto.
            Solicita uno nuevo desde la página de recuperación.
          </Text>
          <div class="mt-6">
            <RouterLink
              :to="{ name: 'forgot-password' }"
              class="font-sans text-primary underline underline-offset-2 hover:text-primary-hover"
            >
              Solicitar un nuevo enlace
            </RouterLink>
          </div>
        </template>

        <!-- Éxito: contraseña cambiada -->
        <template v-else-if="success">
          <Text size="h3" class="mb-4">Contraseña actualizada</Text>
          <Text size="paragraph" class="text-muted-foreground">
            Tu contraseña se ha cambiado correctamente. Ya puedes iniciar sesión
            con la nueva.
          </Text>
          <Button variant="primary" class="mt-6 w-full" @click="goToLogin">
            Ir a iniciar sesión
          </Button>
        </template>

        <!-- Formulario de reset -->
        <template v-else>
          <Text size="h3" class="mb-4">Nueva contraseña</Text>
          <ResetPasswordForm
            :loading="loading"
            :error-message="errorMessage"
            @submit="handleSubmit"
          />
        </template>
      </div>
    </main>
  </div>
</template>