import { test, expect, type Page, type Request } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Gestión de servicios (RA-869d7fc6b) con la API simulada: lista con filtros, alta
 * con categoría nueva, ficha con datos y variaciones con su total. Misma excepción
 * de contraste que login.a11y.spec.ts (RA-869f0v6vm).
 */

const ok = (data: unknown, status = 200, pagination?: unknown) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({ success: true, data, error: null, meta: { pagination } }),
});
const fail = (status: number, code: string, field: string, message: string) => ({
  status,
  contentType: 'application/json',
  body: JSON.stringify({
    success: false,
    data: null,
    error: { code, message, details: [{ field, code: 'X', message }] },
    meta: {},
  }),
});

function service(id: number, name: string, extra: object = {}) {
  return {
    id,
    name,
    description: null,
    durationMinutes: 40,
    basePrice: 22,
    categoryId: 1,
    categoryName: 'Cejas',
    imageUrl: null,
    requiresAllergyTest: false,
    allergyTestHoursBefore: 48,
    isActive: true,
    createdAt: '2026-09-01T09:00:00Z',
    ...extra,
  };
}

const CATEGORIES = [
  { id: 1, name: 'Cejas', displayOrder: 0, isActive: true },
  { id: 2, name: 'Pestañas', displayOrder: 1, isActive: true },
  { id: 3, name: 'Antigua', displayOrder: 2, isActive: false },
];

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
  const categories = [...CATEGORIES];
  let henna = {
    ...service(5, 'Henna de cejas', { requiresAllergyTest: true }),
    variations: [{ id: 7, name: 'Con diseño', priceModifier: 5, durationModifier: 10 }],
    pricings: [],
  };

  // Lo no simulado responde vacío (regla de frontend).
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
  await page.route(/\/api\/v1\/services\/categories$/, (route) => {
    if (route.request().method() === 'POST') {
      record('category', route.request());
      const created = {
        id: 9,
        name: route.request().postDataJSON().name,
        displayOrder: 0,
        isActive: true,
      };
      categories.push(created);
      return route.fulfill(ok(created, 201));
    }
    return route.fulfill(ok({ items: categories }));
  });
  await page.route(/\/api\/v1\/services(\?.*)?$/, (route) => {
    const request = route.request();
    if (request.method() === 'POST') {
      record('create', request);
      return route.fulfill(ok(service(9, request.postDataJSON().name), 201));
    }
    const url = new URL(request.url());
    calls.lists.push(url);
    let items =
      url.searchParams.get('isActive') === 'false'
        ? [service(6, 'Tinte antiguo', { isActive: false })]
        : [
            henna,
            service(8, 'Lifting', { categoryId: 2, categoryName: 'Pestañas', basePrice: 40 }),
          ];
    const categoryId = url.searchParams.get('categoryId');
    if (categoryId) items = items.filter((s) => String(s.categoryId) === categoryId);
    return route.fulfill(
      ok({ items }, 200, { page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 })
    );
  });
  await page.route(/\/api\/v1\/services\/(5|9)$/, (route) => {
    const request = route.request();
    if (request.method() === 'PUT') {
      record('update', request);
      henna = { ...henna, ...request.postDataJSON() };
      return route.fulfill(ok(henna));
    }
    if (request.method() === 'DELETE') {
      calls.deletes.push(new URL(request.url()).pathname);
      henna = { ...henna, isActive: false };
      return route.fulfill(ok(henna));
    }
    const id = Number(new URL(request.url()).pathname.split('/').pop());
    return route.fulfill(
      ok(id === 9 ? { ...service(9, 'Nuevo'), variations: [], pricings: [] } : henna)
    );
  });
  await page.route(/\/api\/v1\/services\/5\/variations(\/\d+)?$/, (route) => {
    const request = route.request();
    const id = Number(new URL(request.url()).pathname.split('/').pop());
    if (request.method() === 'DELETE') {
      calls.deletes.push(new URL(request.url()).pathname);
      henna = { ...henna, variations: henna.variations.filter((v) => v.id !== id) };
      return route.fulfill(ok({}));
    }
    const body = request.postDataJSON();
    record(request.method() === 'POST' ? 'variationAdd' : 'variationEdit', request);
    if (body.name === 'Rechazada') {
      return route.fulfill(
        fail(
          400,
          'GEN_VALIDATION_FAILED',
          'durationModifier',
          'La duración resultante debe ser positiva.'
        )
      );
    }
    const variation = { id: request.method() === 'POST' ? 8 : id, ...body };
    henna = {
      ...henna,
      variations: [...henna.variations.filter((v) => v.id !== variation.id), variation],
    };
    return route.fulfill(ok(variation, request.method() === 'POST' ? 201 : 200));
  });

  await page.goto('/login');
  await page.getByLabel('Usuario').fill('admin@reservarte.com');
  await page.getByLabel('Contraseña').fill('Secreta123!');
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL('/mis-citas');
  await page.getByRole('link', { name: 'Mi cuenta' }).click();
  await page.getByRole('link', { name: 'Servicios' }).click();
  await expect(page).toHaveURL('/servicios');
  return calls;
}

async function openHenna(page: Page) {
  await page.getByRole('button', { name: 'Editar Henna de cejas' }).click();
  await expect(page).toHaveURL('/servicios/5');
  await expect(page.getByLabel('Nombre')).toHaveValue('Henna de cejas');
}

test.describe('Gestión de servicios', () => {
  test('la lista enseña categoría, duración y precio, y filtra', async ({ page }) => {
    const calls = await setup(page);

    const rows = page.locator('main li');
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText('Cejas · 40 min · 22,00 €');
    await expect(rows.nth(1)).toContainText('Pestañas · 40 min · 40,00 €');

    // Las categorías retiradas no se ofrecen en el filtro.
    await page.getByLabel('Categoría').click();
    await expect(page.getByRole('option', { name: 'Antigua' })).toHaveCount(0);
    await page.getByRole('option', { name: 'Pestañas' }).click();
    await expect(rows).toHaveCount(1);
    expect(calls.lists.at(-1)!.searchParams.get('categoryId')).toBe('2');

    await page.getByLabel('Estado').click();
    await page.getByRole('option', { name: 'De baja' }).click();
    await expect.poll(() => calls.lists.at(-1)!.searchParams.get('isActive')).toBe('false');
  });

  test('el alta valida, crea una categoría y la deja elegida', async ({ page }) => {
    const calls = await setup(page);

    await page.getByRole('button', { name: 'Nuevo servicio' }).click();
    await expect(page).toHaveURL('/servicios/nuevo');
    await page.getByLabel(/Precio base/).fill('');
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.getByText('El nombre es obligatorio.')).toBeVisible();
    await expect(page.getByText('Indica el precio.')).toBeVisible();
    expect(calls.bodies.create).toBeUndefined();

    await page.getByLabel('Nombre').fill('Brow lamination');
    await page.getByLabel(/Precio base/).fill('35.5');
    await page.getByLabel(/Duración/).fill('50');
    await page.getByLabel('Exige prueba de alergia previa').check();
    await page.getByLabel(/Antelación mínima/).fill('24');
    await page.getByRole('button', { name: 'Nueva categoría' }).click();
    const dialog = page.getByRole('dialog', { name: 'Nueva categoría' });
    await dialog.getByLabel('Nombre').fill('Laminados');
    await dialog.getByRole('button', { name: 'Crear' }).click();
    // `exact`: mientras se cierra, el diálogo «Nueva categoría» también casa con «Categoría».
    await expect(page.getByLabel('Categoría', { exact: true })).toContainText('Laminados');
    expect(calls.bodies.category![0]).toEqual({ name: 'Laminados', displayOrder: 0 });

    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page).toHaveURL('/servicios/9');
    expect(calls.bodies.create![0]).toEqual({
      name: 'Brow lamination',
      description: null,
      categoryId: 9,
      durationMinutes: 50,
      basePrice: 35.5,
      imageUrl: null,
      requiresAllergyTest: true,
      allergyTestHoursBefore: 24,
    });
  });

  test('la ficha guarda los datos y la baja pide confirmación', async ({ page }) => {
    const calls = await setup(page);
    await openHenna(page);

    await expect(page.getByLabel(/Antelación mínima/)).toHaveValue('48');
    await page.getByLabel('Categoría').click();
    await page.getByRole('option', { name: 'Sin categoría' }).click();
    await page.getByLabel(/Descripción/).fill('Tinte vegetal');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.locator('[data-type="success"]')).toContainText('Cambios guardados.');
    expect(calls.bodies.update![0]).toMatchObject({
      categoryId: null,
      description: 'Tinte vegetal',
      requiresAllergyTest: true,
    });

    await page.getByRole('button', { name: 'Dar de baja' }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Dar de baja' }).click();
    await expect(page.getByRole('button', { name: 'Reactivar' })).toBeVisible();
    expect(calls.deletes).toEqual(['/api/v1/services/5']);
  });

  test('las variaciones enseñan su total y se añaden, editan y quitan', async ({ page }) => {
    const calls = await setup(page);
    await openHenna(page);

    await page.getByRole('tab', { name: 'Variaciones' }).click();
    await expect(page).toHaveURL('/servicios/5?tab=variations');
    const list = page.getByTestId('service-variations');
    await expect(list).toContainText('+5,00 € · +10 min');
    await expect(list).toContainText('Total: 27,00 € · 50 min');

    await page.getByRole('button', { name: 'Añadir variación' }).click();
    const dialog = page.getByRole('dialog', { name: 'Nueva variación' });
    await dialog.getByLabel('Nombre').fill('Exprés');
    await dialog.getByLabel(/Ajuste de duración/).fill('-40');
    await dialog.getByRole('button', { name: 'Guardar' }).click();
    await expect(dialog.getByRole('alert')).toHaveText(
      'Con este ajuste el servicio duraría 0 minutos o menos.'
    );
    expect(calls.bodies.variationAdd).toBeUndefined();

    await dialog.getByLabel(/Ajuste de duración/).fill('-10');
    await dialog.getByLabel(/Ajuste de precio/).fill('-2');
    await dialog.getByRole('button', { name: 'Guardar' }).click();
    await expect(list).toContainText('Total: 20,00 € · 30 min');
    expect(calls.bodies.variationAdd![0]).toEqual({
      name: 'Exprés',
      priceModifier: -2,
      durationModifier: -10,
    });

    // Un 400 de la API se enseña dentro del diálogo, que sigue abierto.
    await page.getByRole('button', { name: 'Editar la variación: Exprés' }).click();
    const edit = page.getByRole('dialog', { name: 'Editar variación' });
    await edit.getByLabel('Nombre').fill('Rechazada');
    await edit.getByRole('button', { name: 'Guardar' }).click();
    await expect(edit.getByRole('alert')).toHaveText('La duración resultante debe ser positiva.');
    await edit.getByRole('button', { name: 'Cancelar' }).click();

    await page.getByRole('button', { name: 'Quitar la variación: Con diseño' }).click();
    await expect(list).not.toContainText('Con diseño');
    expect(calls.deletes).toEqual(['/api/v1/services/5/variations/7']);
  });

  test('la lista y la ficha cumplen WCAG 2.1 AA', async ({ page }) => {
    await setup(page);
    const axe = () =>
      new AxeBuilder({ page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
        .disableRules(['color-contrast'])
        .analyze();

    expect((await axe()).violations).toEqual([]);
    await openHenna(page);
    expect((await axe()).violations).toEqual([]);
    await page.getByRole('button', { name: 'Nueva categoría' }).click();
    expect((await axe()).violations).toEqual([]);
    await page.keyboard.press('Escape');
    await page.getByRole('tab', { name: 'Variaciones' }).click();
    expect((await axe()).violations).toEqual([]);
    await page.getByRole('button', { name: 'Añadir variación' }).click();
    expect((await axe()).violations).toEqual([]);
  });
});
