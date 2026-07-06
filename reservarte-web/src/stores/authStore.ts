import { defineStore } from 'pinia';

/**
 * Usuario autenticado (forma mínima). Se trasladará a types/models.types
 * cuando la tarea de Auth defina el contrato completo con la API.
 */
export interface AuthUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  rol: string;
}

interface LoginPayload {
  user: AuthUser;
  accessToken: string;
  refreshToken: string;
  mfaRequired?: boolean;
}

/** Misma clave que lee el interceptor de client.ts (RA-869d7f79y). */
const AUTH_TOKEN_KEY = 'authToken';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null as AuthUser | null,
    accessToken: localStorage.getItem(AUTH_TOKEN_KEY),
    // El refresh token NO se persiste en localStorage: la tarea de Auth
    // decidirá su almacenamiento definitivo (idealmente cookie httpOnly)
    refreshToken: null as string | null,
    isAuthenticated: localStorage.getItem(AUTH_TOKEN_KEY) !== null,
    mfaRequired: false,
  }),

  actions: {
    /**
     * Registra la sesión tras un login correcto (local u OAuth). La llamada
     * HTTP la hará la página de login en la tarea de Auth; si el usuario
     * tiene 2FA activa, la sesión no se completa hasta setMfaVerified().
     */
    login(payload: LoginPayload) {
      this.user = payload.user;
      this.accessToken = payload.accessToken;
      this.refreshToken = payload.refreshToken;
      this.mfaRequired = payload.mfaRequired ?? false;
      this.isAuthenticated = !this.mfaRequired;

      if (this.isAuthenticated) {
        localStorage.setItem(AUTH_TOKEN_KEY, payload.accessToken);
      }
    },

    logout() {
      this.user = null;
      this.accessToken = null;
      this.refreshToken = null;
      this.isAuthenticated = false;
      this.mfaRequired = false;
      localStorage.removeItem(AUTH_TOKEN_KEY);
    },

    /**
     * TODO(Auth): llamará a POST /api/v1/auth/refresh y rotará el par de
     * tokens. Punto de extensión del interceptor 401 de client.ts.
     * El backend aún no expone endpoints de autenticación.
     */
    async refreshAccessToken(): Promise<void> {
      // Se implementa en la tarea de Auth
    },

    /** Verificación TOTP superada: completa la sesión pendiente de MFA. */
    setMfaVerified() {
      this.mfaRequired = false;
      this.isAuthenticated = true;

      if (this.accessToken) {
        localStorage.setItem(AUTH_TOKEN_KEY, this.accessToken);
      }
    },
  },
});
