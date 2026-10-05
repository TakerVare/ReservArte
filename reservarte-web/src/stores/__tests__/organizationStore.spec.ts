import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import type { AxiosAdapter } from 'axios';
import apiClient from '@lib/api/client';
import { ApiRequestError } from '@lib/api/request';
import { useAuthStore } from '../authStore';
import { useOrganizationStore } from '../organizationStore';

/**
 * Configuración del centro (RA-869f6r71x). La red se sustituye con el adaptador
 * de Axios: se prueba el cliente real con su envelope.
 */

const originalAdapter = apiClient.defaults.adapter;

const CANARIAS = {
  timeZone: 'Atlantic/Canary',
  cancellationHoursThreshold: 48,
  maxNoShowsBeforeBlock: 5,
  updatedAt: '2026-10-05T06:20:52Z',
};

interface Call {
  method?: string;
  url?: string;
  body?: unknown;
}

function respond(data: unknown, calls: Call[] = [], status = 200): AxiosAdapter {
  return async (config) => {
    calls.push({
      method: config.method,
      url: config.url,
      body: config.data ? JSON.parse(config.data as string) : undefined,
    });
    const body =
      status < 400
        ? { success: true, data, error: null, meta: {} }
        : { success: false, data: null, error: data, meta: {} };
    return {
      status,
      statusText: '',
      headers: {},
      config,
      data: body,
    };
  };
}

beforeEach(() => {
  localStorage.clear();
  setActivePinia(createPinia());
});

afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
});

describe('organizationStore', () => {
  it('sin configuración cargada, la zona es la de la península', () => {
    expect(useOrganizationStore().timeZone).toBe('Europe/Madrid');
  });

  it('ensureLoaded pide la configuración una sola vez y deja la zona del centro', async () => {
    const calls: Call[] = [];
    apiClient.defaults.adapter = respond(CANARIAS, calls);
    const store = useOrganizationStore();

    await Promise.all([store.ensureLoaded(), store.ensureLoaded()]);
    await store.ensureLoaded();

    expect(calls).toEqual([
      { method: 'get', url: '/api/v1/organization/settings', body: undefined },
    ]);
    expect(store.timeZone).toBe('Atlantic/Canary');
    expect(store.settings).toEqual(CANARIAS);
  });

  it('si la carga falla, ensureLoaded no lanza y la zona sigue siendo la por defecto', async () => {
    apiClient.defaults.adapter = respond({ code: 'GEN_INTERNAL_ERROR', message: 'Error' }, [], 500);
    const store = useOrganizationStore();

    await expect(store.ensureLoaded()).resolves.toBeUndefined();

    expect(store.settings).toBeNull();
    expect(store.timeZone).toBe('Europe/Madrid');
  });

  it('una respuesta sin zona (red simulada a medias) no deja la zona vacía', async () => {
    apiClient.defaults.adapter = respond({ items: [] });
    const store = useOrganizationStore();

    await store.ensureLoaded();

    expect(store.timeZone).toBe('Europe/Madrid');
  });

  it('load vuelve a pedirla aunque ya esté, y lanza si la API falla', async () => {
    const calls: Call[] = [];
    apiClient.defaults.adapter = respond(CANARIAS, calls);
    const store = useOrganizationStore();
    await store.load();
    await store.load();
    expect(calls).toHaveLength(2);

    apiClient.defaults.adapter = respond({ code: 'GEN_INTERNAL_ERROR', message: 'Error' }, [], 500);
    await expect(store.load()).rejects.toBeInstanceOf(ApiRequestError);
    expect(store.settings).toEqual(CANARIAS);
  });

  it('save envía la configuración entera y se queda con la respuesta', async () => {
    const calls: Call[] = [];
    apiClient.defaults.adapter = respond(CANARIAS, calls);
    const store = useOrganizationStore();
    const input = {
      timeZone: 'Atlantic/Canary',
      cancellationHoursThreshold: 48,
      maxNoShowsBeforeBlock: 5,
    };

    await store.save(input);

    expect(calls).toEqual([{ method: 'put', url: '/api/v1/organization/settings', body: input }]);
    expect(store.timeZone).toBe('Atlantic/Canary');
  });

  it('un 403 al guardar llega como ApiRequestError y no cambia la configuración', async () => {
    apiClient.defaults.adapter = respond(
      { code: 'GEN_FORBIDDEN', message: 'Sin permiso' },
      [],
      403
    );
    const store = useOrganizationStore();

    await expect(
      store.save({
        timeZone: 'Europe/Madrid',
        cancellationHoursThreshold: 24,
        maxNoShowsBeforeBlock: 3,
      })
    ).rejects.toMatchObject({ code: 'GEN_FORBIDDEN' });

    expect(store.settings).toBeNull();
  });

  it('al cerrar la sesión se olvida: la siguiente cuenta la vuelve a pedir', async () => {
    apiClient.defaults.adapter = respond(CANARIAS);
    const store = useOrganizationStore();
    await store.ensureLoaded();

    useAuthStore().logout();

    expect(store.settings).toBeNull();
    expect(store.timeZone).toBe('Europe/Madrid');
  });
});
