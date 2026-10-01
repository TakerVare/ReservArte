/* Mensajes base en español (único locale del MVP).
   Fase 4: añadir en/fr/pt y detección automática (volumen 3 §10.2). */
export default {
  common: {
    errorUnexpected: 'Ha ocurrido un error. Inténtelo de nuevo.',
  },
  auth: {
    login: {
      title: 'Iniciar sesión',
    },
  },
  account: {
    title: 'Mi cuenta',
    admin: {
      title: 'Área de administración',
      appointments: 'Citas',
      users: 'Usuarios',
      services: 'Servicios',
      employees: 'Empleados',
      settings: 'Configuración',
    },
    user: {
      title: 'Área de usuario',
      profile: 'Datos de usuario',
      paymentMethods: 'Métodos de pago',
      notifications: 'Notificaciones',
      settings: 'Configuración',
      privacy: 'Privacidad',
      about: 'Acerca de More Than Brows',
      logout: 'Cerrar sesión',
    },
  },
  payments: {
    redsys: {
      declinedGeneric: 'El pago ha sido rechazado. Inténtelo de nuevo o use otro método.',
    },
  },
};
