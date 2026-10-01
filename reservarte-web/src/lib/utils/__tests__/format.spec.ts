import { describe, expect, it } from 'vitest';
import { formatCurrencyEur } from '../currency.utils';
import { formatAppointmentDateTime, formatDateSpain, formatTimeSpain } from '../date.utils';

// Intl separa importe y símbolo con un espacio duro (U+00A0).
const NBSP = ' ';

describe('formatCurrencyEur', () => {
  it.each([
    [25, `25,00${NBSP}€`],
    [18.5, `18,50${NBSP}€`],
    [1234.567, `1234,57${NBSP}€`],
    [0, `0,00${NBSP}€`],
  ])('%s → %s', (amount, expected) => {
    expect(formatCurrencyEur(amount)).toBe(expected);
  });
});

describe('fechas en formato español', () => {
  const date = new Date(2026, 0, 5, 9, 7);

  it('formatDateSpain → dd/MM/yyyy', () => {
    expect(formatDateSpain(date)).toBe('05/01/2026');
  });

  it('formatTimeSpain → HH:mm en 24 horas', () => {
    expect(formatTimeSpain(date)).toBe('09:07');
    expect(formatTimeSpain(new Date(2026, 0, 5, 18, 30))).toBe('18:30');
  });
});

describe('formatAppointmentDateTime (Figma «24 Dic - 10:00h»)', () => {
  it.each([
    ['2026-12-24', '10:00:00', '24 Dic - 10:00h'],
    ['2026-01-05', '09:30', '5 Ene - 09:30h'],
    ['2026-05-31', '18:15:00', '31 May - 18:15h'],
  ])('%s %s → %s', (date, time, expected) => {
    expect(formatAppointmentDateTime(date, time)).toBe(expected);
  });
});
