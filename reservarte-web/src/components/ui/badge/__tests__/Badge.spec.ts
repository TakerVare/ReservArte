import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import Badge from '../Badge.vue';

describe('Badge', () => {
  it.each([
    ['default', 'bg-accent'],
    ['primary', 'bg-primary'],
    ['outline', 'bg-transparent'],
    ['muted', 'bg-muted'],
    ['destructive', 'bg-destructive'],
  ] as const)('la variante %s usa %s', (variant, expected) => {
    const wrapper = mount(Badge, { props: { variant }, slots: { default: 'Confirmada' } });
    expect(wrapper.classes()).toContain(expected);
    expect(wrapper.text()).toBe('Confirmada');
  });

  it('una clase del llamador sustituye a la de serie', () => {
    const wrapper = mount(Badge, { props: { class: 'px-4' } });
    expect(wrapper.classes()).toContain('px-4');
    expect(wrapper.classes()).not.toContain('px-2');
  });
});
