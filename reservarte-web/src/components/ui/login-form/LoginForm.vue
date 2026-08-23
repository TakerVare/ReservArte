<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink, type RouteLocationRaw } from 'vue-router';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import GoogleIcon from '@assets/icons/oauth-google.svg';
import AppleIcon from '@assets/icons/oauth-apple.svg';
import InstagramIcon from '@assets/icons/oauth-instagram.svg';

export type OAuthProvider = 'google' | 'apple' | 'instagram';

const props = withDefaults(
  defineProps<{
    forgotPasswordTo: RouteLocationRaw;
    socialLogin?: boolean;
    loading?: boolean;
    errorMessage?: string;
  }>(),
  {
    socialLogin: false,
    loading: false,
    errorMessage: '',
  }
);

const emit = defineEmits<{
  submit: [payload: { email: string; password: string }];
  oauth: [provider: OAuthProvider];
}>();

const email = ref('');
const password = ref('');

const inputClasses =
  'w-full border border-input bg-background px-4 py-3 font-sans text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]';

function handleSubmit() {
  if (props.loading) return;
  emit('submit', { email: email.value, password: password.value });
}
</script>

<template>
  <form
    class="mx-auto flex w-full max-w-[600px] flex-col gap-4 md:gap-6"
    @submit.prevent="handleSubmit"
  >
    <div class="flex flex-col gap-2">
      <label for="login-email" class="font-sans text-foreground">Usuario</label>
      <input id="login-email" v-model="email" type="email" required :class="inputClasses" />
    </div>

    <div class="flex flex-col gap-2">
      <label for="login-password" class="font-sans text-foreground">Contraseña</label>
      <input
        id="login-password"
        v-model="password"
        type="password"
        required
        :class="inputClasses"
      />
      <RouterLink
        :to="forgotPasswordTo"
        class="self-end font-sans text-sm text-muted-foreground underline-offset-2 hover:text-primary hover:underline"
      >
        ¿Has olvidado tu contraseña?
      </RouterLink>
    </div>

    <Text v-if="errorMessage" size="notes" class="text-destructive">{{ errorMessage }}</Text>

    <Button type="submit" variant="primary" class="w-full" :disabled="loading">
      {{ loading ? 'Entrando…' : 'Entrar' }}
    </Button>

    <template v-if="socialLogin">
      <div class="flex items-center gap-4">
        <span class="h-px flex-1 bg-border" />
        <Text size="notes" class="text-muted-foreground">o continúa con</Text>
        <span class="h-px flex-1 bg-border" />
      </div>

      <Button
        type="button"
        variant="secondary"
        class="w-full"
        :disabled="loading"
        @click="emit('oauth', 'google')"
      >
        <template #icon-start><GoogleIcon class="h-5 w-5" /></template>
        Continuar con Google
      </Button>
      <Button
        type="button"
        variant="secondary"
        class="w-full"
        :disabled="loading"
        @click="emit('oauth', 'apple')"
      >
        <template #icon-start><AppleIcon class="h-5 w-5" /></template>
        Continuar con Apple
      </Button>
      <Button
        type="button"
        variant="secondary"
        class="w-full"
        :disabled="loading"
        @click="emit('oauth', 'instagram')"
      >
        <template #icon-start><InstagramIcon class="h-5 w-5" /></template>
        Continuar con Instagram
      </Button>
    </template>
  </form>
</template>
