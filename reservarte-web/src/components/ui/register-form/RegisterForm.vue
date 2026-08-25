<script setup lang="ts">
import { useForm, useField } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { RouterLink } from 'vue-router';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { registerSchema } from '@features/auth/validation/register.schema';

const props = withDefaults(
  defineProps<{
    loading?: boolean;
    errorMessage?: string;
  }>(),
  {
    loading: false,
    errorMessage: '',
  }
);

const emit = defineEmits<{
  submit: [payload: {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    password: string;
    acceptedTerms: boolean;
    acceptedPrivacy: boolean;
  }];
}>();

const { handleSubmit } = useForm({
  validationSchema: toTypedSchema(registerSchema),
});

// Un campo por entrada: value (v-model) + errorMessage (mensaje de Zod).
const { value: firstName, errorMessage: firstNameError } = useField<string>('firstName');
const { value: lastName, errorMessage: lastNameError } = useField<string>('lastName');
const { value: email, errorMessage: emailError } = useField<string>('email');
const { value: phone, errorMessage: phoneError } = useField<string>('phone');
const { value: password, errorMessage: passwordError } = useField<string>('password');
const { value: confirmPassword, errorMessage: confirmPasswordError } =
  useField<string>('confirmPassword');
const { value: acceptedTerms, errorMessage: acceptedTermsError } =
  useField<boolean>('acceptedTerms');
const { value: acceptedPrivacy, errorMessage: acceptedPrivacyError } =
  useField<boolean>('acceptedPrivacy');

const onSubmit = handleSubmit((values) => {
  if (props.loading) return;
  emit('submit', {
    firstName: values.firstName,
    lastName: values.lastName,
    email: values.email,
    phone: values.phone || undefined,
    password: values.password,
    acceptedTerms: values.acceptedTerms,
    acceptedPrivacy: values.acceptedPrivacy,
  });
});

const inputClasses =
  'w-full border border-input bg-background px-4 py-3 font-sans text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]';
const errorClasses = 'text-destructive';
const linkClasses =
  'font-sans text-primary underline underline-offset-2 hover:text-primary-hover';
</script>

<template>
  <form
    class="mx-auto flex w-full max-w-[600px] flex-col gap-4 md:gap-6"
    novalidate
    @submit.prevent="onSubmit"
  >
    <div class="flex flex-col gap-2">
      <label for="reg-firstname" class="font-sans text-foreground">Nombre</label>
      <input id="reg-firstname" v-model="firstName" type="text" :class="inputClasses" />
      <Text v-if="firstNameError" size="notes" :class="errorClasses">{{ firstNameError }}</Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reg-lastname" class="font-sans text-foreground">Apellidos</label>
      <input id="reg-lastname" v-model="lastName" type="text" :class="inputClasses" />
      <Text v-if="lastNameError" size="notes" :class="errorClasses">{{ lastNameError }}</Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reg-email" class="font-sans text-foreground">Email</label>
      <input id="reg-email" v-model="email" type="email" :class="inputClasses" />
      <Text v-if="emailError" size="notes" :class="errorClasses">{{ emailError }}</Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reg-phone" class="font-sans text-foreground">
        Teléfono <span class="text-muted-foreground">(opcional)</span>
      </label>
      <input id="reg-phone" v-model="phone" type="tel" :class="inputClasses" />
      <Text v-if="phoneError" size="notes" :class="errorClasses">{{ phoneError }}</Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reg-password" class="font-sans text-foreground">Contraseña</label>
      <input id="reg-password" v-model="password" type="password" :class="inputClasses" />
      <Text v-if="passwordError" size="notes" :class="errorClasses">{{ passwordError }}</Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="reg-confirm" class="font-sans text-foreground">Repite la contraseña</label>
      <input id="reg-confirm" v-model="confirmPassword" type="password" :class="inputClasses" />
      <Text v-if="confirmPasswordError" size="notes" :class="errorClasses">
        {{ confirmPasswordError }}
      </Text>
    </div>

    <!-- Consentimiento RGPD: dos checkboxes independientes, ambos obligatorios,
         con enlace al documento correspondiente (rutas stub por ahora). -->
    <div class="flex flex-col gap-3">
      <div class="flex flex-col gap-1">
        <label class="flex items-start gap-3 font-sans text-foreground">
          <input
            v-model="acceptedTerms"
            type="checkbox"
            class="mt-1 h-5 w-5 shrink-0 accent-[hsl(var(--primary))]"
          />
          <span>
            Acepto los
            <RouterLink :to="{ name: 'legal-terms' }" target="_blank" :class="linkClasses">
              términos y condiciones</RouterLink>.
          </span>
        </label>
        <Text v-if="acceptedTermsError" size="notes" :class="errorClasses">
          {{ acceptedTermsError }}
        </Text>
      </div>

      <div class="flex flex-col gap-1">
        <label class="flex items-start gap-3 font-sans text-foreground">
          <input
            v-model="acceptedPrivacy"
            type="checkbox"
            class="mt-1 h-5 w-5 shrink-0 accent-[hsl(var(--primary))]"
          />
          <span>
            He leído y acepto la
            <RouterLink :to="{ name: 'legal-privacy' }" target="_blank" :class="linkClasses">
              política de privacidad</RouterLink>.
          </span>
        </label>
        <Text v-if="acceptedPrivacyError" size="notes" :class="errorClasses">
          {{ acceptedPrivacyError }}
        </Text>
      </div>
    </div>

    <Text v-if="errorMessage" size="notes" :class="errorClasses">{{ errorMessage }}</Text>

    <Button type="submit" variant="primary" class="w-full" :disabled="loading">
      {{ loading ? 'Creando cuenta…' : 'Crear cuenta' }}
    </Button>
  </form>
</template>