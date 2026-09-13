import axios from 'axios';

// Contrato API (volumen 1 §5.1.1): las respuestas llegan con el envelope
// { success, data, error, meta }. Su manejo corresponde a cada servicio
// de feature, NO a estos interceptores (decisión de la tarea RA-869d7f79y).
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5555',
  timeout: Number(import.meta.env.VITE_API_TIMEOUT) || 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request: adjunta el token Bearer si existe sesión iniciada
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Endpoints de auth cuyo 401 es un resultado de negocio (credenciales/ticket
// inválidos), NO una sesión caducada — no deben disparar la redirección.
const AUTH_ENDPOINTS_WITHOUT_SESSION = [
  '/api/v1/auth/login',
  '/api/v1/auth/mfa/verify',
  '/api/v1/auth/refresh-token',
];

/**
 * Códigos de `error.code` que invalidan la sesión actual y obligan a volver a
 * login. Se discrimina por CÓDIGO y no por status: un 403 por rol insuficiente
 * (los `[Authorize(Roles=…)]` que llegan con el módulo de Empleados) significa
 * «no tienes permiso», y cerrar la sesión ahí sería un error de UX.
 */
const SESSION_ENDING_ERROR_CODES = [
  // La organización resuelta no coincide con la del JWT: la sesión pertenece a
  // otro tenant y no sirve en este contexto (vol. 1 §5.1.2).
  'ORG_TENANT_MISMATCH',
];

// NO añadir aquí códigos de permiso ("no tienes rol suficiente"): eso no
// invalida la sesión, solo deniega esa operación. Además, hoy el 403 real de
// [Authorize(Roles=…)] llega SIN cuerpo (lo emite el middleware de ASP.NET
// Core, sin pasar por los controladores ni por el envelope), de modo que ni
// siquiera trae `error.code` — ver RA-869f1anz3.

function endSession() {
  localStorage.removeItem('authToken');

  // `window.location.href` (y no `router.push`) a propósito: la recarga
  // completa descarta el estado en memoria de Pinia, de modo que no hace falta
  // llamar a `authStore.logout()` desde aquí —lo que ataría este módulo al
  // store y crearía una dependencia circular (client → store → client).
  // OJO si algún día se cambia a navegación SPA sin recarga: entonces SÍ habría
  // que limpiar el store explícitamente o quedaría con la sesión anterior.
  window.location.href = '/login';
}

// Response: 401 en un endpoint protegido → sesión inválida o expirada:
// limpiar credencial y volver a login. (El flujo de refresh token se
// incorporará aquí en la tarea de Auth.)
// 403 con código de fin de sesión → mismo tratamiento.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthEndpoint = AUTH_ENDPOINTS_WITHOUT_SESSION.some((path) =>
      error.config?.url?.includes(path)
    );

    if (error.response?.status === 401 && !isAuthEndpoint) {
      endSession();
    }

    const errorCode = error.response?.data?.error?.code;

    if (error.response?.status === 403 && SESSION_ENDING_ERROR_CODES.includes(errorCode)) {
      endSession();
    }

    return Promise.reject(error);
  }
);

export default apiClient;
