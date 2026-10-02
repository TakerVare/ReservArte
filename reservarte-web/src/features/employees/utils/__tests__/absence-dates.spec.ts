import { describe, expect, it } from 'vitest';
import { absenceToUtc, centerToUtc, describeAbsence, utcToCenter } from '../absence-dates';

describe('absence-dates (hora del centro, Europe/Madrid)', () => {
  it('pasa la hora del centro a UTC según la época del año', () => {
    expect(centerToUtc('2026-11-02T09:00')).toBe('2026-11-02T08:00:00.000Z'); // invierno, +1
    expect(centerToUtc('2027-07-01T09:00')).toBe('2027-07-01T07:00:00.000Z'); // verano, +2
  });

  it('una ausencia de días completos va de las 00:00 del primero a las 00:00 del siguiente al último', () => {
    expect(absenceToUtc({ from: '2026-11-02', to: '2026-11-06', allDay: true })).toEqual({
      startDateTime: '2026-11-01T23:00:00.000Z',
      endDateTime: '2026-11-06T23:00:00.000Z',
    });
    // Cruza el cambio de hora del 28 de marzo de 2027: cada extremo con su desplazamiento.
    expect(absenceToUtc({ from: '2027-03-26', to: '2027-03-29', allDay: true })).toEqual({
      startDateTime: '2027-03-25T23:00:00.000Z',
      endDateTime: '2027-03-29T22:00:00.000Z',
    });
  });

  it('una ausencia con horas usa las horas de inicio y fin', () => {
    expect(
      absenceToUtc({
        from: '2026-11-02',
        to: '2026-11-02',
        allDay: false,
        startTime: '10:00',
        endTime: '14:00',
      })
    ).toEqual({
      startDateTime: '2026-11-02T09:00:00.000Z',
      endDateTime: '2026-11-02T13:00:00.000Z',
    });
  });

  it('utcToCenter devuelve el día y la hora del centro', () => {
    expect(utcToCenter('2026-11-01T23:00:00Z')).toEqual({ date: '2026-11-02', time: '00:00' });
  });

  it('describeAbsence reconoce días completos y da el último día incluido', () => {
    expect(
      describeAbsence({
        startDateTime: '2026-11-01T23:00:00Z',
        endDateTime: '2026-11-06T23:00:00Z',
      })
    ).toEqual({
      allDay: true,
      singleDay: false,
      fromDay: '2 nov 2026',
      toDay: '6 nov 2026',
      startTime: '00:00',
      endTime: '00:00',
    });
    expect(
      describeAbsence({
        startDateTime: '2026-11-01T23:00:00Z',
        endDateTime: '2026-11-02T23:00:00Z',
      })
    ).toMatchObject({ allDay: true, singleDay: true, toDay: '2 nov 2026' });
    expect(
      describeAbsence({
        startDateTime: '2027-03-30T14:00:00Z',
        endDateTime: '2027-03-30T18:00:00Z',
      })
    ).toMatchObject({ allDay: false, singleDay: true, startTime: '16:00', endTime: '20:00' });
  });
});
