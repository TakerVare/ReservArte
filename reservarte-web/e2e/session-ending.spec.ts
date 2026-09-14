import { test, expect } from '@playwright/test';

/**
 * Fin de sesión por código de error (RA-869f18urw).
 *
 * En el 403 el interceptor de `client.ts` discrimina por `error.code` (en el
 * 401 decide por status, con la lista de endpoints exceptuados: login, MFA y
 * refresh, cuyos 401 son resultado de negocio).
 *
 * Todos los 403 de la API llevan envelope (desde RA-869f1anz3), pero no todos
 * significan lo mismo, y este spec separa los que se confunden:
 *  - `ORG_TENANT_MISMATCH` (TenantMiddleware): la sesión es de otro tenant →
 *    es el único que cierra la sesión.
 *  - `GEN_FORBIDDEN`: el 403 real de `[Authorize(Roles=…)]` (evento
 *    OnForbidden de JwtBearer) y de las reglas por dato de los servicios
 *    (p. ej. un Manager que intenta tocar a un Admin, RA-869d7ezz4) →
 *    «no tienes permiso», NO cierra la sesión.
 *  - **Sin cuerpo**: la API ya no lo emite, pero puede llegar de un proxy o
 *    WAF intermedio → el interceptor no debe romperse ni cerrar la sesión.
 *
 * La enumeración NO es el catálogo completo de 403: un controlador puede
 * devolver otro 403 de negocio con envelope (p. ej. `CUST_BLOCKED`), que
 * tampoco cerraría la sesión.
 */

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

/**
 * Responde al endpoint protegido con el envelope de error y el `code` dado.
 * Con `code === null` responde SIN CUERPO: la forma que tenía el 403 de
 * `[Authorize(Roles=…)]` antes de RA-869f1anz3, y la que aún puede llegar de
 * un proxy intermedio.
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

test.describe('Fin de sesión: 401 por status, 403 por error.code', () => {
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

  test('403 SIN cuerpo (p. ej. de un proxy) no rompe el interceptor ni cierra la sesión', async ({
    page,
  }) => {
    await startSession(page);

    // Sin cuerpo no hay error.code que consultar: el interceptor no debe
    // romperse ni cerrar sesión. La API ya no lo emite (RA-869f1anz3), pero
    // un intermediario sí podría.
    await stubAccountMe(page, 403, null);

    await page.goto('/auth/callback#access_token=token-sin-envelope&refresh_token=r');

    await expect(page).toHaveURL('/');

    const token = await page.evaluate(() => localStorage.getItem('authToken'));
    expect(token).toBe('token-sin-envelope');
  });

  // Forma real del 403 de rol desde RA-869f1anz3: envelope con GEN_FORBIDDEN
  // (verificado en runtime contra /api/v1/employees con un token de Employee).
  test('403 GEN_FORBIDDEN (el real de [Authorize(Roles)]) NO cierra la sesión', async ({
    page,
  }) => {
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
