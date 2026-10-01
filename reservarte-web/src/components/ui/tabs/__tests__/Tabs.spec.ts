import { describe, expect, it } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import Tabs from '../Tabs.vue';

const tabs = [
  { value: 'proximas', label: 'Próximas' },
  { value: 'pasadas', label: 'Pasadas' },
];

function mountTabs() {
  return mount(Tabs, {
    props: { tabs, label: 'Citas' },
    slots: { proximas: '<p>Lista de próximas</p>', pasadas: '<p>Lista de pasadas</p>' },
    attachTo: document.body,
  });
}

describe('Tabs', () => {
  it('es una lista de pestañas con nombre y abre la primera', async () => {
    const wrapper = mountTabs();
    await flushPromises();

    expect(wrapper.get('[role="tablist"]').attributes('aria-label')).toBe('Citas');
    const [first, second] = wrapper.findAll('[role="tab"]');
    expect(first.attributes('aria-selected')).toBe('true');
    expect(second.attributes('aria-selected')).toBe('false');
    expect(wrapper.text()).toContain('Lista de próximas');
    expect(wrapper.text()).not.toContain('Lista de pasadas');
    wrapper.unmount();
  });

  it('al elegir otra pestaña muestra su panel', async () => {
    const wrapper = mountTabs();
    await flushPromises();

    await wrapper.findAll('[role="tab"]')[1].trigger('mousedown', { button: 0 });
    await flushPromises();

    expect(wrapper.findAll('[role="tab"]')[1].attributes('aria-selected')).toBe('true');
    expect(wrapper.text()).toContain('Lista de pasadas');
    expect(wrapper.emitted('update:modelValue')?.at(-1)).toEqual(['pasadas']);
    wrapper.unmount();
  });
});
