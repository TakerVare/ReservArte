import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, type AxiosAdapter, type InternalAxiosRequestConfig } from 'axios';
import apiClient from '../client';

/**
 * Interceptores de `client.ts`: Bearer en cada petición y fin de sesión ante
 * un 401 de un endpoint protegido o un 403 con código de fin de sesión. La red
 * se sustituye por un adaptador de Axios, así que se prueba el cliente real.
 */

let lastRequest: InternalAxiosRequestConfig | undefined;
const originalAdapter = apiClient.defaults.adapter;

/** Adaptador que registra la petición y responde con el status y cuerpo dados. */
function respondWith(status: number, data: unknown = null): AxiosAdapter {
  return async (config) => {
    lastRequest = config;
    const response = { status, statusText: '', headers: {}, config, data };
    if (status >= 400) {
      throw new AxiosError('HTTP ' + status, undefined, config, null, response);
    }
    return response;
  };
}

function errorEnvelope(code: string) {
  return { success: false, data: null, error: { code, message: code }, meta: {} };
}

let location: { href: string };

beforeEach(() => {
  localStorage.clear();
  lastRequest = undefined;
  location = { href: 'http://localhost:3000/app' };
  vi.spyOn(window, 'location', 'get').mockReturnValue(location as unknown as Location);
});

afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
});

describe('petición', () => {
  it('adjunta el token Bearer si hay sesión', async () => {
    localStorage.setItem('authToken', 'jwt-123');
    apiClient.defaults.adapter = respondWith(200);

    await apiClient.get('/api/v1/account/me');

    expect(lastRequest?.headers.Authorization).toBe('Bearer jwt-123');
  });

  it('no manda Authorization sin sesión', async () => {
    apiClient.defaults.adapter = respondWith(200);

    await apiClient.get('/api/v1/services');

    expect(lastRequest?.headers.Authorization).toBeUndefined();
  });

  it('usa rutas relativas: no hay baseURL (RA-869f6r69b)', async () => {
    apiClient.defaults.adapter = respondWith(200);

    await apiClient.get('/api/v1/services');

    expect(lastRequest?.baseURL).toBeUndefined();
    expect(lastRequest?.url).toBe('/api/v1/services');
  });
});

describe('fin de sesión', () => {
  it('un 401 de un endpoint protegido borra el token y vuelve a /login', async () => {
    localStorage.setItem('authToken', 'jwt-123');
    apiClient.defaults.adapter = respondWith(401, errorEnvelope('GEN_UNAUTHORIZED'));

    await expect(apiClient.get('/api/v1/account/me')).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem('authToken')).toBeNull();
    expect(location.href).toBe('/login');
  });

  it.each([
    '/api/v1/auth/login',
    '/api/v1/auth/mfa/verify',
    '/api/v1/auth/refresh-token',
    '/api/v1/auth/set-password',
    '/api/v1/auth/reset-password',
  ])('un 401 de %s es un resultado de negocio: no cierra la sesión', async (url) => {
    localStorage.setItem('authToken', 'jwt-123');
    apiClient.defaults.adapter = respondWith(401, errorEnvelope('AUTH_INVALID_CREDENTIALS'));

    await expect(apiClient.post(url, {})).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem('authToken')).toBe('jwt-123');
    expect(location.href).toBe('http://localhost:3000/app');
  });

  it('un 403 ORG_TENANT_MISMATCH cierra la sesión', async () => {
    localStorage.setItem('authToken', 'jwt-123');
    apiClient.defaults.adapter = respondWith(403, errorEnvelope('ORG_TENANT_MISMATCH'));

    await expect(apiClient.get('/api/v1/employees')).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem('authToken')).toBeNull();
    expect(location.href).toBe('/login');
  });

  it.each([
    ['GEN_FORBIDDEN', errorEnvelope('GEN_FORBIDDEN')],
    ['CUST_BLOCKED', errorEnvelope('CUST_BLOCKED')],
    ['sin cuerpo', null],
  ])('un 403 %s solo deniega la operación: no cierra la sesión', async (_, body) => {
    localStorage.setItem('authToken', 'jwt-123');
    apiClient.defaults.adapter = respondWith(403, body);

    await expect(apiClient.get('/api/v1/employees')).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem('authToken')).toBe('jwt-123');
    expect(location.href).toBe('http://localhost:3000/app');
  });

  it('otros errores (500, error de red) no cierran la sesión', async () => {
    localStorage.setItem('authToken', 'jwt-123');
    apiClient.defaults.adapter = respondWith(500, errorEnvelope('GEN_INTERNAL_ERROR'));
    await expect(apiClient.get('/api/v1/employees')).rejects.toBeInstanceOf(AxiosError);

    apiClient.defaults.adapter = async (config) => {
      throw new AxiosError('Network Error', AxiosError.ERR_NETWORK, config);
    };
    await expect(apiClient.get('/api/v1/employees')).rejects.toBeInstanceOf(AxiosError);

    expect(localStorage.getItem('authToken')).toBe('jwt-123');
    expect(location.href).toBe('http://localhost:3000/app');
  });
});
