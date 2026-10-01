import { apiRequest } from '@lib/api/request';

/** Servicio del catálogo, con lo que usa la pantalla de reserva (ServiceDto). */
export interface ServiceOption {
  id: number;
  name: string;
  durationMinutes: number;
  basePrice: number;
}

/** GET /api/v1/services: los activos del centro (cualquier rol autenticado los lee). */
export async function getActiveServices(): Promise<ServiceOption[]> {
  const data = await apiRequest<{ items: ServiceOption[] }>('get', '/api/v1/services', {
    params: { pageSize: 100 },
  });
  return data.items;
}
