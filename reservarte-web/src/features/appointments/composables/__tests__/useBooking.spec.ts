import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { effectScope, nextTick } from 'vue';
import { AxiosError, type AxiosAdapter, type InternalAxiosRequestConfig } from 'axios';
import apiClient from '@lib/api/client';
import { useBooking } from '../useBooking';

/** Petición registrada: método, ruta, query y cuerpo. */
interface Seen {
  method: string;
  url: string;
  params: Record<string, unknown>;
  body: unknown;
}

const originalAdapter = apiClient.defaults.adapter;
let seen: Seen[] = [];

/** Responde según «MÉTODO ruta»: el primer manejador cuya clave coincide. */
function routes(handlers: Record<string, (req: Seen) => [number, unknown]>): AxiosAdapter {
  return async (config: InternalAxiosRequestConfig) => {
    const req: Seen = {
      method: (config.method ?? 'get').toUpperCase(),
      url: config.url ?? '',
      params: (config.params ?? {}) as Record<string, unknown>,
      body: config.data ? JSON.parse(config.data as string) : undefined,
    };
    seen.push(req);
    const key = Object.keys(handlers).find((k) => k === `${req.method} ${req.url}`);
    const [status, data] = key
      ? handlers[key](req)
      : [404, { success: false, data: null, error: { code: 'GEN_NOT_FOUND', message: '' } }];
    const response = { status, statusText: '', headers: {}, config, data };
    if (status >= 400) throw new AxiosError('HTTP ' + status, undefined, config, null, response);
    return response;
  };
}

const ok = (data: unknown): [number, unknown] => [
  200,
  { success: true, data, error: null, meta: {} },
];
const fail = (status: number, code: string): [number, unknown] => [
  status,
  { success: false, data: null, error: { code, message: code }, meta: {} },
];

// 1 de octubre de 2026, 12:00 hora local.
const NOW = new Date(2026, 9, 1, 12, 0);

const detail = { id: 9, appointmentDate: '2026-10-08', startTime: '10:00:00', employeeName: 'Ana' };
const active = {
  id: 5,
  customerId: 7,
  employeeId: 2,
  appointmentDate: '2026-10-20',
  startTime: '11:00:00',
  endTime: '11:30:00',
  status: 'confirmed',
  customerName: 'Laura',
  employeeName: 'Ana',
};

const baseRoutes = {
  'GET /api/v1/services': () =>
    ok({ items: [{ id: 3, name: 'Henna de cejas', durationMinutes: 40, basePrice: 22 }] }),
  'GET /api/v1/appointments/availability/days': () =>
    ok({
      days: ['2026-10-02', '2026-10-08'],
      bookableFrom: '2026-10-01',
      bookableUntil: '2026-11-12',
    }),
  'GET /api/v1/appointments/availability/by-service': () =>
    ok({
      durationMinutes: 40,
      bookableFrom: '2026-10-01',
      bookableUntil: '2026-11-12',
      employees: [
        {
          employeeId: 2,
          employeeName: 'Ana',
          slots: [{ startTime: '10:00:00', endTime: '10:40:00' }],
        },
      ],
    }),
};

const flush = async () => {
  for (let i = 0; i < 5; i++) {
    await nextTick();
    await Promise.resolve();
  }
};

let scope: ReturnType<typeof effectScope>;

function start(isStaff: boolean, extra: Record<string, (req: Seen) => [number, unknown]> = {}) {
  apiClient.defaults.adapter = routes({ ...baseRoutes, ...extra });
  return scope.run(() => useBooking({ isStaff: () => isStaff, now: () => NOW }))!;
}

async function chooseServiceAndDay(booking: ReturnType<typeof start>) {
  await flush();
  booking.serviceId.value = 3;
  await flush();
  booking.date.value = '2026-10-08';
  await flush();
}

beforeEach(() => {
  seen = [];
  scope = effectScope();
});

afterEach(() => {
  scope.stop();
  apiClient.defaults.adapter = originalAdapter;
});

describe('useBooking', () => {
  it('carga los servicios, los días con hueco del mes y los huecos del día elegido', async () => {
    const booking = start(false);
    await chooseServiceAndDay(booking);

    expect(booking.services.value.map((s) => s.name)).toEqual(['Henna de cejas']);
    const daysCall = seen.find((r) => r.url.endsWith('/days'))!;
    expect(daysCall.params).toEqual({ serviceId: 3, from: '2026-10-01', to: '2026-10-31' });
    expect([...booking.availableDays.value]).toEqual(['2026-10-02', '2026-10-08']);
    expect(booking.bookingWindow.value?.bookableUntil).toBe('2026-11-12');
    expect(booking.employees.value[0].employeeName).toBe('Ana');
  });

  it('cambiar de servicio deja el día sin elegir', async () => {
    const booking = start(false);
    await chooseServiceAndDay(booking);

    booking.serviceId.value = 4;
    await flush();

    expect(booking.date.value).toBeNull();
    expect(booking.employees.value).toEqual([]);
  });

  it('la clienta sin cita activa crea una, sin customerId', async () => {
    const booking = start(false, {
      'GET /api/v1/appointments': () => ok({ items: [] }),
      'POST /api/v1/appointments': () => [
        201,
        { success: true, data: detail, error: null, meta: {} },
      ],
    });
    await chooseServiceAndDay(booking);

    const outcome = await booking.book({ employeeId: 2, startTime: '10:00:00' });

    expect(outcome).toEqual({ kind: 'booked', appointment: detail, updated: false });
    const post = seen.find((r) => r.method === 'POST')!;
    expect(post.body).toEqual({
      employeeId: 2,
      appointmentDate: '2026-10-08',
      startTime: '10:00',
      items: [{ serviceId: 3 }],
    });
  });

  it('la clienta con cita activa la modifica', async () => {
    const booking = start(false, {
      'GET /api/v1/appointments': () => ok({ items: [active] }),
      'PUT /api/v1/appointments/5': () => ok(detail),
    });
    await chooseServiceAndDay(booking);

    const outcome = await booking.book({ employeeId: 2, startTime: '10:00:00' });

    expect(outcome).toMatchObject({ kind: 'booked', updated: true });
    expect(seen.some((r) => r.method === 'POST')).toBe(false);
  });

  it('el personal sin clienta elegida no reserva', async () => {
    const booking = start(true);
    await chooseServiceAndDay(booking);

    expect(await booking.book({ employeeId: 2, startTime: '10:00:00' })).toMatchObject({
      kind: 'error',
      code: 'NO_CUSTOMER',
    });
  });

  it('el personal pregunta si la clienta ya tiene cita, y luego modifica o crea según elija', async () => {
    const booking = start(true, {
      'GET /api/v1/appointments': () => ok({ items: [active] }),
      'PUT /api/v1/appointments/5': () => ok(detail),
      'POST /api/v1/appointments': () => [
        201,
        { success: true, data: detail, error: null, meta: {} },
      ],
    });
    booking.customer.value = { id: 7, firstName: 'Laura', lastName: 'G', fullName: 'Laura G' };
    await chooseServiceAndDay(booking);

    const first = await booking.book({ employeeId: 2, startTime: '10:00:00' });
    expect(first).toEqual({ kind: 'choose', active });
    expect(
      seen.find((r) => r.url === '/api/v1/appointments' && r.method === 'GET')!.params
    ).toMatchObject({
      customerId: 7,
      from: '2026-10-01',
    });

    expect(await booking.book({ employeeId: 2, startTime: '10:00:00' }, 'update')).toMatchObject({
      updated: true,
    });
    expect(await booking.book({ employeeId: 2, startTime: '10:00:00' }, 'create')).toMatchObject({
      updated: false,
    });
    expect(seen.find((r) => r.method === 'POST')!.body).toMatchObject({ customerId: 7 });
  });

  it('si el hueco se ha ocupado devuelve el error y recarga los huecos', async () => {
    const booking = start(false, {
      'GET /api/v1/appointments': () => ok({ items: [] }),
      'POST /api/v1/appointments': () => fail(409, 'APT_SLOT_UNAVAILABLE'),
    });
    await chooseServiceAndDay(booking);
    const slotCalls = () => seen.filter((r) => r.url.endsWith('/by-service')).length;
    const before = slotCalls();

    const outcome = await booking.book({ employeeId: 2, startTime: '10:00:00' });
    await flush();

    expect(outcome).toMatchObject({ kind: 'error', code: 'APT_SLOT_UNAVAILABLE' });
    expect(slotCalls()).toBe(before + 1);
    expect(booking.saving.value).toBe(false);
  });
});
