import type { ScheduleSlot } from '../types/employee.types';

/** Tramo en edición: horas `HH:mm` de los `<input type="time">`. */
export interface ScheduleRange {
  start: string;
  end: string;
}

/** Un día de la semana en el editor. 0 = lunes … 6 = domingo, como la API. */
export interface ScheduleDay {
  dayOfWeek: number;
  works: boolean;
  ranges: ScheduleRange[];
}

export type ScheduleDayError = 'empty' | 'order' | 'overlap';

export const WEEK_DAYS = [0, 1, 2, 3, 4, 5, 6] as const;

/** Jornada partida del centro: lo que propone el editor al marcar un día o añadir un tramo. */
const MORNING: ScheduleRange = { start: '10:00', end: '14:00' };
const AFTERNOON: ScheduleRange = { start: '16:00', end: '20:00' };

const hhmm = (time: string) => time.slice(0, 5);

/** Una hora más, sin pasar de las 23:59 (el horario no cruza la medianoche). */
function addHour(time: string): string {
  const [hours, minutes] = time.split(':').map(Number) as [number, number];
  const total = Math.min(hours * 60 + minutes + 60, 23 * 60 + 59);
  return `${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`;
}

/** De la respuesta de la API a los siete días del editor, con los tramos ordenados. */
export function toWeek(slots: ScheduleSlot[]): ScheduleDay[] {
  return WEEK_DAYS.map((dayOfWeek) => {
    const ranges = slots
      .filter((slot) => slot.dayOfWeek === dayOfWeek)
      .map((slot) => ({ start: hhmm(slot.startTime), end: hhmm(slot.endTime) }))
      .sort((a, b) => a.start.localeCompare(b.start));
    return { dayOfWeek, works: ranges.length > 0, ranges };
  });
}

/** Del editor al PUT: solo los días con jornada, con segundos como `TimeOnly`. */
export function fromWeek(days: ScheduleDay[]): ScheduleSlot[] {
  return days
    .filter((day) => day.works)
    .flatMap((day) =>
      day.ranges.map((range) => ({
        dayOfWeek: day.dayOfWeek,
        startTime: `${range.start}:00`,
        endTime: `${range.end}:00`,
      }))
    );
}

/** Tramo que se propone al añadir: mañana, tarde o, después, a continuación del último. */
export function nextRange(ranges: ScheduleRange[]): ScheduleRange {
  if (ranges.length === 0) return { ...MORNING };
  const last = [...ranges].sort((a, b) => a.start.localeCompare(b.start)).at(-1)!;
  if (last.end <= MORNING.end) return { ...AFTERNOON };
  return { start: last.end, end: addHour(last.end) };
}

/**
 * Las mismas reglas que `UpdateAvailabilityRequestValidator`: fin posterior al
 * inicio y sin solapes en el mismo día. Además, un día marcado sin tramos es un
 * error del editor (no se guardaría nada para ese día).
 */
export function validateDay(day: ScheduleDay): ScheduleDayError | null {
  if (!day.works) return null;
  if (day.ranges.length === 0) return 'empty';
  if (day.ranges.some((range) => !range.start || !range.end || range.end <= range.start)) {
    return 'order';
  }
  const ordered = [...day.ranges].sort((a, b) => a.start.localeCompare(b.start));
  for (let i = 1; i < ordered.length; i++) {
    if (ordered[i]!.start < ordered[i - 1]!.end) return 'overlap';
  }
  return null;
}

/** Copia la jornada de un día a los demás días laborables (lunes a viernes). */
export function copyToWeekdays(days: ScheduleDay[], source: number): ScheduleDay[] {
  const from = days.find((day) => day.dayOfWeek === source);
  if (!from) return days;
  return days.map((day) =>
    day.dayOfWeek <= 4 && day.dayOfWeek !== source
      ? {
          dayOfWeek: day.dayOfWeek,
          works: from.works,
          ranges: from.ranges.map((range) => ({ ...range })),
        }
      : day
  );
}
