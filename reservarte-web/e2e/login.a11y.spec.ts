import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Auditoría de accesibilidad de la LoginPage (WCAG 2.1 AA / RD 1112/2018).
 * Cubre tres estados del formulario: inicial, con mensaje de error, y con el
 * hueco de CAPTCHA visible (tras varios intentos fallidos). Las respuestas del
 * backend se interceptan (page.route) para provocar los estados sin depender
 * de la API ni de la BD: aquí solo importa el DOM renderizado en cada estado.
 *
 * ─────────────────────────────────────────────────────────────────────────
 * EXCEPCIÓN CONSCIENTE — regla `color-contrast` DESACTIVADA:
 * El color de marca rosa (#FFB6C1 / --primary) NO cumple el ratio de contraste
 * WCAG AA (1.4.3): ~1.62 frente al 4.5:1 exigido, en botones primarios y
 * secundarios. Se ha decidido PRIORIZAR LA IDENTIDAD DE MARCA sobre este
 * criterio de contraste. Esto es una excepción conocida y aceptada, NO
 * conformidad: la LoginPage tiene una violación de contraste real que este
 * test deja pasar deliberadamente.
 * Deuda trazada en ClickUp RA-869f0v6vm (revisar contraste del color de marca).
 * Cuando se resuelva, RETIRAR el `.disableRules(['color-contrast'])` de los
 * tres tests y verificar que pasan sin excluir nada.
 * El resto de reglas WCAG 2.1 AA SÍ se comprueban.
 * ─────────────────────────────────────────────────────────────────────────
 */

// Tags de axe que componen WCAG 2.1 nivel AA (incluye A y AA de 2.0 y 2.1).
const WCAG_21_AA = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'];

// Reglas excluidas conscientemente (ver cabecera). Mantener alineado con la
// deuda RA-869f0v6vm; vaciar esta lista cuando el contraste se corrija.
const EXCLUDED_RULES = ['color-contrast'];

test.describe('Accesibilidad — LoginPage', () => {
  test('estado inicial sin violaciones WCAG 2.1 AA', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible();

    const results = await new AxeBuilder({ page })
      .withTags(WCAG_21_AA)
      .disableRules(EXCLUDED_RULES)
      .analyze();

    expect(results.violations).toEqual([]);
  });

  test('estado con error sin violaciones WCAG 2.1 AA', async ({ page }) => {
    // Intercepta el login y devuelve un 401 con el envelope de error, para que
    // la página muestre el mensaje de error (text-destructive) sin backend.
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          data: null,
          error: { code: 'AUTH_INVALID_CREDENTIALS', message: 'Email o contraseña incorrectos.' },
          meta: {},
        }),
      })
    );

    await page.goto('/login');
    await page.getByLabel('Usuario').fill('test@test.com');
    await page.getByLabel('Contraseña').fill('wrongpass');
    await page.getByRole('button', { name: 'Entrar' }).click();

    // Espera a que aparezca el mensaje de error antes de auditar.
    await expect(page.getByText('Email o contraseña incorrectos.')).toBeVisible();

    const results = await new AxeBuilder({ page })
      .withTags(WCAG_21_AA)
      .disableRules(EXCLUDED_RULES)
      .analyze();

    expect(results.violations).toEqual([]);
  });

  test('estado con CAPTCHA visible sin violaciones WCAG 2.1 AA', async ({ page }) => {
    // Mismo intercept de error; el CAPTCHA aparece tras 3 intentos fallidos.
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          data: null,
          error: { code: 'AUTH_INVALID_CREDENTIALS', message: 'Email o contraseña incorrectos.' },
          meta: {},
        }),
      })
    );

    await page.goto('/login');

    // Tres intentos fallidos para superar el umbral (FAILED_ATTEMPTS_THRESHOLD = 3).
    for (let i = 0; i < 3; i++) {
      await page.getByLabel('Usuario').fill('test@test.com');
      await page.getByLabel('Contraseña').fill('wrongpass');
      await page.getByRole('button', { name: 'Entrar' }).click();
      await expect(page.getByText('Email o contraseña incorrectos.')).toBeVisible();
    }

    // El hueco de CAPTCHA (role="group", aria-label) debe estar visible.
    await expect(page.getByRole('group', { name: 'Verificación de seguridad' })).toBeVisible();

    const results = await new AxeBuilder({ page })
      .withTags(WCAG_21_AA)
      .disableRules(EXCLUDED_RULES)
      .analyze();

    expect(results.violations).toEqual([]);
  });
});