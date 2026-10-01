import { test, expect, type Page, type Request } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Pantalla de reserva (RA-869fagpyg, H-44 y H-45) con la API simulada: la clienta
 * crea su cita o modifica la activa; el personal elige clienta y, si ya tiene
 * cita, decide modificarla o crear otra. Misma excepción de contraste que
 * login.a11y.spec.ts (RA-869f0v6vm).
 */

const ok = (data: unknown, status = 200) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({ success: true, data, error: null, meta: {} }),
});

function iso(date: Date) {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

const today = new Date();
const days = Array.from({ length: 40 }, (_, i) =>
  iso(new Date(today.getFullYear(), today.getMonth(), today.getDate() + i + 1))
);
const until = iso(new Date(today.getFullYear(), today.getMonth(), today.getDate() + 42));

const active = {
  id: 5,
  customerId: 7,
  employeeId: 2,
  appointmentDate: days[10],
  startTime: '11:00:00',
  endTime: '11:40:00',
  status: 'confirmed',
  customerName: 'Laura Gómez',
  employeeName: 'Ana',
};

interface Calls {
  posts: unknown[];
  puts: { url: string; body: unknown }[];
  appointmentQueries: URL[];
}

async function mockApi(
  page: Page,
  role: 'Customer' | 'Employee',
  activeAppointment: unknown | null
) {
  const calls: Calls = { posts: [], puts: [], appointmentQueries: [] };
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill(
      ok({
        accessToken: 'fake',
        refreshToken: 'r',
        mfaRequired: false,
        mfaTicket: null,
        user: {
          id: role === 'Customer' ? 7 : 2,
          email: 'x@example.com',
          firstName: 'X',
          lastName: 'Y',
          rol: role,
        },
      })
    )
  );
  await page.route(/\/api\/v1\/services(\?|$)/, (route) =>
    route.fulfill(
      ok({
        items: [
          { id: 3, name: 'Henna de cejas', durationMinutes: 40, basePrice: 22 },
          { id: 4, name: 'Laminado de cejas', durationMinutes: 60, basePrice: 45 },
        ],
      })
    )
  );
  await page.route(/\/availability\/days\?/, (route) =>
    route.fulfill(ok({ days, bookableFrom: iso(today), bookableUntil: until }))
  );
  await page.route(/\/availability\/by-service\?/, (route) =>
    route.fulfill(
      ok({
        durationMinutes: 40,
        bookableFrom: iso(today),
        bookableUntil: until,
        employees: [
          {
            employeeId: 2,
            employeeName: 'Ana',
            slots: [
              { startTime: '10:00:00', endTime: '10:40:00' },
              { startTime: '10:15:00', endTime: '10:55:00' },
            ],
          },
          {
            employeeId: 3,
            employeeName: 'Lucía',
            slots: [{ startTime: '12:00:00', endTime: '12:40:00' }],
          },
        ],
      })
    )
  );
  await page.route(/\/api\/v1\/customers(\?|$)/, (route) =>
    route.fulfill(
      ok({
        items: [
          {
            id: 7,
            firstName: 'Laura',
            lastName: 'Gómez',
            fullName: 'Laura Gómez',
            profileImageUrl: null,
          },
        ],
      })
    )
  );
  await page.route(/\/api\/v1\/appointments(\/\d+)?(\?|$)/, async (route, request: Request) => {
    const method = request.method();
    const detail = {
      ...active,
      id: 9,
      appointmentDate: days[0],
      startTime: '10:00:00',
      totalPrice: 22,
    };
    if (method === 'POST') {
      calls.posts.push(request.postDataJSON());
      return route.fulfill(ok(detail, 201));
    }
    if (method === 'PUT') {
      calls.puts.push({ url: request.url(), body: request.postDataJSON() });
      return route.fulfill(ok(detail));
    }
    calls.appointmentQueries.push(new URL(request.url()));
    return route.fulfill(ok({ items: activeAppointment ? [activeAppointment] : [] }));
  });
  return calls;
}

async function login(page: Page) {
  await page.goto('/login');
  await page.getByLabel('Usuario').fill('x@example.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL('/mis-citas');
}

async function chooseServiceAndDay(page: Page) {
  await page.getByRole('combobox').click();
  await page.getByRole('option', { name: 'Henna de cejas · 40 min' }).click();
  const available = page.locator('[data-available]:not([data-disabled])');
  if ((await available.count()) === 0)
    await page.getByRole('button', { name: 'Mes siguiente' }).click();
  await available.first().click();
  await expect(page.getByText('Citas disponibles:')).toBeVisible();
}

test.describe('Pantalla de reserva', () => {
  test('la clienta reserva desde Mis citas y al aceptar vuelve a Mis citas', async ({ page }) => {
    const calls = await mockApi(page, 'Customer', null);
    await login(page);
    await page.getByRole('button', { name: 'Reservar Cita' }).first().click();
    await expect(page).toHaveURL('/reservar');
    await expect(page.getByRole('button', { name: 'Seleccionar cliente' })).toHaveCount(0);

    await chooseServiceAndDay(page);
    await expect(page.getByText('Ana')).toBeVisible();
    await expect(page.getByText('Lucía')).toBeVisible();
    await page.getByRole('button', { name: '10:15' }).click();

    const dialog = page.getByRole('dialog', { name: 'Cita reservada' });
    await expect(dialog).toBeVisible();
    expect(calls.posts).toHaveLength(1);
    expect(calls.posts[0]).toMatchObject({
      employeeId: 2,
      startTime: '10:15',
      items: [{ serviceId: 3 }],
    });
    expect(calls.posts[0]).not.toHaveProperty('customerId');

    await dialog.getByRole('button', { name: 'Aceptar' }).click();
    await expect(page).toHaveURL('/mis-citas');
  });

  test('la clienta con una cita activa la modifica en vez de crear otra', async ({ page }) => {
    const calls = await mockApi(page, 'Customer', active);
    await login(page);
    await page.getByRole('button', { name: 'Modificar' }).first().click();
    await expect(page).toHaveURL('/reservar');

    await chooseServiceAndDay(page);
    await page.getByRole('button', { name: '12:00' }).click();

    await expect(page.getByRole('dialog', { name: 'Cita reservada' })).toBeVisible();
    expect(calls.posts).toHaveLength(0);
    expect(calls.puts).toHaveLength(1);
    expect(calls.puts[0].url).toMatch(/\/api\/v1\/appointments\/5$/);
    expect(calls.puts[0].body).toMatchObject({ employeeId: 3, startTime: '12:00' });
  });

  test('el personal elige la clienta y, si ya tiene cita, decide crear otra', async ({ page }) => {
    const calls = await mockApi(page, 'Employee', active);
    await login(page);
    await page.getByRole('link', { name: 'Mi cuenta' }).click();
    await page.getByRole('link', { name: 'Citas' }).click();
    await page.getByRole('button', { name: 'Nueva cita' }).click();
    await expect(page).toHaveURL('/reservar');
    await expect(page.getByTestId('booking-customer')).toHaveText(
      'Selecciona el cliente al que se le asignará la cita.'
    );

    await page.getByRole('button', { name: 'Seleccionar cliente' }).click();
    await page.getByRole('searchbox', { name: 'Buscar cliente' }).fill('laura');
    await page.getByRole('button', { name: 'Laura Gómez' }).click();
    await expect(page.getByTestId('booking-customer')).toHaveText('Cita para: Laura Gómez');

    await chooseServiceAndDay(page);
    await page.getByRole('button', { name: '10:00' }).click();

    const choose = page.getByRole('dialog', { name: 'Ya tiene una cita' });
    await expect(choose).toBeVisible();
    expect(calls.appointmentQueries.some((u) => u.searchParams.get('customerId') === '7')).toBe(
      true
    );
    await choose.getByRole('button', { name: 'Crear una nueva' }).click();

    await expect(page.getByRole('dialog', { name: 'Cita reservada' })).toBeVisible();
    expect(calls.posts).toHaveLength(1);
    expect(calls.posts[0]).toMatchObject({ customerId: 7, employeeId: 2, startTime: '10:00' });
    expect(calls.puts).toHaveLength(0);
  });

  test('el calendario empieza en lunes, marca hoy y deshabilita los días pasados', async ({
    page,
  }) => {
    await mockApi(page, 'Customer', null);
    await login(page);
    await page.getByRole('button', { name: 'Reservar Cita' }).first().click();
    await page.getByRole('combobox').click();
    await page.getByRole('option', { name: 'Henna de cejas · 40 min' }).click();

    await expect(page.locator('th').first()).toHaveText(/lun/i);
    const todayCell = page.locator('[data-today]');
    await expect(todayCell).toHaveClass(/bg-primary/);
    // Lo anterior a hoy no se puede elegir: ni siquiera se puede ir al mes anterior.
    await expect(page.getByRole('button', { name: 'Mes anterior' })).toBeDisabled();
    if (today.getDate() > 1) {
      const yesterday = page.locator(
        `[data-value="${iso(new Date(today.getFullYear(), today.getMonth(), today.getDate() - 1))}"]`
      );
      await expect(yesterday).toHaveAttribute('data-disabled', '');
    }
  });

  test('sin violaciones WCAG 2.1 AA con huecos y con el selector de cliente abierto', async ({
    page,
  }) => {
    await mockApi(page, 'Employee', null);
    await login(page);
    await page.getByRole('link', { name: 'Mi cuenta' }).click();
    await page.getByRole('link', { name: 'Citas' }).click();
    await page.getByRole('button', { name: 'Nueva cita' }).click();
    await chooseServiceAndDay(page);

    const axe = () =>
      new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();
    expect((await axe()).violations).toEqual([]);

    await page.getByRole('button', { name: 'Seleccionar cliente' }).click();
    await expect(page.getByRole('dialog', { name: 'Seleccionar cliente' })).toBeVisible();
    expect((await axe()).violations).toEqual([]);
  });
});
