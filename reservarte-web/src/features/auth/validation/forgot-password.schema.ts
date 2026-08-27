import { z } from 'zod';

/** Validación de la solicitud de recuperación: solo el email. */
export const forgotPasswordSchema = z.object({
  email: z
    .string()
    .min(1, 'El email es obligatorio.')
    .email('El email no tiene un formato válido.'),
});

export type ForgotPasswordSchema = z.infer<typeof forgotPasswordSchema>;