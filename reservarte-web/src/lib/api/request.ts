import axios from 'axios';
import apiClient from './client';
import type { PagedResult, Pagination } from '@lib/composables/useDataList';

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
  meta?: { pagination?: Pagination | null } | null;
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

type Method = 'get' | 'post' | 'put' | 'delete';
type RequestOptions = { params?: Record<string, unknown>; body?: unknown };

async function send<T>(
  method: Method,
  url: string,
  options: RequestOptions
): Promise<ApiEnvelope<T> & { data: T }> {
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
    return envelope as ApiEnvelope<T> & { data: T };
  } catch (err) {
    if (err instanceof ApiRequestError) throw err;
    if (axios.isAxiosError(err)) {
      const body = (err.response?.data as ApiEnvelope<unknown> | undefined)?.error;
      throw new ApiRequestError(body ?? (err.response ? UNKNOWN_ERROR : NETWORK_ERROR));
    }
    throw new ApiRequestError(UNKNOWN_ERROR);
  }
}

/**
 * Llama a la API y desenvuelve el envelope (RA-869fagpyg): devuelve `data` o lanza
 * `ApiRequestError`. Las features lo usan en vez de repetir el desenvuelto.
 */
export async function apiRequest<T>(
  method: Method,
  url: string,
  options: RequestOptions = {}
): Promise<T> {
  return (await send<T>(method, url, options)).data;
}

/**
 * GET de un listado paginado (RA-869d7fbyt): `data.items` y la paginación de
 * `meta.pagination`, ya en la forma de `useDataList`.
 */
export async function apiPagedRequest<T>(
  url: string,
  params: Record<string, unknown> = {}
): Promise<PagedResult<T>> {
  const envelope = await send<{ items: T[] }>('get', url, { params });
  const pagination = envelope.meta?.pagination ?? {
    page: 1,
    pageSize: envelope.data.items.length,
    totalCount: envelope.data.items.length,
    totalPages: 1,
  };
  return { items: envelope.data.items, pagination };
}
