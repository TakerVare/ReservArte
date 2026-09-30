import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import Button from '../Button.vue';

/** Capa de componente: montaje con @vue/test-utils y comportamiento del botón base. */
describe('Button', () => {
  it('renderiza el contenido y es type="button" por defecto (no envía formularios)', () => {
    const wrapper = mount(Button, { slots: { default: 'Guardar' } });

    expect(wrapper.text()).toBe('Guardar');
    expect(wrapper.attributes('type')).toBe('button');
  });

  it('respeta type="submit"', () => {
    const wrapper = mount(Button, { props: { type: 'submit' } });

    expect(wrapper.attributes('type')).toBe('submit');
  });

  it('usa tokens de tema, no colores literales', () => {
    const primary = mount(Button).classes();
    const secondary = mount(Button, { props: { variant: 'secondary' } }).classes();

    expect(primary).toContain('bg-primary');
    expect(secondary).toContain('text-primary');
  });

  it('emite click', async () => {
    const wrapper = mount(Button);

    await wrapper.trigger('click');

    expect(wrapper.emitted('click')).toHaveLength(1);
  });
});
