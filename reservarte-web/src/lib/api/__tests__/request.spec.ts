import { afterEach, describe, expect, it } from 'vitest';
import type { AxiosAdapter } from 'axios';
import apiClient from '../client';
import { apiPagedRequest } from '../request';

const originalAdapter = apiClient.defaults.adapter;

function respond(body: unknown, seen: { params?: unknown }[] = []): AxiosAdapter {
  return async (config) => {
    seen.push({ params: config.params });
    return { status: 200, statusText: '', headers: {}, config, data: body };
  };
}

afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
});

describe('apiPagedRequest', () => {
  it('devuelve los elementos y la paginación de meta, y pasa los parámetros', async () => {
    const seen: { params?: unknown }[] = [];
    const pagination = { page: 2, pageSize: 20, totalCount: 41, totalPages: 3 };
    apiClient.defaults.adapter = respond(
      { success: true, data: { items: [{ id: 1 }] }, error: null, meta: { pagination } },
      seen
    );

    const result = await apiPagedRequest('/api/v1/employees', { page: 2, isActive: true });

    expect(result).toEqual({ items: [{ id: 1 }], pagination });
    expect(seen[0]!.params).toEqual({ page: 2, isActive: true });
  });

  it('sin paginación en meta, la deduce de una sola página', async () => {
    apiClient.defaults.adapter = respond({
      success: true,
      data: { items: [{ id: 1 }, { id: 2 }] },
      error: null,
      meta: {},
    });

    const result = await apiPagedRequest('/api/v1/employees');

    expect(result.pagination).toEqual({ page: 1, pageSize: 2, totalCount: 2, totalPages: 1 });
  });
});
