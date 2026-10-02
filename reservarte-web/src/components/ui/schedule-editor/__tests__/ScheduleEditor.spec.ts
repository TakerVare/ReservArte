import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import { toWeek, type ScheduleDay } from '@features/employees/utils/schedule';
import ScheduleEditor from '../ScheduleEditor.vue';

function mountEditor(days: ScheduleDay[]) {
  const wrapper = mount(ScheduleEditor, {
    props: {
      modelValue: days,
      'onUpdate:modelValue': (value: ScheduleDay[]) => wrapper.setProps({ modelValue: value }),
    },
    global: { plugins: [i18n] },
  });
  return wrapper;
}

const day = (wrapper: ReturnType<typeof mountEditor>, n: number) =>
  wrapper.get(`[data-testid="schedule-day-${n}"]`);
const button = (wrapper: ReturnType<typeof mountEditor>, text: string) =>
  wrapper.findAll('button').find((b) => b.text() === text)!;

describe('ScheduleEditor', () => {
  it('pinta los siete días con su nombre; los que no trabaja, como libres', () => {
    const wrapper = mountEditor(
      toWeek([{ dayOfWeek: 0, startTime: '10:00:00', endTime: '14:00:00' }])
    );
    expect(day(wrapper, 0).text()).toContain('Lunes');
    expect(
      day(wrapper, 0)
        .findAll<HTMLInputElement>('input[type="time"]')
        .map((i) => i.element.value)
    ).toEqual(['10:00', '14:00']);
    expect(day(wrapper, 6).text()).toContain('Domingo');
    expect(day(wrapper, 6).text()).toContain('Libre');
    expect(day(wrapper, 6).get('input[type="checkbox"]').attributes('aria-label')).toBe(
      'Trabaja el Domingo'
    );
  });

  it('marcar un día propone la mañana y «Añadir tramo» la tarde', async () => {
    const wrapper = mountEditor(toWeek([]));
    await day(wrapper, 1).get('input[type="checkbox"]').setValue(true);
    await button(wrapper, 'Añadir tramo').trigger('click');
    expect(wrapper.props('modelValue')[1]).toEqual({
      dayOfWeek: 1,
      works: true,
      ranges: [
        { start: '10:00', end: '14:00' },
        { start: '16:00', end: '20:00' },
      ],
    });
  });

  it('no emite «save» con errores y los enseña en su día', async () => {
    const wrapper = mountEditor(
      toWeek([{ dayOfWeek: 2, startTime: '14:00:00', endTime: '10:00:00' }])
    );
    await button(wrapper, 'Guardar horario').trigger('click');
    expect(wrapper.emitted('save')).toBeUndefined();
    expect(day(wrapper, 2).get('[role="alert"]').text()).toBe(
      'La hora de fin debe ser posterior a la de inicio.'
    );

    await day(wrapper, 2).findAll('input[type="time"]')[1]!.setValue('18:00');
    await button(wrapper, 'Guardar horario').trigger('click');
    expect(wrapper.emitted('save')).toHaveLength(1);
  });

  it('quitar el último tramo de un día marcado pide un tramo o desmarcarlo', async () => {
    const wrapper = mountEditor(
      toWeek([{ dayOfWeek: 0, startTime: '10:00:00', endTime: '14:00:00' }])
    );
    await day(wrapper, 0).get('button[aria-label="Quitar el tramo 1 del Lunes"]').trigger('click');
    await button(wrapper, 'Guardar horario').trigger('click');
    expect(day(wrapper, 0).get('[role="alert"]').text()).toBe(
      'Añade al menos un tramo o desmarca el día.'
    );
  });
});
