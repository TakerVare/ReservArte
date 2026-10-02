import { afterEach, describe, expect, it } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { i18n } from '@/i18n';
import AbsenceDialog from '../AbsenceDialog.vue';

let wrapper: VueWrapper | undefined;

async function open() {
  wrapper = mount(AbsenceDialog, {
    props: { open: false, 'onUpdate:open': (v: boolean) => wrapper!.setProps({ open: v }) },
    global: { plugins: [i18n] },
    attachTo: document.body,
  });
  await wrapper.setProps({ open: true });
  await flushPromises();
}

const input = (id: string) => document.getElementById(id) as HTMLInputElement;
async function type(id: string, value: string) {
  input(id).value = value;
  input(id).dispatchEvent(new Event('input'));
  await flushPromises();
}
async function submit() {
  document.getElementById('absence-form')!.dispatchEvent(new Event('submit'));
  await flushPromises();
}

afterEach(() => {
  wrapper?.unmount();
  document.body.innerHTML = '';
});

describe('AbsenceDialog', () => {
  it('sin fechas avisa y no emite', async () => {
    await open();
    await submit();
    expect(document.querySelector('[role="alert"]')?.textContent).toBe('Indica las fechas.');
    expect(wrapper!.emitted('confirm')).toBeUndefined();
  });

  it('el último día propone el primero y emite días completos en UTC', async () => {
    await open();
    await type('absence-from', '2026-11-02');
    expect(input('absence-to').value).toBe('2026-11-02');
    await type('absence-reason', '  Médico  ');
    await submit();
    expect(wrapper!.emitted('confirm')![0]).toEqual([
      {
        startDateTime: '2026-11-01T23:00:00.000Z',
        endDateTime: '2026-11-02T23:00:00.000Z',
        type: 'vacation',
        reason: 'Médico',
      },
    ]);
  });

  it('con horas, el fin debe ser posterior al inicio', async () => {
    await open();
    await type('absence-from', '2026-11-02');
    const allDay = document.querySelector('input[type="checkbox"]') as HTMLInputElement;
    allDay.checked = false;
    allDay.dispatchEvent(new Event('change'));
    await flushPromises();
    await type('absence-start', '14:00');
    await type('absence-end', '10:00');
    await submit();
    expect(document.querySelector('[role="alert"]')?.textContent).toBe(
      'El fin debe ser posterior al inicio.'
    );
    expect(wrapper!.emitted('confirm')).toBeUndefined();
  });
});
