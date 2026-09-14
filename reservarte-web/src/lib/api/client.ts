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
  // Invitación de alta (RA-869f17y68): su 401 significa «enlace caducado o ya
  // usado», no «sesión inválida». Sin esta excepción, el empleado que abre una
  // invitación vencida acaba en /login sin ver el motivo.
  '/api/v1/auth/set-password',
  // Restablecimiento (RA-869f1m12x): mismo caso. Su 401 es «enlace no válido o
  // caducado»; sin la excepción, cualquiera con un enlace vencido —tenga o no
  // sesión— era enviado a /login sin ver el mensaje.
  '/api/v1/auth/reset-password',
];

/**
 * Códigos de `error.code` que invalidan la sesión actual y obligan a volver a
 * login. Se discrimina por CÓDIGO y no por status: un 403 por rol insuficiente
 * (`[Authorize(Roles=…)]` del módulo de Empleados) significa «no tienes
 * permiso», y cerrar la sesión ahí sería un error de UX.
 */
const SESSION_ENDING_ERROR_CODES = [
  // La organización resuelta no coincide con la del JWT: la sesión pertenece a
  // otro tenant y no sirve en este contexto (vol. 1 §5.1.2).
  'ORG_TENANT_MISMATCH',
];

// NO añadir aquí códigos de permiso: `GEN_FORBIDDEN` (el 403 de
// [Authorize(Roles=…)] y de las reglas por dato de los servicios, con envelope
// desde RA-869f1anz3) no invalida la sesión, solo deniega esa operación.
// El 401 de un endpoint protegido también trae envelope (`GEN_UNAUTHORIZED`),
// pero se sigue decidiendo por status: cualquier 401 fuera de los endpoints de
// auth es una sesión que ya no vale, traiga el cuerpo que traiga.

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
