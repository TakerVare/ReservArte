/**
 * Lee el `sub` (id de la cuenta) del payload de un JWT, sin verificarlo
 * (RA-869fajbw0). Solo sirve para que la SPA filtre lo que pinta cuando el
 * usuario aún no está en el store (tras recargar, hasta `869f6r6hc`); la
 * autorización la decide siempre la API. Devuelve null si el token no se puede leer.
 */
export function jwtSubject(token: string | null | undefined): number | null {
  const payload = token?.split('.')[1];
  if (!payload) return null;
  try {
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = JSON.parse(
      atob(base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '='))
    );
    const id = Number(json.sub);
    return Number.isInteger(id) && id > 0 ? id : null;
  } catch {
    return null;
  }
}
