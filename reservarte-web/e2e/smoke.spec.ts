import { test, expect } from '@playwright/test';

/**
 * Test de humo: valida que la infraestructura E2E funciona de punta a punta
 * (Playwright arranca/reutiliza el frontend, navega y evalúa el DOM) en los
 * tres navegadores. No prueba lógica de negocio; solo el andamiaje.
 */
test('la página de login carga correctamente', async ({ page }) => {
  await page.goto('/login');

  // El formulario de login debe estar presente (campos usuario y contraseña).
  await expect(page.getByLabel('Usuario')).toBeVisible();
  await expect(page.getByLabel('Contraseña')).toBeVisible();

  // El botón de envío del formulario.
  await expect(page.getByRole('button', { name: 'Entrar' })).toBeVisible();
});