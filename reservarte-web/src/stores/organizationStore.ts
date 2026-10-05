import { defineStore } from 'pinia';
import {
  getOrganizationSettings,
  updateOrganizationSettings,
} from '@features/organization/api/settings.api';
import {
  DEFAULT_TIME_ZONE,
  type OrganizationSettings,
  type OrganizationSettingsInput,
} from '@features/organization/types/settings.types';

/** Petición en curso, compartida por quienes piden la configuración a la vez. */
let pending: Promise<void> | null = null;

/**
 * Configuración del centro (RA-869f6r71x). La zona horaria sale de aquí: las
 * ausencias y las pruebas de alergia se muestran y se guardan en hora del centro.
 * Se pide una vez por sesión; quien la necesite llama antes a `ensureLoaded()`.
 */
export const useOrganizationStore = defineStore('organization', {
  state: () => ({
    settings: null as OrganizationSettings | null,
  }),

  getters: {
    /**
     * Zona del centro. Mientras no se haya cargado la configuración, o si la
     * carga falla, la zona por defecto: la misma que aplica la API a un centro
     * sin configurar.
     */
    timeZone: (state): string => state.settings?.timeZone || DEFAULT_TIME_ZONE,
  },

  actions: {
    /**
     * Carga la configuración si aún no está. No lanza: sin ella, la pantalla
     * sigue con la zona por defecto. La de Configuración usa `load()`, que sí avisa.
     */
    async ensureLoaded(): Promise<void> {
      if (this.settings) return;
      try {
        await this.load();
      } catch {
        // La zona por defecto cubre el fallo.
      }
    },

    /** Pide la configuración a la API; lanza `ApiRequestError` si falla. */
    load(): Promise<void> {
      pending ??= getOrganizationSettings()
        .then((settings) => {
          this.settings = settings;
        })
        .finally(() => {
          pending = null;
        });
      return pending;
    },

    async save(input: OrganizationSettingsInput): Promise<void> {
      this.settings = await updateOrganizationSettings(input);
    },

    /** Al cerrar la sesión: la siguiente cuenta vuelve a pedirla. */
    reset() {
      this.settings = null;
    },
  },
});
