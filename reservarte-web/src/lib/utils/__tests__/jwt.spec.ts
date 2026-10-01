import { describe, expect, it } from 'vitest';
import { jwtSubject } from '../jwt.utils';

/** JWT sin firmar con el payload dado, codificado en base64url como los de la API. */
function token(payload: object) {
  const encode = (value: object) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'HS256' })}.${encode(payload)}.firma`;
}

describe('jwtSubject', () => {
  it('lee el sub como id de la cuenta', () => {
    expect(jwtSubject(token({ sub: '42', role: 'Employee' }))).toBe(42);
  });

  it('descodifica base64url con caracteres que el base64 normal no usa', () => {
    expect(jwtSubject(token({ sub: '7', name: 'Ñandú?>>>' }))).toBe(7);
  });

  it.each([null, undefined, '', 'no-es-un-jwt', 'a.%%%.b', token({ sub: 'abc' }), token({})])(
    'devuelve null si no hay un sub válido (%s)',
    (value) => {
      expect(jwtSubject(value)).toBeNull();
    }
  );
});
