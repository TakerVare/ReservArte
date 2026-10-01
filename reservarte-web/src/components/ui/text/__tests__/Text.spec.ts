import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import Text from '../Text.vue';

describe('Text', () => {
  it('fija la altura de línea «normal» de Figma (el preflight de Tailwind pone 1,5)', () => {
    const wrapper = mount(Text, { slots: { default: 'Hola' } });
    expect(wrapper.classes()).toContain('leading-[normal]');
  });

  it('una clase de altura de línea del llamador sustituye a la de serie', () => {
    const wrapper = mount(Text, { props: { class: 'leading-none' }, slots: { default: 'Hola' } });
    expect(wrapper.classes()).toContain('leading-none');
    expect(wrapper.classes()).not.toContain('leading-[normal]');
  });

  it('la mantiene aunque llegue un tamaño de letra desde fuera', () => {
    const wrapper = mount(Text, {
      props: { size: 'big-message', class: 'text-[48px] md:text-[64px]' },
      slots: { default: 'Hola' },
    });
    expect(wrapper.classes()).toContain('leading-[normal]');
    expect(wrapper.classes()).toContain('text-[48px]');
  });

  it.each([
    ['h2', 'H2'],
    ['paragraph', 'P'],
  ] as const)('el tamaño %s usa la etiqueta %s', (size, tag) => {
    const wrapper = mount(Text, { props: { size }, slots: { default: 'Hola' } });
    expect(wrapper.element.tagName).toBe(tag);
  });
});
