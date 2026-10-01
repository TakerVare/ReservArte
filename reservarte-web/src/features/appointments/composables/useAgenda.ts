import { computed, ref, shallowRef, watch } from 'vue';
import {
  addDays,
  addMonths,
  addWeeks,
  endOfMonth,
  endOfWeek,
  format,
  startOfDay,
  startOfMonth,
  startOfWeek,
} from 'date-fns';
import { es } from 'date-fns/locale';
import { getAppointmentsInRange } from '../api/appointments.api';
import type { AppointmentSummary } from '../types/appointment.types';

export type AgendaView = 'day' | 'week' | 'month';

export interface AgendaGroup {
  /** `yyyy-MM-dd`. */
  date: string;
  label: string;
  items: AppointmentSummary[];
}

const iso = (date: Date) => format(date, 'yyyy-MM-dd');
const capitalize = (text: string) => text.charAt(0).toUpperCase() + text.slice(1);

/**
 * Agenda del personal (RA-869fajn7g): vista de día, semana (lunes a domingo) o mes,
 * con navegación al bloque anterior o siguiente y vuelta a hoy. Carga todas las
 * citas activas del periodo y las filtra por empleada en la SPA; la semana y el mes
 * se agrupan por fecha. Si llegan dos respuestas, cuenta la del último periodo.
 */
export function useAgenda(options: { today?: () => Date } = {}) {
  const today = options.today ?? (() => new Date());

  const view = ref<AgendaView>('day');
  const anchor = ref<Date>(startOfDay(today()));
  const items = shallowRef<AppointmentSummary[]>([]);
  const employeeId = ref<number | null>(null);
  const loading = ref(false);
  const failed = ref(false);

  const range = computed(() => {
    const date = anchor.value;
    if (view.value === 'week') {
      return {
        from: startOfWeek(date, { weekStartsOn: 1 }),
        to: endOfWeek(date, { weekStartsOn: 1 }),
      };
    }
    if (view.value === 'month') return { from: startOfMonth(date), to: endOfMonth(date) };
    return { from: date, to: date };
  });

  const title = computed(() => {
    const { from, to } = range.value;
    if (view.value === 'day') return capitalize(format(from, "EEEE, d 'de' MMMM", { locale: es }));
    if (view.value === 'month') return capitalize(format(from, "MMMM 'de' yyyy", { locale: es }));
    return `${format(from, 'd MMM', { locale: es })} – ${format(to, 'd MMM yyyy', { locale: es })}`;
  });

  const employees = computed(() => {
    const byId = new Map<number, string>();
    for (const a of items.value) byId.set(a.employeeId, a.employeeName);
    return [...byId]
      .map(([id, name]) => ({ id, name }))
      .sort((a, b) => a.name.localeCompare(b.name, 'es'));
  });

  const groups = computed<AgendaGroup[]>(() => {
    const visible = items.value
      .filter((a) => employeeId.value === null || a.employeeId === employeeId.value)
      .slice()
      .sort((a, b) =>
        `${a.appointmentDate}T${a.startTime}`.localeCompare(`${b.appointmentDate}T${b.startTime}`)
      );
    const byDate = new Map<string, AppointmentSummary[]>();
    for (const a of visible)
      byDate.set(a.appointmentDate, [...(byDate.get(a.appointmentDate) ?? []), a]);
    return [...byDate].map(([date, list]) => {
      const [y, m, d] = date.split('-').map(Number);
      return {
        date,
        label: capitalize(format(new Date(y, m - 1, d), "EEEE, d 'de' MMMM", { locale: es })),
        items: list,
      };
    });
  });

  let lastRequest = 0;

  async function load() {
    const request = ++lastRequest;
    loading.value = true;
    failed.value = false;
    try {
      const result = await getAppointmentsInRange(iso(range.value.from), iso(range.value.to));
      if (request !== lastRequest) return;
      items.value = result;
    } catch {
      if (request === lastRequest) failed.value = true;
    } finally {
      if (request === lastRequest) loading.value = false;
    }
  }

  function move(step: 1 | -1) {
    const date = anchor.value;
    anchor.value =
      view.value === 'week'
        ? addWeeks(date, step)
        : view.value === 'month'
          ? addMonths(date, step)
          : addDays(date, step);
  }

  watch(
    () => `${iso(range.value.from)}|${iso(range.value.to)}`,
    () => void load()
  );

  void load();

  return {
    view,
    anchor,
    range: computed(() => ({ from: iso(range.value.from), to: iso(range.value.to) })),
    title,
    employees,
    employeeId,
    groups,
    loading,
    failed,
    previous: () => move(-1),
    next: () => move(1),
    goToday: () => {
      anchor.value = startOfDay(today());
    },
    reload: load,
  };
}
