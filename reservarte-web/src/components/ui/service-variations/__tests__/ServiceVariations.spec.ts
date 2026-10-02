import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import ServiceVariations from '../ServiceVariations.vue';

const mountList = () =>
  mount(ServiceVariations, {
    props: {
      basePrice: 22,
      durationMinutes: 40,
      variations: [
        { id: 1, name: 'Con diseño', priceModifier: 5, durationModifier: 10 },
        { id: 2, name: 'Exprés', priceModifier: -2.5, durationModifier: -10 },
        { id: 3, name: 'Igual', priceModifier: 0, durationModifier: 0 },
      ],
    },
    global: { plugins: [i18n] },
  });

describe('ServiceVariations', () => {
  it('enseña cada ajuste con su signo y el total resultante', () => {
    const rows = mountList()
      .findAll('li')
      .map((li) => li.text().replace(/\s+/g, ' '));
    expect(rows[0]).toContain('+5,00 € · +10 min');
    expect(rows[0]).toContain('Total: 27,00 € · 50 min');
    expect(rows[1]).toContain('-2,50 € · -10 min');
    expect(rows[1]).toContain('Total: 19,50 € · 30 min');
    // Sin cambio, sin signo.
    expect(rows[2]).toContain('0,00 € · 0 min');
  });

  it('emite la baja de la variación pulsada', async () => {
    const wrapper = mountList();
    await wrapper.get('button[aria-label="Quitar la variación: Exprés"]').trigger('click');
    expect(wrapper.emitted('remove')![0]).toEqual([
      { id: 2, name: 'Exprés', priceModifier: -2.5, durationModifier: -10 },
    ]);
  });
});
