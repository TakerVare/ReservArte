<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { format } from 'date-fns';
import { Banner } from '@components/ui/banner';
import { PageTitle } from '@components/ui/page-title';
import { Button } from '@components/ui/button';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import { Dialog } from '@components/ui/dialog';
import { BookingCalendar } from '@components/ui/booking-calendar';
import { EmployeeAvailability } from '@components/ui/employee-availability';
import { CustomerPicker, type CustomerPickerItem } from '@components/ui/customer-picker';
import { useAuthStore } from '@stores/authStore';
import { useUiStore } from '@stores/uiStore';
import { useDataList } from '@lib/composables/useDataList';
import { formatAppointmentDateTime } from '@lib/utils/date.utils';
import { searchCustomers, type CustomerOption } from '@features/customers/api/customers.api';
import {
  useBooking,
  type BookingMode,
  type SelectedSlot,
} from '@features/appointments/composables/useBooking';
import type {
  AppointmentDetail,
  AppointmentSummary,
} from '@features/appointments/types/appointment.types';
import type { UserRole } from '@features/auth/types/auth.types';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import UserPlusIcon from '@assets/icons/user-plus.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Reserva y modificación de citas (RA-869fagpyg, H-44 y H-45; Figma «Selección de
 * cita» 387:56629). La clienta elige servicio, día y hueco: crea su cita o
 * modifica la que tiene activa. El personal elige antes la clienta y, si ya tiene
 * una cita activa, decide si modificarla o crear otra. Al terminar, a Mis citas.
 */

const STAFF_ROLES: readonly UserRole[] = ['Admin', 'Manager', 'Employee'];

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();

// Tras recargar, el usuario no se conoce hasta 869f6r6hc: se trata como clienta.
const isStaff = computed(() => {
  const role = auth.user?.rol;
  return role !== undefined && STAFF_ROLES.includes(role);
});

const booking = useBooking({
  isStaff: () => isStaff.value,
  currentUserId: () => auth.currentUserId,
});
const today = format(new Date(), 'yyyy-MM-dd');

// ── Servicio ──────────────────────────────────────────────────────────────
const serviceOptions = computed(() =>
  booking.services.value.map((s) => ({
    value: String(s.id),
    label: t('booking.service.option', { name: s.name, minutes: s.durationMinutes }),
  }))
);
const selectedService = computed({
  get: () => (booking.serviceId.value === null ? undefined : String(booking.serviceId.value)),
  set: (value) => {
    booking.serviceId.value = value ? Number(value) : null;
  },
});

// ── Clienta (solo personal) ───────────────────────────────────────────────
const pickerOpen = ref(false);
const customers = useDataList<CustomerOption>(
  async ({ search, page, pageSize }) =>
    isStaff.value
      ? searchCustomers(search, page, pageSize)
      : { items: [], pagination: { page: 1, pageSize, totalCount: 0, totalPages: 0 } },
  { pageSize: 20 }
);
const pickerItems = computed<CustomerPickerItem[]>(() =>
  customers.items.value.map((c) => ({ id: c.id, name: c.fullName, photoUrl: c.profileImageUrl }))
);
function onCustomerSelected(item: CustomerPickerItem) {
  booking.customer.value = customers.items.value.find((c) => c.id === item.id) ?? null;
}

// ── Huecos ────────────────────────────────────────────────────────────────
const employeeEntries = computed(() =>
  booking.employees.value.map((e) => ({
    id: e.employeeId,
    name: e.employeeName,
    slots: e.slots.map((s) => s.startTime.slice(0, 5)),
  }))
);

const pending = ref<SelectedSlot | null>(null);
const chooseOpen = ref(false);
const activeToReplace = ref<AppointmentSummary | null>(null);
const successOpen = ref(false);
const booked = ref<AppointmentDetail | null>(null);

async function reserve(mode: BookingMode = 'auto') {
  if (!pending.value) return;
  const outcome = await booking.book(pending.value, mode);

  if (outcome.kind === 'choose') {
    activeToReplace.value = outcome.active;
    chooseOpen.value = true;
    return;
  }

  chooseOpen.value = false;
  if (outcome.kind === 'booked') {
    booked.value = outcome.appointment;
    successOpen.value = true;
    return;
  }

  ui.addToast(errorMessage(outcome.code), 'error');
}

function onSelectSlot(payload: { employeeId: number; slot: string }) {
  if (isStaff.value && !booking.customer.value) {
    ui.addToast(t('booking.errors.noCustomer'), 'warning');
    return;
  }
  pending.value = { employeeId: payload.employeeId, startTime: payload.slot };
  void reserve();
}

function errorMessage(code: string) {
  switch (code) {
    case 'APT_SLOT_UNAVAILABLE':
      return t('booking.errors.slotTaken');
    case 'GEN_VALIDATION_FAILED':
      return t('booking.errors.outsideWindow');
    case 'CUST_BLOCKED':
      return t('booking.errors.blocked');
    default:
      return t('common.errorUnexpected');
  }
}

const activeLabel = computed(() =>
  activeToReplace.value
    ? formatAppointmentDateTime(
        activeToReplace.value.appointmentDate,
        activeToReplace.value.startTime
      )
    : ''
);
const bookedLabel = computed(() =>
  booked.value
    ? formatAppointmentDateTime(booked.value.appointmentDate, booked.value.startTime)
    : ''
);

// Al cerrar el aviso de reserva correcta, a Mis citas.
watch(successOpen, (open, wasOpen) => {
  if (wasOpen && !open) void router.push({ name: 'my-appointments' });
});

function goBack() {
  if (window.history.length > 1) router.back();
  else void router.push({ name: 'my-appointments' });
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex w-full max-w-[393px] flex-col items-center md:max-w-[600px]">
      <PageTitle :label="t('booking.title')" />

      <div class="flex w-full flex-wrap items-center justify-between gap-4 px-7 py-[18px]">
        <Button size="sm" variant="primary" @click="goBack">
          <template #icon-start><ArrowLeftIcon class="h-4 w-4" aria-hidden="true" /></template>
          {{ t('ui.list.back') }}
        </Button>
        <Button v-if="isStaff" size="sm" variant="secondary" @click="pickerOpen = true">
          {{ t('booking.customer.select') }}
          <template #icon-end><UserPlusIcon class="h-4 w-4" aria-hidden="true" /></template>
        </Button>
      </div>

      <Text
        v-if="booking.failed.value"
        size="paragraph"
        role="alert"
        class="px-7 py-4 text-center text-destructive"
      >
        {{ t('booking.errors.load') }}
      </Text>

      <div class="flex w-full flex-col gap-2 px-7 py-4">
        <label for="booking-service" class="font-sans text-foreground">{{
          t('booking.service.label')
        }}</label>
        <Select
          id="booking-service"
          v-model="selectedService"
          :options="serviceOptions"
          :placeholder="t('booking.service.placeholder')"
        />
      </div>

      <Text v-if="isStaff" size="h4" class="w-full px-7 pt-4" data-testid="booking-customer">
        {{
          booking.customer.value
            ? t('booking.customer.selected', { name: booking.customer.value.fullName })
            : t('booking.customer.missing')
        }}
      </Text>

      <div class="flex w-full justify-center px-2.5 py-6">
        <BookingCalendar
          v-if="booking.serviceId.value !== null"
          v-model="booking.date.value"
          v-model:month="booking.monthStart.value"
          :available-days="booking.availableDays.value"
          :today="today"
          :max-date="booking.bookingWindow.value?.bookableUntil"
        />
        <Text v-else size="paragraph" class="text-center text-muted-foreground">
          {{ t('booking.service.first') }}
        </Text>
      </div>

      <div v-if="booking.date.value" class="w-full px-7" :aria-busy="booking.loadingSlots.value">
        <Text
          v-if="booking.loadingSlots.value"
          size="paragraph"
          role="status"
          class="py-8 text-center"
        >
          {{ t('ui.list.loading') }}
        </Text>
        <Text
          v-else-if="employeeEntries.length === 0"
          size="paragraph"
          class="py-8 text-center text-muted-foreground"
        >
          {{ t('booking.slots.none') }}
        </Text>
        <EmployeeAvailability
          v-else
          :title="t('booking.slots.title')"
          :employees="employeeEntries"
          :disabled="booking.saving.value"
          @select-slot="onSelectSlot"
        />
      </div>
    </main>

    <CustomerPicker
      v-if="isStaff"
      v-model:open="pickerOpen"
      v-model:search="customers.search.value"
      :customers="pickerItems"
      :loading="customers.loading.value"
      @select="onCustomerSelected"
    />

    <Dialog
      v-model:open="chooseOpen"
      :title="t('booking.choose.title')"
      :description="t('booking.choose.description', { when: activeLabel })"
    >
      <template #footer>
        <Button variant="secondary" :disabled="booking.saving.value" @click="reserve('create')">
          {{ t('booking.choose.create') }}
        </Button>
        <Button :disabled="booking.saving.value" @click="reserve('update')">
          {{ t('booking.choose.update') }}
        </Button>
      </template>
    </Dialog>

    <Dialog
      v-model:open="successOpen"
      :title="t('booking.success.title')"
      :description="
        t('booking.success.description', {
          when: bookedLabel,
          employee: booked?.employeeName ?? '',
        })
      "
    >
      <template #footer>
        <Button @click="successOpen = false">{{ t('booking.success.accept') }}</Button>
      </template>
    </Dialog>
  </div>
</template>
