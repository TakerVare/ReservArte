import { describe, expect, it } from 'vitest';
import type { z } from 'zod';
import { registerSchema } from '../register.schema';
import { resetPasswordSchema } from '../reset-password.schema';
import { setPasswordSchema } from '../set-password.schema';
import { forgotPasswordSchema } from '../forgot-password.schema';

/**
 * Esquemas Zod de autenticación. La política de contraseña replica la del
 * backend (RegisterRequestValidator y ResetPasswordRequestValidator): mínimo 8
 * con mayúscula, minúscula, dígito y símbolo. Si allí cambia, estos tests
 * deben cambiar con ella.
 */

const VALID_PASSWORD = 'Segura1!';

/** Mensajes de error de un campo concreto. */
function errorsFor(result: z.SafeParseReturnType<unknown, unknown>, field: string): string[] {
  if (result.success) return [];
  return result.error.issues.filter((i) => i.path.join('.') === field).map((i) => i.message);
}

const validRegister = {
  firstName: 'Ana',
  lastName: 'López',
  email: 'ana@example.com',
  phone: '',
  password: VALID_PASSWORD,
  confirmPassword: VALID_PASSWORD,
  acceptedTerms: true,
  acceptedPrivacy: true,
  acceptedDataProcessing: true,
};

describe('política de contraseña (registro, restablecimiento e invitación)', () => {
  const schemas = [
    ['registro', registerSchema, 'password', validRegister],
    [
      'restablecimiento',
      resetPasswordSchema,
      'newPassword',
      { email: 'ana@example.com', newPassword: VALID_PASSWORD, confirmPassword: VALID_PASSWORD },
    ],
    [
      'invitación',
      setPasswordSchema,
      'newPassword',
      { email: 'ana@example.com', newPassword: VALID_PASSWORD, confirmPassword: VALID_PASSWORD },
    ],
  ] as const;

  describe.each(schemas)('%s', (_, schema, field, valid) => {
    it('acepta una contraseña que cumple todas las reglas', () => {
      expect(schema.safeParse(valid).success).toBe(true);
    });

    it.each([
      ['Seg1!ab', 'La contraseña debe tener al menos 8 caracteres.'],
      ['segura1!', 'Debe contener al menos una mayúscula.'],
      ['SEGURA1!', 'Debe contener al menos una minúscula.'],
      ['Segura!!', 'Debe contener al menos un dígito.'],
      ['Segura12', 'Debe contener al menos un símbolo.'],
    ])('rechaza «%s»: %s', (password, message) => {
      const result = schema.safeParse({ ...valid, [field]: password, confirmPassword: password });

      expect(errorsFor(result, field)).toContain(message);
    });

    it('marca en confirmPassword que las contraseñas no coinciden', () => {
      const result = schema.safeParse({ ...valid, confirmPassword: 'Otra1234!' });

      expect(errorsFor(result, 'confirmPassword')).toEqual(['Las contraseñas no coinciden.']);
    });
  });
});

describe('registro', () => {
  it.each(['acceptedTerms', 'acceptedPrivacy', 'acceptedDataProcessing'])(
    'exige %s marcado',
    (field) => {
      const result = registerSchema.safeParse({ ...validRegister, [field]: false });

      expect(result.success).toBe(false);
      expect(errorsFor(result, field)).toHaveLength(1);
    }
  );

  it('el teléfono es opcional: vacío o ausente vale, más de 20 caracteres no', () => {
    const withoutPhone: Partial<typeof validRegister> = { ...validRegister };
    delete withoutPhone.phone;

    expect(registerSchema.safeParse(withoutPhone).success).toBe(true);
    expect(registerSchema.safeParse({ ...validRegister, phone: '' }).success).toBe(true);
    expect(
      errorsFor(registerSchema.safeParse({ ...validRegister, phone: '1'.repeat(21) }), 'phone')
    ).toEqual(['El teléfono no puede superar los 20 caracteres.']);
  });

  it('limita nombre y apellidos a 100 caracteres', () => {
    const result = registerSchema.safeParse({
      ...validRegister,
      firstName: 'a'.repeat(101),
      lastName: 'b'.repeat(101),
    });

    expect(errorsFor(result, 'firstName')).toHaveLength(1);
    expect(errorsFor(result, 'lastName')).toHaveLength(1);
  });
});

describe('email', () => {
  it.each([
    ['', 'El email es obligatorio.'],
    ['ana@', 'El email no tiene un formato válido.'],
  ])('rechaza «%s» con «%s»', (email, message) => {
    expect(errorsFor(forgotPasswordSchema.safeParse({ email }), 'email')).toContain(message);
  });

  it('acepta un email válido', () => {
    expect(forgotPasswordSchema.safeParse({ email: 'ana@example.com' }).success).toBe(true);
  });
});
