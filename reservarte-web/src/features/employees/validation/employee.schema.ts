import { z } from 'zod';
import { EMPLOYEE_ROLES } from '../types/employee.types';

/** Hoy en `yyyy-MM-dd`, en la hora del navegador. */
function today(): string {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

/**
 * Ficha de empleado (RA-869d7fbyt). Replica `Create/UpdateEmployeeRequestValidator`
 * del backend: si allí cambia una regla, aquí también.
 */
export const employeeSchema = z.object({
  firstName: z
    .string()
    .trim()
    .min(1, 'El nombre es obligatorio.')
    .max(100, 'El nombre no puede superar los 100 caracteres.'),
  lastName: z
    .string()
    .trim()
    .min(1, 'Los apellidos son obligatorios.')
    .max(100, 'Los apellidos no pueden superar los 100 caracteres.'),
  email: z
    .string()
    .trim()
    .min(1, 'El email es obligatorio.')
    .email('El email no tiene un formato válido.')
    .max(255, 'El email no puede superar los 255 caracteres.'),
  phone: z
    .string()
    .trim()
    .max(20, 'El teléfono no puede superar los 20 caracteres.')
    .regex(/^[+0-9\s().-]*$/, 'El teléfono solo puede contener dígitos y los signos + ( ) . -')
    .optional()
    .or(z.literal('')),
  rol: z.enum(EMPLOYEE_ROLES, { errorMap: () => ({ message: 'El rol es obligatorio.' }) }),
  hireDate: z
    .string()
    .refine((value) => !value || value <= today(), 'La fecha de alta no puede ser futura.')
    .optional()
    .or(z.literal('')),
  isActive: z.boolean(),
});

export type EmployeeFormValues = z.infer<typeof employeeSchema>;
