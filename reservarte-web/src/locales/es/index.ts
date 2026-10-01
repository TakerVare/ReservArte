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
    list: {
      back: 'Volver',
      create: 'Nuevo',
      search: 'Buscar',
      edit: 'Editar {name}',
      delete: 'Eliminar {name}',
      view: 'Ver {name}',
      loading: 'Cargando…',
      empty: 'No hay resultados.',
      error: 'No se ha podido cargar el listado.',
      retry: 'Reintentar',
      previous: 'Anterior',
      next: 'Siguiente',
      page: 'Página {page} de {total}',
    },
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
  booking: {
    title: 'Selección de cita',
    service: {
      label: 'Servicio',
      placeholder: 'Elige un servicio',
      option: '{name} · {minutes} min',
      first: 'Elige un servicio para ver los días con citas disponibles.',
    },
    customer: {
      select: 'Seleccionar cliente',
      pickerTitle: 'Seleccionar cliente',
      search: 'Buscar cliente',
      none: 'No hay clientes que coincidan.',
      selected: 'Cita para: {name}',
      missing: 'Selecciona el cliente al que se le asignará la cita.',
    },
    calendar: {
      label: 'Calendario de citas',
      previous: 'Mes anterior',
      next: 'Mes siguiente',
    },
    slots: {
      title: 'Citas disponibles:',
      none: 'No hay citas disponibles ese día.',
    },
    choose: {
      title: 'Ya tiene una cita',
      description:
        'El cliente ya tiene una cita el {when}. ¿Quieres modificarla o crear una nueva?',
      update: 'Modificar esa cita',
      create: 'Crear una nueva',
    },
    success: {
      title: 'Cita reservada',
      description: 'La cita ha quedado reservada para el {when} con {employee}.',
      accept: 'Aceptar',
    },
    errors: {
      load: 'No se han podido cargar los datos de la reserva. Inténtalo de nuevo más tarde.',
      noCustomer: 'Selecciona primero el cliente.',
      slotTaken: 'Ese hueco se acaba de ocupar. Elige otro.',
      outsideWindow: 'Esa fecha está fuera de las fechas en las que se puede reservar.',
      blocked: 'Este cliente no puede reservar citas.',
    },
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
      customers: 'Clientes',
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
