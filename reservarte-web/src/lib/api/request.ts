import axios from 'axios';
import apiClient from './client';

/** Espejo de `ApiErrorDetail` (ReservArte-Shared/Api). */
export interface ApiErrorDetail {
  field?: string;
  code?: string;
  message: string;
}

interface ApiErrorBody {
  code: string;
  message: string;
  details?: ApiErrorDetail[] | null;
}

interface ApiEnvelope<T> {
  success: boolean;
  data: T | null;
  error: ApiErrorBody | null;
}

/**
 * Error de una llamada a la API con el código del envelope (`APT_ACTIVE_EXISTS`,
 * `GEN_VALIDATION_FAILED`…) y sus detalles por campo. Sin respuesta de la API,
 * `NETWORK_ERROR`.
 */
export class ApiRequestError extends Error {
  code: string;
  details: ApiErrorDetail[];

  constructor(error: ApiErrorBody) {
    super(error.message);
    this.name = 'ApiRequestError';
    this.code = error.code;
    this.details = error.details ?? [];
  }
}

const UNKNOWN_ERROR: ApiErrorBody = {
  code: 'UNKNOWN',
  message: 'Ha ocurrido un error inesperado.',
};
const NETWORK_ERROR: ApiErrorBody = {
  code: 'NETWORK_ERROR',
  message: 'No se pudo conectar con el servidor. Comprueba tu conexión.',
};

/**
 * Llama a la API y desenvuelve el envelope (RA-869fagpyg): devuelve `data` o lanza
 * `ApiRequestError`. Las features lo usan en vez de repetir el desenvuelto.
 */
export async function apiRequest<T>(
  method: 'get' | 'post' | 'put' | 'delete',
  url: string,
  options: { params?: Record<string, unknown>; body?: unknown } = {}
): Promise<T> {
  try {
    const { data: envelope } = await apiClient.request<ApiEnvelope<T>>({
      method,
      url,
      params: options.params,
      data: options.body,
    });
    if (!envelope.success || envelope.data === null) {
      throw new ApiRequestError(envelope.error ?? UNKNOWN_ERROR);
    }
    return envelope.data;
  } catch (err) {
    if (err instanceof ApiRequestError) throw err;
    if (axios.isAxiosError(err)) {
      const body = (err.response?.data as ApiEnvelope<unknown> | undefined)?.error;
      throw new ApiRequestError(body ?? (err.response ? UNKNOWN_ERROR : NETWORK_ERROR));
    }
    throw new ApiRequestError(UNKNOWN_ERROR);
  }
}
