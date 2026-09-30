import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAuthStore, type AuthUser } from '../authStore';

/**
 * Store de sesión. El access token se persiste en localStorage con la clave
 * que lee el interceptor de client.ts; el refresh token, no (hasta 869f6r6hc).
 */

const user: AuthUser = {
  id: 7,
  email: 'ana@reservarte.com',
  firstName: 'Ana',
  lastName: 'López',
  rol: 'Employee',
};

beforeEach(() => {
  localStorage.clear();
  setActivePinia(createPinia());
});

describe('authStore', () => {
  it('arranca sin sesión si no hay token guardado', () => {
    const store = useAuthStore();

    expect(store.isAuthenticated).toBe(false);
    expect(store.accessToken).toBeNull();
  });

  it('arranca autenticado si hay token guardado (el usuario llega después)', () => {
    localStorage.setItem('authToken', 'jwt-guardado');

    const store = useAuthStore();

    expect(store.isAuthenticated).toBe(true);
    expect(store.accessToken).toBe('jwt-guardado');
    expect(store.user).toBeNull();
  });

  it('login guarda el access token para el interceptor, pero no el refresh token', () => {
    const store = useAuthStore();

    store.login({ user, accessToken: 'jwt-1', refreshToken: 'refresh-1' });

    expect(store.isAuthenticated).toBe(true);
    expect(store.user).toEqual(user);
    expect(store.refreshToken).toBe('refresh-1');
    expect(localStorage.getItem('authToken')).toBe('jwt-1');
    expect(Object.values(localStorage)).not.toContain('refresh-1');
  });

  it('login con 2FA pendiente deja el ticket y no autentica ni guarda nada', () => {
    const store = useAuthStore();

    store.login({ mfaRequired: true, mfaTicket: 'ticket-1', accessToken: 'no-debe-guardarse' });

    expect(store.isAuthenticated).toBe(false);
    expect(store.mfaRequired).toBe(true);
    expect(store.mfaTicket).toBe('ticket-1');
    expect(store.accessToken).toBeNull();
    expect(localStorage.getItem('authToken')).toBeNull();
  });

  it('setMfaVerified completa la sesión pendiente y descarta el ticket', () => {
    const store = useAuthStore();
    store.login({ mfaRequired: true, mfaTicket: 'ticket-1' });

    store.setMfaVerified({ user, accessToken: 'jwt-2', refreshToken: 'refresh-2' });

    expect(store.isAuthenticated).toBe(true);
    expect(store.mfaRequired).toBe(false);
    expect(store.mfaTicket).toBeNull();
    expect(localStorage.getItem('authToken')).toBe('jwt-2');
  });

  it('logout limpia el estado y el token guardado', () => {
    const store = useAuthStore();
    store.login({ user, accessToken: 'jwt-1', refreshToken: 'refresh-1' });

    store.logout();

    expect(store.$state).toMatchObject({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      mfaRequired: false,
      mfaTicket: null,
    });
    expect(localStorage.getItem('authToken')).toBeNull();
  });
});
