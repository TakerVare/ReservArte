import { defineStore } from 'pinia';

/** Espejo de UserDto (ReservArte-Application/DTOs/Auth/AuthResponse.cs). */
export interface AuthUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  rol: string;
}

interface LoginPayload {
  user?: AuthUser | null;
  accessToken?: string | null;
  refreshToken?: string | null;
  mfaRequired?: boolean;
  /** Ticket intermedio de 2FA (canjeable en /auth/mfa/verify). */
  mfaTicket?: string | null;
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
    mfaTicket: null as string | null,
  }),

  actions: {
    /**
     * Registra la sesión tras un login correcto (local u OAuth), o deja la
     * sesión pendiente de 2FA (mfaRequired + mfaTicket, sin tokens) hasta
     * que setMfaVerified() la complete.
     */
    login(payload: LoginPayload) {
      this.mfaRequired = payload.mfaRequired ?? false;
      this.mfaTicket = payload.mfaTicket ?? null;

      if (this.mfaRequired) {
        this.isAuthenticated = false;
        return;
      }

      this.user = payload.user ?? null;
      this.accessToken = payload.accessToken ?? null;
      this.refreshToken = payload.refreshToken ?? null;
      this.isAuthenticated = true;

      if (this.accessToken) {
        localStorage.setItem(AUTH_TOKEN_KEY, this.accessToken);
      }
    },

    logout() {
      this.user = null;
      this.accessToken = null;
      this.refreshToken = null;
      this.isAuthenticated = false;
      this.mfaRequired = false;
      this.mfaTicket = null;
      localStorage.removeItem(AUTH_TOKEN_KEY);
    },

    /**
     * TODO(Auth): llamará a POST /api/v1/auth/refresh y rotará el par de
     * tokens. Punto de extensión del interceptor 401 de client.ts.
     */
    async refreshAccessToken(): Promise<void> {
      // Pendiente: la página/composable que lo necesite (interceptor 401).
    },

    /**
     * Verificación de 2FA superada: completa la sesión pendiente con el par
     * de tokens y el usuario que devuelve /auth/mfa/verify. A diferencia del
     * login normal, aquí los tokens NO estaban en el store (el login con
     * mfaRequired solo dejó el ticket), así que llegan como parámetro.
     */
    setMfaVerified(payload: {
      user?: AuthUser | null;
      accessToken?: string | null;
      refreshToken?: string | null;
    }) {
      this.user = payload.user ?? null;
      this.accessToken = payload.accessToken ?? null;
      this.refreshToken = payload.refreshToken ?? null;
      this.mfaRequired = false;
      this.mfaTicket = null;
      this.isAuthenticated = true;
      if (this.accessToken) {
        localStorage.setItem(AUTH_TOKEN_KEY, this.accessToken);
      }
    },
  },
});
