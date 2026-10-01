import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import Input from '../Input.vue';

describe('Input', () => {
  it('enlaza el valor con v-model', async () => {
    const wrapper = mount(Input, {
      props: {
        modelValue: 'Ana',
        'onUpdate:modelValue': (v: unknown) => wrapper.setProps({ modelValue: v as string }),
      },
    });
    expect((wrapper.element as HTMLInputElement).value).toBe('Ana');

    await wrapper.setValue('Laura');
    expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual(['Laura']);
  });

  it('pasa los atributos nativos al input', () => {
    const wrapper = mount(Input, {
      attrs: { id: 'email', type: 'email', placeholder: 'tu@correo' },
    });
    expect(wrapper.attributes()).toMatchObject({
      id: 'email',
      type: 'email',
      placeholder: 'tu@correo',
    });
  });

  it('con invalid marca aria-invalid y pinta el borde de error', () => {
    const wrapper = mount(Input, { props: { invalid: true } });
    expect(wrapper.attributes('aria-invalid')).toBe('true');
    expect(wrapper.classes()).toContain('border-destructive');
    expect(wrapper.classes()).not.toContain('border-input');
  });

  it('sin invalid no lleva aria-invalid', () => {
    const wrapper = mount(Input);
    expect(wrapper.attributes('aria-invalid')).toBeUndefined();
    expect(wrapper.classes()).toContain('border-input');
  });
});
