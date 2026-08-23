export interface LoginCredentials {
  email: string;
  password: string;
  captcha?: string;
}

/** Espejo de UserDto (ReservArte-Application/DTOs/Auth/AuthResponse.cs). */
export interface AuthApiUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  rol: string;
}

/** Espejo de AuthResponse (ReservArte-Application/DTOs/Auth/AuthResponse.cs). */
export interface AuthApiResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthApiUser | null;
  mfaRequired: boolean;
  mfaTicket: string | null;
}

/** Espejo de ApiError (ReservArte-Shared/Api/ApiError.cs). */
export interface ApiErrorShape {
  code: string;
  message: string;
  details?: unknown;
}

export type OAuthProvider = 'google' | 'apple' | 'instagram';
