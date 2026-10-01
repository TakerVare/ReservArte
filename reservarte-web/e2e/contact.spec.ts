import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Contacto (RA-869faaunu, Figma `387:56672`): pública, con el mapa, el horario y los
 * datos del centro de `config/center.ts`. Misma excepción de contraste que
 * login.a11y.spec.ts (RA-869f0v6vm).
 */

test.describe('Contacto', () => {
  // Sin red hacia Google: los tests no dependen de un tercero; solo se comprueba
  // qué pide el iframe (H-42).
  test.beforeEach(async ({ page }) => {
    await page.route('https://www.google.com/**', (route) =>
      route.fulfill({ status: 200, contentType: 'text/html', body: '<html></html>' })
    );
  });

  test('es pública y muestra horario, teléfono e Instagram', async ({ page }) => {
    await page.goto('/contacto');

    await expect(page).toHaveURL('/contacto');
    await expect(page.getByText('Horario de apertura')).toBeVisible();
    await expect(page.getByText('Lunes a viernes:')).toBeVisible();
    await expect(page.getByText('de 10:00 a 14:00')).toBeVisible();
    await expect(page.getByText('de 15:00 a 20:00')).toBeVisible();
    await expect(page.getByText('Datos de contacto')).toBeVisible();

    await expect(page.getByRole('link', { name: '649 227 139' })).toHaveAttribute(
      'href',
      'tel:+34649227139'
    );
    const instagram = page.getByRole('link', { name: '@morethanbrows.zgz' });
    await expect(instagram).toHaveAttribute('href', 'https://www.instagram.com/morethanbrows.zgz/');
    await expect(instagram).toHaveAttribute('target', '_blank');
    await expect(instagram).toHaveAttribute('rel', 'noopener noreferrer');
  });

  test('el mapa de Google se centra en la dirección del centro', async ({ page }) => {
    await page.goto('/contacto');

    const map = page.getByTitle('Mapa de ubicación del centro');
    await expect(map).toHaveAttribute(
      'src',
      'https://www.google.com/maps?q=' +
        encodeURIComponent('Calle Bolonia, 4, Zaragoza (50008)') +
        '&output=embed'
    );
  });

  test('sin violaciones WCAG 2.1 AA (salvo el contraste de marca)', async ({ page }) => {
    await page.goto('/contacto');
    await expect(page.getByText('Horario de apertura')).toBeVisible();

    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
      .disableRules(['color-contrast'])
      .analyze();

    expect(results.violations).toEqual([]);
  });
});
