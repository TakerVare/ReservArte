import { test, expect, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Configuración del centro (RA-869f6r71x) con la API simulada: carga, edición,
 * errores del formulario y de la API, y quién ve la entrada en el menú. Misma
 * excepción de contraste que login.a11y.spec.ts (RA-869f0v6vm).
 */

const SETTINGS = '**/api/v1/organization/settings';

const ok = (data: unknown) => ({
  status: 200,
  contentType: 'application/json',
  body: JSON.stringify({ success: true, data, error: null, meta: {} }),
});
const fail = (status: number, code: string, message: string, details: unknown = null) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({ success: false, data: null, error: { code, message, details }, meta: {} }),
});

const DEFAULTS = {
  timeZone: 'Europe/Madrid',
  cancellationHoursThreshold: 24,
  maxNoShowsBeforeBlock: 3,
  updatedAt: null,
};

interface Calls {
  gets: number;
  puts: unknown[];
}

/** Sesión del rol indicado, en la pantalla de Usuario. La configuración responde lo guardado. */
async function startSessionAs(page: Page, rol: string) {
  const calls: Calls = { gets: 0, puts: [] };
  let saved: Record<string, unknown> = { ...DEFAULTS };

  // Lo no simulado responde vacío (regla de frontend).
  await page.route('**/api/v1/**', (route) => route.fulfill(ok({ items: [] })));
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill(
      ok({
        accessToken: 'fake',
        refreshToken: 'r',
        mfaRequired: false,
        mfaTicket: null,
        user: { id: 1, email: 'admin@reservarte.com', firstName: 'A', lastName: 'B', rol },
      })
    )
  );
  await page.route(SETTINGS, (route) => {
    const request = route.request();
    if (request.method() === 'PUT') {
      calls.puts.push(request.postDataJSON());
      saved = { ...request.postDataJSON(), updatedAt: '2026-10-05T06:20:52Z' };
      return route.fulfill(ok(saved));
    }
    calls.gets += 1;
    return route.fulfill(ok(saved));
  });

  await page.goto('/login');
  await page.getByLabel('Usuario').fill('admin@reservarte.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL('/mis-citas');
  await page.getByRole('link', { name: 'Mi cuenta' }).click();
  await expect(page).toHaveURL('/cuenta');
  return calls;
}

/** «Configuración» del Área de administración (la del Área de usuario es otra pantalla). */
async function openSettings(page: Page) {
  await page.getByRole('link', { name: 'Configuración' }).first().click();
  await expect(page).toHaveURL('/configuracion');
  await expect(page.getByRole('heading', { name: 'Configuración del centro' })).toBeVisible();
}

test.describe('Configuración del centro', () => {
  test('muestra la configuración guardada y la reemplaza entera al guardar', async ({ page }) => {
    const calls = await startSessionAs(page, 'Admin');
    await openSettings(page);

    await expect(page.getByLabel('Zona horaria')).toContainText('Península y Baleares');
    await expect(page.getByLabel('Cancelación tardía (horas)')).toHaveValue('24');
    await expect(page.getByLabel('No presentaciones antes de bloquear')).toHaveValue('3');

    await page.getByLabel('Zona horaria').click();
    await page.getByRole('option', { name: 'Canarias (Atlantic/Canary)' }).click();
    await page.getByLabel('Cancelación tardía (horas)').fill('48');
    await page.getByLabel('No presentaciones antes de bloquear').fill('5');
    await page.getByRole('button', { name: 'Guardar' }).click();

    await expect(page.locator('[data-type="success"]')).toContainText('Configuración guardada.');
    expect(calls.puts).toEqual([
      { timeZone: 'Atlantic/Canary', cancellationHoursThreshold: 48, maxNoShowsBeforeBlock: 5 },
    ]);
    await expect(page.getByLabel('Zona horaria')).toContainText('Canarias');
    await expect(page.getByLabel('Cancelación tardía (horas)')).toHaveValue('48');

    // Al volver a entrar se pide de nuevo y sale lo guardado.
    await page.getByRole('button', { name: 'Volver' }).click();
    await expect(page).toHaveURL('/cuenta');
    await openSettings(page);
    await expect(page.getByLabel('Zona horaria')).toContainText('Canarias');
    await expect(page.getByLabel('No presentaciones antes de bloquear')).toHaveValue('5');
    expect(calls.gets).toBe(2);
  });

  test('valida los rangos antes de enviar y admite 0 horas', async ({ page }) => {
    const calls = await startSessionAs(page, 'Manager');
    await openSettings(page);

    await page.getByLabel('Cancelación tardía (horas)').fill('');
    await page.getByLabel('No presentaciones antes de bloquear').fill('0');
    await page.getByRole('button', { name: 'Guardar' }).click();

    await expect(page.getByText('Indica las horas.')).toBeVisible();
    await expect(page.getByText('Debe estar entre 1 y 99.')).toBeVisible();
    expect(calls.puts).toEqual([]);

    await page.getByLabel('Cancelación tardía (horas)').fill('721');
    await expect(page.getByText('Debe estar entre 0 y 720 horas.')).toBeVisible();

    await page.getByLabel('Cancelación tardía (horas)').fill('0');
    await page.getByLabel('No presentaciones antes de bloquear').fill('1');
    await page.getByRole('button', { name: 'Guardar' }).click();

    await expect(page.locator('[data-type="success"]')).toContainText('Configuración guardada.');
    expect(calls.puts).toEqual([
      { timeZone: 'Europe/Madrid', cancellationHoursThreshold: 0, maxNoShowsBeforeBlock: 1 },
    ]);
  });

  test('el error de la API sale en su campo, y un 403 como aviso', async ({ page }) => {
    await startSessionAs(page, 'Admin');
    await openSettings(page);

    await page.route(SETTINGS, (route) =>
      route.request().method() === 'PUT'
        ? route.fulfill(
            fail(400, 'GEN_VALIDATION_FAILED', 'La petición no supera las validaciones.', [
              {
                field: 'timeZone',
                code: 'UnknownTimeZone',
                message: 'La zona horaria no es válida.',
              },
            ])
          )
        : route.fallback()
    );
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.getByText('La zona horaria no es válida.')).toBeVisible();

    await page.unroute(SETTINGS);
    await page.route(SETTINGS, (route) =>
      route.request().method() === 'PUT'
        ? route.fulfill(
            fail(403, 'GEN_FORBIDDEN', 'No tienes permiso para realizar esta operación.')
          )
        : route.fulfill(ok(DEFAULTS))
    );
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.locator('[data-type="error"]')).toContainText(
      'No tienes permiso para realizar esta operación.'
    );
    // Un 403 de permiso no cierra la sesión.
    await expect(page).toHaveURL('/configuracion');
  });

  test('si no carga, avisa y deja reintentar', async ({ page }) => {
    await startSessionAs(page, 'Admin');
    await page.route(SETTINGS, (route) =>
      route.fulfill(fail(500, 'GEN_INTERNAL_ERROR', 'Error interno.'))
    );
    await page.getByRole('link', { name: 'Configuración' }).first().click();

    await expect(page.getByText('No se ha podido cargar la configuración.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Guardar' })).toHaveCount(0);

    await page.unroute(SETTINGS);
    await page.route(SETTINGS, (route) => route.fulfill(ok(DEFAULTS)));
    await page.getByRole('button', { name: 'Reintentar' }).click();

    await expect(page.getByLabel('Cancelación tardía (horas)')).toHaveValue('24');
  });

  test('una empleada no tiene la entrada en el Área de administración', async ({ page }) => {
    await startSessionAs(page, 'Employee');

    await expect(page.getByRole('heading', { name: 'Área de administración' })).toBeVisible();
    // Solo queda la «Configuración» de la cuenta, en el Área de usuario.
    await expect(page.getByRole('link', { name: 'Configuración' })).toHaveCount(1);
    await page.getByRole('link', { name: 'Configuración' }).click();
    await expect(page).toHaveURL('/cuenta/configuracion');
  });

  // Sin el desplegable abierto: Reka (patrón de Radix) oculta el resto de la página
  // con aria-hidden mientras está abierto y axe lo marca (`aria-hidden-focus`); los
  // demás specs tampoco lo miden así.
  test('cumple WCAG 2.1 AA, también con errores', async ({ page }) => {
    await startSessionAs(page, 'Admin');
    await openSettings(page);
    const axe = () =>
      new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();

    expect((await axe()).violations).toEqual([]);
    await page.getByLabel('Cancelación tardía (horas)').fill('');
    await page.getByLabel('No presentaciones antes de bloquear').fill('0');
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.getByText('Indica las horas.')).toBeVisible();
    expect((await axe()).violations).toEqual([]);
  });
});
