import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import CustomerConsents from '../CustomerConsents.vue';

const consents = [
  { consentType: 'data_processing' as const, isGranted: true, grantedAt: '2026-09-29T10:00:00Z' },
  { consentType: 'marketing' as const, isGranted: false, revokedAt: '2026-09-30T10:00:00Z' },
];

const mountConsents = () =>
  mount(CustomerConsents, { props: { consents }, global: { plugins: [i18n] } });

describe('CustomerConsents', () => {
  it('pinta las cuatro finalidades del piloto con su estado, también las que no tienen registro', () => {
    const rows = mountConsents().findAll('li');
    expect(rows).toHaveLength(4);
    expect(rows[0]!.text()).toContain('Aceptado el 29/09/2026');
    expect(rows[1]!.text()).toContain('Retirado el 30/09/2026');
    expect(rows[2]!.text()).toContain('No aceptado');
    expect(rows.map((r) => r.get('button').text())).toEqual(['Retirar', 'Dar', 'Dar', 'Dar']);
  });

  it('emite el cambio contrario al estado actual', async () => {
    const wrapper = mountConsents();
    const buttons = wrapper.findAll('button');
    await buttons[0]!.trigger('click');
    await buttons[1]!.trigger('click');
    expect(wrapper.emitted('change')).toEqual([
      ['data_processing', false],
      ['marketing', true],
    ]);
  });
});
