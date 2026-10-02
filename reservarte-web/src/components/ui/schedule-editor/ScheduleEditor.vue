<script setup lang="ts">
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Plus, X } from 'lucide-vue-next';
import { Button } from '@components/ui/button';
import { Input } from '@components/ui/input';
import { Text } from '@components/ui/text';
import {
  copyToWeekdays,
  nextRange,
  validateDay,
  type ScheduleDay,
} from '@features/employees/utils/schedule';

/**
 * Horario semanal de un empleado (RA-869d7fc0h; sin diseño en Figma, con el estilo
 * de la app). Un bloque por día, de lunes a domingo: casilla «trabaja» y sus tramos
 * (jornada partida = dos tramos). Valida como el backend (fin posterior al inicio y
 * sin solapes) y solo emite `save` si todo cuadra; la semana entera se guarda de una
 * vez, porque la API la reemplaza completa.
 */
withDefaults(defineProps<{ busy?: boolean }>(), { busy: false });

const days = defineModel<ScheduleDay[]>({ required: true });
const emit = defineEmits<{ save: [days: ScheduleDay[]] }>();

const { t } = useI18n();

const attempted = ref(false);
const errors = computed(() => days.value.map((day) => validateDay(day)));
const hasErrors = computed(() => errors.value.some(Boolean));

const dayName = (dayOfWeek: number) => t(`employees.schedule.days.${dayOfWeek}`);

function update(index: number, change: (day: ScheduleDay) => ScheduleDay) {
  days.value = days.value.map((day, i) => (i === index ? change(day) : day));
}

function toggle(index: number, works: boolean) {
  update(index, (day) => ({
    ...day,
    works,
    ranges: works && day.ranges.length === 0 ? [nextRange([])] : day.ranges,
  }));
}

function addRange(index: number) {
  update(index, (day) => ({ ...day, ranges: [...day.ranges, nextRange(day.ranges)] }));
}

function removeRange(index: number, rangeIndex: number) {
  update(index, (day) => ({ ...day, ranges: day.ranges.filter((_, i) => i !== rangeIndex) }));
}

function setTime(index: number, rangeIndex: number, field: 'start' | 'end', value: unknown) {
  update(index, (day) => ({
    ...day,
    ranges: day.ranges.map((range, i) =>
      i === rangeIndex ? { ...range, [field]: String(value ?? '') } : range
    ),
  }));
}

function copyMonday() {
  days.value = copyToWeekdays(days.value, 0);
}

function save() {
  attempted.value = true;
  if (hasErrors.value) return;
  emit('save', days.value);
}

const checkboxClasses = 'h-5 w-5 shrink-0 accent-[hsl(var(--primary))]';
const iconButtonClasses =
  'flex h-11 w-11 shrink-0 items-center justify-center text-primary outline-none transition-colors hover:text-primary-hover focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50';
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
      <Text as="h2" size="h3">{{ t('employees.schedule.title') }}</Text>
      <Text size="notes" class="text-muted-foreground">{{ t('employees.schedule.hint') }}</Text>
    </div>

    <Button size="xs" variant="secondary" class="self-start" :disabled="busy" @click="copyMonday">
      {{ t('employees.schedule.copyMonday') }}
    </Button>

    <ul class="flex flex-col border-b border-border">
      <li
        v-for="(day, index) in days"
        :key="day.dayOfWeek"
        class="flex flex-col gap-3 border-t border-border py-4"
        :data-testid="`schedule-day-${day.dayOfWeek}`"
      >
        <div class="flex items-center justify-between gap-4">
          <label class="flex items-center gap-3">
            <input
              type="checkbox"
              :class="checkboxClasses"
              :checked="day.works"
              :disabled="busy"
              :aria-label="t('employees.schedule.works', { day: dayName(day.dayOfWeek) })"
              @change="toggle(index, ($event.target as HTMLInputElement).checked)"
            />
            <Text as="span" size="h4">{{ dayName(day.dayOfWeek) }}</Text>
          </label>
          <Text v-if="!day.works" as="span" size="notes" class="text-muted-foreground">
            {{ t('employees.schedule.dayOff') }}
          </Text>
        </div>

        <template v-if="day.works">
          <div
            v-for="(range, rangeIndex) in day.ranges"
            :key="rangeIndex"
            role="group"
            :aria-label="
              t('employees.schedule.rangeLabel', { day: dayName(day.dayOfWeek), n: rangeIndex + 1 })
            "
            class="flex items-end gap-2 pl-8"
          >
            <div class="flex min-w-0 flex-1 flex-col gap-1">
              <label
                :for="`schedule-${day.dayOfWeek}-${rangeIndex}-start`"
                class="font-sans text-[14px] text-foreground"
              >
                {{ t('employees.schedule.from') }}
              </label>
              <Input
                :id="`schedule-${day.dayOfWeek}-${rangeIndex}-start`"
                type="time"
                :model-value="range.start"
                :invalid="attempted && !!errors[index]"
                :disabled="busy"
                class="px-2"
                @update:model-value="setTime(index, rangeIndex, 'start', $event)"
              />
            </div>
            <div class="flex min-w-0 flex-1 flex-col gap-1">
              <label
                :for="`schedule-${day.dayOfWeek}-${rangeIndex}-end`"
                class="font-sans text-[14px] text-foreground"
              >
                {{ t('employees.schedule.to') }}
              </label>
              <Input
                :id="`schedule-${day.dayOfWeek}-${rangeIndex}-end`"
                type="time"
                :model-value="range.end"
                :invalid="attempted && !!errors[index]"
                :disabled="busy"
                class="px-2"
                @update:model-value="setTime(index, rangeIndex, 'end', $event)"
              />
            </div>
            <button
              type="button"
              :class="iconButtonClasses"
              :disabled="busy"
              :aria-label="
                t('employees.schedule.removeRange', {
                  n: rangeIndex + 1,
                  day: dayName(day.dayOfWeek),
                })
              "
              @click="removeRange(index, rangeIndex)"
            >
              <X class="h-5 w-5" aria-hidden="true" />
            </button>
          </div>

          <Text
            v-if="attempted && errors[index]"
            size="notes"
            class="pl-8 text-destructive"
            role="alert"
          >
            {{ t(`employees.schedule.errors.${errors[index]}`) }}
          </Text>

          <Button
            size="xs"
            variant="secondary"
            class="ml-8 self-start"
            :disabled="busy"
            @click="addRange(index)"
          >
            <template #icon-start><Plus class="h-3 w-3" aria-hidden="true" /></template>
            {{ t('employees.schedule.addRange') }}
          </Button>
        </template>
      </li>
    </ul>

    <Text v-if="attempted && hasErrors" size="notes" class="text-destructive" role="alert">
      {{ t('employees.schedule.invalid') }}
    </Text>

    <Button variant="primary" size="sm" class="self-start" :disabled="busy" @click="save">
      {{ t('employees.schedule.save') }}
    </Button>
  </div>
</template>
