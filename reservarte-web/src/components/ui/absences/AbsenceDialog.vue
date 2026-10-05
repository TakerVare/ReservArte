<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';
import { Input } from '@components/ui/input';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import {
  ABSENCE_TYPES,
  type AbsenceInput,
  type AbsenceType,
} from '@features/employees/types/employee.types';
import { absenceToUtc } from '@features/employees/utils/absence-dates';

/**
 * Alta de una ausencia (RA-869d7fc0h): tipo, días (con el último incluido) y, si no
 * son días completos, horas. Las fechas se piden en hora del centro y se emiten en
 * UTC, como las quiere la API. Cada vez que se abre empieza en blanco.
 */
const props = withDefaults(
  defineProps<{
    /** Zona del centro (IANA), en la que se piden las fechas. */
    timeZone: string;
    busy?: boolean;
  }>(),
  { busy: false }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{ confirm: [input: AbsenceInput] }>();

const { t } = useI18n();

const type = ref<AbsenceType>('vacation');
const from = ref('');
const to = ref('');
const allDay = ref(true);
const startTime = ref('10:00');
const endTime = ref('14:00');
const reason = ref('');
const error = ref<string | null>(null);

watch(open, (isOpen) => {
  if (!isOpen) return;
  type.value = 'vacation';
  from.value = '';
  to.value = '';
  allDay.value = true;
  startTime.value = '10:00';
  endTime.value = '14:00';
  reason.value = '';
  error.value = null;
});

// El último día propone el primero: la mayoría de ausencias de un día se dan de alta así.
watch(from, (value) => {
  if (value && (!to.value || to.value < value)) to.value = value;
});

const typeOptions = computed(() =>
  ABSENCE_TYPES.map((value) => ({ value, label: t(`employees.absences.types.${value}`) }))
);
const typeModel = computed({
  get: () => type.value,
  set: (value: string | undefined) => {
    type.value = (value as AbsenceType) ?? 'vacation';
  },
});

function submit() {
  if (!from.value || !to.value || (!allDay.value && (!startTime.value || !endTime.value))) {
    error.value = t('employees.absences.dialog.errors.required');
    return;
  }
  const range = absenceToUtc(
    {
      from: from.value,
      to: to.value,
      allDay: allDay.value,
      startTime: startTime.value,
      endTime: endTime.value,
    },
    props.timeZone
  );
  if (Date.parse(range.endDateTime) <= Date.parse(range.startDateTime)) {
    error.value = t('employees.absences.dialog.errors.order');
    return;
  }
  if (reason.value.trim().length > 500) {
    error.value = t('employees.absences.dialog.errors.reason');
    return;
  }
  error.value = null;
  emit('confirm', { ...range, type: type.value, reason: reason.value.trim() || null });
}

const labelClasses = 'font-sans text-[14px] text-foreground';
</script>

<template>
  <Dialog v-model:open="open" :title="t('employees.absences.dialog.title')">
    <form id="absence-form" class="flex flex-col gap-4" novalidate @submit.prevent="submit">
      <div class="flex flex-col gap-1">
        <label for="absence-type" :class="labelClasses">{{
          t('employees.absences.dialog.type')
        }}</label>
        <Select id="absence-type" v-model="typeModel" :options="typeOptions" :disabled="busy" />
      </div>

      <div class="flex gap-4">
        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <label for="absence-from" :class="labelClasses">{{
            t('employees.absences.dialog.from')
          }}</label>
          <Input id="absence-from" v-model="from" type="date" class="px-2" :disabled="busy" />
        </div>
        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <label for="absence-to" :class="labelClasses">{{
            t('employees.absences.dialog.to')
          }}</label>
          <Input
            id="absence-to"
            v-model="to"
            type="date"
            class="px-2"
            :min="from || undefined"
            :disabled="busy"
          />
        </div>
      </div>

      <label class="flex items-center gap-3 font-sans text-foreground">
        <input
          v-model="allDay"
          type="checkbox"
          class="h-5 w-5 shrink-0 accent-[hsl(var(--primary))]"
          :disabled="busy"
        />
        {{ t('employees.absences.dialog.allDay') }}
      </label>

      <div v-if="!allDay" class="flex gap-4">
        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <label for="absence-start" :class="labelClasses">{{
            t('employees.absences.dialog.startTime')
          }}</label>
          <Input id="absence-start" v-model="startTime" type="time" class="px-2" :disabled="busy" />
        </div>
        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <label for="absence-end" :class="labelClasses">{{
            t('employees.absences.dialog.endTime')
          }}</label>
          <Input id="absence-end" v-model="endTime" type="time" class="px-2" :disabled="busy" />
        </div>
      </div>

      <div class="flex flex-col gap-1">
        <label for="absence-reason" :class="labelClasses">
          {{ t('employees.absences.dialog.reason') }}
          <span class="text-muted-foreground">{{ t('employees.form.optional') }}</span>
        </label>
        <Input id="absence-reason" v-model="reason" maxlength="500" :disabled="busy" />
      </div>

      <Text v-if="error" size="notes" class="text-destructive" role="alert">{{ error }}</Text>
    </form>

    <template #footer>
      <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
        {{ t('employees.absences.dialog.cancel') }}
      </Button>
      <Button size="sm" variant="primary" type="submit" form="absence-form" :disabled="busy">
        {{ t('employees.absences.dialog.save') }}
      </Button>
    </template>
  </Dialog>
</template>
