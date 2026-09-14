import { test, expect } from '@playwright/test';

/**
 * Invitación de alta de empleado (RA-869f17y68).
 *
 * Mismo contrato de token que el restablecimiento: en el enlace del email viaja
 * URL-encoded y Vue Router DECODIFICA el segmento `:token`, así que el POST
 * debe llevarlo decodificado EXACTAMENTE UNA VEZ. Lo que cambia es el destino:
 * `/auth/set-password`, porque el token de invitación lo emite otro proveedor
 * (7 días) y `/auth/reset-password` lo rechazaría.
 */

// Token sintético con los tres caracteres conflictivos de base64: '+' '/' '='.
const RAW_TOKEN = 'CfDJ8+abc/def+ghi==';
const ENCODED_TOKEN = encodeURIComponent(RAW_TOKEN);

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

test.describe('SetPasswordPage — invitación de alta', () => {
  test('el POST va a set-password con el token decodificado exactamente una vez', async ({
    page,
  }) => {
    let tokenEnviado: string | undefined;

    await page.route('**/api/v1/auth/set-password', async (route) => {
      if (route.request().method() === 'OPTIONS') {
        return route.fulfill({ status: 204, headers: CORS_HEADERS });
      }
      tokenEnviado = route.request().postDataJSON()?.token;
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: CORS_HEADERS,
        body: JSON.stringify({ success: true, data: {}, error: null, meta: null }),
      });
    });

    await page.goto(`/set-password/${ENCODED_TOKEN}`);

    await page.getByLabel('Email').fill('ana@reservarte.com');
    await page.getByLabel('Contraseña', { exact: true }).fill('Nueva1234!');
    await page.getByLabel('Repite la contraseña').fill('Nueva1234!');
    await page.getByRole('button', { name: 'Guardar contraseña' }).click();

    await expect
      .poll(() => tokenEnviado, { message: 'el POST de set-password no llegó a interceptarse' })
      .toBeDefined();

    expect(tokenEnviado).toBe(RAW_TOKEN);

    // Tras el éxito, la página ofrece ir a login.
    await expect(page.getByText('Contraseña creada')).toBeVisible();
  });

  test('sin token en la URL no se muestra el formulario', async ({ page }) => {
    await page.goto('/set-password/');

    await expect(page.getByText('Enlace no válido')).toBeVisible();
    await expect(page.getByLabel('Contraseña', { exact: true })).toHaveCount(0);
  });

  test('un enlace caducado o ya usado muestra el error del backend', async ({ page }) => {
    await page.route('**/api/v1/auth/set-password', async (route) => {
      if (route.request().method() === 'OPTIONS') {
        return route.fulfill({ status: 204, headers: CORS_HEADERS });
      }
      return route.fulfill({
        status: 401,
        contentType: 'application/json',
        headers: CORS_HEADERS,
        body: JSON.stringify({
          success: false,
          data: null,
          error: {
            code: 'AUTH_INVALID_CREDENTIALS',
            message: 'El enlace de invitación no es válido o ha caducado.',
            details: null,
          },
          meta: null,
        }),
      });
    });

    await page.goto(`/set-password/${ENCODED_TOKEN}`);

    await page.getByLabel('Email').fill('ana@reservarte.com');
    await page.getByLabel('Contraseña', { exact: true }).fill('Nueva1234!');
    await page.getByLabel('Repite la contraseña').fill('Nueva1234!');
    await page.getByRole('button', { name: 'Guardar contraseña' }).click();

    await expect(
      page.getByText('El enlace de invitación no es válido o ha caducado.')
    ).toBeVisible();

    // El 401 de este endpoint NO debe cerrar sesión ni sacar al usuario de la
    // página: es un resultado de negocio, no una sesión caducada.
    await expect(page).toHaveURL(new RegExp('/set-password/'));
  });
});
