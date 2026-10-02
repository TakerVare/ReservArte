import { describe, expect, it } from 'vitest';
import { customerSchema } from '../customer.schema';

const valid = {
  firstName: 'Eva',
  lastName: 'Ruiz',
  email: 'eva@example.com',
  phone: '',
  birthDate: '',
  category: 'new',
  preferredContactMethod: 'email',
  isActive: true,
  creating: true,
  consentDataProcessing: true,
  consentMarketing: false,
  consentPhotos: false,
  consentWhatsapp: false,
};

const errors = (values: object) => {
  const result = customerSchema.safeParse({ ...valid, ...values });
  return result.success ? {} : result.error.flatten().fieldErrors;
};

describe('customerSchema', () => {
  it('acepta un alta mínima con el consentimiento de datos', () => {
    expect(errors({})).toEqual({});
  });

  it('en el alta exige el consentimiento de tratamiento de datos', () => {
    expect(errors({ consentDataProcessing: false })).toEqual({
      consentDataProcessing: ['El consentimiento de tratamiento de datos es obligatorio.'],
    });
  });

  it('al editar no pide consentimientos: se gestionan aparte', () => {
    expect(errors({ creating: false, consentDataProcessing: false })).toEqual({});
  });

  it('exige nombre, apellidos y email válido, y valida teléfono y nacimiento', () => {
    expect(errors({ firstName: ' ', email: 'eva' })).toEqual({
      firstName: ['El nombre es obligatorio.'],
      email: ['El email no tiene un formato válido.'],
    });
    expect(errors({ phone: '976 abc' }).phone).toBeDefined();
    expect(errors({ birthDate: '2999-01-01' }).birthDate).toEqual([
      'La fecha de nacimiento no puede ser futura.',
    ]);
  });

  it('solo admite las categorías y los canales de la API', () => {
    expect(errors({ category: 'vip', preferredContactMethod: 'whatsapp' })).toEqual({});
    expect(errors({ category: 'oro' }).category).toBeDefined();
    expect(errors({ preferredContactMethod: 'fax' }).preferredContactMethod).toEqual([
      'El canal de contacto es obligatorio.',
    ]);
  });
});
