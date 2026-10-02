import { z } from 'zod';

/** Número de un `<input type="number">`: vacío es «falta», no 0. */
const number = (message: string) =>
  z.preprocess(
    (value) => (value === '' || value === null || value === undefined ? undefined : Number(value)),
    z.number({ required_error: message, invalid_type_error: message })
  );

/**
 * Ficha de servicio (RA-869d7fc6b). Replica `Create/UpdateServiceRequestValidator`:
 * si allí cambia una regla, aquí también. Los números llegan como texto de los
 * `<input type="number">` y se convierten aquí; vacío es «falta», no 0.
 */
export const serviceSchema = z
  .object({
    name: z
      .string()
      .trim()
      .min(1, 'El nombre es obligatorio.')
      .max(200, 'El nombre no puede superar los 200 caracteres.'),
    description: z
      .string()
      .max(1000, 'La descripción no puede superar los 1000 caracteres.')
      .optional()
      .or(z.literal('')),
    categoryId: z.string(),
    durationMinutes: number('Indica la duración en minutos.').pipe(
      z
        .number()
        .int('La duración va en minutos enteros.')
        .gt(0, 'La duración debe ser mayor que 0 minutos.')
    ),
    basePrice: number('Indica el precio.').pipe(
      z.number().min(0, 'El precio base no puede ser negativo.')
    ),
    requiresAllergyTest: z.boolean(),
    allergyTestHoursBefore: number('Indica las horas.').pipe(
      z.number().int('Las horas van en números enteros.')
    ),
    isActive: z.boolean(),
  })
  .refine((values) => !values.requiresAllergyTest || values.allergyTestHoursBefore > 0, {
    path: ['allergyTestHoursBefore'],
    message: 'La antelación de la prueba de alergia debe ser mayor que 0 horas.',
  });

export type ServiceFormValues = z.infer<typeof serviceSchema>;
