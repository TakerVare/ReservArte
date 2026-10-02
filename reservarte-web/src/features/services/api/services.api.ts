import { apiPagedRequest, apiRequest } from '@lib/api/request';
import type { PagedResult } from '@lib/composables/useDataList';
import type {
  Service,
  ServiceCategory,
  ServiceDetail,
  ServiceInput,
  ServiceVariation,
  VariationInput,
} from '../types/service.types';

const BASE = '/api/v1/services';

/** Servicio del catálogo, con lo que usa la pantalla de reserva (ServiceDto). */
export interface ServiceOption {
  id: number;
  name: string;
  durationMinutes: number;
  basePrice: number;
  categoryName?: string | null;
}

/** GET /api/v1/services: los activos del centro (cualquier rol autenticado los lee). */
export async function getActiveServices(): Promise<ServiceOption[]> {
  const data = await apiRequest<{ items: ServiceOption[] }>('get', '/api/v1/services', {
    params: { pageSize: 100 },
  });
  return data.items;
}

/** Lista paginada del catálogo (gestión). Sin `isActive`, la API da solo los activos. */
export function getServices(query: {
  search?: string;
  categoryId?: number;
  isActive?: boolean;
  page: number;
  pageSize: number;
}): Promise<PagedResult<Service>> {
  return apiPagedRequest<Service>(BASE, {
    search: query.search || undefined,
    categoryId: query.categoryId,
    isActive: query.isActive,
    page: query.page,
    pageSize: query.pageSize,
  });
}

/** Detalle con las variaciones y tarifas vigentes. */
export function getService(id: number): Promise<ServiceDetail> {
  return apiRequest<ServiceDetail>('get', `${BASE}/${id}`);
}

/** Admin y Manager. Categoría inexistente → 400 `categoryId`. */
export function createService(input: ServiceInput): Promise<Service> {
  return apiRequest<Service>('post', BASE, { body: input });
}

export function updateService(id: number, input: ServiceInput): Promise<Service> {
  return apiRequest<Service>('put', `${BASE}/${id}`, { body: input });
}

/** Baja lógica: las citas cerradas siguen apuntando al servicio. */
export function deactivateService(id: number): Promise<Service> {
  return apiRequest<Service>('delete', `${BASE}/${id}`);
}

export function reactivateService(id: number): Promise<Service> {
  return apiRequest<Service>('post', `${BASE}/${id}/reactivate`);
}

/**
 * Categorías del centro. Sin `isActive` llegan todas, también las retiradas: la ficha
 * de un servicio puede seguir apuntando a una.
 */
export async function getCategories(): Promise<ServiceCategory[]> {
  const data = await apiRequest<{ items: ServiceCategory[] }>('get', `${BASE}/categories`);
  return data.items;
}

export function createCategory(name: string): Promise<ServiceCategory> {
  return apiRequest<ServiceCategory>('post', `${BASE}/categories`, {
    body: { name, displayOrder: 0 },
  });
}

/** Una variación que deje la duración en 0 o menos → 400 `durationModifier`. */
export function addVariation(id: number, input: VariationInput): Promise<ServiceVariation> {
  return apiRequest<ServiceVariation>('post', `${BASE}/${id}/variations`, { body: input });
}

export function updateVariation(
  id: number,
  variationId: number,
  input: VariationInput
): Promise<ServiceVariation> {
  return apiRequest<ServiceVariation>('put', `${BASE}/${id}/variations/${variationId}`, {
    body: input,
  });
}

/** Baja lógica, idempotente. */
export function deleteVariation(id: number, variationId: number): Promise<ServiceVariation> {
  return apiRequest<ServiceVariation>('delete', `${BASE}/${id}/variations/${variationId}`);
}
