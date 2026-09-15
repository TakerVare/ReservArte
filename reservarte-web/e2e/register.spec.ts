import { test, expect, type Page } from '@playwright/test';

/**
 * Registro público (RA-869f1xc2n).
 *
 * El tratamiento de datos para gestionar las citas (consentimiento granular
 * data_processing) tiene su propio checkbox obligatorio, distinto de términos y
 * privacidad, y viaja en el POST de registro para que el backend lo guarde en la
 * ficha de cliente.
 */

const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': '*',
  'Access-Control-Allow-Methods': '*',
};

async function mockLegalVersions(page: Page) {
  await page.route('**/api/v1/legal/versions', async (route) => {
    if (route.request().method() === 'OPTIONS') {
      return route.fulfill({ status: 204, headers: CORS_HEADERS });
    }
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      headers: CORS_HEADERS,
      body: JSON.stringify({
        success: true,
        data: { termsVersion: '1.0', privacyVersion: '1.0' },
        error: null,
        meta: null,
      }),
    });
  });
}

async function fillRegisterForm(page: Page) {
  await page.getByLabel('Nombre', { exact: true }).fill('Lucía');
  await page.getByLabel('Apellidos', { exact: true }).fill('Martínez');
  await page.getByLabel('Email', { exact: true }).fill('lucia@correo.com');
  await page.getByLabel('Contraseña', { exact: true }).fill('Clave-segura-1');
  await page.getByLabel('Repite la contraseña').fill('Clave-segura-1');
  await page.getByRole('checkbox', { name: /términos y condiciones/ }).check();
  await page.getByRole('checkbox', { name: /política de privacidad/ }).check();
}

test.describe('RegisterPage — consentimiento de tratamiento de datos', () => {
  test('el POST de registro lleva el consentimiento de tratamiento de datos', async ({ page }) => {
    let cuerpo: Record<string, unknown> | undefined;

    await mockLegalVersions(page);
    await page.route('**/api/v1/auth/register', async (route) => {
      if (route.request().method() === 'OPTIONS') {
        return route.fulfill({ status: 204, headers: CORS_HEADERS });
      }
      cuerpo = route.request().postDataJSON();
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        headers: CORS_HEADERS,
        body: JSON.stringify({
          success: true,
          data: {
            accessToken: 'access',
            refreshToken: 'refresh',
            user: {
              id: 4,
              email: 'lucia@correo.com',
              firstName: 'Lucía',
              lastName: 'Martínez',
              rol: 'Customer',
            },
            mfaRequired: false,
            mfaTicket: null,
          },
          error: null,
          meta: null,
        }),
      });
    });

    await page.goto('/register');
    await fillRegisterForm(page);
    await page.getByRole('checkbox', { name: /tratamiento de mis datos/ }).check();
    await page.getByRole('button', { name: 'Crear cuenta' }).click();

    await expect
      .poll(() => cuerpo, { message: 'el POST de registro no llegó a interceptarse' })
      .toBeDefined();

    expect(cuerpo).toMatchObject({
      acceptedTerms: true,
      acceptedPrivacy: true,
      acceptedDataProcessing: true,
      acceptedTermsVersion: '1.0',
      acceptedPrivacyVersion: '1.0',
    });
  });

  test('sin marcar el tratamiento de datos no se envía el registro', async ({ page }) => {
    let enviado = false;

    await mockLegalVersions(page);
    await page.route('**/api/v1/auth/register', async (route) => {
      enviado = true;
      return route.fulfill({ status: 204, headers: CORS_HEADERS });
    });

    await page.goto('/register');
    await fillRegisterForm(page);
    await page.getByRole('button', { name: 'Crear cuenta' }).click();

    await expect(
      page.getByText('Debes aceptar el tratamiento de tus datos para gestionar tus citas.')
    ).toBeVisible();
    expect(enviado).toBe(false);
  });
});
