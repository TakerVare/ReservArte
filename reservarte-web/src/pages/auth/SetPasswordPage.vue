<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter, RouterLink } from 'vue-router';
import { Banner } from '@components/ui/banner';
import { SetPasswordForm } from '@components/ui/set-password-form';
import { Text } from '@components/ui/text';
import { setPassword, AuthApiError } from '@features/auth/api/auth.api';
import { Button } from '@components/ui/button';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Alta de empleado (RA-869f17y68): la cuenta se crea sin contraseña y el
 * empleado la establece desde el enlace de la invitación. Es un flujo distinto
 * del restablecimiento —de ahí página y endpoint propios—: el token lo emite el
 * proveedor «Invitation» (7 días) y solo vale una vez.
 */
const route = useRoute();
const router = useRouter();

const loading = ref(false);
const errorMessage = ref('');
const success = ref(false);

// El token llega en la ruta (/set-password/:token). Sin token no hay nada que
// canjear: el enlace es inservible.
const token = computed(() => {
  const raw = route.params.token;
  return typeof raw === 'string' ? raw : '';
});

async function handleSubmit(payload: { email: string; newPassword: string }) {
  errorMessage.value = '';
  loading.value = true;
  try {
    await setPassword({
      email: payload.email,
      token: token.value,
      newPassword: payload.newPassword,
    });
    success.value = true;
  } catch (err) {
    errorMessage.value =
      err instanceof AuthApiError
        ? err.message
        : 'No se pudo crear la contraseña. Inténtalo de nuevo.';
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
            Esta invitación no es válida o está incompleta. Pide en tu centro que te la reenvíen.
          </Text>
          <div class="mt-6">
            <RouterLink
              :to="{ name: 'login' }"
              class="font-sans text-primary underline underline-offset-2 hover:text-primary-hover"
            >
              Ir a iniciar sesión
            </RouterLink>
          </div>
        </template>

        <!-- Éxito: contraseña creada -->
        <template v-else-if="success">
          <Text size="h3" class="mb-4">Contraseña creada</Text>
          <Text size="paragraph" class="text-muted-foreground">
            Ya puedes iniciar sesión con tu email y tu nueva contraseña.
          </Text>
          <Button variant="primary" class="mt-6 w-full" @click="goToLogin">
            Ir a iniciar sesión
          </Button>
        </template>

        <!-- Formulario de alta de contraseña -->
        <template v-else>
          <Text size="h3" class="mb-4">Crea tu contraseña</Text>
          <Text size="paragraph" class="mb-6 text-muted-foreground">
            Te damos la bienvenida. Elige una contraseña para tu cuenta; con ella y tu email
            entrarás a partir de ahora.
          </Text>
          <SetPasswordForm
            :loading="loading"
            :error-message="errorMessage"
            @submit="handleSubmit"
          />
        </template>
      </div>
    </main>
  </div>
</template>
