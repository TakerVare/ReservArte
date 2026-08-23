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

// Response: 401 en un endpoint protegido → sesión inválida o expirada:
// limpiar credencial y volver a login. (El flujo de refresh token se
// incorporará aquí en la tarea de Auth.)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthEndpoint = AUTH_ENDPOINTS_WITHOUT_SESSION.some((path) =>
      error.config?.url?.includes(path)
    );

    if (error.response?.status === 401 && !isAuthEndpoint) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
