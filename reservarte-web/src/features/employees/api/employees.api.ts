import { apiPagedRequest, apiRequest } from '@lib/api/request';
import type { PagedResult } from '@lib/composables/useDataList';
import type {
  Absence,
  AbsenceInput,
  Employee,
  EmployeeAvailability,
  EmployeeInput,
  ScheduleSlot,
} from '../types/employee.types';

const BASE = '/api/v1/employees';

/** GET /api/v1/employees: solo Admin y Manager. Sin `isActive`, la API da solo los activos. */
export function getEmployees(query: {
  search?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
}): Promise<PagedResult<Employee>> {
  return apiPagedRequest<Employee>(BASE, {
    search: query.search || undefined,
    isActive: query.isActive,
    page: query.page,
    pageSize: query.pageSize,
  });
}

export function getEmployee(id: number): Promise<Employee> {
  return apiRequest<Employee>('get', `${BASE}/${id}`);
}

/** Alta: la cuenta nace sin contraseña y la API envía la invitación. */
export function createEmployee(input: EmployeeInput): Promise<Employee> {
  return apiRequest<Employee>('post', BASE, { body: input });
}

export function updateEmployee(id: number, input: EmployeeInput): Promise<Employee> {
  return apiRequest<Employee>('put', `${BASE}/${id}`, { body: input });
}

/** Baja lógica: desactiva la ficha y bloquea la cuenta. */
export function deactivateEmployee(id: number): Promise<Employee> {
  return apiRequest<Employee>('delete', `${BASE}/${id}`);
}

export function reactivateEmployee(id: number): Promise<Employee> {
  return apiRequest<Employee>('post', `${BASE}/${id}/reactivate`);
}

/** Reenvía la invitación. 409 si la cuenta ya tiene contraseña o está de baja. */
export function resendInvitation(id: number): Promise<Employee> {
  return apiRequest<Employee>('post', `${BASE}/${id}/invitation`);
}

/** Horario semanal y ausencias que solapan `[from, to]` (ISO en UTC). */
export function getAvailability(
  id: number,
  range: { from: string; to: string }
): Promise<EmployeeAvailability> {
  return apiRequest<EmployeeAvailability>('get', `${BASE}/${id}/availability`, { params: range });
}

/** Reemplaza la semana entera: lo que no va en `slots` deja de existir. */
export function replaceSchedule(id: number, slots: ScheduleSlot[]): Promise<EmployeeAvailability> {
  return apiRequest<EmployeeAvailability>('put', `${BASE}/${id}/availability`, {
    body: { weeklySchedule: slots.map((slot) => ({ ...slot, isRecurring: true })) },
  });
}

export function addAbsence(id: number, input: AbsenceInput): Promise<Absence> {
  return apiRequest<Absence>('post', `${BASE}/${id}/exceptions`, { body: input });
}

export function deleteAbsence(id: number, absenceId: number): Promise<Absence> {
  return apiRequest<Absence>('delete', `${BASE}/${id}/exceptions/${absenceId}`);
}
