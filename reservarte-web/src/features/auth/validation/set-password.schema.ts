/**
 * Validación del establecimiento de contraseña desde la invitación de alta
 * (RA-869f17y68). La política es EXACTAMENTE la misma que la del
 * restablecimiento —y la del backend—, así que no se duplica: se reexporta con
 * el nombre del flujo, para que la página de invitación no dependa del nombre
 * del flujo de recuperación. Si algún día divergen, este es el sitio donde
 * separarlas.
 */
export { resetPasswordSchema as setPasswordSchema } from './reset-password.schema';
export type { ResetPasswordSchema as SetPasswordSchema } from './reset-password.schema';
