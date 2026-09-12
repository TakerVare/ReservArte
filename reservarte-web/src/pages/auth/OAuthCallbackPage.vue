<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Text } from '@components/ui/text';
import { useAuthStore } from '@stores/authStore';
import { fetchCurrentUser } from '@features/auth/api/auth.api';

const router = useRouter();
const authStore = useAuthStore();

/**
 * Destino del reto OAuth: es la `returnUrl` que el backend recibe en el
 * challenge y la pantalla a la que redirige al volver del proveedor.
 *
 * Los tokens llegan en el FRAGMENTO de la URL (vol. 1 §4.4;
 * `ExternalAuthController.Callback`), nunca en query ni en el cuerpo, para
 * que no queden en logs de servidor ni en el historial HTTP:
 *
 *   éxito → #access_token=...&refresh_token=...
 *   fallo → #error=<código>
 *
 * Sobre 2FA: el criterio original de la tarea contemplaba un caso
 * `mfaRequired` aquí, pero el contrato real del backend no lo emite — el
 * callback externo devuelve siempre el par de tokens definitivo o un error.
 * Que un usuario con 2FA activa entre por OAuth sin segundo factor es una
 * carencia del backend, no de esta pantalla; queda anotada en el backlog.
 */
function consumeFragment(): URLSearchParams {
  const params = new URLSearchParams(window.location.hash.replace(/^#/, ''));
  // Los tokens se retiran de la barra de direcciones en cuanto se leen: no
  // deben sobrevivir en el historial ni en un copiar/pegar de la URL.
  window.history.replaceState(null, '', window.location.pathname);
  return params;
}

onMounted(async () => {
  const params = consumeFragment();
  const accessToken = params.get('access_token');
  const refreshToken = params.get('refresh_token');
  const oauthError = params.get('error');

  if (oauthError || !accessToken || !refreshToken) {
    // El código concreto del proveedor no se expone al usuario: LoginPage
    // traduce `oauth_failed` a un mensaje genérico. `replace` (y no `push`)
    // para que el botón Atrás no devuelva a este callback ya consumido.
    router.replace({ name: 'login', query: { error: 'oauth_failed' } });
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

  router.replace('/');
});
</script>

<template>
  <div class="flex min-h-screen items-center justify-center">
    <Text size="paragraph" class="text-muted-foreground" role="status">Iniciando sesión…</Text>
  </div>
</template>
