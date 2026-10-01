import { describe, expect, it } from 'vitest';
import { STATUS_BADGE, allowedTransitions, canModify } from '../appointment-status';
import type { AppointmentStatus } from '../../types/appointment.types';

describe('estados de cita en el listado del personal', () => {
  it('cada estado tiene su color: pendiente con borde, confirmada en rosa, canceladas en rojo', () => {
    expect(STATUS_BADGE.pending).toBe('outline');
    expect(STATUS_BADGE.confirmed).toBe('primary');
    expect(STATUS_BADGE.in_progress).toBe('default');
    expect(STATUS_BADGE.completed).toBe('muted');
    expect(STATUS_BADGE.cancelled_by_customer).toBe('destructive');
    expect(STATUS_BADGE.no_show).toBe('destructive');
  });

  it.each<[AppointmentStatus, string[]]>([
    ['pending', ['confirm']],
    ['confirmed', ['start']],
    ['in_progress', ['complete']],
    ['completed', []],
    ['cancelled', []],
    ['no_show', []],
  ])('una empleada sobre una cita %s puede: %j', (status, expected) => {
    expect(allowedTransitions(status, 'Employee')).toEqual(expected);
  });

  it.each(['Admin', 'Manager'] as const)(
    '%s además marca no presentada si aún no se ha cerrado',
    (role) => {
      expect(allowedTransitions('pending', role)).toEqual(['confirm', 'no-show']);
      expect(allowedTransitions('in_progress', role)).toEqual(['complete', 'no-show']);
      expect(allowedTransitions('completed', role)).toEqual([]);
    }
  );

  it('solo se modifica lo pendiente o confirmado', () => {
    expect(canModify('pending')).toBe(true);
    expect(canModify('confirmed')).toBe(true);
    expect(canModify('in_progress')).toBe(false);
    expect(canModify('cancelled')).toBe(false);
  });
});
