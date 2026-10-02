<script setup lang="ts">
import { computed, watch } from 'vue';
import { useField, useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import {
  CONTACT_METHODS,
  CUSTOMER_CATEGORIES,
  type ContactMethod,
  type CustomerCategory,
} from '@features/customers/types/customer.types';
import {
  customerSchema,
  type CustomerFormValues,
} from '@features/customers/validation/customer.schema';

/**
 * Datos de la ficha de cliente (RA-869d7fc51), con el patrón de la ficha de empleado
 * (Figma «Detalle usuario» 387:56778, por indicación de Guillermo): Nombre,
 * Apellidos, Email, Teléfono, Fecha de nacimiento, Categoría, Contacto preferido y
 * Estado, y Guardar/Cancelar. En el alta, los consentimientos RGPD (el de
 * tratamiento de datos, obligatorio); después se gestionan con `CustomerConsents`. Sin tarjeta guardada en el piloto (Fase 7, `869f2gnbm`).
 */
const props = withDefaults(
  defineProps<{
    initial?: Partial<CustomerFormValues>;
    creating?: boolean;
    busy?: boolean;
    serverErrors?: Partial<Record<keyof CustomerFormValues, string>>;
  }>(),
  { initial: undefined, creating: false, busy: false, serverErrors: undefined }
);

const emit = defineEmits<{ submit: [values: CustomerFormValues]; cancel: [] }>();

const { t } = useI18n();

const empty = (): CustomerFormValues => ({
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  birthDate: '',
  category: 'new',
  preferredContactMethod: 'email',
  isActive: true,
  creating: props.creating,
  consentDataProcessing: false,
  consentMarketing: false,
  consentPhotos: false,
  consentWhatsapp: false,
});

const { handleSubmit, resetForm, setFieldError } = useForm({
  validationSchema: toTypedSchema(customerSchema),
  initialValues: { ...empty(), ...props.initial },
});

const { value: firstName, errorMessage: firstNameError } = useField<string>('firstName');
const { value: lastName, errorMessage: lastNameError } = useField<string>('lastName');
const { value: email, errorMessage: emailError } = useField<string>('email');
const { value: phone, errorMessage: phoneError } = useField<string>('phone');
const { value: birthDate, errorMessage: birthDateError } = useField<string>('birthDate');
const { value: category } = useField<CustomerCategory>('category');
const { value: contact, errorMessage: contactError } =
  useField<ContactMethod>('preferredContactMethod');
const { value: isActive } = useField<boolean>('isActive');
const { value: consentDataProcessing, errorMessage: consentError } =
  useField<boolean>('consentDataProcessing');
const { value: consentMarketing } = useField<boolean>('consentMarketing');
const { value: consentPhotos } = useField<boolean>('consentPhotos');
const { value: consentWhatsapp } = useField<boolean>('consentWhatsapp');

watch(
  () => props.initial,
  (initial) => resetForm({ values: { ...empty(), ...initial } })
);

watch(
  () => props.serverErrors,
  (serverErrors) => {
    for (const [field, message] of Object.entries(serverErrors ?? {})) {
      setFieldError(field as keyof CustomerFormValues, message);
    }
  }
);

const categoryOptions = computed(() =>
  CUSTOMER_CATEGORIES.map((value) => ({ value, label: t(`customers.categories.${value}`) }))
);
const categoryModel = computed({
  get: () => category.value,
  set: (value: string | undefined) => {
    category.value = (value as CustomerCategory) ?? 'new';
  },
});
const contactOptions = computed(() =>
  CONTACT_METHODS.map((value) => ({ value, label: t(`customers.contactMethods.${value}`) }))
);
const contactModel = computed({
  get: () => contact.value,
  set: (value: string | undefined) => {
    contact.value = (value as ContactMethod) ?? 'email';
  },
});
const statusOptions = computed(() => [
  { value: 'active', label: t('customers.form.active') },
  { value: 'inactive', label: t('customers.form.inactive') },
]);
const statusModel = computed({
  get: () => (isActive.value ? 'active' : 'inactive'),
  set: (value: string | undefined) => {
    isActive.value = value !== 'inactive';
  },
});

const consentBoxes = [
  { key: 'data_processing', model: consentDataProcessing },
  { key: 'marketing', model: consentMarketing },
  { key: 'photos', model: consentPhotos },
  { key: 'whatsapp', model: consentWhatsapp },
] as const;

const onSubmit = handleSubmit((values) => {
  if (props.busy) return;
  emit('submit', values as CustomerFormValues);
});

const labelClasses =
  'font-sans text-[18px] font-bold leading-[normal] tracking-[0.01em] text-foreground';
const optionalClasses = 'font-sans text-[14px] font-normal text-muted-foreground';
const errorClasses = 'text-destructive';
const checkboxClasses = 'mt-0.5 h-5 w-5 shrink-0 accent-[hsl(var(--primary))]';
</script>

<template>
  <form class="flex w-full flex-col gap-4" novalidate @submit.prevent="onSubmit">
    <Text as="h2" size="h3" class="py-2.5">{{ t('customers.form.legend') }}</Text>

    <div class="flex flex-col gap-2">
      <label for="cus-firstname" :class="labelClasses">{{ t('customers.form.firstName') }}</label>
      <Input
        id="cus-firstname"
        v-model="firstName"
        autocomplete="off"
        :invalid="!!firstNameError"
        :aria-describedby="firstNameError ? 'cus-firstname-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="firstNameError" id="cus-firstname-error" size="notes" :class="errorClasses">
        {{ firstNameError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="cus-lastname" :class="labelClasses">{{ t('customers.form.lastName') }}</label>
      <Input
        id="cus-lastname"
        v-model="lastName"
        autocomplete="off"
        :invalid="!!lastNameError"
        :aria-describedby="lastNameError ? 'cus-lastname-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="lastNameError" id="cus-lastname-error" size="notes" :class="errorClasses">
        {{ lastNameError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="cus-email" :class="labelClasses">{{ t('customers.form.email') }}</label>
      <Input
        id="cus-email"
        v-model="email"
        type="email"
        autocomplete="off"
        :invalid="!!emailError"
        :aria-describedby="emailError ? 'cus-email-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="emailError" id="cus-email-error" size="notes" :class="errorClasses">
        {{ emailError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="cus-phone" :class="labelClasses">
        {{ t('customers.form.phone') }}
        <span :class="optionalClasses">{{ t('customers.form.optional') }}</span>
      </label>
      <Input
        id="cus-phone"
        v-model="phone"
        type="tel"
        autocomplete="off"
        :invalid="!!phoneError"
        :aria-describedby="phoneError ? 'cus-phone-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="phoneError" id="cus-phone-error" size="notes" :class="errorClasses">
        {{ phoneError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="cus-birthdate" :class="labelClasses">
        {{ t('customers.form.birthDate') }}
        <span :class="optionalClasses">{{ t('customers.form.optional') }}</span>
      </label>
      <Input
        id="cus-birthdate"
        v-model="birthDate"
        type="date"
        :invalid="!!birthDateError"
        :aria-describedby="birthDateError ? 'cus-birthdate-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="birthDateError" id="cus-birthdate-error" size="notes" :class="errorClasses">
        {{ birthDateError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="cus-category" :class="labelClasses">{{ t('customers.form.category') }}</label>
      <Select
        id="cus-category"
        v-model="categoryModel"
        :options="categoryOptions"
        :disabled="busy"
      />
    </div>

    <div class="flex flex-col gap-2">
      <label for="cus-contact" :class="labelClasses">{{ t('customers.form.contact') }}</label>
      <Select
        id="cus-contact"
        v-model="contactModel"
        :options="contactOptions"
        :invalid="!!contactError"
        :disabled="busy"
      />
      <Text v-if="contactError" size="notes" :class="errorClasses">{{ contactError }}</Text>
    </div>

    <div v-if="!creating" class="flex flex-col gap-2">
      <label for="cus-status" :class="labelClasses">{{ t('customers.form.status') }}</label>
      <Select id="cus-status" v-model="statusModel" :options="statusOptions" :disabled="busy" />
    </div>

    <fieldset v-if="creating" class="flex flex-col gap-3 pt-2">
      <legend :class="labelClasses" class="pb-2">{{ t('customers.form.consents.legend') }}</legend>
      <Text size="notes" class="text-muted-foreground">
        {{ t('customers.form.consents.hint') }}
      </Text>
      <label
        v-for="box in consentBoxes"
        :key="box.key"
        class="flex items-start gap-3 font-sans text-foreground"
      >
        <input
          v-model="box.model.value"
          type="checkbox"
          :class="checkboxClasses"
          :disabled="busy"
          :aria-describedby="
            box.key === 'data_processing' && consentError ? 'cus-consent-error' : undefined
          "
        />
        {{ t(`customers.form.consents.${box.key}`) }}
      </label>
      <Text v-if="consentError" id="cus-consent-error" size="notes" :class="errorClasses">
        {{ consentError }}
      </Text>
      <Text size="notes" class="text-muted-foreground">
        {{ t('customers.form.invitationHint') }}
      </Text>
    </fieldset>

    <!-- Como en Figma: Guardar a la izquierda y Cancelar a la derecha. -->
    <div class="flex items-center justify-between gap-4 pt-2">
      <Button type="submit" size="sm" variant="primary" class="min-w-[106px]" :disabled="busy">
        {{ t('customers.form.save') }}
      </Button>
      <Button
        size="sm"
        variant="secondary"
        class="min-w-[106px]"
        :disabled="busy"
        @click="emit('cancel')"
      >
        {{ t('customers.form.cancel') }}
      </Button>
    </div>
  </form>
</template>
