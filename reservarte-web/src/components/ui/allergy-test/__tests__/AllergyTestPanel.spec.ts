import { afterEach, describe, expect, it } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { i18n } from '@/i18n';
import AllergyTestPanel from '../AllergyTestPanel.vue';

let wrapper: VueWrapper | undefined;

// 2 de octubre de 2026, 10:27 en Madrid (08:27 UTC).
const NOW = new Date('2026-10-02T08:27:00Z');

async function mountOpen(lastTestAt: string | null = null, timeZone = 'Europe/Madrid') {
  wrapper = mount(AllergyTestPanel, {
    props: {
      lastTestAt,
      timeZone,
      now: () => NOW,
      open: false,
      'onUpdate:open': (v: boolean) => wrapper!.setProps({ open: v }),
    },
    global: { plugins: [i18n] },
    attachTo: document.body,
  });
  await wrapper.setProps({ open: true });
  await flushPromises();
  return wrapper;
}

const input = (id: string) => document.getElementById(id) as HTMLInputElement;
async function type(id: string, value: string) {
  input(id).value = value;
  input(id).dispatchEvent(new Event('input'));
  await flushPromises();
}
async function submit() {
  document.getElementById('allergy-form')!.dispatchEvent(new Event('submit'));
  await flushPromises();
}

afterEach(() => {
  wrapper?.unmount();
  document.body.innerHTML = '';
});

describe('AllergyTestPanel', () => {
  it('enseña la última prueba en hora del centro', async () => {
    const w = await mountOpen('2026-09-15T15:30:00Z');
    expect(w.get('[data-testid="allergy-last"]').text()).toBe('Última prueba: 15/09/2026 17:30');
  });

  it('la hora del centro es la de su zona: en Canarias, una hora menos', async () => {
    const w = await mountOpen('2026-09-15T15:30:00Z', 'Atlantic/Canary');
    expect(w.get('[data-testid="allergy-last"]').text()).toBe('Última prueba: 15/09/2026 16:30');
    expect(input('allergy-time').value).toBe('09:27');
    await submit();
    expect(w.emitted('record')![0]).toEqual(['2026-10-02T08:27:00.000Z']);
  });

  it('propone ahora en hora del centro y emite el instante en UTC', async () => {
    const w = await mountOpen();
    expect(input('allergy-date').value).toBe('2026-10-02');
    expect(input('allergy-time').value).toBe('10:27');
    await submit();
    expect(w.emitted('record')![0]).toEqual(['2026-10-02T08:27:00.000Z']);
  });

  it('no deja registrar una prueba futura', async () => {
    const w = await mountOpen();
    await type('allergy-time', '10:30');
    await submit();
    expect(document.querySelector('[role="alert"]')?.textContent).toBe(
      'La prueba no puede ser futura.'
    );
    expect(w.emitted('record')).toBeUndefined();
  });
});
