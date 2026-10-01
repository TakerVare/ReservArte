import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { effectScope, nextTick } from 'vue';
import type { AxiosAdapter } from 'axios';
import apiClient from '@lib/api/client';
import { useAgenda } from '../useAgenda';

const originalAdapter = apiClient.defaults.adapter;
let queries: Record<string, unknown>[] = [];
let items: unknown[] = [];

const adapter: AxiosAdapter = async (config) => {
  queries.push(config.params as Record<string, unknown>);
  return {
    status: 200,
    statusText: '',
    headers: {},
    config,
    data: { success: true, data: { items }, error: null, meta: {} },
  };
};

function appointment(
  id: number,
  date: string,
  start: string,
  employeeId: number,
  employeeName: string
) {
  return {
    id,
    customerId: 1,
    employeeId,
    appointmentDate: date,
    startTime: start,
    endTime: start,
    status: 'confirmed',
    customerName: 'Laura',
    employeeName,
  };
}

// Jueves 1 de octubre de 2026.
const TODAY = new Date(2026, 9, 1, 12, 0);

const flush = async () => {
  for (let i = 0; i < 5; i++) {
    await nextTick();
    await Promise.resolve();
  }
};

let scope: ReturnType<typeof effectScope>;
const start = () => scope.run(() => useAgenda({ today: () => TODAY }))!;

beforeEach(() => {
  queries = [];
  items = [];
  scope = effectScope();
  apiClient.defaults.adapter = adapter;
});

afterEach(() => {
  scope.stop();
  apiClient.defaults.adapter = originalAdapter;
});

describe('useAgenda', () => {
  it('empieza en el día de hoy y lo pide a la API', async () => {
    const agenda = start();
    await flush();

    expect(agenda.title.value).toBe('Jueves, 1 de octubre');
    expect(queries[0]).toMatchObject({ from: '2026-10-01', to: '2026-10-01' });
  });

  it('la semana va de lunes a domingo y el mes de primero a último', async () => {
    const agenda = start();
    agenda.view.value = 'week';
    await flush();
    expect(agenda.range.value).toEqual({ from: '2026-09-28', to: '2026-10-04' });
    expect(agenda.title.value).toBe('28 sep – 4 oct 2026');

    agenda.view.value = 'month';
    await flush();
    expect(agenda.range.value).toEqual({ from: '2026-10-01', to: '2026-10-31' });
    expect(agenda.title.value).toBe('Octubre de 2026');
    expect(queries.at(-1)).toMatchObject({ from: '2026-10-01', to: '2026-10-31' });
  });

  it('anterior y siguiente mueven un bloque de la vista, y hoy vuelve', async () => {
    const agenda = start();
    agenda.next();
    expect(agenda.range.value.from).toBe('2026-10-02');

    agenda.view.value = 'week';
    agenda.previous();
    expect(agenda.range.value).toEqual({ from: '2026-09-21', to: '2026-09-27' });

    agenda.view.value = 'month';
    agenda.next();
    expect(agenda.range.value.from).toBe('2026-10-01');

    agenda.goToday();
    agenda.view.value = 'day';
    expect(agenda.range.value.from).toBe('2026-10-01');
  });

  it('agrupa por fecha ordenando por hora y filtra por empleada', async () => {
    items = [
      appointment(3, '2026-10-02', '12:00:00', 2, 'Lucía'),
      appointment(1, '2026-10-01', '11:00:00', 1, 'Ana'),
      appointment(2, '2026-10-01', '09:00:00', 2, 'Lucía'),
    ];
    const agenda = start();
    agenda.view.value = 'week';
    await flush();

    expect(agenda.groups.value.map((g) => [g.label, g.items.map((a) => a.id)])).toEqual([
      ['Jueves, 1 de octubre', [2, 1]],
      ['Viernes, 2 de octubre', [3]],
    ]);
    expect(agenda.employees.value).toEqual([
      { id: 1, name: 'Ana' },
      { id: 2, name: 'Lucía' },
    ]);

    agenda.employeeId.value = 1;
    expect(agenda.groups.value.map((g) => g.items.map((a) => a.id))).toEqual([[1]]);
  });
});
