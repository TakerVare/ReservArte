import { describe, expect, it } from 'vitest';
import { settingsSchema } from '../settings.schema';

const valid = {
  timeZone: 'Europe/Madrid',
  cancellationHoursThreshold: '24',
  maxNoShowsBeforeBlock: '3',
};
const fieldsWithError = (input: object) => {
  const result = settingsSchema.safeParse(input);
  return result.success ? [] : result.error.issues.map((issue) => issue.path[0]);
};

describe('settingsSchema', () => {
  it('convierte los números que llegan como texto de los inputs', () => {
    expect(settingsSchema.parse(valid)).toEqual({
      timeZone: 'Europe/Madrid',
      cancellationHoursThreshold: 24,
      maxNoShowsBeforeBlock: 3,
    });
  });

  it('un campo vacío es «falta», no 0', () => {
    expect(fieldsWithError({ ...valid, cancellationHoursThreshold: '' })).toEqual([
      'cancellationHoursThreshold',
    ]);
    expect(fieldsWithError({ ...valid, maxNoShowsBeforeBlock: '' })).toEqual([
      'maxNoShowsBeforeBlock',
    ]);
    expect(fieldsWithError({ ...valid, timeZone: '' })).toEqual(['timeZone']);
  });

  it.each([
    [0, true],
    [720, true],
    [-1, false],
    [721, false],
    [1.5, false],
  ])('el umbral de cancelación %s horas es válido: %s (0 a 720, entero)', (hours, ok) => {
    expect(settingsSchema.safeParse({ ...valid, cancellationHoursThreshold: hours }).success).toBe(
      ok
    );
  });

  it.each([
    [1, true],
    [99, true],
    [0, false],
    [100, false],
  ])('%s no presentaciones es válido: %s (1 a 99)', (noShows, ok) => {
    expect(settingsSchema.safeParse({ ...valid, maxNoShowsBeforeBlock: noShows }).success).toBe(ok);
  });
});
