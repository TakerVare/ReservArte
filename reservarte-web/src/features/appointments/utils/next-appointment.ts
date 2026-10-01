import { format } from 'date-fns';
import type { AppointmentStatus, AppointmentSummary } from '../types/appointment.types';

/** Estados de una cita que todavía va a celebrarse. */
const UPCOMING_STATUSES: readonly AppointmentStatus[] = ['pending', 'confirmed'];

/**
 * La próxima cita: la primera, por fecha y hora, de las pendientes o
 * confirmadas que aún no han empezado. `now` se compara en la hora local, que
 * es la del centro (la API da fecha y hora sin zona).
 */
export function pickNextAppointment(
  appointments: readonly AppointmentSummary[],
  now: Date
): AppointmentSummary | null {
  const nowKey = format(now, "yyyy-MM-dd'T'HH:mm:ss");

  return appointments
    .filter((a) => UPCOMING_STATUSES.includes(a.status))
    .filter((a) => `${a.appointmentDate}T${a.startTime}` >= nowKey)
    .reduce<AppointmentSummary | null>((next, a) => {
      if (!next) return a;
      const key = `${a.appointmentDate}T${a.startTime}`;
      return key < `${next.appointmentDate}T${next.startTime}` ? a : next;
    }, null);
}
