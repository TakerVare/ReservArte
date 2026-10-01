import { test, expect, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Pantalla de Usuario y navegación del área privada (RA-869ep9p36): sin
 * Sidebar ni Header; la gestión se abre desde /cuenta, al que se llega por el
 * BottomNav. Misma excepción de contraste que login.a11y.spec.ts (RA-869f0v6vm).
 */

async function startSessionAs(page: Page, role: string) {
  await page.route('**/api/v1/account/me', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: { id: '1', email: 'a@b.com', role, organizationId: 'org' },
        error: null,
        meta: null,
      }),
    })
  );
  await page.goto('/auth/callback#access_token=fake-access&refresh_token=fake-refresh');
  await expect(page).toHaveURL('/');
  // Navegación SPA (sin recargar): el usuario sigue en el store.
  await page.getByRole('link', { name: 'Cuenta' }).click();
  await expect(page).toHaveURL('/cuenta');
}

test.describe('Pantalla de Usuario', () => {
  test('el personal ve las dos áreas y entra a un módulo desde el menú', async ({ page }) => {
    await startSessionAs(page, 'Employee');

    await expect(page.getByRole('heading', { name: 'Área de administración' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Área de usuario' })).toBeVisible();

    await page.getByRole('link', { name: 'Servicios' }).click();
    await expect(page).toHaveURL('/servicios');
  });

  test('una clienta solo ve el área de usuario', async ({ page }) => {
    await startSessionAs(page, 'Customer');

    await expect(page.getByRole('heading', { name: 'Área de usuario' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Área de administración' })).toHaveCount(0);
  });

  test('el área privada no tiene Sidebar ni Header: solo el BottomNav', async ({ page }) => {
    await startSessionAs(page, 'Admin');
    await page.goto('/servicios');

    await expect(page.getByRole('button', { name: 'Abrir menú' })).toHaveCount(0);
    await expect(page.locator('aside')).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Cuenta' })).toBeVisible();
  });

  test('«Cerrar sesión» vuelve a login y borra el token', async ({ page }) => {
    await startSessionAs(page, 'Employee');

    await page.getByRole('button', { name: 'Cerrar sesión' }).click();

    await expect(page).toHaveURL(/\/login/);
    expect(await page.evaluate(() => localStorage.getItem('authToken'))).toBeNull();
  });

  test('sin violaciones WCAG 2.1 AA (salvo el contraste de marca)', async ({ page }) => {
    await startSessionAs(page, 'Admin');

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .disableRules(['color-contrast'])
      .analyze();

    expect(results.violations).toEqual([]);
  });
});
