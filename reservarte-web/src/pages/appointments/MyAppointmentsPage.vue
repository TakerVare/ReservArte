<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@stores/authStore';
import { format } from 'date-fns';
import { Banner } from '@components/ui/banner';
import { AppointmentSection } from '@components/ui/appointment-section';
import { Text } from '@components/ui/text';
import { getAppointmentsFrom } from '@features/appointments/api/appointments.api';
import { pickNextAppointment } from '@features/appointments/utils/next-appointment';
import type { AppointmentSummary } from '@features/appointments/types/appointment.types';
import { formatAppointmentDateTime } from '@lib/utils/date.utils';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Inicio con sesión (destino «Inicio» del BottomNav y aterrizaje tras el login,
 * RA-869faaunu): la próxima cita (Figma `387:56617`) o, sin ella, la invitación
 * a reservar (`387:56660`).
 */

const { t } = useI18n();
const auth = useAuthStore();

const loading = ref(true);
const failed = ref(false);
const next = ref<AppointmentSummary | null>(null);

const dateTime = computed(() =>
  next.value
    ? formatAppointmentDateTime(next.value.appointmentDate, next.value.startTime)
    : undefined
);

onMounted(async () => {
  const now = new Date();
  // Solo las citas de la propia cuenta como clienta (RA-869fajbw0): al personal la API
  // le devuelve las de todo el centro, que se consultan en el Área de administración.
  const customerId = auth.currentUserId;
  if (customerId === null) {
    loading.value = false;
    return;
  }
  try {
    next.value = pickNextAppointment(
      await getAppointmentsFrom(format(now, 'yyyy-MM-dd'), customerId),
      now
    );
  } catch {
    failed.value = true;
  } finally {
    loading.value = false;
  }
});

const router = useRouter();

// «Modificar» y «Reservar Cita» abren la pantalla de reserva (H-45): allí la
// clienta modifica su cita activa o crea una. «Cancelar» abrirá el CancelModal
// (`869d7fcfy`, H-42).
function onModify() {
  void router.push({ name: 'booking' });
}
function onCancel() {}
function onBook() {
  void router.push({ name: 'booking' });
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <h1 class="sr-only">{{ t('myAppointments.title') }}</h1>
    <main class="w-full max-w-[393px] px-2.5 md:max-w-[600px]">
      <Text v-if="loading" size="paragraph" class="py-8 text-center" role="status">
        {{ t('myAppointments.loading') }}
      </Text>
      <Text
        v-else-if="failed"
        size="paragraph"
        class="py-8 text-center text-destructive"
        role="alert"
      >
        {{ t('myAppointments.loadError') }}
      </Text>
      <AppointmentSection
        v-else
        :title="t('myAppointments.next')"
        :date-time="dateTime"
        :empty-message="t('myAppointments.empty')"
        :modify-label="t('myAppointments.modify')"
        :cancel-label="t('myAppointments.cancel')"
        :book-label="t('myAppointments.book')"
        @modify="onModify"
        @cancel="onCancel"
        @book="onBook"
      />
    </main>
  </div>
</template>
