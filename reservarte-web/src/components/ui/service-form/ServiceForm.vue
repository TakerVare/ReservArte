<script setup lang="ts">
import { computed, watch } from 'vue';
import { useField, useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useI18n } from 'vue-i18n';
import { Plus } from 'lucide-vue-next';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import type { ServiceCategory } from '@features/services/types/service.types';
import {
  serviceSchema,
  type ServiceFormValues,
} from '@features/services/validation/service.schema';

/**
 * Datos de un servicio del catálogo (RA-869d7fc6b), con el estilo de las fichas de la
 * app: nombre, descripción, categoría (con «Nueva categoría»), duración, precio base,
 * prueba de alergia con su antelación y estado. Presentacional: emite `submit` con
 * los valores validados (VeeValidate + Zod, mismas reglas que la API).
 */
const props = withDefaults(
  defineProps<{
    initial?: Partial<ServiceFormValues>;
    categories: ServiceCategory[];
    creating?: boolean;
    busy?: boolean;
    serverErrors?: Partial<Record<keyof ServiceFormValues, string>>;
  }>(),
  { initial: undefined, creating: false, busy: false, serverErrors: undefined }
);

const emit = defineEmits<{
  submit: [values: ServiceFormValues];
  cancel: [];
  'new-category': [];
}>();

const { t } = useI18n();

const NONE = 'none';
const EMPTY: ServiceFormValues = {
  name: '',
  description: '',
  categoryId: NONE,
  durationMinutes: 30,
  basePrice: 0,
  requiresAllergyTest: false,
  allergyTestHoursBefore: 48,
  isActive: true,
};

const { handleSubmit, resetForm, setFieldError, setFieldValue } = useForm({
  validationSchema: toTypedSchema(serviceSchema),
  initialValues: { ...EMPTY, ...props.initial },
});

const { value: name, errorMessage: nameError } = useField<string>('name');
const { value: description, errorMessage: descriptionError } = useField<string>('description');
const { value: categoryId, errorMessage: categoryError } = useField<string>('categoryId');
const { value: duration, errorMessage: durationError } = useField<number>('durationMinutes');
const { value: price, errorMessage: priceError } = useField<number>('basePrice');
const { value: requiresAllergyTest } = useField<boolean>('requiresAllergyTest');
const { value: allergyHours, errorMessage: allergyHoursError } =
  useField<number>('allergyTestHoursBefore');
const { value: isActive } = useField<boolean>('isActive');

watch(
  () => props.initial,
  (initial) => resetForm({ values: { ...EMPTY, ...initial } })
);

watch(
  () => props.serverErrors,
  (serverErrors) => {
    for (const [field, message] of Object.entries(serverErrors ?? {})) {
      setFieldError(field as keyof ServiceFormValues, message);
    }
  }
);

/** La página lo llama tras crear una categoría, para dejarla elegida. */
function selectCategory(id: number) {
  setFieldValue('categoryId', String(id));
}
defineExpose({ selectCategory });

// Las retiradas solo salen si son la del servicio: no se ofrecen para uno nuevo.
const categoryOptions = computed(() => [
  { value: NONE, label: t('services.noCategory') },
  ...props.categories
    .filter((c) => c.isActive || String(c.id) === categoryId.value)
    .map((c) => ({ value: String(c.id), label: c.name })),
]);
const categoryModel = computed({
  get: () => categoryId.value,
  set: (value: string | undefined) => {
    categoryId.value = value ?? NONE;
  },
});
const statusOptions = computed(() => [
  { value: 'active', label: t('services.form.active') },
  { value: 'inactive', label: t('services.form.inactive') },
]);
const statusModel = computed({
  get: () => (isActive.value ? 'active' : 'inactive'),
  set: (value: string | undefined) => {
    isActive.value = value !== 'inactive';
  },
});

const onSubmit = handleSubmit((values) => {
  if (props.busy) return;
  emit('submit', values as ServiceFormValues);
});

const labelClasses =
  'font-sans text-[18px] font-bold leading-[normal] tracking-[0.01em] text-foreground';
const optionalClasses = 'font-sans text-[14px] font-normal text-muted-foreground';
const errorClasses = 'text-destructive';
</script>

<template>
  <form class="flex w-full flex-col gap-4" novalidate @submit.prevent="onSubmit">
    <Text as="h2" size="h3" class="py-2.5">{{ t('services.form.legend') }}</Text>

    <div class="flex flex-col gap-2">
      <label for="srv-name" :class="labelClasses">{{ t('services.form.name') }}</label>
      <Input
        id="srv-name"
        v-model="name"
        :invalid="!!nameError"
        :aria-describedby="nameError ? 'srv-name-error' : undefined"
        :disabled="busy"
      />
      <Text v-if="nameError" id="srv-name-error" size="notes" :class="errorClasses">
        {{ nameError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="srv-description" :class="labelClasses">
        {{ t('services.form.description') }}
        <span :class="optionalClasses">{{ t('services.form.optional') }}</span>
      </label>
      <textarea
        id="srv-description"
        v-model="description"
        rows="3"
        :disabled="busy"
        :aria-invalid="!!descriptionError || undefined"
        :aria-describedby="descriptionError ? 'srv-description-error' : undefined"
        class="w-full resize-y border border-input bg-background px-4 py-3 font-sans text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)] disabled:cursor-not-allowed disabled:opacity-60"
      />
      <Text v-if="descriptionError" id="srv-description-error" size="notes" :class="errorClasses">
        {{ descriptionError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="srv-category" :class="labelClasses">{{ t('services.form.category') }}</label>
      <Select
        id="srv-category"
        v-model="categoryModel"
        :options="categoryOptions"
        :invalid="!!categoryError"
        :disabled="busy"
      />
      <Button
        size="xs"
        variant="secondary"
        class="self-start"
        :disabled="busy"
        @click="emit('new-category')"
      >
        <template #icon-start><Plus class="h-3 w-3" aria-hidden="true" /></template>
        {{ t('services.form.newCategory') }}
      </Button>
      <Text v-if="categoryError" size="notes" :class="errorClasses">{{ categoryError }}</Text>
    </div>

    <div class="flex gap-4">
      <div class="flex min-w-0 flex-1 flex-col gap-2">
        <label for="srv-duration" :class="labelClasses">{{ t('services.form.duration') }}</label>
        <Input
          id="srv-duration"
          v-model="duration"
          type="number"
          min="1"
          step="5"
          inputmode="numeric"
          :invalid="!!durationError"
          :aria-describedby="durationError ? 'srv-duration-error' : undefined"
          :disabled="busy"
        />
        <Text v-if="durationError" id="srv-duration-error" size="notes" :class="errorClasses">
          {{ durationError }}
        </Text>
      </div>
      <div class="flex min-w-0 flex-1 flex-col gap-2">
        <label for="srv-price" :class="labelClasses">{{ t('services.form.price') }}</label>
        <Input
          id="srv-price"
          v-model="price"
          type="number"
          min="0"
          step="0.5"
          inputmode="decimal"
          :invalid="!!priceError"
          :aria-describedby="priceError ? 'srv-price-error' : undefined"
          :disabled="busy"
        />
        <Text v-if="priceError" id="srv-price-error" size="notes" :class="errorClasses">
          {{ priceError }}
        </Text>
      </div>
    </div>

    <label class="flex items-center gap-3 font-sans text-foreground">
      <input
        v-model="requiresAllergyTest"
        type="checkbox"
        class="h-5 w-5 shrink-0 accent-[hsl(var(--primary))]"
        :disabled="busy"
      />
      {{ t('services.form.allergy') }}
    </label>

    <div v-if="requiresAllergyTest" class="flex flex-col gap-2">
      <label for="srv-allergy-hours" :class="labelClasses">
        {{ t('services.form.allergyHours') }}
      </label>
      <Input
        id="srv-allergy-hours"
        v-model="allergyHours"
        type="number"
        min="1"
        inputmode="numeric"
        :invalid="!!allergyHoursError"
        :aria-describedby="allergyHoursError ? 'srv-allergy-hours-error' : undefined"
        :disabled="busy"
      />
      <Text
        v-if="allergyHoursError"
        id="srv-allergy-hours-error"
        size="notes"
        :class="errorClasses"
      >
        {{ allergyHoursError }}
      </Text>
    </div>

    <div v-if="!creating" class="flex flex-col gap-2">
      <label for="srv-status" :class="labelClasses">{{ t('services.form.status') }}</label>
      <Select id="srv-status" v-model="statusModel" :options="statusOptions" :disabled="busy" />
    </div>

    <div class="flex items-center justify-between gap-4 pt-2">
      <Button type="submit" size="sm" variant="primary" class="min-w-[106px]" :disabled="busy">
        {{ t('services.form.save') }}
      </Button>
      <Button
        size="sm"
        variant="secondary"
        class="min-w-[106px]"
        :disabled="busy"
        @click="emit('cancel')"
      >
        {{ t('services.form.cancel') }}
      </Button>
    </div>
  </form>
</template>
