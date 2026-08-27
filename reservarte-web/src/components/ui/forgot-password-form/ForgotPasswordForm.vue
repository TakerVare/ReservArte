<script setup lang="ts">
import { useForm, useField } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { forgotPasswordSchema } from '@features/auth/validation/forgot-password.schema';

const props = withDefaults(
  defineProps<{
    loading?: boolean;
    errorMessage?: string;
  }>(),
  { loading: false, errorMessage: '' }
);

const emit = defineEmits<{
  submit: [payload: { email: string }];
}>();

const { handleSubmit } = useForm({
  validationSchema: toTypedSchema(forgotPasswordSchema),
});
const { value: email, errorMessage: emailError } = useField<string>('email');

const onSubmit = handleSubmit((values) => {
  if (props.loading) return;
  emit('submit', { email: values.email });
});

const inputClasses =
  'w-full border border-input bg-background px-4 py-3 font-sans text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]';
</script>

<template>
  <form
    class="mx-auto flex w-full max-w-[600px] flex-col gap-4 md:gap-6"
    novalidate
    @submit.prevent="onSubmit"
  >
    <Text size="paragraph" class="text-muted-foreground">
      Introduce tu email y, si está registrado, te enviaremos un enlace para
      restablecer tu contraseña.
    </Text>

    <div class="flex flex-col gap-2">
      <label for="forgot-email" class="font-sans text-foreground">Email</label>
      <input id="forgot-email" v-model="email" type="email" :class="inputClasses" />
      <Text v-if="emailError" size="notes" class="text-destructive">{{ emailError }}</Text>
    </div>

    <Text v-if="errorMessage" size="notes" class="text-destructive">{{ errorMessage }}</Text>

    <Button type="submit" variant="primary" class="w-full" :disabled="loading">
      {{ loading ? 'Enviando…' : 'Enviar enlace' }}
    </Button>
  </form>
</template>