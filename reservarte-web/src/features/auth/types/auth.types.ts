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
  /** Catálogo canónico del backend (RA-869f18116): PascalCase, no minúsculas. */
  rol: UserRole;
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

/**
 * Roles del producto, espejo de `Roles` en el backend
 * (ReservArte-Domain/Entities/Roles.cs). PascalCase: el claim `role` del JWT
 * los emite así y `[Authorize(Roles = …)]` distingue mayúsculas.
 */
export type UserRole = 'Admin' | 'Manager' | 'Employee' | 'Customer';

export type OAuthProvider = 'google' | 'apple' | 'instagram';

/** Cuerpo de POST /api/v1/auth/mfa/verify (vol. 1 §4.4.2). El campo code
 *  admite un código TOTP de 6 dígitos O un código de recuperación. */
export interface MfaVerifyCredentials {
  mfaTicket: string;
  code: string;
}

/** Cuerpo de POST /api/v1/auth/register (vol. 1 §4.4.1). Incluye el
 *  consentimiento RGPD con las versiones vigentes de los documentos y el
 *  consentimiento granular de tratamiento de datos (RA-869f1xc2n). */
export interface RegisterCredentials {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
  acceptedTerms: boolean;
  acceptedPrivacy: boolean;
  acceptedTermsVersion: string;
  acceptedPrivacyVersion: string;
  acceptedDataProcessing: boolean;
}

/** Respuesta de GET /api/v1/legal/versions (versiones vigentes globales). */
export interface LegalVersions {
  termsVersion: string;
  privacyVersion: string;
}
