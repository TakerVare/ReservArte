import { z } from 'zod';

/** Número de un `<input type="number">`: vacío es «falta», no 0. */
const number = (message: string) =>
  z.preprocess(
    (value) => (value === '' || value === null || value === undefined ? undefined : Number(value)),
    z.number({ required_error: message, invalid_type_error: message })
  );

export const CANCELLATION_HOURS = { min: 0, max: 720 } as const;
export const NO_SHOWS = { min: 1, max: 99 } as const;

/**
 * Configuración del centro (RA-869f6r71x). Replica
 * `UpdateOrganizationSettingsRequestValidator`: si allí cambia un rango, aquí
 * también. Que la zona exista lo comprueba la API (`UnknownTimeZone`).
 */
export const settingsSchema = z.object({
  timeZone: z.string().min(1, 'Elige la zona horaria.'),
  cancellationHoursThreshold: number('Indica las horas.').pipe(
    z
      .number()
      .int('Las horas van en números enteros.')
      .min(
        CANCELLATION_HOURS.min,
        `Debe estar entre ${CANCELLATION_HOURS.min} y ${CANCELLATION_HOURS.max} horas.`
      )
      .max(
        CANCELLATION_HOURS.max,
        `Debe estar entre ${CANCELLATION_HOURS.min} y ${CANCELLATION_HOURS.max} horas.`
      )
  ),
  maxNoShowsBeforeBlock: number('Indica el número de no presentaciones.').pipe(
    z
      .number()
      .int('Va en números enteros.')
      .min(NO_SHOWS.min, `Debe estar entre ${NO_SHOWS.min} y ${NO_SHOWS.max}.`)
      .max(NO_SHOWS.max, `Debe estar entre ${NO_SHOWS.min} y ${NO_SHOWS.max}.`)
  ),
});

export type SettingsFormValues = z.infer<typeof settingsSchema>;
