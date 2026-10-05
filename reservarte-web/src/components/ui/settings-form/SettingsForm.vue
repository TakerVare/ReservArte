<script setup lang="ts">
import { computed, watch } from 'vue';
import { useField, useForm } from 'vee-validate';
import { toTypedSchema } from '@vee-validate/zod';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import { TIME_ZONE_OPTIONS } from '@features/organization/types/settings.types';
import {
  CANCELLATION_HOURS,
  NO_SHOWS,
  settingsSchema,
  type SettingsFormValues,
} from '@features/organization/validation/settings.schema';

/**
 * Configuración del centro (RA-869f6r71x), con el estilo de las fichas de la app:
 * zona horaria, umbral de cancelación y máximo de no presentaciones.
 * Presentacional: emite `submit` con los valores validados (VeeValidate + Zod,
 * mismas reglas que la API).
 */
const props = withDefaults(
  defineProps<{
    initial: SettingsFormValues;
    busy?: boolean;
    serverErrors?: Partial<Record<keyof SettingsFormValues, string>>;
  }>(),
  { busy: false, serverErrors: undefined }
);

const emit = defineEmits<{ submit: [values: SettingsFormValues] }>();

const { t, te } = useI18n();

const { handleSubmit, resetForm, setFieldError } = useForm({
  validationSchema: toTypedSchema(settingsSchema),
  initialValues: { ...props.initial },
});

const { value: timeZone, errorMessage: timeZoneError } = useField<string>('timeZone');
const { value: hours, errorMessage: hoursError } = useField<number>('cancellationHoursThreshold');
const { value: noShows, errorMessage: noShowsError } = useField<number>('maxNoShowsBeforeBlock');

watch(
  () => props.initial,
  (initial) => resetForm({ values: { ...initial } })
);

watch(
  () => props.serverErrors,
  (serverErrors) => {
    for (const [field, message] of Object.entries(serverErrors ?? {})) {
      setFieldError(field as keyof SettingsFormValues, message);
    }
  }
);

// La zona guardada sale siempre, aunque no sea de las que se ofrecen.
const timeZoneOptions = computed(() =>
  [...new Set<string>([...TIME_ZONE_OPTIONS, props.initial.timeZone])].map((value) => ({
    value,
    label: te(`settings.timeZones.${value}`) ? t(`settings.timeZones.${value}`) : value,
  }))
);
const timeZoneModel = computed({
  get: () => timeZone.value,
  set: (value: string | undefined) => {
    timeZone.value = value ?? '';
  },
});

const onSubmit = handleSubmit((values) => {
  if (props.busy) return;
  emit('submit', values as SettingsFormValues);
});

const labelClasses =
  'font-sans text-[18px] font-bold leading-[normal] tracking-[0.01em] text-foreground';
const hintClasses = 'text-muted-foreground';
const errorClasses = 'text-destructive';
</script>

<template>
  <form class="flex w-full flex-col gap-6" novalidate @submit.prevent="onSubmit">
    <Text as="h2" size="h3" class="py-2.5">{{ t('settings.form.legend') }}</Text>

    <div class="flex flex-col gap-2">
      <label for="set-time-zone" :class="labelClasses">{{ t('settings.form.timeZone') }}</label>
      <Select
        id="set-time-zone"
        v-model="timeZoneModel"
        :options="timeZoneOptions"
        :invalid="!!timeZoneError"
        :disabled="busy"
      />
      <Text size="notes" :class="hintClasses">{{ t('settings.form.timeZoneHint') }}</Text>
      <Text v-if="timeZoneError" size="notes" role="alert" :class="errorClasses">
        {{ timeZoneError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="set-cancellation" :class="labelClasses">
        {{ t('settings.form.cancellationHours') }}
      </label>
      <Input
        id="set-cancellation"
        v-model="hours"
        type="number"
        :min="CANCELLATION_HOURS.min"
        :max="CANCELLATION_HOURS.max"
        step="1"
        inputmode="numeric"
        :invalid="!!hoursError"
        :aria-describedby="
          hoursError ? 'set-cancellation-error set-cancellation-hint' : 'set-cancellation-hint'
        "
        :disabled="busy"
      />
      <Text id="set-cancellation-hint" size="notes" :class="hintClasses">
        {{ t('settings.form.cancellationHoursHint') }}
      </Text>
      <Text v-if="hoursError" id="set-cancellation-error" size="notes" :class="errorClasses">
        {{ hoursError }}
      </Text>
    </div>

    <div class="flex flex-col gap-2">
      <label for="set-no-shows" :class="labelClasses">{{ t('settings.form.noShows') }}</label>
      <Input
        id="set-no-shows"
        v-model="noShows"
        type="number"
        :min="NO_SHOWS.min"
        :max="NO_SHOWS.max"
        step="1"
        inputmode="numeric"
        :invalid="!!noShowsError"
        :aria-describedby="
          noShowsError ? 'set-no-shows-error set-no-shows-hint' : 'set-no-shows-hint'
        "
        :disabled="busy"
      />
      <Text id="set-no-shows-hint" size="notes" :class="hintClasses">
        {{ t('settings.form.noShowsHint') }}
      </Text>
      <Text v-if="noShowsError" id="set-no-shows-error" size="notes" :class="errorClasses">
        {{ noShowsError }}
      </Text>
    </div>

    <div class="pt-2">
      <Button type="submit" size="sm" variant="primary" class="min-w-[106px]" :disabled="busy">
        {{ t('settings.form.save') }}
      </Button>
    </div>
  </form>
</template>
