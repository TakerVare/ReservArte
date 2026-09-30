import { test, expect } from '@playwright/test';

/**
 * Un solo mecanismo de URL de la API (RA-869f6r69b): la SPA llama a `/api/...`
 * en su mismo origen y, en desarrollo, el proxy de Vite lo reenvía a la API.
 *
 * Se comprueba la URL real de la petición, no su resultado: `page.route`
 * resuelve el CORS de las respuestas interceptadas, así que un cliente que
 * volviera a apuntar a otro origen (p. ej. una baseURL absoluta) seguiría
 * pasando cualquier test que solo mirase la respuesta.
 */

test.describe('Origen de la API', () => {
  test('las llamadas Axios van a /api del mismo origen que la SPA', async ({ page, baseURL }) => {
    let requestUrl = '';
    await page.route('**/api/v1/auth/login', (route) => {
      requestUrl = route.request().url();
      return route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          data: null,
          error: { code: 'AUTH_INVALID_CREDENTIALS', message: 'Email o contraseña incorrectos.' },
          meta: {},
        }),
      });
    });

    await page.goto('/login');
    await page.getByLabel('Usuario').fill('test@test.com');
    await page.getByLabel('Contraseña').fill('wrongpass');
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect.poll(() => requestUrl).not.toBe('');

    const url = new URL(requestUrl);
    expect(url.origin).toBe(new URL(baseURL!).origin);
    expect(url.pathname).toBe('/api/v1/auth/login');
  });

  test('el reto OAuth va a /api del mismo origen y vuelve al origen de la SPA', async ({
    page,
    baseURL,
  }) => {
    const origin = new URL(baseURL!).origin;
    let challengeUrl = '';
    await page.route('**/api/v1/auth/external/**', (route) => {
      challengeUrl = route.request().url();
      return route.fulfill({ status: 200, contentType: 'text/html', body: '<p>reto</p>' });
    });

    await page.goto('/login');
    await page.getByRole('button', { name: 'Continuar con Google' }).click();
    await expect.poll(() => challengeUrl).not.toBe('');

    const url = new URL(challengeUrl);
    expect(url.origin).toBe(origin);
    expect(url.pathname).toBe('/api/v1/auth/external/google/challenge');
    expect(url.searchParams.get('returnUrl')).toBe(`${origin}/auth/callback`);
  });
});
