import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import ListItem from '../ListItem.vue';

const mountItem = (props: Record<string, unknown> = {}) =>
  mount(ListItem, { props: { label: 'María García', ...props }, global: { plugins: [i18n] } });

describe('ListItem', () => {
  it('sin avatar ni detalle, solo el nombre y las tres acciones', () => {
    const wrapper = mountItem();
    expect(wrapper.find('[data-testid^="avatar"]').exists()).toBe(false);
    expect(wrapper.findAll('button').map((b) => b.attributes('aria-label'))).toEqual([
      'Editar María García',
      'Eliminar María García',
      'Ver María García',
    ]);
  });

  it('con avatar, foto a la izquierda; con detalle, segunda línea', () => {
    const wrapper = mountItem({ avatar: true, photoUrl: '/m.png', detail: 'Gerencia' });
    expect(wrapper.get('img').attributes('src')).toBe('/m.png');
    expect(wrapper.text()).toContain('Gerencia');
  });

  it('sin «deletable» no ofrece eliminar', () => {
    const wrapper = mountItem({ deletable: false });
    expect(wrapper.findAll('button')).toHaveLength(2);
    expect(wrapper.text()).not.toContain('Eliminar');
  });
});
