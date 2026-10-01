/* Mensajes base en español (único locale del MVP).
   Fase 4: añadir en/fr/pt y detección automática (volumen 3 §10.2). */
export default {
  common: {
    errorUnexpected: 'Ha ocurrido un error. Inténtelo de nuevo.',
  },
  ui: {
    dialog: { close: 'Cerrar' },
    select: { placeholder: 'Selecciona una opción' },
    toast: { region: 'Avisos', close: 'Cerrar aviso' },
  },
  nav: {
    home: 'Inicio',
    contact: 'Contacto',
    account: 'Mi cuenta',
  },
  myAppointments: {
    title: 'Mis citas',
    next: 'Próxima cita:',
    empty: 'No hay citas asignadas',
    modify: 'Modificar',
    cancel: 'Cancelar',
    book: 'Reservar Cita',
    loading: 'Cargando tu próxima cita…',
    loadError: 'No se ha podido cargar tu próxima cita. Inténtalo de nuevo más tarde.',
  },
  contact: {
    title: 'Contacto',
    mapTitle: 'Mapa de ubicación del centro',
    scheduleTitle: 'Horario de apertura',
    contactTitle: 'Datos de contacto',
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
