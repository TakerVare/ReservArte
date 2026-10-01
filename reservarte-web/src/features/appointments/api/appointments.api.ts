import { apiRequest } from '@lib/api/request';
import type {
  AppointmentDetail,
  AppointmentTransition,
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

/**
 * Todas las citas activas del periodo [from, to] (agenda del personal, RA-869fajn7g),
 * recorriendo las páginas: la API da 100 como mucho por página.
 */
export async function getAppointmentsInRange(
  from: string,
  to: string
): Promise<AppointmentSummary[]> {
  const all: AppointmentSummary[] = [];
  for (let page = 1; page <= 20; page++) {
    const data = await apiRequest<{ items: AppointmentSummary[] }>('get', '/api/v1/appointments', {
      params: { from, to, page, pageSize: MAX_PAGE_SIZE },
    });
    all.push(...data.items);
    if (data.items.length < MAX_PAGE_SIZE) break;
  }
  return all;
}

/** Ficha de una cita, con servicios, precio y avisos. */
export function getAppointment(id: number): Promise<AppointmentDetail> {
  return apiRequest<AppointmentDetail>('get', `/api/v1/appointments/${id}`);
}

/** Cambio de estado (confirmar, iniciar, completar o no presentada). */
export function transitionAppointment(
  id: number,
  transition: AppointmentTransition
): Promise<AppointmentDetail> {
  return apiRequest<AppointmentDetail>('post', `/api/v1/appointments/${id}/${transition}`);
}

/**
 * Cancelación (RA-869d7fcfy): la clienta, solo las suyas; el personal, cualquiera.
 * Quién cancela lo deduce la API de la sesión. Sin penalización en el piloto.
 */
export function cancelAppointment(id: number, reason?: string): Promise<AppointmentDetail> {
  return apiRequest<AppointmentDetail>('post', `/api/v1/appointments/${id}/cancel`, {
    body: { reason: reason?.trim() || undefined },
  });
}
