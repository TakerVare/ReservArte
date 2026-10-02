import { z } from 'zod';
import { CONTACT_METHODS, CUSTOMER_CATEGORIES } from '../types/customer.types';

/** Hoy en `yyyy-MM-dd`, en la hora del navegador. */
function today(): string {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

/**
 * Ficha de clienta (RA-869d7fc51). Replica `Create/UpdateCustomerRequestValidator`:
 * si allí cambia una regla, aquí también. Los consentimientos solo cuentan en el
 * alta; el de tratamiento de datos es obligatorio.
 */
export const customerSchema = z
  .object({
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
    birthDate: z
      .string()
      .refine((value) => !value || value <= today(), 'La fecha de nacimiento no puede ser futura.')
      .optional()
      .or(z.literal('')),
    category: z.enum(CUSTOMER_CATEGORIES),
    preferredContactMethod: z.enum(CONTACT_METHODS, {
      errorMap: () => ({ message: 'El canal de contacto es obligatorio.' }),
    }),
    isActive: z.boolean(),
    creating: z.boolean(),
    consentDataProcessing: z.boolean(),
    consentMarketing: z.boolean(),
    consentPhotos: z.boolean(),
    consentWhatsapp: z.boolean(),
  })
  .refine((values) => !values.creating || values.consentDataProcessing, {
    path: ['consentDataProcessing'],
    message: 'El consentimiento de tratamiento de datos es obligatorio.',
  });

export type CustomerFormValues = z.infer<typeof customerSchema>;
