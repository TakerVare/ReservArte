import { apiRequest } from '@lib/api/request';
import type {
  AppointmentDetail,
  AppointmentSummary,
  AvailableDays,
  BookingRequest,
  ServiceSlots,
} from '../types/appointment.types';

/** Tope de `pageSize` que acepta la API. */
const MAX_PAGE_SIZE = 100;

/**
 * GET /api/v1/appointments desde `from` (`yyyy-MM-dd`), solo las activas. A una
 * clienta la API le devuelve solo sus citas (RA-869d7f519); el personal puede
 * acotar a una clienta con `customerId`.
 */
export async function getAppointmentsFrom(
  from: string,
  customerId?: number
): Promise<AppointmentSummary[]> {
  const data = await apiRequest<{ items: AppointmentSummary[] }>('get', '/api/v1/appointments', {
    params: { from, customerId, pageSize: MAX_PAGE_SIZE },
  });
  return data.items;
}

/** Días con hueco del servicio entre `from` y `to` (H-45), dentro de la ventana del rol. */
export function getAvailableDays(
  serviceId: number,
  from: string,
  to: string
): Promise<AvailableDays> {
  return apiRequest<AvailableDays>('get', '/api/v1/appointments/availability/days', {
    params: { serviceId, from, to },
  });
}

/** Huecos del servicio en `date`, agrupados por los empleados que lo prestan (H-45). */
export function getServiceSlots(serviceId: number, date: string): Promise<ServiceSlots> {
  return apiRequest<ServiceSlots>('get', '/api/v1/appointments/availability/by-service', {
    params: { serviceId, date },
  });
}

/** Alta de cita. La clienta no envía `customerId`: la API la toma del token (H-44). */
export function createAppointment(request: BookingRequest): Promise<AppointmentDetail> {
  return apiRequest<AppointmentDetail>('post', '/api/v1/appointments', { body: request });
}

/** Modificación de una cita que aún no ha empezado. */
export function updateAppointment(
  id: number,
  request: Omit<BookingRequest, 'customerId'>
): Promise<AppointmentDetail> {
  return apiRequest<AppointmentDetail>('put', `/api/v1/appointments/${id}`, { body: request });
}
