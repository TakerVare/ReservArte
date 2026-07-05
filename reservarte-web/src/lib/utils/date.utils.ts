import { format } from 'date-fns';
import { es } from 'date-fns/locale';

export function formatDateSpain(value: Date | number): string {
  return format(value, 'dd/MM/yyyy', { locale: es });
}

export function formatTimeSpain(value: Date | number): string {
  return format(value, 'HH:mm', { locale: es });
}
