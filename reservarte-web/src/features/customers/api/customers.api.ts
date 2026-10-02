import { apiPagedRequest, apiRequest } from '@lib/api/request';
import type { PagedResult } from '@lib/composables/useDataList';
import type { AppointmentDetail } from '@features/appointments/types/appointment.types';
import type {
  CreateCustomerInput,
  Customer,
  CustomerCategory,
  CustomerDetail,
  CustomerAllergy,
  CustomerInput,
  CustomerNote,
  ConsentType,
} from '../types/customer.types';

const BASE = '/api/v1/customers';

/** Clienta en el buscador del personal (CustomerDto), con foto y nombre. */
export interface CustomerOption {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  profileImageUrl?: string | null;
}

/** GET /api/v1/customers?search: solo el personal (RA-869d7f3bt). */
export async function searchCustomers(
  search: string,
  page: number,
  pageSize: number
): Promise<PagedResult<CustomerOption>> {
  const data = await apiRequest<{ items: CustomerOption[] }>('get', '/api/v1/customers', {
    params: { search: search || undefined, page, pageSize },
  });
  // La paginación viaja en `meta`, fuera de `data`: el buscador solo usa la primera página.
  return {
    items: data.items,
    pagination: { page, pageSize, totalCount: data.items.length, totalPages: 1 },
  };
}

/** GET /api/v1/customers: el personal. Sin `isActive`, la API da solo las activas. */
export function getCustomers(query: {
  search?: string;
  category?: CustomerCategory;
  isActive?: boolean;
  page: number;
  pageSize: number;
}): Promise<PagedResult<Customer>> {
  return apiPagedRequest<Customer>(BASE, {
    search: query.search || undefined,
    category: query.category,
    isActive: query.isActive,
    page: query.page,
    pageSize: query.pageSize,
  });
}

/** Perfil completo: ficha, consentimientos, alergias y notas vigentes. */
export function getCustomer(id: number): Promise<CustomerDetail> {
  return apiRequest<CustomerDetail>('get', `${BASE}/${id}`);
}

/** Alta (Admin y Manager). 409 si el email ya tiene ficha en el centro. */
export function createCustomer(input: CreateCustomerInput): Promise<Customer> {
  return apiRequest<Customer>('post', BASE, { body: input });
}

export function updateCustomer(id: number, input: CustomerInput): Promise<Customer> {
  return apiRequest<Customer>('put', `${BASE}/${id}`, { body: input });
}

/** Baja lógica, sin bloquear la cuenta (la clienta conserva su acceso). */
export function deactivateCustomer(id: number): Promise<Customer> {
  return apiRequest<Customer>('delete', `${BASE}/${id}`);
}

export function reactivateCustomer(id: number): Promise<Customer> {
  return apiRequest<Customer>('post', `${BASE}/${id}/reactivate`);
}

/** La firma la ficha de empleado activa de quien llama; sin ella, 403. */
export function addCustomerNote(id: number, note: string): Promise<CustomerNote> {
  return apiRequest<CustomerNote>('post', `${BASE}/${id}/notes`, { body: { note } });
}

/** Solo su autora, Admin o Manager; si no, 403. */
export function deleteCustomerNote(id: number, noteId: number): Promise<CustomerNote> {
  return apiRequest<CustomerNote>('delete', `${BASE}/${id}/notes/${noteId}`);
}

/** Registra la última prueba de alergia (`testedAt` en ISO con zona, no futura). */
export function recordAllergyTest(id: number, testedAt: string): Promise<Customer> {
  return apiRequest<Customer>('put', `${BASE}/${id}/allergy-test`, { body: { testedAt } });
}

/** Historial de citas, de la más reciente a la más antigua. */
export function getCustomerHistory(
  id: number,
  page: number,
  pageSize = 20
): Promise<PagedResult<AppointmentDetail>> {
  return apiPagedRequest<AppointmentDetail>(`${BASE}/${id}/history`, { page, pageSize });
}

/**
 * Da o retira un consentimiento (Admin y Manager) y devuelve el perfil: retirar el de
 * tratamiento de datos da de baja la ficha (H-47).
 */
export function setConsent(
  id: number,
  consentType: ConsentType,
  granted: boolean
): Promise<CustomerDetail> {
  return apiRequest<CustomerDetail>('put', `${BASE}/${id}/consents/${consentType}`, {
    body: { granted },
  });
}

export type AllergyInput = Pick<CustomerAllergy, 'allergyDescription' | 'severity'>;

/** Todo el personal, como la prueba de alergia. */
export function addAllergy(id: number, input: AllergyInput): Promise<CustomerAllergy> {
  return apiRequest<CustomerAllergy>('post', `${BASE}/${id}/allergies`, { body: input });
}

export function updateAllergy(
  id: number,
  allergyId: number,
  input: AllergyInput
): Promise<CustomerAllergy> {
  return apiRequest<CustomerAllergy>('put', `${BASE}/${id}/allergies/${allergyId}`, {
    body: input,
  });
}

/** Baja lógica, idempotente. */
export function deleteAllergy(id: number, allergyId: number): Promise<CustomerAllergy> {
  return apiRequest<CustomerAllergy>('delete', `${BASE}/${id}/allergies/${allergyId}`);
}

/** Admin y Manager. Bloqueada, la clienta no puede reservar (`CUST_BLOCKED`). */
export function blockCustomer(id: number, reason: string): Promise<Customer> {
  return apiRequest<Customer>('post', `${BASE}/${id}/block`, { body: { reason } });
}

export function unblockCustomer(id: number): Promise<Customer> {
  return apiRequest<Customer>('post', `${BASE}/${id}/unblock`);
}
