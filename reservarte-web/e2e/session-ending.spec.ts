import { test, expect } from '@playwright/test';

/**
 * Fin de sesión por código de error (RA-869f18urw).
 *
 * El interceptor de `client.ts` discrimina por `error.code`, no por status:
 *  - 403 + ORG_TENANT_MISMATCH → la sesión pertenece a otro tenant: se cierra.
 *  - 403 con cualquier otro código (p. ej. rol insuficiente, que llegará con
 *    los [Authorize(Roles=…)] del módulo de Empleados) → NO se cierra.
 *
 * El segundo caso es el que de verdad protege este spec: tratar «todo 403»
 * como fin de sesión expulsaría al usuario cada vez que toque algo sin permiso.
 */

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

/** Responde al endpoint protegido con el status y código indicados. */
async function stubAccountMe(page: import('@playwright/test').Page, status: number, code: string) {
  await page.route('**/api/v1/account/me', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      return route.fulfill({ status: 204, headers: CORS_HEADERS });
    }
    return route.fulfill({
      status,
      contentType: 'application/json',
      headers: CORS_HEADERS,
      body: JSON.stringify({
        success: false,
        data: null,
        error: { code, message: 'stub', details: null },
        meta: null,
      }),
    });
  });
}

/** Deja una sesión iniciada y aterriza en el área privada vía callback OAuth. */
async function startSession(page: import('@playwright/test').Page) {
  await page.route('**/api/v1/account/me', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: CORS_HEADERS,
      body: JSON.stringify({
        success: true,
        data: { id: '1', email: 'a@b.com', role: 'employee', organizationId: 'org' },
        error: null,
        meta: null,
      }),
    })
  );

  await page.goto('/auth/callback#access_token=fake-access&refresh_token=fake-refresh');
  await expect(page).toHaveURL('/');
  await page.unroute('**/api/v1/account/me');
}

test.describe('Fin de sesión por error.code', () => {
  test('403 ORG_TENANT_MISMATCH cierra la sesión y vuelve a login', async ({ page }) => {
    await startSession(page);
    await stubAccountMe(page, 403, 'ORG_TENANT_MISMATCH');

    // Provoca una llamada al endpoint protegido desde la propia app.
    await page.evaluate(() => {
      window.location.href = '/auth/callback#access_token=otro&refresh_token=otro';
    });

    await expect(page).toHaveURL(/\/login/);

    const token = await page.evaluate(() => localStorage.getItem('authToken'));
    expect(token).toBeNull();
  });

  test('403 de otro código NO cierra la sesión', async ({ page }) => {
    await startSession(page);
    await stubAccountMe(page, 403, 'GEN_FORBIDDEN');

    // Se provoca la llamada DESDE la app (el callback consulta /me con el
    // mismo cliente Axios), que es la única forma de ejercitar el interceptor:
    // un fetch suelto no pasaría por él y el test no probaría nada.
    await page.goto('/auth/callback#access_token=token-tras-403&refresh_token=r');

    // La página tolera que /me falle y sigue a la app: la sesión NO se cierra.
    await expect(page).toHaveURL('/');

    const token = await page.evaluate(() => localStorage.getItem('authToken'));
    expect(token).toBe('token-tras-403');
  });
});
