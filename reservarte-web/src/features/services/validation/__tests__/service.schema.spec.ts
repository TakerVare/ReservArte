import { describe, expect, it } from 'vitest';
import { serviceSchema } from '../service.schema';

const valid = {
  name: 'Henna',
  description: '',
  categoryId: 'none',
  durationMinutes: '40',
  basePrice: '22',
  requiresAllergyTest: false,
  allergyTestHoursBefore: '48',
  isActive: true,
};

const errors = (values: object) => {
  const result = serviceSchema.safeParse({ ...valid, ...values });
  return result.success ? {} : result.error.flatten().fieldErrors;
};

describe('serviceSchema', () => {
  it('acepta los números que llegan como texto y los convierte', () => {
    const result = serviceSchema.safeParse(valid);
    expect(result.success && result.data.durationMinutes).toBe(40);
    expect(result.success && result.data.basePrice).toBe(22);
  });

  it('un precio vacío falta: no se convierte en 0 €', () => {
    expect(errors({ basePrice: '' })).toEqual({ basePrice: ['Indica el precio.'] });
    expect(errors({ basePrice: '0' })).toEqual({});
  });

  it('la duración es un entero mayor que 0 y el precio no es negativo', () => {
    expect(errors({ durationMinutes: '0' }).durationMinutes).toEqual([
      'La duración debe ser mayor que 0 minutos.',
    ]);
    expect(errors({ durationMinutes: '12.5' }).durationMinutes).toBeDefined();
    expect(errors({ basePrice: '-1' }).basePrice).toEqual([
      'El precio base no puede ser negativo.',
    ]);
  });

  it('con prueba de alergia, la antelación tiene que ser mayor que 0', () => {
    expect(errors({ allergyTestHoursBefore: '0' })).toEqual({});
    expect(errors({ requiresAllergyTest: true, allergyTestHoursBefore: '0' })).toEqual({
      allergyTestHoursBefore: ['La antelación de la prueba de alergia debe ser mayor que 0 horas.'],
    });
  });

  it('nombre obligatorio y descripción de 1000 caracteres como mucho', () => {
    expect(errors({ name: '  ' }).name).toEqual(['El nombre es obligatorio.']);
    expect(errors({ description: 'x'.repeat(1001) }).description).toBeDefined();
  });
});
