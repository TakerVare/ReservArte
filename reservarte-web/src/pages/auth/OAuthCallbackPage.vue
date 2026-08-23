<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { Text } from '@components/ui/text';
import { useAuthStore } from '@stores/authStore';
import { fetchCurrentUser } from '@features/auth/api/auth.api';

const router = useRouter();
const authStore = useAuthStore();
const errorMessage = ref('');

// El backend redirige aquí con los tokens en el fragmento de la URL
// (vol. 1 §4.4): #access_token=...&refresh_token=... — nunca en query ni
// en el cuerpo, así no quedan en logs de servidor ni en el historial HTTP.
onMounted(async () => {
  const params = new URLSearchParams(window.location.hash.replace(/^#/, ''));
  const accessToken = params.get('access_token');
  const refreshToken = params.get('refresh_token');
  const oauthError = params.get('error');

  if (oauthError || !accessToken || !refreshToken) {
    errorMessage.value = 'No se pudo completar el inicio de sesión con el proveedor externo.';
    setTimeout(() => router.push({ name: 'login' }), 2000);
    return;
  }

  authStore.login({ accessToken, refreshToken, mfaRequired: false });

  // El fragmento solo trae tokens, no el usuario: se completa con /me.
  // Si falla, la sesión ya ha quedado iniciada igualmente (los datos de
  // perfil se podrán recargar más adelante).
  try {
    const claims = await fetchCurrentUser();
    authStore.$patch({
      user: {
        id: Number(claims.id) || 0,
        email: claims.email,
        firstName: '',
        lastName: '',
        rol: claims.role,
      },
    });
  } catch {
    // Ver comentario anterior: no bloquea la sesión ya iniciada.
  }

  router.push('/');
});
</script>

<template>
  <div class="flex min-h-screen items-center justify-center">
    <Text v-if="errorMessage" size="paragraph" class="text-destructive">{{ errorMessage }}</Text>
    <Text v-else size="paragraph" class="text-muted-foreground">Iniciando sesión…</Text>
  </div>
</template>
