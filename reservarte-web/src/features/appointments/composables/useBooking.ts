import { nextTick, ref, shallowRef, watch } from 'vue';
import { format } from 'date-fns';
import { ApiRequestError } from '@lib/api/request';
import { getActiveServices, type ServiceOption } from '@features/services/api/services.api';
import type { CustomerOption } from '@features/customers/api/customers.api';
import {
  createAppointment,
  getAppointment,
  getAppointmentsFrom,
  getAvailableDays,
  getServiceSlots,
  updateAppointment,
} from '../api/appointments.api';
import { pickNextAppointment } from '../utils/next-appointment';
import type {
  AppointmentDetail,
  AppointmentSummary,
  BookingWindow,
  ServiceSlots,
} from '../types/appointment.types';

/** Lo que pasa al pulsar un hueco. */
export type BookingOutcome =
  | { kind: 'booked'; appointment: AppointmentDetail; updated: boolean }
  /** Solo personal: la clienta ya tiene una cita activa y hay que elegir. */
  | { kind: 'choose'; active: AppointmentSummary }
  | { kind: 'error'; code: string; message: string };

/** Qué hacer con la cita activa de la clienta al reservar. */
export type BookingMode = 'auto' | 'update' | 'create';

export interface SelectedSlot {
  employeeId: number;
  startTime: string;
}

/**
 * Estado y reglas de la pantalla de reserva (RA-869fagpyg, H-44 y H-45):
 * servicio → día → hueco. La clienta crea su cita o modifica la que tiene
 * activa; el personal reserva para la clienta elegida y, si ya tiene una cita
 * activa, la pantalla pregunta si modificarla o crear otra.
 */
export function useBooking(options: {
  isStaff: () => boolean;
  /** Cuenta conectada: la cita activa «propia» se pide filtrada por ella (RA-869fajbw0). */
  currentUserId: () => number | null;
  now?: () => Date;
}) {
  const now = options.now ?? (() => new Date());

  const services = shallowRef<ServiceOption[]>([]);
  const serviceId = ref<number | null>(null);
  const customer = ref<CustomerOption | null>(null);
  /** Primer día del mes visible, `yyyy-MM-dd`. */
  const monthStart = ref(format(now(), 'yyyy-MM-01'));
  const availableDays = ref<Set<string>>(new Set());
  const bookingWindow = ref<BookingWindow | null>(null);
  const date = ref<string | null>(null);
  const employees = shallowRef<ServiceSlots['employees']>([]);
  const loadingDays = ref(false);
  const loadingSlots = ref(false);
  const saving = ref(false);
  const failed = ref(false);
  /** Cita concreta que se está modificando (desde el listado del personal, RA-869fajn7g). */
  const target = ref<AppointmentDetail | null>(null);

  let daysRequest = 0;
  let slotsRequest = 0;

  async function loadServices() {
    try {
      services.value = await getActiveServices();
    } catch {
      failed.value = true;
    }
  }

  async function loadDays() {
    const request = ++daysRequest;
    if (serviceId.value === null) {
      availableDays.value = new Set();
      return;
    }
    loadingDays.value = true;
    try {
      const [year, month] = monthStart.value.split('-').map(Number);
      const monthEnd = format(new Date(year, month, 0), 'yyyy-MM-dd');
      const result = await getAvailableDays(serviceId.value, monthStart.value, monthEnd);
      if (request !== daysRequest) return;
      availableDays.value = new Set(result.days);
      bookingWindow.value = {
        bookableFrom: result.bookableFrom,
        bookableUntil: result.bookableUntil,
      };
    } catch {
      if (request === daysRequest) failed.value = true;
    } finally {
      if (request === daysRequest) loadingDays.value = false;
    }
  }

  async function loadSlots() {
    const request = ++slotsRequest;
    if (serviceId.value === null || date.value === null) {
      employees.value = [];
      return;
    }
    loadingSlots.value = true;
    try {
      const result = await getServiceSlots(serviceId.value, date.value);
      if (request !== slotsRequest) return;
      employees.value = result.employees;
    } catch {
      if (request === slotsRequest) failed.value = true;
    } finally {
      if (request === slotsRequest) loadingSlots.value = false;
    }
  }

  // Cambiar de servicio deja sin elegir el día: sus huecos ya no valen.
  watch(serviceId, () => {
    date.value = null;
    void loadDays();
  });
  watch(monthStart, () => void loadDays());
  watch(date, () => void loadSlots());

  /** La cita activa de quien va a tener la cita: la propia clienta o la elegida por el personal. */
  async function findActive(): Promise<AppointmentSummary | null> {
    const today = format(now(), 'yyyy-MM-dd');
    // Siempre filtrada por clienta: sin filtro, al personal (o a quien la pantalla aún
    // no reconoce como personal tras recargar) la API le da las citas de todo el centro,
    // y se modificaría la de otra persona.
    const customerId = options.isStaff() ? customer.value!.id : options.currentUserId();
    if (customerId === null) return null;
    const items = await getAppointmentsFrom(today, customerId);
    return pickNextAppointment(items, now());
  }

  /**
   * Prepara la pantalla para modificar una cita concreta: su clienta, su servicio y
   * su día. Al reservar se modifica esa cita, no la próxima de la clienta.
   */
  async function loadTarget(id: number) {
    try {
      const detail = await getAppointment(id);
      target.value = detail;
      customer.value = {
        id: detail.customerId,
        fullName: detail.customerName,
        firstName: '',
        lastName: '',
      };
      serviceId.value = detail.items?.[0]?.serviceId ?? null;
      // El cambio de servicio limpia el día en su watch: el día se fija después.
      await nextTick();
      monthStart.value = `${detail.appointmentDate.slice(0, 8)}01`;
      date.value = detail.appointmentDate;
    } catch {
      failed.value = true;
    }
  }

  async function book(slot: SelectedSlot, mode: BookingMode = 'auto'): Promise<BookingOutcome> {
    if (serviceId.value === null || date.value === null) {
      return { kind: 'error', code: 'INCOMPLETE', message: '' };
    }
    if (options.isStaff() && customer.value === null) {
      return { kind: 'error', code: 'NO_CUSTOMER', message: '' };
    }

    saving.value = true;
    try {
      const body = {
        employeeId: slot.employeeId,
        appointmentDate: date.value,
        startTime: slot.startTime.slice(0, 5),
        items: [{ serviceId: serviceId.value }],
      };

      if (target.value) {
        const appointment = await updateAppointment(target.value.id, body);
        return { kind: 'booked', appointment, updated: true };
      }

      const active = mode === 'create' ? null : await findActive();
      if (active && options.isStaff() && mode === 'auto') {
        return { kind: 'choose', active };
      }

      const appointment = active
        ? await updateAppointment(active.id, body)
        : await createAppointment(
            options.isStaff() ? { ...body, customerId: customer.value!.id } : body
          );
      return { kind: 'booked', appointment, updated: active !== null };
    } catch (err) {
      const error =
        err instanceof ApiRequestError
          ? err
          : new ApiRequestError({ code: 'UNKNOWN', message: '' });
      // Si el hueco se ha ocupado entretanto, lo que se ve ya no es cierto.
      if (error.code === 'APT_SLOT_UNAVAILABLE') {
        void loadSlots();
        void loadDays();
      }
      return { kind: 'error', code: error.code, message: error.message };
    } finally {
      saving.value = false;
    }
  }

  void loadServices();

  return {
    services,
    serviceId,
    customer,
    monthStart,
    availableDays,
    bookingWindow,
    date,
    employees,
    loadingDays,
    loadingSlots,
    saving,
    failed,
    target,
    loadTarget,
    book,
    reloadSlots: loadSlots,
  };
}
