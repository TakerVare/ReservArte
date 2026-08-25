import { z } from 'zod';

/**
 * Esquema de validación del registro (vol. 1 §4.4.1). La política de contraseña
 * replica el RegisterRequestValidator del backend (FluentValidation): mínimo 8
 * caracteres con mayúscula, minúscula, dígito y símbolo. Debe mantenerse alineada
 * con el backend — si allí cambia, aquí también.
 */
export const registerSchema = z
  .object({
    firstName: z
      .string()
      .min(1, 'El nombre es obligatorio.')
      .max(100, 'El nombre no puede superar los 100 caracteres.'),
    lastName: z
      .string()
      .min(1, 'Los apellidos son obligatorios.')
      .max(100, 'Los apellidos no pueden superar los 100 caracteres.'),
    email: z
      .string()
      .min(1, 'El email es obligatorio.')
      .email('El email no tiene un formato válido.'),
    phone: z
      .string()
      .max(20, 'El teléfono no puede superar los 20 caracteres.')
      .optional()
      .or(z.literal('')),
    password: z
      .string()
      .min(8, 'La contraseña debe tener al menos 8 caracteres.')
      .regex(/[A-Z]/, 'Debe contener al menos una mayúscula.')
      .regex(/[a-z]/, 'Debe contener al menos una minúscula.')
      .regex(/[0-9]/, 'Debe contener al menos un dígito.')
      .regex(/[^a-zA-Z0-9]/, 'Debe contener al menos un símbolo.'),
    confirmPassword: z.string().min(1, 'Confirma la contraseña.'),
    acceptedTerms: z.literal(true, {
      errorMap: () => ({ message: 'Debes aceptar los términos y condiciones.' }),
    }),
    acceptedPrivacy: z.literal(true, {
      errorMap: () => ({ message: 'Debes aceptar la política de privacidad.' }),
    }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Las contraseñas no coinciden.',
    path: ['confirmPassword'],
  });

export type RegisterSchema = z.infer<typeof registerSchema>;