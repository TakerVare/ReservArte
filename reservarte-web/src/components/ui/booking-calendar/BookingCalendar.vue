<script setup lang="ts">
import { computed } from 'vue';
import {
  CalendarCell,
  CalendarCellTrigger,
  CalendarGrid,
  CalendarGridBody,
  CalendarGridHead,
  CalendarGridRow,
  CalendarHeadCell,
  CalendarHeader,
  CalendarHeading,
  CalendarNext,
  CalendarPrev,
  CalendarRoot,
} from 'reka-ui';
import { parseDate, type DateValue } from '@internationalized/date';
import { ChevronLeft, ChevronRight } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';

/**
 * Calendario de la pantalla de reserva (RA-869fagpyg, H-45; Figma «Selección de
 * cita» 387:56629) sobre el `Calendar` de Reka UI, que pone el teclado y la parte
 * accesible. A diferencia del diseño, la semana empieza en lunes. Hoy va con
 * círculo `primary` (#FFB6C1), los días con hueco con círculo `accent` (#FFE4E1) y
 * los días pasados o fuera de la ventana de reserva, deshabilitados.
 *
 * Fechas como `yyyy-MM-dd`; `month` es el primer día del mes visible.
 */
const props = withDefaults(
  defineProps<{
    availableDays: ReadonlySet<string>;
    /** Hoy, `yyyy-MM-dd`: lo anterior queda deshabilitado. */
    today: string;
    /** Último día de la ventana de reserva, `yyyy-MM-dd`. */
    maxDate?: string;
  }>(),
  { maxDate: undefined }
);

const selected = defineModel<string | null>({ default: null });
const month = defineModel<string>('month', { required: true });

const { t } = useI18n();

const value = computed<DateValue | undefined>({
  get: () => (selected.value ? parseDate(selected.value) : undefined),
  set: (date) => {
    selected.value = date ? date.toString() : null;
  },
});

const placeholder = computed<DateValue>({
  get: () => parseDate(month.value),
  set: (date) => {
    month.value = date.set({ day: 1 }).toString();
  },
});

const minValue = computed(() => parseDate(props.today));
const maxValue = computed(() => (props.maxDate ? parseDate(props.maxDate) : undefined));

function dayClasses(date: DateValue) {
  const iso = date.toString();
  if (iso === props.today) return 'bg-primary text-primary-foreground font-bold';
  if (props.availableDays.has(iso)) return 'bg-accent text-foreground';
  return 'text-foreground';
}
</script>

<template>
  <CalendarRoot
    v-slot="{ weekDays, grid }"
    v-model="value"
    v-model:placeholder="placeholder"
    :week-starts-on="1"
    locale="es-ES"
    weekday-format="short"
    :min-value="minValue"
    :max-value="maxValue"
    fixed-weeks
    :calendar-label="t('booking.calendar.label')"
    class="w-full max-w-[343px] bg-background p-4 shadow-[0_10px_30px_hsl(var(--foreground)/10%)]"
  >
    <CalendarHeader class="flex items-center justify-between pb-2">
      <CalendarHeading
        class="font-sans text-[20px] font-bold text-foreground first-letter:uppercase"
      />
      <div class="flex gap-4">
        <CalendarPrev
          :aria-label="t('booking.calendar.previous')"
          class="text-foreground outline-none hover:text-primary focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-30"
        >
          <ChevronLeft class="h-6 w-6" aria-hidden="true" />
        </CalendarPrev>
        <CalendarNext
          :aria-label="t('booking.calendar.next')"
          class="text-foreground outline-none hover:text-primary focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-30"
        >
          <ChevronRight class="h-6 w-6" aria-hidden="true" />
        </CalendarNext>
      </div>
    </CalendarHeader>

    <CalendarGrid v-for="m in grid" :key="m.value.toString()" class="w-full border-collapse">
      <CalendarGridHead>
        <CalendarGridRow class="flex justify-between">
          <CalendarHeadCell
            v-for="day in weekDays"
            :key="day"
            class="w-8 font-sans text-[13px] font-bold uppercase text-muted-foreground"
          >
            {{ day }}
          </CalendarHeadCell>
        </CalendarGridRow>
      </CalendarGridHead>
      <CalendarGridBody>
        <CalendarGridRow v-for="(week, i) in m.rows" :key="i" class="flex justify-between py-2">
          <CalendarCell v-for="date in week" :key="date.toString()" :date="date" class="h-8 w-8">
            <CalendarCellTrigger
              :day="date"
              :month="m.value"
              :data-available="availableDays.has(date.toString()) || undefined"
              :class="[
                'flex h-8 w-8 items-center justify-center rounded-full font-sans text-[18px] outline-none',
                'focus-visible:ring-2 focus-visible:ring-ring',
                'data-[selected]:ring-2 data-[selected]:ring-foreground',
                'data-[disabled]:pointer-events-none data-[disabled]:bg-transparent data-[disabled]:font-normal data-[disabled]:text-muted-foreground data-[disabled]:opacity-40',
                'data-[outside-view]:invisible',
                dayClasses(date),
              ]"
            />
          </CalendarCell>
        </CalendarGridRow>
      </CalendarGridBody>
    </CalendarGrid>
  </CalendarRoot>
</template>
