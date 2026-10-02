/** Roles que admite una ficha de empleado (`Roles.AssignableToEmployee`). */
export const EMPLOYEE_ROLES = ['Admin', 'Manager', 'Employee'] as const;
export type EmployeeRole = (typeof EMPLOYEE_ROLES)[number];

/** Tipos de ausencia (`EmployeeExceptionTypes`). */
export const ABSENCE_TYPES = ['vacation', 'sick_leave', 'personal', 'training', 'other'] as const;
export type AbsenceType = (typeof ABSENCE_TYPES)[number];

/** Espejo de `EmployeeDto`. */
export interface Employee {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone?: string | null;
  rol: EmployeeRole;
  profileImageUrl?: string | null;
  /** `yyyy-MM-dd`. */
  hireDate?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

/** Cuerpo del alta y de la edición (`CreateEmployeeRequest`/`UpdateEmployeeRequest`). */
export interface EmployeeInput {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string | null;
  rol: EmployeeRole;
  profileImageUrl?: string | null;
  hireDate?: string | null;
}

/** Tramo del horario semanal (`EmployeeAvailabilityDto`). 0 = lunes … 6 = domingo. */
export interface ScheduleSlot {
  dayOfWeek: number;
  /** `HH:mm` o `HH:mm:ss`, hora local del centro. */
  startTime: string;
  endTime: string;
}

/** Ausencia (`EmployeeExceptionDto`). Fechas en UTC (ISO con zona). */
export interface Absence {
  id: number;
  startDateTime: string;
  endDateTime: string;
  type: AbsenceType;
  reason?: string | null;
  isActive: boolean;
}

export interface AbsenceInput {
  startDateTime: string;
  endDateTime: string;
  type: AbsenceType;
  reason?: string | null;
}

/** `EmployeeAvailabilityResponse`. */
export interface EmployeeAvailability {
  employeeId: number;
  weeklySchedule: (ScheduleSlot & { id: number; isRecurring: boolean; isActive: boolean })[];
  exceptions: Absence[];
  exceptionsFrom: string;
  exceptionsTo: string;
}

/** Servicio que presta un empleado (`EmployeeServiceDto`). */
export interface EmployeeServiceItem {
  serviceId: number;
  name: string;
  durationMinutes: number;
  proficiencyLevel: number;
  /** El servicio sigue en el catálogo; si no, no cuenta para la reserva. */
  serviceIsActive: boolean;
}
