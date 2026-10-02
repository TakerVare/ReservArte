import { test, expect, type Page, type Request } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Gestión de clientes (RA-869d7fc34 + RA-869d7fc51) con la API simulada: lista con
 * filtros, alta con consentimientos RGPD, ficha con datos, notas del personal,
 * prueba de alergia e historial de citas. Misma excepción de contraste que
 * login.a11y.spec.ts (RA-869f0v6vm).
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

function customer(id: number, firstName: string, lastName: string, extra: object = {}) {
  return {
    id,
    firstName,
    lastName,
    fullName: `${firstName} ${lastName}`,
    email: `cliente${id}@example.com`,
    phone: null,
    profileImageUrl: null,
    birthDate: null,
    category: 'regular',
    loyaltyPoints: 0,
    isBlocked: false,
    blockedReason: null,
    preferredContactMethod: 'email',
    lastAllergyTestAt: null,
    isActive: true,
    createdAt: '2026-09-01T09:00:00Z',
    ...extra,
  };
}

const CARMEN = customer(4, 'Carmen', 'López', { category: 'vip' });
const SOFIA = customer(5, 'Sofía', 'Ruiz', { isBlocked: true, blockedReason: 'Impagos' });
const MARTA = customer(6, 'Marta', 'Díaz', { isActive: false });

function appointment(id: number, date: string, status: string) {
  return {
    id,
    customerId: 4,
    employeeId: 2,
    appointmentDate: date,
    startTime: '10:00:00',
    endTime: '10:40:00',
    status,
    customerName: 'Carmen López',
    employeeName: 'Lucía Martínez',
    totalPrice: 22,
    items: [
      { serviceId: 3, serviceName: 'Henna de cejas', price: 22, durationMinutes: 40, order: 1 },
    ],
  };
}

interface Calls {
  lists: URL[];
  bodies: Record<string, unknown[]>;
  deletes: string[];
  history: URL[];
}

async function setup(page: Page, options: { notesForbidden?: boolean } = {}) {
  const calls: Calls = { lists: [], bodies: {}, deletes: [], history: [] };
  const record = (key: string, request: Request) => {
    (calls.bodies[key] ??= []).push(request.postDataJSON());
  };
  let carmen = {
    ...CARMEN,
    consents: [
      {
        consentType: 'data_processing',
        isGranted: true,
        grantedAt: '2026-09-29T10:00:00Z',
        revokedAt: null,
      },
      {
        consentType: 'marketing',
        isGranted: false,
        grantedAt: null,
        revokedAt: '2026-09-30T10:00:00Z',
      },
    ],
    allergies: [{ id: 1, allergyDescription: 'Tinte PPD', severity: 'high' }],
    notes: [
      {
        id: 31,
        note: 'Prefiere cita por la tarde',
        employeeId: 2,
        employeeName: 'Lucía Martínez',
        createdAt: '2026-09-30T15:00:00Z',
      },
    ],
  };

  // Lo no simulado responde vacío (regla de frontend): sin esto, con la API real en
  // marcha, su 401 al token falso cerraría la sesión.
  await page.route('**/api/v1/**', (route) => route.fulfill(ok({ items: [] })));
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
  await page.route(/\/api\/v1\/customers(\?.*)?$/, (route) => {
    const request = route.request();
    if (request.method() === 'POST') {
      record('create', request);
      const body = request.postDataJSON();
      return route.fulfill(ok(customer(9, body.firstName, body.lastName), 201));
    }
    const url = new URL(request.url());
    calls.lists.push(url);
    let items = url.searchParams.get('isActive') === 'false' ? [MARTA] : [carmen, SOFIA];
    const category = url.searchParams.get('category');
    if (category) items = items.filter((c) => c.category === category);
    return route.fulfill(
      ok({ items }, 200, { page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 })
    );
  });
  await page.route(/\/api\/v1\/customers\/(4|9)$/, (route) => {
    const request = route.request();
    if (request.method() === 'PUT') {
      record('update', request);
      const body = request.postDataJSON();
      if (body.email === 'cliente5@example.com') {
        return route.fulfill(fail(409, 'GEN_CONFLICT', 'Ya existe un cliente con ese email.'));
      }
      carmen = { ...carmen, ...body, fullName: `${body.firstName} ${body.lastName}` };
      return route.fulfill(ok(carmen));
    }
    if (request.method() === 'DELETE') {
      calls.deletes.push(new URL(request.url()).pathname);
      carmen = { ...carmen, isActive: false };
      return route.fulfill(ok(carmen));
    }
    const id = Number(new URL(request.url()).pathname.split('/').pop());
    return route.fulfill(
      ok(
        id === 9
          ? { ...customer(9, 'Eva', 'Ruiz'), consents: [], allergies: [], notes: [] }
          : carmen
      )
    );
  });
  await page.route(/\/api\/v1\/customers\/\d+\/reactivate$/, (route) => {
    carmen = { ...carmen, isActive: true };
    return route.fulfill(ok(carmen));
  });
  await page.route(/\/api\/v1\/customers\/\d+\/notes(\/\d+)?$/, (route) => {
    const request = route.request();
    if (request.method() === 'DELETE') {
      calls.deletes.push(new URL(request.url()).pathname);
      carmen = { ...carmen, notes: [] };
      return route.fulfill(ok({}));
    }
    record('note', request);
    if (options.notesForbidden) {
      return route.fulfill(
        fail(
          403,
          'GEN_FORBIDDEN',
          'Solo el personal con ficha de empleado activa puede escribir notas.'
        )
      );
    }
    const note = {
      id: 32,
      note: request.postDataJSON().note,
      employeeId: 1,
      employeeName: 'Ana Admin',
      createdAt: '2026-10-02T08:00:00Z',
    };
    carmen = { ...carmen, notes: [note, ...carmen.notes] };
    return route.fulfill(ok(note, 201));
  });
  await page.route(/\/api\/v1\/customers\/\d+\/allergy-test$/, (route) => {
    record('allergy', route.request());
    carmen = { ...carmen, lastAllergyTestAt: route.request().postDataJSON().testedAt };
    return route.fulfill(ok(carmen));
  });
  await page.route(/\/api\/v1\/customers\/\d+\/history(\?.*)?$/, (route) => {
    const url = new URL(route.request().url());
    calls.history.push(url);
    const page = Number(url.searchParams.get('page'));
    const items =
      page === 1
        ? [appointment(51, '2026-10-01', 'confirmed'), appointment(50, '2026-09-20', 'completed')]
        : [appointment(49, '2026-08-10', 'no_show')];
    return route.fulfill(ok({ items }, 200, { page, pageSize: 20, totalCount: 3, totalPages: 2 }));
  });

  await page.goto('/login');
  await page.getByLabel('Usuario').fill('admin@reservarte.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL('/mis-citas');
  await page.getByRole('link', { name: 'Mi cuenta' }).click();
  await page.getByRole('link', { name: 'Clientes' }).click();
  await expect(page).toHaveURL('/clientes');
  return calls;
}

async function openCarmen(page: Page) {
  await page.getByRole('button', { name: 'Editar Carmen López' }).click();
  await expect(page).toHaveURL('/clientes/4');
  await expect(page.getByLabel('Nombre')).toHaveValue('Carmen');
}

test.describe('Gestión de clientes', () => {
  test('la lista enseña categoría y bloqueo, y filtra por estado y categoría', async ({ page }) => {
    const calls = await setup(page);

    const rows = page.locator('main li');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('VIP');
    await expect(rows.nth(1)).toContainText('Habitual · Bloqueado');
    await expect(rows.nth(1).getByTestId('avatar-initials')).toHaveText('SR');

    await page.getByLabel('Categoría').click();
    await page.getByRole('option', { name: 'VIP' }).click();
    await expect(rows).toHaveCount(1);
    expect(calls.lists.at(-1)!.searchParams.get('category')).toBe('vip');

    await page.getByLabel('Estado').click();
    await page.getByRole('option', { name: 'De baja' }).click();
    await expect.poll(() => calls.lists.at(-1)!.searchParams.get('isActive')).toBe('false');
    // La categoría se mantiene al cambiar el estado.
    expect(calls.lists.at(-1)!.searchParams.get('category')).toBe('vip');
  });

  test('el alta exige el consentimiento de datos y manda solo los aceptados', async ({ page }) => {
    const calls = await setup(page);

    await page.getByRole('button', { name: 'Nuevo cliente' }).click();
    await expect(page).toHaveURL('/clientes/nuevo');
    await page.getByLabel('Nombre').fill('Eva');
    await page.getByLabel('Apellidos').fill('Ruiz');
    await page.getByLabel('Email').fill('eva@example.com');
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(
      page.getByText('El consentimiento de tratamiento de datos es obligatorio.')
    ).toBeVisible();
    expect(calls.bodies.create).toBeUndefined();

    await page.getByLabel(/Tratamiento de sus datos/).check();
    await page.getByLabel('Avisos por WhatsApp').check();
    await page.getByLabel('Contacto preferido').click();
    await page.getByRole('option', { name: 'WhatsApp' }).click();
    await page.getByRole('button', { name: 'Guardar' }).click();

    await expect(page).toHaveURL('/clientes/9');
    expect(calls.bodies.create![0]).toEqual({
      firstName: 'Eva',
      lastName: 'Ruiz',
      email: 'eva@example.com',
      phone: null,
      birthDate: null,
      category: 'new',
      preferredContactMethod: 'whatsapp',
      profileImageUrl: null,
      grantedConsents: ['data_processing', 'whatsapp'],
    });
  });

  test('la ficha enseña los consentimientos, guarda y marca el email repetido', async ({
    page,
  }) => {
    const calls = await setup(page);
    await openCarmen(page);

    const consents = page.getByTestId('customer-consents');
    await expect(consents).toContainText('Aceptado el 29/09/2026');
    await expect(consents).toContainText('Retirado el 30/09/2026');

    await page.getByLabel(/Fecha de nacimiento/).fill('1990-05-17');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.locator('[data-type="success"]')).toContainText('Cambios guardados.');
    expect(calls.bodies.update![0]).toMatchObject({ birthDate: '1990-05-17', category: 'vip' });

    await page.getByLabel('Email').fill('cliente5@example.com');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.getByText('Ya existe un cliente con ese email.')).toBeVisible();

    await page.getByRole('button', { name: 'Dar de baja' }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Dar de baja' }).click();
    await expect(page.getByRole('button', { name: 'Reactivar' })).toBeVisible();
    expect(calls.deletes).toEqual(['/api/v1/customers/4']);
  });

  test('las notas llevan su autora; se añaden y se borran', async ({ page }) => {
    const calls = await setup(page);
    await openCarmen(page);

    await page.getByRole('tab', { name: 'Notas' }).click();
    await expect(page).toHaveURL('/clientes/4?tab=notes');
    await expect(page.getByText('Lucía Martínez · 30/09/2026')).toBeVisible();

    await page.getByLabel('Nueva nota').fill('  Trae su propio tinte  ');
    await page.getByRole('button', { name: 'Añadir nota' }).click();
    await expect(page.getByText('Trae su propio tinte')).toBeVisible();
    await expect(page.getByLabel('Nueva nota')).toHaveValue('');
    expect(calls.bodies.note![0]).toEqual({ note: 'Trae su propio tinte' });

    await page.getByRole('button', { name: /Borrar la nota del 30\/09\/2026/ }).click();
    await expect(page.getByText('No hay notas.')).toBeVisible();
    expect(calls.deletes).toEqual(['/api/v1/customers/4/notes/31']);
  });

  test('sin ficha de empleado, la nota no se guarda y se explica por qué', async ({ page }) => {
    await setup(page, { notesForbidden: true });
    await openCarmen(page);

    await page.getByRole('tab', { name: 'Notas' }).click();
    await page.getByLabel('Nueva nota').fill('No se guardará');
    await page.getByRole('button', { name: 'Añadir nota' }).click();
    await expect(page.locator('[data-type="error"]')).toContainText(
      'Solo el personal con ficha de empleado activa puede escribir notas.'
    );
    // El borrador se conserva para no perder lo escrito.
    await expect(page.getByLabel('Nueva nota')).toHaveValue('No se guardará');
  });

  test('la prueba de alergia se registra en hora del centro y viaja en UTC', async ({ page }) => {
    const calls = await setup(page);
    await openCarmen(page);

    await page.getByRole('tab', { name: 'Alergias' }).click();
    await expect(page.getByTestId('allergy-last')).toHaveText('No consta ninguna prueba.');
    await expect(page.getByText('Tinte PPD')).toBeVisible();
    await expect(page.getByText('Grave')).toBeVisible();

    await page.getByRole('button', { name: 'Registrar prueba' }).click();
    const dialog = page.getByRole('dialog', { name: 'Registrar prueba de alergia' });
    await dialog.getByLabel('Día').fill('2026-09-15');
    await dialog.getByLabel('Hora').fill('17:30');
    await dialog.getByRole('button', { name: 'Guardar' }).click();

    await expect(page.getByTestId('allergy-last')).toHaveText('Última prueba: 15/09/2026 17:30');
    // Septiembre, horario de verano (UTC+2).
    expect(calls.bodies.allergy![0]).toEqual({ testedAt: '2026-09-15T15:30:00.000Z' });
  });

  test('el historial va de la más reciente a la más antigua y «Ver más» pide la siguiente página', async ({
    page,
  }) => {
    const calls = await setup(page);
    await openCarmen(page);

    await page.getByRole('tab', { name: 'Citas' }).click();
    const rows = page.getByTestId('customer-history').locator('li');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('1 Oct - 10:00h');
    await expect(rows.nth(0)).toContainText('Henna de cejas');
    await expect(rows.nth(0)).toContainText('Con Lucía Martínez');
    await expect(rows.nth(0)).toContainText('Confirmada');

    await page.getByRole('button', { name: 'Ver más' }).click();
    await expect(rows).toHaveCount(3);
    await expect(page.getByRole('button', { name: 'Ver más' })).toHaveCount(0);
    expect(calls.history.map((u) => u.searchParams.get('page'))).toEqual(['1', '2']);
  });

  test('la lista y la ficha cumplen WCAG 2.1 AA', async ({ page }) => {
    await setup(page);
    const axe = () =>
      new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();

    expect((await axe()).violations).toEqual([]);
    await openCarmen(page);
    expect((await axe()).violations).toEqual([]);
    for (const tab of ['Notas', 'Alergias', 'Citas']) {
      await page.getByRole('tab', { name: tab }).click();
      expect((await axe()).violations).toEqual([]);
    }
    await page.getByRole('tab', { name: 'Alergias' }).click();
    await page.getByRole('button', { name: 'Registrar prueba' }).click();
    expect((await axe()).violations).toEqual([]);
    await page.goto('/clientes/nuevo');
    await expect(page.getByLabel('Nombre')).toBeVisible();
    expect((await axe()).violations).toEqual([]);
  });
});
