/**
 * Estados de cita, espejo de `AppointmentStatuses`
 * (ReservArte-Domain/Entities/Appointment.cs). Los 8 del backend, en snake_case.
 */
export type AppointmentStatus =
  | 'pending'
  | 'confirmed'
  | 'in_progress'
  | 'completed'
  | 'cancelled'
  | 'cancelled_by_customer'
  | 'cancelled_by_business'
  | 'no_show';

/**
 * Espejo de AppointmentSummaryDto (ReservArte-Application/DTOs/Appointments),
 * con los campos que usa la SPA. `appointmentDate` (`yyyy-MM-dd`) y `startTime`
 * (`HH:mm:ss`) son la fecha y la hora del centro, sin zona.
 */
export interface AppointmentSummary {
  id: number;
  customerId: number;
  employeeId: number;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: AppointmentStatus;
  customerName: string;
  employeeName: string;
}
