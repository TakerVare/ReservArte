import { describe, expect, it } from 'vitest';
import { copyToWeekdays, fromWeek, nextRange, toWeek, validateDay } from '../schedule';

const slot = (dayOfWeek: number, startTime: string, endTime: string) => ({
  dayOfWeek,
  startTime,
  endTime,
});

describe('schedule', () => {
  it('toWeek da los siete días, de lunes (0) a domingo, con los tramos ordenados', () => {
    const week = toWeek([slot(0, '16:00:00', '20:00:00'), slot(0, '10:00:00', '14:00:00')]);
    expect(week.map((day) => day.dayOfWeek)).toEqual([0, 1, 2, 3, 4, 5, 6]);
    expect(week[0]).toEqual({
      dayOfWeek: 0,
      works: true,
      ranges: [
        { start: '10:00', end: '14:00' },
        { start: '16:00', end: '20:00' },
      ],
    });
    expect(week[1]).toEqual({ dayOfWeek: 1, works: false, ranges: [] });
  });

  it('fromWeek manda solo los días con jornada y las horas con segundos', () => {
    const week = toWeek([slot(0, '10:00:00', '14:00:00'), slot(2, '09:00:00', '15:00:00')]);
    week[2] = { ...week[2]!, works: false };
    expect(fromWeek(week)).toEqual([slot(0, '10:00:00', '14:00:00')]);
  });

  it('nextRange propone mañana, después tarde y después una hora a continuación', () => {
    expect(nextRange([])).toEqual({ start: '10:00', end: '14:00' });
    expect(nextRange([{ start: '10:00', end: '14:00' }])).toEqual({ start: '16:00', end: '20:00' });
    expect(nextRange([{ start: '16:00', end: '20:00' }])).toEqual({ start: '20:00', end: '21:00' });
    expect(nextRange([{ start: '22:00', end: '23:30' }])).toEqual({ start: '23:30', end: '23:59' });
  });

  it('validateDay aplica las reglas de la API', () => {
    const day = (ranges: { start: string; end: string }[], works = true) => ({
      dayOfWeek: 0,
      works,
      ranges,
    });
    expect(validateDay(day([], false))).toBeNull();
    expect(validateDay(day([]))).toBe('empty');
    expect(validateDay(day([{ start: '14:00', end: '14:00' }]))).toBe('order');
    expect(validateDay(day([{ start: '10:00', end: '' }]))).toBe('order');
    expect(
      validateDay(
        day([
          { start: '16:00', end: '20:00' },
          { start: '10:00', end: '16:30' },
        ])
      )
    ).toBe('overlap');
    // Tramos contiguos: no se solapan.
    expect(
      validateDay(
        day([
          { start: '10:00', end: '14:00' },
          { start: '14:00', end: '18:00' },
        ])
      )
    ).toBeNull();
  });

  it('copyToWeekdays copia el día de martes a viernes, sin tocar el fin de semana', () => {
    const week = toWeek([slot(0, '10:00:00', '14:00:00'), slot(5, '10:00:00', '13:00:00')]);
    const copied = copyToWeekdays(week, 0);
    expect(copied.slice(0, 5).every((day) => day.works && day.ranges[0]!.end === '14:00')).toBe(
      true
    );
    expect(copied[5]!.ranges).toEqual([{ start: '10:00', end: '13:00' }]);
    expect(copied[6]!.works).toBe(false);
    // Copia, no comparte: editar el martes no cambia el lunes.
    copied[1]!.ranges[0]!.end = '15:00';
    expect(copied[0]!.ranges[0]!.end).toBe('14:00');
  });
});
