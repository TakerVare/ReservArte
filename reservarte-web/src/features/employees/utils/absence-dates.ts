import { fromAbsolute, parseDate, parseDateTime, toZoned } from '@internationalized/date';
import { format } from 'date-fns';
import { es } from 'date-fns/locale';

/**
 * Fechas en hora del centro. La zona llega siempre como argumento: es la de la
 * configuración del centro (`organizationStore.timeZone`, RA-869f6r71x).
 */

/** Lo que pide el formulario de ausencia: días del centro y, si no es el día entero, horas. */
export interface AbsenceRangeInput {
  /** `yyyy-MM-dd`. */
  from: string;
  /** `yyyy-MM-dd`, igual o posterior a `from`. */
  to: string;
  allDay: boolean;
  /** `HH:mm`, solo si `allDay` es falso. */
  startTime?: string;
  endTime?: string;
}

/** Hora local del centro (`yyyy-MM-ddTHH:mm`) → instante UTC en ISO con `Z`, como pide la API. */
export function centerToUtc(dateTime: string, timeZone: string): string {
  return toZoned(parseDateTime(dateTime), timeZone).toAbsoluteString();
}

/**
 * Intervalo UTC de una ausencia. El día entero va de las 00:00 del primer día a
 * las 00:00 del día siguiente al último, en hora del centro: así una semana de
 * vacaciones cubre también el cambio de hora, si cae en medio.
 */
export function absenceToUtc(
  input: AbsenceRangeInput,
  timeZone: string
): { startDateTime: string; endDateTime: string } {
  if (input.allDay) {
    const dayAfter = parseDate(input.to).add({ days: 1 }).toString();
    return {
      startDateTime: centerToUtc(`${input.from}T00:00`, timeZone),
      endDateTime: centerToUtc(`${dayAfter}T00:00`, timeZone),
    };
  }
  return {
    startDateTime: centerToUtc(`${input.from}T${input.startTime}`, timeZone),
    endDateTime: centerToUtc(`${input.to}T${input.endTime}`, timeZone),
  };
}

interface CenterMoment {
  date: string;
  time: string;
}

/** Instante UTC (ISO) → día y hora del centro. */
export function utcToCenter(iso: string, timeZone: string): CenterMoment {
  const zoned = fromAbsolute(Date.parse(iso), timeZone);
  const pad = (value: number) => String(value).padStart(2, '0');
  return {
    date: `${zoned.year}-${pad(zoned.month)}-${pad(zoned.day)}`,
    time: `${pad(zoned.hour)}:${pad(zoned.minute)}`,
  };
}

/** «3 nov 2026», sin depender de la zona del navegador. */
function formatDay(date: string): string {
  const [year, month, day] = date.split('-').map(Number) as [number, number, number];
  return format(new Date(year, month - 1, day), 'd MMM yyyy', { locale: es });
}

/** Piezas de una ausencia en hora del centro; el texto lo compone i18n (`employees.absences.when`). */
export interface AbsenceWhen {
  allDay: boolean;
  /** El mismo día de principio a fin. */
  singleDay: boolean;
  /** «3 nov 2026». */
  fromDay: string;
  /** Último día de la ausencia (en las de día entero, el anterior al fin exclusivo). */
  toDay: string;
  startTime: string;
  endTime: string;
}

/**
 * Pasa una ausencia a hora del centro. Si empieza y acaba a las 00:00, es de días
 * enteros y su fin (exclusivo) se muestra como el día anterior.
 */
export function describeAbsence(
  absence: { startDateTime: string; endDateTime: string },
  timeZone: string
): AbsenceWhen {
  const start = utcToCenter(absence.startDateTime, timeZone);
  const end = utcToCenter(absence.endDateTime, timeZone);
  const allDay = start.time === '00:00' && end.time === '00:00';
  const lastDay = allDay ? parseDate(end.date).subtract({ days: 1 }).toString() : end.date;
  return {
    allDay,
    singleDay: lastDay === start.date,
    fromDay: formatDay(start.date),
    toDay: formatDay(lastDay),
    startTime: start.time,
    endTime: end.time,
  };
}
