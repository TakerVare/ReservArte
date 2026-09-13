import { test, expect } from '@playwright/test';

/**
 * Contrato del token de restablecimiento (RA-869f18rp7).
 *
 * En el enlace del email el token viaja URL-encoded; Vue Router DECODIFICA el
 * segmento `:token` al exponerlo en route.params, de modo que el POST al
 * backend debe llevarlo decodificado EXACTAMENTE UNA VEZ. El backend tolera
 * además la variante encoded (Uri.UnescapeDataString, inocuo sobre base64),
 * pero ese es un colchón, no el contrato: si el router dejara de decodificar,
 * o decodificara dos veces, este test debe caer.
 */

// Token sintético con los tres caracteres conflictivos de base64: '+' '/' '='.
const RAW_TOKEN = 'CfDJ8+abc/def+ghi==';
const ENCODED_TOKEN = encodeURIComponent(RAW_TOKEN); // CfDJ8%2Babc%2Fdef%2Bghi%3D%3D

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

test.describe('ResetPasswordPage — contrato del token', () => {
  test('el POST lleva el token decodificado exactamente una vez', async ({ page }) => {
    let tokenEnviado: string | undefined;

    await page.route('**/api/v1/auth/reset-password', async (route) => {
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

    // La URL lleva el token como en el email: URL-encoded.
    await page.goto(`/reset-password/${ENCODED_TOKEN}`);

    await page.getByLabel('Email').fill('maria@reservarte.com');
    await page.getByLabel('Nueva contraseña', { exact: true }).fill('Nueva1234!');
    await page.getByLabel('Repite la contraseña').fill('Nueva1234!');
    await page.getByRole('button', { name: 'Cambiar contraseña' }).click();

    await expect
      .poll(() => tokenEnviado, { message: 'el POST de reset no llegó a interceptarse' })
      .toBeDefined();

    // Ni encoded (0 decodificaciones) ni corrupto (2 decodificaciones):
    // decodificado exactamente una vez, con '+', '/' y '=' intactos.
    expect(tokenEnviado).toBe(RAW_TOKEN);
  });

  test('sin token en la URL no se muestra el formulario', async ({ page }) => {
    await page.goto('/reset-password/');

    await expect(page.getByText('Enlace no válido')).toBeVisible();
    await expect(page.getByLabel('Nueva contraseña', { exact: true })).toHaveCount(0);
  });
});
