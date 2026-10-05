/** Espejo de `OrganizationSettingsDto` (RA-869f74u7y). */
export interface OrganizationSettings {
  /** Zona horaria del centro, identificador IANA. */
  timeZone: string;
  /** Horas de antelación por debajo de las cuales una cancelación es tardía. */
  cancellationHoursThreshold: number;
  /** No presentaciones que bloquean a una clienta. */
  maxNoShowsBeforeBlock: number;
  /** Último guardado; null si el centro sigue con los valores por defecto. */
  updatedAt: string | null;
}

/** Cuerpo de `PUT /api/v1/organization/settings`: reemplaza la configuración entera. */
export type OrganizationSettingsInput = Omit<OrganizationSettings, 'updatedAt'>;

/** La zona de un centro sin configuración, la misma que aplica la API. */
export const DEFAULT_TIME_ZONE = 'Europe/Madrid';

/**
 * Zonas que ofrece la pantalla de Configuración: las de España. Un centro con
 * otra zona guardada la conserva (el formulario la añade a la lista).
 */
export const TIME_ZONE_OPTIONS = ['Europe/Madrid', 'Atlantic/Canary'] as const;
