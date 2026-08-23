import axios from 'axios';
import apiClient from '@lib/api/client';
import type {
  AuthApiResponse,
  LoginCredentials,
  ApiErrorShape,
  OAuthProvider,
} from '../types/auth.types';

interface ApiEnvelope<T> {
  success: boolean;
  data: T | null;
  error: ApiErrorShape | null;
}

export class AuthApiError extends Error {
  code: string;
  details?: unknown;

  constructor(error: ApiErrorShape) {
    super(error.message);
    this.name = 'AuthApiError';
    this.code = error.code;
    this.details = error.details;
  }
}

const UNKNOWN_ERROR: ApiErrorShape = {
  code: 'UNKNOWN',
  message: 'Ha ocurrido un error inesperado.',
};

const NETWORK_ERROR: ApiErrorShape = {
  code: 'NETWORK_ERROR',
  message: 'No se pudo conectar con el servidor. Comprueba tu conexión.',
};

function toAuthApiError(err: unknown): AuthApiError {
  if (axios.isAxiosError(err)) {
    const envelopeError = (err.response?.data as ApiEnvelope<unknown> | undefined)?.error;
    return new AuthApiError(envelopeError ?? NETWORK_ERROR);
  }
  return new AuthApiError(UNKNOWN_ERROR);
}

/** POST /api/v1/auth/login (vol. 1 §4.4.1). */
export async function login(credentials: LoginCredentials): Promise<AuthApiResponse> {
  try {
    const { data: envelope } = await apiClient.post<ApiEnvelope<AuthApiResponse>>(
      '/api/v1/auth/login',
      credentials
    );

    if (!envelope.success || !envelope.data) {
      throw new AuthApiError(envelope.error ?? UNKNOWN_ERROR);
    }

    return envelope.data;
  } catch (err) {
    if (err instanceof AuthApiError) throw err;
    throw toAuthApiError(err);
  }
}

/**
 * URL de reto OAuth (GET, navegación completa del navegador — no una
 * llamada Axios — ya que el backend responde con un 302 al proveedor).
 */
export function getOAuthChallengeUrl(provider: OAuthProvider): string {
  const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5555';
  const appUrl = import.meta.env.VITE_APP_URL || window.location.origin;
  const returnUrl = `${appUrl}/auth/callback`;

  return `${baseUrl}/api/v1/auth/external/${provider}/challenge?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export interface CurrentUserClaims {
  id: string;
  email: string;
  role: string;
  organizationId: string;
}

/**
 * GET /api/v1/account/me. Necesario tras el callback OAuth: el fragmento
 * de retorno solo trae los tokens (vol. 1 §4.4), no los datos del usuario.
 */
export async function fetchCurrentUser(): Promise<CurrentUserClaims> {
  try {
    const { data: envelope } =
      await apiClient.get<ApiEnvelope<CurrentUserClaims>>('/api/v1/account/me');

    if (!envelope.success || !envelope.data) {
      throw new AuthApiError(envelope.error ?? UNKNOWN_ERROR);
    }

    return envelope.data;
  } catch (err) {
    if (err instanceof AuthApiError) throw err;
    throw toAuthApiError(err);
  }
}
