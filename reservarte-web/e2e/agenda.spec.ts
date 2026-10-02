import { test, expect, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Listado de citas del personal (RA-869fajn7g) con la API simulada: vistas de día,
 * semana y mes con navegación, filtro por empleada, estado en color, detalle con sus
 * acciones según estado y rol, y «Modificar» esa cita en la pantalla de reserva.
 * Misma excepción de contraste que login.a11y.spec.ts (RA-869f0v6vm).
 */

const ok = (data: unknown, status = 200) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({ success: true, data, error: null, meta: {} }),
});

const pad = (n: number) => String(n).padStart(2, '0');
const iso = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const today = new Date();
const TODAY = iso(today);

function appointment(
  id: number,
  status: string,
  employeeId: number,
  employeeName: string,
  start: string,
  customerName: string
) {
  return {
    id,
    customerId: 40 + id,
    employeeId,
    appointmentDate: TODAY,
    startTime: `${start}:00`,
    endTime: `${start.slice(0, 2)}:40:00`,
    status,
    customerName,
    employeeName,
  };
}

const list = [
  appointment(1, 'pending', 2, 'Ana', '10:00', 'Laura Gómez'),
  appointment(2, 'confirmed', 3, 'Lucía', '09:00', 'Sofía Ruiz'),
  appointment(3, 'cancelled_by_customer', 2, 'Ana', '12:00', 'Marta Díaz'),
];

async function setup(page: Page, role: 'Admin' | 'Employee') {
  const ranges: URL[] = [];
  const transitions: string[] = [];
  const puts: string[] = [];
  let status = 'pending';
  // Lo no simulado responde vacío: con la API real en marcha, su 401 al token falso
  // cerraría la sesión (RA-869d7fbyt). Las rutas de después mandan.
  await page.route('**/api/v1/**', (route) => route.fulfill(ok({ items: [] })));
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill(
      ok({
        accessToken: 'fake',
        refreshToken: 'r',
        mfaRequired: false,
        mfaTicket: null,
        user: { id: 2, email: 'x@reservarte.com', firstName: 'X', lastName: 'Y', rol: role },
      })
    )
  );
  await page.route(/\/api\/v1\/appointments\/\d+\/(confirm|start|complete|no-show)$/, (route) => {
    transitions.push(new URL(route.request().url()).pathname);
    status = 'confirmed';
    return route.fulfill(ok({ ...list[0], status }));
  });
  await page.route(/\/api\/v1\/appointments\/1$/, (route) => {
    if (route.request().method() === 'PUT') {
      puts.push(route.request().url());
      return route.fulfill(ok({ ...list[0], totalPrice: 22 }));
    }
    return route.fulfill(
      ok({
        ...list[0],
        status,
        totalPrice: 22,
        notes: null,
        items: [
          { serviceId: 3, serviceName: 'Henna de cejas', price: 22, durationMinutes: 40, order: 1 },
        ],
        warnings: [
          {
            code: 'AllergyTestMissing',
            serviceId: 3,
            message: '«Henna de cejas» exige prueba de alergia.',
          },
        ],
      })
    );
  });
  await page.route(/\/api\/v1\/appointments(\?|$)/, (route) => {
    const url = new URL(route.request().url());
    if (url.searchParams.get('to')) ranges.push(url);
    const items = url.searchParams.get('customerId') ? [] : list;
    return route.fulfill(ok({ items }));
  });
  await page.route(/\/api\/v1\/services(\?|$)/, (route) =>
    route.fulfill(
      ok({ items: [{ id: 3, name: 'Henna de cejas', durationMinutes: 40, basePrice: 22 }] })
    )
  );
  await page.route(/\/availability\/days\?/, (route) =>
    route.fulfill(ok({ days: [TODAY], bookableFrom: TODAY, bookableUntil: TODAY }))
  );
  await page.route(/\/availability\/by-service\?/, (route) =>
    route.fulfill(
      ok({
        durationMinutes: 40,
        bookableFrom: TODAY,
        bookableUntil: TODAY,
        employees: [
          {
            employeeId: 2,
            employeeName: 'Ana',
            slots: [{ startTime: '16:00:00', endTime: '16:40:00' }],
          },
        ],
      })
    )
  );

  await page.goto('/login');
  await page.getByLabel('Usuario').fill('x@reservarte.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL('/mis-citas');
  await page.getByRole('link', { name: 'Mi cuenta' }).click();
  await page.getByRole('link', { name: 'Citas' }).click();
  await expect(page).toHaveURL('/citas');
  return { ranges, transitions, puts };
}

test.describe('Listado de citas del personal', () => {
  test('el día de hoy, ordenado por hora, con el estado de cada cita', async ({ page }) => {
    const { ranges } = await setup(page, 'Employee');

    const rows = page.locator('main ul button');
    await expect(rows).toHaveCount(3);
    await expect(rows.nth(0)).toContainText('09:00 – 09:40 · Sofía Ruiz');
    await expect(rows.nth(0)).toContainText('Confirmada');
    await expect(rows.nth(1)).toContainText('Pendiente');
    await expect(rows.nth(2)).toContainText('Cancelada por la clienta');
    expect(ranges[0].searchParams.get('from')).toBe(TODAY);
    expect(ranges[0].searchParams.get('to')).toBe(TODAY);
  });

  test('semana y mes piden su periodo y el navegador mueve un bloque', async ({ page }) => {
    const { ranges } = await setup(page, 'Employee');

    await page.getByRole('tab', { name: 'Semana' }).click();
    await expect(page.getByRole('tab', { name: 'Semana' })).toHaveAttribute(
      'aria-selected',
      'true'
    );
    await expect.poll(() => ranges.length).toBeGreaterThan(1);
    const week = ranges.at(-1)!;
    const from = new Date(week.searchParams.get('from')!);
    expect(from.getUTCDay()).toBe(1); // lunes
    expect(page.getByRole('heading', { level: 3 }).first()).toBeTruthy();

    await page.getByRole('button', { name: 'Semana siguiente' }).click();
    await expect
      .poll(() => ranges.at(-1)!.searchParams.get('from'))
      .not.toBe(week.searchParams.get('from'));

    await page.getByRole('tab', { name: 'Mes' }).click();
    await expect.poll(() => ranges.at(-1)!.searchParams.get('from')!.endsWith('-01')).toBe(true);
    await page.getByRole('button', { name: 'Hoy' }).click();
    await expect.poll(() => ranges.at(-1)!.searchParams.get('from')).toBe(`${TODAY.slice(0, 8)}01`);
  });

  test('el filtro por empleada deja solo sus citas', async ({ page }) => {
    await setup(page, 'Employee');

    await page.getByRole('combobox').click();
    await page.getByRole('option', { name: 'Lucía' }).click();

    await expect(page.locator('main ul button')).toHaveCount(1);
    await expect(page.locator('main ul button')).toContainText('Sofía Ruiz');
  });

  test('el detalle enseña servicios, precio y avisos, y una empleada confirma una pendiente', async ({
    page,
  }) => {
    const { transitions } = await setup(page, 'Employee');

    await page.getByRole('button', { name: /Laura Gómez/ }).click();
    const dialog = page.getByRole('dialog', { name: 'Laura Gómez' });
    await expect(dialog).toContainText('Henna de cejas');
    await expect(dialog).toContainText('22,00');
    await expect(dialog.getByRole('note')).toContainText('prueba de alergia');
    await expect(dialog.getByRole('button', { name: 'No presentada' })).toHaveCount(0);

    await dialog.getByRole('button', { name: 'Confirmar' }).click();

    await expect(dialog).toContainText('Confirmada');
    expect(transitions).toEqual(['/api/v1/appointments/1/confirm']);
    await expect(dialog.getByRole('button', { name: 'Confirmar' })).toHaveCount(0);
  });

  test('un admin además puede marcarla como no presentada', async ({ page }) => {
    await setup(page, 'Admin');

    await page.getByRole('button', { name: /Laura Gómez/ }).click();

    await expect(
      page.getByRole('dialog').getByRole('button', { name: 'No presentada' })
    ).toBeVisible();
  });

  test('«Modificar» abre la reserva para esa cita y la actualiza a ella', async ({ page }) => {
    const { puts } = await setup(page, 'Employee');

    await page.getByRole('button', { name: /Laura Gómez/ }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Modificar' }).click();

    await expect(page).toHaveURL('/reservar?cita=1');
    await expect(page.getByTestId('booking-customer')).toHaveText('Cita para: Laura Gómez');
    await expect(page.getByRole('button', { name: 'Seleccionar cliente' })).toHaveCount(0);
    await page.getByRole('button', { name: '16:00' }).click();

    await expect(page.getByRole('dialog', { name: 'Cita reservada' })).toBeVisible();
    expect(puts).toHaveLength(1);
    await page.getByRole('button', { name: 'Aceptar' }).click();
    await expect(page).toHaveURL('/citas');
  });

  test('el personal cancela desde el detalle con «Otro motivo» (RA-869d7fcfy)', async ({
    page,
  }) => {
    const bodies: unknown[] = [];
    await setup(page, 'Employee');
    await page.route(/\/api\/v1\/appointments\/1\/cancel$/, (route) => {
      bodies.push(route.request().postDataJSON());
      return route.fulfill(ok({ ...list[0], status: 'cancelled_by_business' }));
    });

    await page.getByRole('button', { name: /Laura Gómez/ }).click();
    await page
      .getByRole('dialog', { name: 'Laura Gómez' })
      .getByRole('button', { name: 'Cancelar cita' })
      .click();
    const cancel = page.getByRole('dialog', { name: 'Cancelar cita' });
    await cancel.getByRole('combobox').click();
    await page.getByRole('option', { name: 'Otro motivo' }).click();
    await cancel.getByRole('textbox', { name: 'Escribe el motivo' }).fill('Avería en la cabina');
    await cancel.getByRole('button', { name: 'Cancelar cita' }).click();

    await expect(page.locator('[data-type="success"]')).toContainText('Cita cancelada.');
    expect(bodies).toEqual([{ reason: 'Avería en la cabina' }]);
  });

  test('sin violaciones WCAG 2.1 AA en el listado y en el detalle', async ({ page }) => {
    await setup(page, 'Admin');
    const axe = () =>
      new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();

    await page.getByRole('tab', { name: 'Semana' }).click();
    await expect(page.getByRole('heading', { level: 3 }).first()).toBeVisible();
    expect((await axe()).violations).toEqual([]);

    await page.getByRole('button', { name: /Laura Gómez/ }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    expect((await axe()).violations).toEqual([]);
  });
});
