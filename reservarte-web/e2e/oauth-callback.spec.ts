import { test, expect } from '@playwright/test';

/**
 * E2E de la pantalla de retorno OAuth (RA-869d7f7r1).
 *
 * El backend redirige a /auth/callback con los tokens en el FRAGMENTO de la
 * URL (vol. 1 §4.4), o con #error=<código> si el proveedor falló. Estos tests
 * cubren los dos caminos sin necesitar la API real: el fragmento lo escribe
 * el propio navegador y la única llamada de red (/account/me) se intercepta.
 */

const OAUTH_ERROR_TEXT =
  'No se pudo completar el inicio de sesión con el proveedor externo. Inténtalo de nuevo.';

/** Cabeceras CORS: la SPA (:3000) llama a la API (:5555) en otro origen. */
const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

test.describe('OAuthCallbackPage', () => {
  test('con tokens en el fragmento inicia sesión y entra en la app', async ({ page }) => {
    await page.route('**/api/v1/account/me', async (route) => {
      if (route.request().method() === 'OPTIONS') {
        return route.fulfill({ status: 204, headers: CORS_HEADERS });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: CORS_HEADERS,
        body: JSON.stringify({
          success: true,
          data: {
            id: '42',
            email: 'oauth@reservarte.com',
            role: 'Admin',
            organizationId: 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE',
          },
          error: null,
          meta: null,
        }),
      });
    });

    await page.goto('/auth/callback#access_token=fake-access&refresh_token=fake-refresh');

    // Sesión iniciada: aterriza en el área privada (el guard requiresAuth
    // la habría devuelto a /login si el store no tuviera sesión).
    await expect(page).toHaveURL('/');

    // El token queda disponible para el interceptor Bearer de client.ts.
    const storedToken = await page.evaluate(() => localStorage.getItem('authToken'));
    expect(storedToken).toBe('fake-access');

    // Los tokens no sobreviven en la barra de direcciones ni en el historial.
    const hash = await page.evaluate(() => window.location.hash);
    expect(hash).toBe('');
  });

  test('con error del proveedor vuelve a login con el aviso', async ({ page }) => {
    await page.goto('/auth/callback#error=external_auth_failed');

    await expect(page).toHaveURL('/login?error=oauth_failed');
    await expect(page.getByRole('alert')).toHaveText(OAUTH_ERROR_TEXT);
  });

  test('sin fragmento vuelve a login con el aviso', async ({ page }) => {
    await page.goto('/auth/callback');

    await expect(page).toHaveURL('/login?error=oauth_failed');
    await expect(page.getByRole('alert')).toHaveText(OAUTH_ERROR_TEXT);
  });

  test('el retorno fallido no deja sesión iniciada', async ({ page }) => {
    await page.goto('/auth/callback#error=external_auth_failed');
    await expect(page).toHaveURL('/login?error=oauth_failed');

    const storedToken = await page.evaluate(() => localStorage.getItem('authToken'));
    expect(storedToken).toBeNull();
  });
});
