import { apiRequest } from '@lib/api/request';
import type { PagedResult } from '@lib/composables/useDataList';

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
