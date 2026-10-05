import { apiRequest } from '@lib/api/request';
import type { OrganizationSettings, OrganizationSettingsInput } from '../types/settings.types';

const URL = '/api/v1/organization/settings';

/** Configuración del centro: la lee cualquier rol autenticado. */
export function getOrganizationSettings(): Promise<OrganizationSettings> {
  return apiRequest<OrganizationSettings>('get', URL);
}

/** Reemplaza la configuración entera (Admin o Manager). */
export function updateOrganizationSettings(
  input: OrganizationSettingsInput
): Promise<OrganizationSettings> {
  return apiRequest<OrganizationSettings>('put', URL, { body: input });
}
