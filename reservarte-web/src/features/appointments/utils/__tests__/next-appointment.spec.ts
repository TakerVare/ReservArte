import { describe, expect, it } from 'vitest';
import { pickNextAppointment } from '../next-appointment';
import type { AppointmentStatus, AppointmentSummary } from '../../types/appointment.types';

function appointment(
  id: number,
  appointmentDate: string,
  startTime: string,
  status: AppointmentStatus = 'confirmed'
): AppointmentSummary {
  return {
    id,
    customerId: 1,
    employeeId: 1,
    appointmentDate,
    startTime,
    endTime: '23:59:00',
    status,
    customerName: 'Clienta',
    employeeName: 'Empleada',
  };
}

// 1 de octubre de 2026, 12:00, hora local.
const now = new Date(2026, 9, 1, 12, 0);

describe('pickNextAppointment', () => {
  it('sin citas devuelve null', () => {
    expect(pickNextAppointment([], now)).toBeNull();
  });

  it('elige la más cercana aunque la API las dé de la más reciente a la más antigua', () => {
    const items = [
      appointment(3, '2026-12-24', '10:00:00'),
      appointment(2, '2026-10-02', '09:00:00'),
      appointment(1, '2026-10-01', '17:30:00'),
    ];
    expect(pickNextAppointment(items, now)?.id).toBe(1);
  });

  it('descarta las de hoy que ya han empezado', () => {
    const items = [
      appointment(1, '2026-10-01', '11:59:00'),
      appointment(2, '2026-10-01', '12:00:00'),
    ];
    expect(pickNextAppointment(items, now)?.id).toBe(2);
  });

  it.each<AppointmentStatus>([
    'in_progress',
    'completed',
    'cancelled',
    'cancelled_by_customer',
    'cancelled_by_business',
    'no_show',
  ])('ignora las citas en estado %s', (status) => {
    expect(pickNextAppointment([appointment(1, '2026-10-05', '10:00:00', status)], now)).toBeNull();
  });

  it('cuenta las pendientes como próximas', () => {
    const items = [appointment(1, '2026-10-05', '10:00:00', 'pending')];
    expect(pickNextAppointment(items, now)?.id).toBe(1);
  });
});
