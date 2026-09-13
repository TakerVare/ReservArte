import { test, expect } from '@playwright/test';

/**
 * Fin de sesión por código de error (RA-869f18urw).
 *
 * En el 403 el interceptor de `client.ts` discrimina por `error.code` (en el
 * 401 decide por status, con la lista de endpoints exceptuados: login, MFA y
 * refresh, cuyos 401 son resultado de negocio).
 *
 * Los 403 NO tienen todos la misma forma, y conviene no confundirlos:
 *  - **Con envelope**, emitidos por TenantMiddleware → traen `error.code`.
 *    `ORG_TENANT_MISMATCH` es el único que hoy cierra la sesión.
 *  - **Sin cuerpo**, emitidos por el middleware de autorización de ASP.NET
 *    Core ([Authorize(Roles=…)]) → no traen nada que leer. Es la forma que
 *    tendrá el 403 de rol cuando llegue RA-869d7ezz4.
 *
 * Los dos últimos casos de este spec cubren esa distinción: ninguno debe
 * cerrar la sesión, pero por motivos distintos.
 */

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

/**
 * Responde al endpoint protegido. Con `code === null` responde SIN CUERPO, que
 * es exactamente lo que hace ASP.NET Core en un 403 de `[Authorize(Roles=…)]`:
 * lo emite el middleware de autorización, sin pasar por los controladores ni
 * por el envelope (verificado en runtime: 401 sin token → 0 bytes de cuerpo).
 */
async function stubAccountMe(
  page: import('@playwright/test').Page,
  status: number,
  code: string | null
) {
  await page.route('**/api/v1/account/me', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      return route.fulfill({ status: 204, headers: CORS_HEADERS });
    }
    if (code === null) {
      return route.fulfill({ status, headers: CORS_HEADERS, body: '' });
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

  test('403 SIN envelope (el real de [Authorize(Roles)]) NO cierra la sesión', async ({ page }) => {
    await startSession(page);

    // Sin cuerpo no hay error.code que consultar: el interceptor no debe
    // romperse ni cerrar sesión. Es la forma que tendrá el 403 de rol cuando
    // llegue RA-869d7ezz4 (ver RA-869f1anz3).
    await stubAccountMe(page, 403, null);

    await page.goto('/auth/callback#access_token=token-sin-envelope&refresh_token=r');

    await expect(page).toHaveURL('/');

    const token = await page.evaluate(() => localStorage.getItem('authToken'));
    expect(token).toBe('token-sin-envelope');
  });

  // Escenario de RA-869f1anz3: si algún día los 403 de autorización pasan a
  // llevar envelope, tendrán un código de permiso. Tampoco entonces debe
  // cerrarse la sesión. Hoy este caso NO representa al 403 de rol (que va sin
  // cuerpo, cubierto por el test anterior).
  test('403 CON envelope de otro código tampoco cierra la sesión', async ({ page }) => {
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
