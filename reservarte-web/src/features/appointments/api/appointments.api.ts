import axios from 'axios';
import apiClient from '@lib/api/client';
import type { ApiErrorShape } from '@features/auth/types/auth.types';
import type { AppointmentSummary } from '../types/appointment.types';

interface ApiEnvelope<T> {
  success: boolean;
  data: T | null;
  error: ApiErrorShape | null;
}

export class AppointmentsApiError extends Error {
  code: string;

  constructor(error: ApiErrorShape) {
    super(error.message);
    this.name = 'AppointmentsApiError';
    this.code = error.code;
  }
}

const UNKNOWN_ERROR: ApiErrorShape = {
  code: 'UNKNOWN',
  message: 'Ha ocurrido un error inesperado.',
};

/** Tope de `pageSize` que acepta la API. */
const MAX_PAGE_SIZE = 100;

/**
 * GET /api/v1/appointments desde `from` (`yyyy-MM-dd`), solo las activas.
 * A una clienta la API le devuelve solo sus citas (RA-869d7f519).
 */
export async function getAppointmentsFrom(from: string): Promise<AppointmentSummary[]> {
  try {
    const { data: envelope } = await apiClient.get<ApiEnvelope<{ items: AppointmentSummary[] }>>(
      '/api/v1/appointments',
      { params: { from, pageSize: MAX_PAGE_SIZE } }
    );

    if (!envelope.success || !envelope.data) {
      throw new AppointmentsApiError(envelope.error ?? UNKNOWN_ERROR);
    }

    return envelope.data.items;
  } catch (err) {
    if (err instanceof AppointmentsApiError) throw err;
    const envelopeError = axios.isAxiosError(err)
      ? (err.response?.data as ApiEnvelope<unknown> | undefined)?.error
      : undefined;
    throw new AppointmentsApiError(envelopeError ?? UNKNOWN_ERROR);
  }
}
