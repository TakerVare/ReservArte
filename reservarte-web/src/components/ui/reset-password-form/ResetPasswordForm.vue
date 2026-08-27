<script setup lang="ts">
import { useForm, useField } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { resetPasswordSchema } from '@features/auth/validation/reset-password.schema';

const props = withDefaults(
  defineProps<{
    loading?: boolean;
    errorMessage?: string;
  }>(),
  { loading: false, errorMessage: '' }
);

const emit = defineEmits<{
  submit: [payload: { email: string; newPassword: string }];
}>();

const { handleSubmit } = useForm({
  validationSchema: toTypedSchema(resetPasswordSchema),
});
const { value: email, errorMessage: emailError } = useField<string>('email');
const { value: newPassword, errorMessage: newPasswordError } = useField<string>('newPassword');
const { value: confirmPassword, errorMessage: confirmPasswordError } =
  useField<string>('confirmPassword');

const onSubmit = handleSubmit((values) => {
  if (props.loading) return;
  emit('submit', { email: values.email, newPassword: values.newPassword });
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
    <div class="flex flex-col gap-2">
      <label for="reset-email" class="font-sans text-foreground">Email</label>
      <input id="reset-email" v-model="email" type="email" :class="inputClasses" />
      <Text v-if="emailError" size="notes" class="text-destructive">{{ emailError }}</Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reset-password" class="font-sans text-foreground">Nueva contraseña</label>
      <input id="reset-password" v-model="newPassword" type="password" :class="inputClasses" />
      <Text v-if="newPasswordError" size="notes" class="text-destructive">
        {{ newPasswordError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reset-confirm" class="font-sans text-foreground">Repite la contraseña</label>
      <input id="reset-confirm" v-model="confirmPassword" type="password" :class="inputClasses" />
      <Text v-if="confirmPasswordError" size="notes" class="text-destructive">
        {{ confirmPasswordError }}
      </Text>
    </div>

    <Text v-if="errorMessage" size="notes" class="text-destructive">{{ errorMessage }}</Text>

    <Button type="submit" variant="primary" class="w-full" :disabled="loading">
      {{ loading ? 'Guardando…' : 'Cambiar contraseña' }}
    </Button>
  </form>
</template>