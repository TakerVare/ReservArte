import { z } from 'zod';

/**
 * Validación del restablecimiento. La política de contraseña replica el
 * ResetPasswordRequestValidator del backend (igual que el registro): mínimo 8
 * con mayúscula, minúscula, dígito y símbolo. Mantener alineada con el backend.
 * El token NO se valida aquí (llega por la ruta, no es un campo del formulario).
 */
export const resetPasswordSchema = z
  .object({
    email: z
      .string()
      .min(1, 'El email es obligatorio.')
      .email('El email no tiene un formato válido.'),
    newPassword: z
      .string()
      .min(8, 'La contraseña debe tener al menos 8 caracteres.')
      .regex(/[A-Z]/, 'Debe contener al menos una mayúscula.')
      .regex(/[a-z]/, 'Debe contener al menos una minúscula.')
      .regex(/[0-9]/, 'Debe contener al menos un dígito.')
      .regex(/[^a-zA-Z0-9]/, 'Debe contener al menos un símbolo.'),
    confirmPassword: z.string().min(1, 'Confirma la contraseña.'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Las contraseñas no coinciden.',
    path: ['confirmPassword'],
  });

export type ResetPasswordSchema = z.infer<typeof resetPasswordSchema>;