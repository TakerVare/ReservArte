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

/** Respuesta de la cita tras crearla o modificarla (AppointmentDetailDto), con lo que usa la SPA. */
export interface AppointmentDetail extends AppointmentSummary {
  totalPrice: number;
}

/** Cuerpo de alta y modificación de cita. */
export interface BookingRequest {
  /** Solo el personal; la clienta reserva para sí misma (H-44). */
  customerId?: number;
  employeeId: number;
  appointmentDate: string;
  startTime: string;
  items: { serviceId: number }[];
}

/** Ventana de reserva de quien consulta (H-44), fechas `yyyy-MM-dd`. */
export interface BookingWindow {
  bookableFrom: string;
  bookableUntil: string;
}

/** GET /appointments/availability/days. */
export interface AvailableDays extends BookingWindow {
  days: string[];
}

export interface TimeSlot {
  startTime: string;
  endTime: string;
}

/** GET /appointments/availability/by-service. */
export interface ServiceSlots extends BookingWindow {
  durationMinutes: number;
  employees: { employeeId: number; employeeName: string; slots: TimeSlot[] }[];
}
