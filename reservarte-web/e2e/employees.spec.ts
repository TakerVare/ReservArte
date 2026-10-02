import { test, expect, type Page, type Request } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Gestión de empleados (RA-869d7fbyt + RA-869d7fc0h) con la API simulada: lista con
 * foto e iniciales, filtro de baja, alta que lleva al horario, ficha con datos,
 * horario semanal (la semana entera en un PUT) y ausencias en UTC. Misma excepción
 * de contraste que login.a11y.spec.ts (RA-869f0v6vm).
 */

const ok = (data: unknown, status = 200, pagination?: unknown) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({ success: true, data, error: null, meta: { pagination } }),
});
const fail = (status: number, code: string, message: string) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({ success: false, data: null, error: { code, message }, meta: {} }),
});

function employee(id: number, firstName: string, lastName: string, extra: object = {}) {
  return {
    id,
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`,
    email: `${firstName.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase()}@reservarte.com`,
    phone: null,
    rol: 'Employee',
    profileImageUrl: null,
    hireDate: '2024-03-01',
    isActive: true,
    createdAt: '2024-03-01T09:00:00Z',
    ...extra,
  };
}

// Imagen de 1×1 px: la foto de María llega servida por la propia prueba.
const PIXEL = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=',
  'base64'
);

const MARIA = employee(2, 'María', 'García', { profileImageUrl: '/fotos/maria.png' });
const LUCIA = employee(3, 'Lucía', 'Martínez', { rol: 'Manager' });
const ANA = employee(4, 'Ana', 'Sanz', { isActive: false });

interface Calls {
  lists: URL[];
  bodies: Record<string, unknown[]>;
  deletes: string[];
}

async function setup(page: Page) {
  const calls: Calls = { lists: [], bodies: {}, deletes: [] };
  const record = (key: string, request: Request) => {
    (calls.bodies[key] ??= []).push(request.postDataJSON());
  };
  let maria = { ...MARIA };
  let schedule = [
    { id: 1, dayOfWeek: 0, startTime: '10:00:00', endTime: '14:00:00' },
    { id: 2, dayOfWeek: 0, startTime: '16:00:00', endTime: '20:00:00' },
    { id: 3, dayOfWeek: 2, startTime: '09:00:00', endTime: '15:00:00' },
  ].map((slot) => ({ ...slot, isRecurring: true, isActive: true }));
  let absences = [
    {
      id: 7,
      startDateTime: '2026-11-01T23:00:00Z',
      endDateTime: '2026-11-06T23:00:00Z',
      type: 'vacation',
      reason: 'Puente',
      isActive: true,
    },
  ];
  const availability = () => ({
    employeeId: 2,
    weeklySchedule: schedule,
    exceptions: absences,
    exceptionsFrom: '2026-10-01T00:00:00Z',
    exceptionsTo: '2027-10-01T00:00:00Z',
  });

  // Lo que la prueba no simula (Mis citas, tras entrar) responde vacío: sin esto iría a la
  // API real con el token falso y su 401 cerraría la sesión. Las rutas de después mandan.
  await page.route('**/api/v1/**', (route) => route.fulfill(ok({ items: [] })));
  let assigned = [11];
  await page.route(/\/api\/v1\/services(\?.*)?$/, (route) =>
    route.fulfill(
      ok({
        items: [
          {
            id: 10,
            name: 'Henna de cejas',
            durationMinutes: 40,
            basePrice: 22,
            categoryName: 'Cejas',
          },
          {
            id: 11,
            name: 'Lifting de pestañas',
            durationMinutes: 60,
            basePrice: 45,
            categoryName: 'Pestañas',
          },
          {
            id: 12,
            name: 'Diseño de cejas',
            durationMinutes: 30,
            basePrice: 15,
            categoryName: 'Cejas',
          },
        ],
      })
    )
  );
  await page.route(/\/api\/v1\/employees\/\d+\/services$/, (route) => {
    const request = route.request();
    if (request.method() === 'PUT') {
      record('services', request);
      assigned = request.postDataJSON().serviceIds;
    }
    const names: Record<number, string> = {
      10: 'Henna de cejas',
      11: 'Lifting de pestañas',
      12: 'Diseño de cejas',
    };
    return route.fulfill(
      ok({
        employeeId: 2,
        services: assigned.map((serviceId) => ({
          serviceId,
          name: names[serviceId],
          durationMinutes: 40,
          proficiencyLevel: 1,
          serviceIsActive: true,
        })),
      })
    );
  });
  await page.route('**/fotos/maria.png', (route) =>
    route.fulfill({ status: 200, contentType: 'image/png', body: PIXEL })
  );
  await page.route('**/api/v1/auth/login', (route) =>
    route.fulfill(
      ok({
        accessToken: 'fake',
        refreshToken: 'r',
        mfaRequired: false,
        mfaTicket: null,
        user: { id: 1, email: 'admin@reservarte.com', firstName: 'A', lastName: 'B', rol: 'Admin' },
      })
    )
  );
  await page.route(/\/api\/v1\/employees(\?.*)?$/, (route) => {
    const request = route.request();
    if (request.method() === 'POST') {
      record('create', request);
      const body = request.postDataJSON();
      return route.fulfill(ok(employee(9, body.firstName, body.lastName), 201));
    }
    const url = new URL(request.url());
    calls.lists.push(url);
    const items = url.searchParams.get('isActive') === 'false' ? [ANA] : [maria, LUCIA];
    return route.fulfill(
      ok({ items }, 200, { page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 })
    );
  });
  await page.route(/\/api\/v1\/employees\/(2|9)$/, (route) => {
    const request = route.request();
    if (request.method() === 'PUT') {
      record('update', request);
      const body = request.postDataJSON();
      if (body.email === 'lucia@reservarte.com') {
        return route.fulfill(fail(409, 'GEN_CONFLICT', 'Ya existe una cuenta con ese email.'));
      }
      maria = { ...maria, ...body, fullName: `${body.firstName} ${body.lastName}` };
      return route.fulfill(ok(maria));
    }
    if (request.method() === 'DELETE') {
      calls.deletes.push(new URL(request.url()).pathname);
      maria = { ...maria, isActive: false };
      return route.fulfill(ok(maria));
    }
    const id = Number(new URL(request.url()).pathname.split('/').pop());
    return route.fulfill(ok(id === 9 ? employee(9, 'Eva', 'Ruiz') : maria));
  });
  await page.route(/\/api\/v1\/employees\/\d+\/reactivate$/, (route) => {
    maria = { ...maria, isActive: true };
    return route.fulfill(ok(maria));
  });
  await page.route(/\/api\/v1\/employees\/\d+\/availability(\?.*)?$/, (route) => {
    const request = route.request();
    if (request.method() === 'PUT') {
      record('schedule', request);
      const body = request.postDataJSON();
      schedule = body.weeklySchedule.map((slot: object, i: number) => ({
        ...slot,
        id: 100 + i,
        isActive: true,
      }));
    } else {
      calls.lists.push(new URL(request.url()));
    }
    return route.fulfill(ok(availability()));
  });
  await page.route(/\/api\/v1\/employees\/\d+\/exceptions(\/\d+)?$/, (route) => {
    const request = route.request();
    if (request.method() === 'DELETE') {
      calls.deletes.push(new URL(request.url()).pathname);
      absences = [];
      return route.fulfill(ok({}));
    }
    record('absence', request);
    const body = request.postDataJSON();
    absences = [...absences, { ...body, id: 8, isActive: true }];
    return route.fulfill(ok({ ...body, id: 8, isActive: true }, 201));
  });

  await page.goto('/login');
  await page.getByLabel('Usuario').fill('admin@reservarte.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL('/mis-citas');
  await page.getByRole('link', { name: 'Mi cuenta' }).click();
  await page.getByRole('link', { name: 'Empleados' }).click();
  await expect(page).toHaveURL('/empleados');
  return calls;
}

async function openMaria(page: Page) {
  await page.getByRole('button', { name: 'Editar María García' }).click();
  await expect(page).toHaveURL('/empleados/2');
  await expect(page.getByLabel('Nombre')).toHaveValue('María');
}

test.describe('Gestión de empleados', () => {
  test('la lista muestra foto o iniciales, el rol y filtra los de baja', async ({ page }) => {
    const calls = await setup(page);

    const rows = page.locator('main li');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0).getByTestId('avatar-photo')).toBeVisible();
    await expect(rows.nth(1).getByTestId('avatar-initials')).toHaveText('LM');
    await expect(rows.nth(1)).toContainText('Gerencia');
    expect(calls.lists[0]!.searchParams.get('isActive')).toBe('true');

    await page.getByLabel('Estado').click();
    await page.getByRole('option', { name: 'De baja' }).click();
    await expect(rows).toHaveCount(1);
    await expect(rows.nth(0)).toContainText('Empleado · De baja');
    // Una ficha de baja no se puede volver a dar de baja.
    await expect(page.getByRole('button', { name: 'Eliminar Ana Sanz' })).toHaveCount(0);
    expect(calls.lists.at(-1)!.searchParams.get('isActive')).toBe('false');
  });

  test('dar de baja desde la lista pide confirmación', async ({ page }) => {
    const calls = await setup(page);

    await page.getByRole('button', { name: 'Eliminar María García' }).click();
    const dialog = page.getByRole('dialog', { name: 'Dar de baja' });
    await expect(dialog).toContainText('María García dejará de poder entrar');
    await dialog.getByRole('button', { name: 'Dar de baja' }).click();
    await expect(page.locator('[data-type="success"]')).toContainText('María García está de baja.');
    expect(calls.deletes).toEqual(['/api/v1/employees/2']);
  });

  test('el alta valida, crea y lleva al horario del nuevo empleado', async ({ page }) => {
    const calls = await setup(page);

    await page.getByRole('button', { name: 'Nuevo empleado' }).click();
    await expect(page).toHaveURL('/empleados/nuevo');
    await expect(page.getByLabel('Estado')).toHaveCount(0);
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.getByText('El nombre es obligatorio.')).toBeVisible();
    expect(calls.bodies.create).toBeUndefined();

    await page.getByLabel('Nombre').fill('Eva');
    await page.getByLabel('Apellidos').fill('Ruiz');
    await page.getByLabel('Email').fill('eva@reservarte.com');
    await page.getByLabel('Rol').click();
    await page.getByRole('option', { name: 'Gerencia' }).click();
    await page.getByRole('button', { name: 'Guardar' }).click();

    await expect(page).toHaveURL('/empleados/9?tab=services');
    await expect(page.getByRole('tab', { name: 'Servicios' })).toHaveAttribute(
      'aria-selected',
      'true'
    );
    expect(calls.bodies.create![0]).toMatchObject({
      firstName: 'Eva',
      lastName: 'Ruiz',
      email: 'eva@reservarte.com',
      phone: null,
      rol: 'Manager',
      hireDate: null,
    });
  });

  test('la ficha guarda los datos y muestra el email repetido en su campo', async ({ page }) => {
    const calls = await setup(page);
    await openMaria(page);

    await page.getByLabel(/Teléfono/).fill('600 111 222');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.locator('[data-type="success"]')).toContainText('Cambios guardados.');
    expect(calls.bodies.update![0]).toMatchObject({
      phone: '600 111 222',
      hireDate: '2024-03-01',
      profileImageUrl: '/fotos/maria.png',
    });

    await page.getByLabel('Email').fill('lucia@reservarte.com');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.getByText('Ya existe una cuenta con ese email.')).toBeVisible();
    await expect(page.getByLabel('Email')).toHaveAttribute('aria-invalid', 'true');
  });

  test('cambiar el estado en la ficha da de baja y «Reactivar» la deshace', async ({ page }) => {
    const calls = await setup(page);
    await openMaria(page);

    await page.getByLabel('Estado').click();
    await page.getByRole('option', { name: 'De baja' }).click();
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Reactivar' })).toBeVisible();
    await expect(page.getByText('Empleado · De baja')).toBeVisible();
    expect(calls.bodies.update).toHaveLength(1);
    expect(calls.deletes).toEqual(['/api/v1/employees/2']);

    await page.getByRole('button', { name: 'Reactivar' }).click();
    await expect(page.getByRole('button', { name: 'Dar de baja' })).toBeVisible();
    await expect(page.getByLabel('Estado')).toContainText('Activo');
  });

  test('los servicios que presta salen por categoría y se guardan todos de una vez', async ({
    page,
  }) => {
    const calls = await setup(page);
    await openMaria(page);

    await page.getByRole('tab', { name: 'Servicios' }).click();
    await expect(page).toHaveURL('/empleados/2?tab=services');
    const cejas = page.getByRole('group', { name: 'Cejas' });
    await expect(cejas.getByRole('checkbox')).toHaveCount(2);
    // Dentro de cada categoría, por nombre.
    await expect(cejas.locator('label').first()).toContainText('Diseño de cejas');
    await expect(page.getByRole('checkbox', { name: /Lifting de pestañas/ })).toBeChecked();
    await expect(page.getByTestId('services-count')).toHaveText('1 de 3 servicios marcados');

    await page.getByRole('checkbox', { name: /Henna de cejas/ }).check();
    await page.getByRole('checkbox', { name: /Lifting de pestañas/ }).uncheck();
    await expect(page.getByTestId('services-count')).toHaveText('1 de 3 servicios marcados');
    await page.getByRole('button', { name: 'Guardar servicios' }).click();
    await expect(page.locator('[data-type="success"]')).toContainText('Servicios guardados.');
    expect(calls.bodies.services![0]).toEqual({ serviceIds: [10] });

    await page.getByRole('button', { name: 'Marcar todos', exact: true }).click();
    await page.getByRole('button', { name: 'Guardar servicios' }).click();
    await expect.poll(() => calls.bodies.services!.length).toBe(2);
    expect((calls.bodies.services![1] as { serviceIds: number[] }).serviceIds.sort()).toEqual([
      10, 11, 12,
    ]);
  });

  test('el horario se edita por días y se guarda la semana entera', async ({ page }) => {
    const calls = await setup(page);
    await openMaria(page);

    await page.getByRole('tab', { name: 'Horario' }).click();
    await expect(page).toHaveURL('/empleados/2?tab=schedule');
    const monday = page.getByTestId('schedule-day-0');
    await expect(monday.locator('input[type=time]')).toHaveCount(4);
    await expect(page.getByTestId('schedule-day-1')).toContainText('Libre');

    // Un solape no se manda a la API.
    await monday.locator('input[type=time]').nth(2).fill('13:00');
    await page.getByRole('button', { name: 'Guardar horario' }).click();
    await expect(monday.getByRole('alert')).toHaveText('Hay tramos que se solapan.');
    expect(calls.bodies.schedule).toBeUndefined();
    await monday.locator('input[type=time]').nth(2).fill('16:00');

    await page.getByRole('button', { name: 'Copiar el lunes de martes a viernes' }).click();
    await page.getByLabel('Trabaja el Sábado').check();
    await page.getByLabel('Trabaja el Miércoles').uncheck();
    await page.getByRole('button', { name: 'Guardar horario' }).click();
    await expect(page.locator('[data-type="success"]')).toContainText('Horario guardado.');

    const sent = (calls.bodies.schedule![0] as { weeklySchedule: { dayOfWeek: number }[] })
      .weeklySchedule;
    expect(sent.map((slot) => slot.dayOfWeek)).toEqual([0, 0, 1, 1, 3, 3, 4, 4, 5]);
    expect(sent[0]).toEqual({
      dayOfWeek: 0,
      startTime: '10:00:00',
      endTime: '14:00:00',
      isRecurring: true,
    });
    expect(sent.at(-1)).toMatchObject({ dayOfWeek: 5, startTime: '10:00:00', endTime: '14:00:00' });
  });

  test('las ausencias se dan de alta en hora del centro y viajan en UTC', async ({ page }) => {
    const calls = await setup(page);
    await openMaria(page);

    await page.getByRole('tab', { name: 'Ausencias' }).click();
    await expect(page.getByText('Del 2 nov 2026 al 6 nov 2026')).toBeVisible();
    await expect(page.getByText('Puente')).toBeVisible();

    await page.getByRole('button', { name: 'Añadir ausencia' }).click();
    const dialog = page.getByRole('dialog', { name: 'Nueva ausencia' });
    await dialog.getByLabel('Tipo').click();
    await page.getByRole('option', { name: 'Formación' }).click();
    await dialog.getByLabel('Desde el día').fill('2027-03-30');
    await dialog.getByLabel('Días completos').uncheck();
    await dialog.getByLabel('Hora de inicio').fill('16:00');
    await dialog.getByLabel('Hora de fin').fill('20:00');
    await dialog.getByRole('button', { name: 'Guardar ausencia' }).click();

    await expect(page.getByText('30 mar 2027, de 16:00 a 20:00')).toBeVisible();
    // Marzo de 2027 ya está en horario de verano (UTC+2).
    expect(calls.bodies.absence![0]).toEqual({
      startDateTime: '2027-03-30T14:00:00.000Z',
      endDateTime: '2027-03-30T18:00:00.000Z',
      type: 'training',
      reason: null,
    });

    await page
      .getByRole('button', { name: 'Quitar la ausencia: Del 2 nov 2026 al 6 nov 2026' })
      .click();
    await expect(page.getByText('No hay ausencias previstas.')).toBeVisible();
    expect(calls.deletes).toEqual(['/api/v1/employees/2/exceptions/7']);
  });

  test('la ficha cumple WCAG 2.1 AA en sus cuatro pestañas', async ({ page }) => {
    await setup(page);
    const axe = () =>
      new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();

    expect((await axe()).violations).toEqual([]);
    await openMaria(page);
    expect((await axe()).violations).toEqual([]);
    await page.getByRole('tab', { name: 'Servicios' }).click();
    expect((await axe()).violations).toEqual([]);
    await page.getByRole('tab', { name: 'Horario' }).click();
    expect((await axe()).violations).toEqual([]);
    await page.getByRole('tab', { name: 'Ausencias' }).click();
    await page.getByRole('button', { name: 'Añadir ausencia' }).click();
    expect((await axe()).violations).toEqual([]);
  });
});
