import { describe, expect, it } from 'vitest';
import { employeeSchema } from '../employee.schema';

const valid = {
  firstName: 'Eva',
  lastName: 'Ruiz',
  email: 'eva@reservarte.com',
  phone: '',
  rol: 'Employee',
  hireDate: '',
  isActive: true,
};

const errors = (values: object) => {
  const result = employeeSchema.safeParse({ ...valid, ...values });
  return result.success ? {} : result.error.flatten().fieldErrors;
};

describe('employeeSchema', () => {
  it('acepta una ficha mínima, sin teléfono ni fecha de alta', () => {
    expect(errors({})).toEqual({});
  });

  it('exige nombre, apellidos y un email válido', () => {
    expect(errors({ firstName: '  ', lastName: '', email: 'eva' })).toEqual({
      firstName: ['El nombre es obligatorio.'],
      lastName: ['Los apellidos son obligatorios.'],
      email: ['El email no tiene un formato válido.'],
    });
  });

  it('el teléfono admite los signos de la API y nada más', () => {
    expect(errors({ phone: '+34 (976) 12-34.56' })).toEqual({});
    expect(errors({ phone: '976 abc' }).phone).toBeDefined();
    expect(errors({ phone: '1'.repeat(21) }).phone).toBeDefined();
  });

  it('solo admite los roles de personal', () => {
    expect(errors({ rol: 'Manager' })).toEqual({});
    expect(errors({ rol: 'Customer' }).rol).toEqual(['El rol es obligatorio.']);
  });

  it('la fecha de alta no puede ser futura', () => {
    expect(errors({ hireDate: '2020-01-31' })).toEqual({});
    expect(errors({ hireDate: '2999-01-01' }).hireDate).toEqual([
      'La fecha de alta no puede ser futura.',
    ]);
  });
});
