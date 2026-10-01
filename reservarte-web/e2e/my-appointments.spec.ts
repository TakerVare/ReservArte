import { test, expect, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Mis citas (RA-869faaunu): inicio con sesión y aterrizaje tras el login.
 * Próxima cita (Figma `387:56617`) o estado vacío (`387:56660`). Misma
 * excepción de contraste que login.a11y.spec.ts (RA-869f0v6vm).
 */

const APPOINTMENTS = /\/api\/v1\/appointments(\?|$)/;

function appointment(id: number, appointmentDate: string, startTime: string, status: string) {
  return {
    id,
    customerId: 7,
    employeeId: 2,
    appointmentDate,
    startTime,
    endTime: '23:00:00',
    status,
    customerName: 'Laura',
    employeeName: 'Ana',
  };
}

async function stubAppointments(page: Page, items: unknown[]) {
  const requests: URL[] = [];
  await page.route(APPOINTMENTS, (route) => {
    requests.push(new URL(route.request().url()));
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, data: { items }, error: null, meta: null }),
    });
  });
  return requests;
}

async function loginAsCustomer(page: Page) {
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        data: {
          accessToken: 'fake-access',
          refreshToken: 'fake-refresh',
          user: {
            id: 7,
            email: 'laura@example.com',
            firstName: 'Laura',
            lastName: 'G',
            rol: 'Customer',
          },
          mfaRequired: false,
          mfaTicket: null,
        },
        error: null,
        meta: null,
      }),
    })
  );
  await page.goto('/login');
  await page.getByLabel('Usuario').fill('laura@example.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
}

test.describe('Mis citas', () => {
  test('tras el login aterriza en Mis citas y muestra la próxima cita', async ({ page }) => {
    // Fechas lejanas: la página filtra las que ya han empezado con el reloj real.
    const requests = await stubAppointments(page, [
      appointment(3, '2099-12-30', '17:00:00', 'confirmed'),
      appointment(2, '2099-12-24', '10:00:00', 'pending'),
      appointment(1, '2099-12-20', '09:00:00', 'cancelled_by_customer'),
    ]);

    await loginAsCustomer(page);

    await expect(page).toHaveURL('/mis-citas');
    await expect(page.getByText('Próxima cita:')).toBeVisible();
    await expect(page.getByText('24 Dic - 10:00h')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Modificar' }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Cancelar' }).first()).toBeVisible();

    // Pide las citas desde hoy, en la API de su mismo origen.
    expect(requests).toHaveLength(1);
    expect(requests[0].searchParams.get('from')).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    // Solo las de la propia cuenta (RA-869fajbw0).
    expect(requests[0].searchParams.get('customerId')).toBe('7');
  });

  test('sin citas próximas muestra el estado vacío con «Reservar Cita»', async ({ page }) => {
    await stubAppointments(page, [appointment(1, '2099-12-24', '10:00:00', 'cancelled')]);

    await loginAsCustomer(page);

    await expect(page.getByText('No hay citas asignadas')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reservar Cita' }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Modificar' })).toHaveCount(0);
  });

  test('si la API falla lo dice, sin inventar un estado vacío', async ({ page }) => {
    await page.route(APPOINTMENTS, (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          data: null,
          error: { code: 'GEN_INTERNAL_ERROR', message: 'stub', details: null },
          meta: null,
        }),
      })
    );

    await loginAsCustomer(page);

    await expect(page.getByRole('alert')).toContainText('No se ha podido cargar tu próxima cita');
    await expect(page.getByText('No hay citas asignadas')).toHaveCount(0);
  });

  test('la raíz lleva a Mis citas y el «Inicio» del BottomNav también', async ({ page }) => {
    await stubAppointments(page, []);
    await loginAsCustomer(page);
    await expect(page).toHaveURL('/mis-citas');

    await page.getByRole('link', { name: 'Contacto' }).click();
    await page.getByRole('link', { name: 'Inicio' }).click();
    await expect(page).toHaveURL('/mis-citas');

    await page.goto('/');
    // Tras recargar, la sesión sigue en localStorage: la raíz redirige a Mis citas.
    await expect(page).toHaveURL('/mis-citas');
  });

  test('el personal no ve en Mis citas las citas de las clientas (RA-869fajbw0)', async ({
    page,
  }) => {
    // Como la API real: al personal sin filtro le da la agenda del centro; filtrando por su
    // cuenta, solo las suyas como clienta (ninguna).
    await page.route(APPOINTMENTS, (route) => {
      const customerId = new URL(route.request().url()).searchParams.get('customerId');
      const items =
        customerId === '2' ? [] : [appointment(9, '2099-12-24', '10:00:00', 'confirmed')];
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, data: { items }, error: null, meta: null }),
      });
    });
    await page.route('**/api/v1/auth/login', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          data: {
            accessToken: 'fake-access',
            refreshToken: 'fake-refresh',
            mfaRequired: false,
            mfaTicket: null,
            user: {
              id: 2,
              email: 'maria@reservarte.com',
              firstName: 'María',
              lastName: 'G',
              rol: 'Employee',
            },
          },
          error: null,
          meta: null,
        }),
      })
    );
    await page.goto('/login');
    await page.getByLabel('Usuario').fill('maria@reservarte.com');
    await page.getByLabel('Contraseña').fill('Secreta123!');
    await page.getByRole('button', { name: 'Entrar' }).click();

    await expect(page).toHaveURL('/mis-citas');
    await expect(page.getByText('No hay citas asignadas')).toBeVisible();
    await expect(page.getByText('24 Dic - 10:00h')).toHaveCount(0);
  });

  test('sin sesión, Mis citas manda a login', async ({ page }) => {
    await page.goto('/mis-citas');
    await expect(page).toHaveURL(/\/login\?redirect=(%2F|\/)mis-citas$/);
  });

  for (const [name, items] of [
    ['con cita', [appointment(2, '2099-12-24', '10:00:00', 'confirmed')]],
    ['sin cita', []],
  ] as const) {
    test(`sin violaciones WCAG 2.1 AA ${name} (salvo el contraste de marca)`, async ({ page }) => {
      await stubAppointments(page, [...items]);
      await loginAsCustomer(page);
      await expect(page.getByText('Próxima cita:')).toBeVisible();

      const results = await new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();

      expect(results.violations).toEqual([]);
    });
  }
});
