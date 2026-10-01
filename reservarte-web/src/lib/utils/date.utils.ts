import { format } from 'date-fns';
import { es } from 'date-fns/locale';

export function formatDateSpain(value: Date | number): string {
  return format(value, 'dd/MM/yyyy', { locale: es });
}

export function formatTimeSpain(value: Date | number): string {
  return format(value, 'HH:mm', { locale: es });
}

/**
 * Fecha y hora de una cita como en Figma: «24 Dic - 10:00h». Recibe la fecha
 * (`yyyy-MM-dd`) y la hora (`HH:mm` o `HH:mm:ss`) del centro, sin zona.
 */
export function formatAppointmentDateTime(date: string, time: string): string {
  const [year, month, day] = date.split('-').map(Number);
  const monthName = format(new Date(year, month - 1, day), 'MMM', { locale: es });
  return `${day} ${monthName.charAt(0).toUpperCase()}${monthName.slice(1)} - ${time.slice(0, 5)}h`;
}
