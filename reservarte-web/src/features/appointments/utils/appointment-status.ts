import type { BadgeVariant } from '@components/ui/badge';
import type { UserRole } from '@features/auth/types/auth.types';
import type { AppointmentStatus, AppointmentTransition } from '../types/appointment.types';

/**
 * Color de cada estado de cita (RA-869fajn7g, recoge 3.15), solo con tokens:
 * pendiente con borde, confirmada en rosa, en curso en rosa claro, completada en
 * gris y canceladas y no presentada en rojo.
 */
export const STATUS_BADGE: Record<AppointmentStatus, BadgeVariant> = {
  pending: 'outline',
  confirmed: 'primary',
  in_progress: 'default',
  completed: 'muted',
  cancelled: 'destructive',
  cancelled_by_customer: 'destructive',
  cancelled_by_business: 'destructive',
  no_show: 'destructive',
};

const MANAGEMENT: readonly UserRole[] = ['Admin', 'Manager'];

/**
 * Acciones que admite la máquina de estados del backend desde cada estado y para
 * cada rol: confirmar (pendiente), iniciar (confirmada), completar (en curso) y no
 * presentada (pendiente, confirmada o en curso; solo Admin y Manager).
 */
export function allowedTransitions(
  status: AppointmentStatus,
  role: UserRole | undefined
): AppointmentTransition[] {
  const actions: AppointmentTransition[] = [];
  if (status === 'pending') actions.push('confirm');
  if (status === 'confirmed') actions.push('start');
  if (status === 'in_progress') actions.push('complete');
  if (
    ['pending', 'confirmed', 'in_progress'].includes(status) &&
    role &&
    MANAGEMENT.includes(role)
  ) {
    actions.push('no-show');
  }
  return actions;
}

/** Solo se modifica lo que aún no ha empezado (regla del backend). */
export function canModify(status: AppointmentStatus): boolean {
  return status === 'pending' || status === 'confirmed';
}

/** Se cancela lo que aún ocupa agenda: pendiente, confirmada o en curso (backend). */
export function canCancel(status: AppointmentStatus): boolean {
  return status === 'pending' || status === 'confirmed' || status === 'in_progress';
}
