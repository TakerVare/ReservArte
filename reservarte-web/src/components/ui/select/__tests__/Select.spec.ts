import { afterEach, describe, expect, it } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { i18n } from '@/i18n';
import Select from '../Select.vue';

const options = [
  { value: 'cejas', label: 'Diseño de cejas' },
  { value: 'lifting', label: 'Lifting de pestañas' },
  { value: 'tinte', label: 'Tinte', disabled: true },
];

let wrapper: VueWrapper | undefined;

function mountSelect(props: Record<string, unknown> = {}) {
  wrapper = mount(Select, {
    props: {
      options,
      id: 'servicio',
      'onUpdate:modelValue': (v: string | undefined) => wrapper!.setProps({ modelValue: v }),
      ...props,
    },
    global: { plugins: [i18n] },
    attachTo: document.body,
  });
  return wrapper;
}

afterEach(() => {
  wrapper?.unmount();
  document.body.innerHTML = '';
});

describe('Select', () => {
  it('sin valor muestra el texto de ayuda por defecto en un combobox con id', () => {
    const w = mountSelect();
    const trigger = w.get('[role="combobox"]');
    expect(trigger.attributes('id')).toBe('servicio');
    expect(trigger.text()).toContain('Selecciona una opción');
  });

  it('con valor muestra su etiqueta', async () => {
    const w = mountSelect({ modelValue: 'lifting' });
    await flushPromises();
    expect(w.get('[role="combobox"]').text()).toContain('Lifting de pestañas');
  });

  it('al abrirlo lista las opciones y al elegir una emite su valor', async () => {
    const w = mountSelect();
    await w.get('[role="combobox"]').trigger('keydown', { key: 'Enter' });
    await flushPromises();

    const items = [...document.querySelectorAll('[role="option"]')];
    expect(items.map((i) => i.textContent?.trim())).toEqual([
      'Diseño de cejas',
      'Lifting de pestañas',
      'Tinte',
    ]);
    expect(items[2].getAttribute('aria-disabled')).toBe('true');

    (items[0] as HTMLElement).dispatchEvent(new PointerEvent('pointerup', { bubbles: true }));
    await flushPromises();

    expect(w.emitted('update:modelValue')?.at(-1)).toEqual(['cejas']);
  });

  it('con invalid marca aria-invalid', () => {
    const w = mountSelect({ invalid: true });
    expect(w.get('[role="combobox"]').attributes('aria-invalid')).toBe('true');
  });
});
