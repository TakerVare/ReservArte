import axios from 'axios';

// Contrato API (volumen 1 §5.1.1): las respuestas llegan con el envelope
// { success, data, error, meta }. Su manejo corresponde a cada servicio
// de feature, NO a estos interceptores (decisión de la tarea RA-869d7f79y).
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5218',
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

// Response: 401 → sesión inválida o expirada: limpiar credencial y volver
// a login. (El flujo de refresh token se incorporará aquí en la tarea de Auth.)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
