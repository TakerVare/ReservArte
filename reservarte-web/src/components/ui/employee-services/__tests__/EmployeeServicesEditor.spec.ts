import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import type { ServiceOption } from '@features/services/api/services.api';
import EmployeeServicesEditor from '../EmployeeServicesEditor.vue';

const services: ServiceOption[] = [
  { id: 10, name: 'Henna de cejas', durationMinutes: 40, basePrice: 22, categoryName: 'Cejas' },
  { id: 11, name: 'Lifting', durationMinutes: 60, basePrice: 45, categoryName: null },
  { id: 12, name: 'Diseño de cejas', durationMinutes: 30, basePrice: 15, categoryName: 'Cejas' },
];

function mountEditor(selected: number[]) {
  const wrapper = mount(EmployeeServicesEditor, {
    props: {
      services,
      modelValue: selected,
      'onUpdate:modelValue': (value: number[]) => wrapper.setProps({ modelValue: value }),
    },
    global: { plugins: [i18n] },
  });
  return wrapper;
}

const button = (wrapper: ReturnType<typeof mountEditor>, text: string) =>
  wrapper.findAll('button').find((b) => b.text() === text)!;

describe('EmployeeServicesEditor', () => {
  it('agrupa por categoría, ordena por nombre y deja los sin categoría aparte', () => {
    const wrapper = mountEditor([]);
    const groups = wrapper.findAll('fieldset');
    expect(groups.map((g) => g.get('legend').text())).toEqual(['Cejas', 'Sin categoría']);
    expect(groups[0]!.findAll('label').map((l) => l.text())).toEqual([
      'Diseño de cejas30 min',
      'Henna de cejas40 min',
    ]);
  });

  it('marca y desmarca, y cuenta solo lo que está en el catálogo', async () => {
    // El 99 es un servicio ya retirado: no se cuenta ni se vuelve a mandar.
    const wrapper = mountEditor([11, 99]);
    expect(wrapper.get('[data-testid="services-count"]').text()).toBe('1 de 3 servicios marcados');

    const henna = wrapper.findAll('label').find((l) => l.text().startsWith('Henna'))!;
    await henna.get('input').setValue(true);
    await button(wrapper, 'Guardar servicios').trigger('click');
    expect(wrapper.emitted('save')![0]).toEqual([[11, 10]]);

    await button(wrapper, 'Desmarcar todos').trigger('click');
    await button(wrapper, 'Guardar servicios').trigger('click');
    expect(wrapper.emitted('save')![1]).toEqual([[]]);
  });

  it('sin catálogo lo dice y no ofrece guardar', () => {
    const wrapper = mount(EmployeeServicesEditor, {
      props: { services: [], modelValue: [] },
      global: { plugins: [i18n] },
    });
    expect(wrapper.text()).toContain('El catálogo no tiene servicios activos.');
    expect(wrapper.findAll('button')).toHaveLength(0);
  });
});
