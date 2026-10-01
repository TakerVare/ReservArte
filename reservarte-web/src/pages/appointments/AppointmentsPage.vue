<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { ChevronLeft, ChevronRight } from 'lucide-vue-next';
import { Banner } from '@components/ui/banner';
import { PageTitle } from '@components/ui/page-title';
import { Button } from '@components/ui/button';
import { Badge } from '@components/ui/badge';
import { Select } from '@components/ui/select';
import { Tabs } from '@components/ui/tabs';
import { Text } from '@components/ui/text';
import { AppointmentDetailDialog } from '@components/ui/appointment-detail';
import { CancelAppointmentDialog } from '@components/ui/cancel-appointment';
import { formatAppointmentDateTime } from '@lib/utils/date.utils';
import { useAuthStore } from '@stores/authStore';
import { useUiStore } from '@stores/uiStore';
import { useAgenda, type AgendaView } from '@features/appointments/composables/useAgenda';
import {
  cancelAppointment,
  getAppointment,
  transitionAppointment,
} from '@features/appointments/api/appointments.api';
import {
  STATUS_BADGE,
  allowedTransitions,
  canCancel,
  canModify,
} from '@features/appointments/utils/appointment-status';
import type {
  AppointmentDetail,
  AppointmentStatus,
  AppointmentSummary,
  AppointmentTransition,
} from '@features/appointments/types/appointment.types';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Listado de citas del personal (RA-869fajn7g; sin diseño en Figma, con el estilo de
 * la app). Vistas de día, semana y mes con navegación por bloques y filtro por
 * empleada; cada cita con su estado en color y, al pulsarla, su detalle con las
 * acciones que admite. «Nueva cita» abre la pantalla de reserva.
 */

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();

const agenda = useAgenda();

const tabs = computed(() =>
  (['day', 'week', 'month'] as AgendaView[]).map((value) => ({
    value,
    label: t(`agenda.views.${value}`),
  }))
);
const view = computed({
  get: () => agenda.view.value,
  set: (value: string | undefined) => {
    agenda.view.value = (value as AgendaView) ?? 'day';
  },
});

const ALL = 'all';
const employeeOptions = computed(() => [
  { value: ALL, label: t('agenda.filter.all') },
  ...agenda.employees.value.map((e) => ({ value: String(e.id), label: e.name })),
]);
const employeeFilter = computed({
  get: () => (agenda.employeeId.value === null ? ALL : String(agenda.employeeId.value)),
  set: (value: string | undefined) => {
    agenda.employeeId.value = !value || value === ALL ? null : Number(value);
  },
});

const statusLabel = (status: AppointmentStatus) => t(`agenda.status.${status}`);
const hours = (a: AppointmentSummary) => `${a.startTime.slice(0, 5)} – ${a.endTime.slice(0, 5)}`;

// ── Detalle ───────────────────────────────────────────────────────────────
const detailOpen = ref(false);
const detail = ref<AppointmentDetail | null>(null);
const busy = ref(false);

const detailTransitions = computed(() =>
  detail.value ? allowedTransitions(detail.value.status, auth.user?.rol) : []
);

async function openDetail(item: AppointmentSummary) {
  try {
    detail.value = await getAppointment(item.id);
    detailOpen.value = true;
  } catch {
    ui.addToast(t('common.errorUnexpected'), 'error');
  }
}

async function onTransition(action: AppointmentTransition) {
  if (!detail.value) return;
  busy.value = true;
  try {
    await transitionAppointment(detail.value.id, action);
    detail.value = await getAppointment(detail.value.id);
    ui.addToast(t(`agenda.done.${action}`), 'success');
    void agenda.reload();
  } catch {
    ui.addToast(t('agenda.errors.transition'), 'error');
  } finally {
    busy.value = false;
  }
}

// ── Cancelar (RA-869d7fcfy) ──────────────────────────────────────────────
const cancelOpen = ref(false);
const staffReasons = computed(() => [
  t('cancel.reasons.staff.customerAsked'),
  t('cancel.reasons.staff.employeeUnavailable'),
  t('cancel.reasons.staff.centerClosed'),
]);
const detailWhen = computed(() =>
  detail.value
    ? formatAppointmentDateTime(detail.value.appointmentDate, detail.value.startTime)
    : ''
);

async function confirmCancel(reason: string | undefined) {
  if (!detail.value) return;
  busy.value = true;
  try {
    await cancelAppointment(detail.value.id, reason);
    cancelOpen.value = false;
    detail.value = await getAppointment(detail.value.id);
    ui.addToast(t('cancel.done'), 'success');
    void agenda.reload();
  } catch {
    ui.addToast(t('cancel.failed'), 'error');
  } finally {
    busy.value = false;
  }
}

function onModify() {
  if (!detail.value) return;
  void router.push({ name: 'booking', query: { cita: String(detail.value.id) } });
}

function goBack() {
  if (window.history.length > 1) router.back();
  else void router.push({ name: 'account' });
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex w-full max-w-[393px] flex-col md:max-w-[600px] xl:max-w-[800px]">
      <PageTitle :label="t('agenda.title')" />

      <div class="flex items-center justify-between px-7 py-[18px]">
        <Button size="sm" variant="primary" @click="goBack">
          <template #icon-start><ArrowLeftIcon class="h-4 w-4" aria-hidden="true" /></template>
          {{ t('ui.list.back') }}
        </Button>
        <Button size="sm" variant="secondary" @click="router.push({ name: 'booking' })">
          {{ t('agenda.new') }}
        </Button>
      </div>

      <Tabs v-model="view" :tabs="tabs" :label="t('agenda.views.label')">
        <template v-for="tab in tabs" :key="tab.value" #[tab.value]>
          <div class="flex flex-col gap-4 px-[18px]">
            <div class="flex items-center justify-between gap-2">
              <button
                type="button"
                :aria-label="t(`agenda.nav.previous.${tab.value}`)"
                class="p-1 text-foreground outline-none hover:text-primary focus-visible:ring-2 focus-visible:ring-ring"
                @click="agenda.previous()"
              >
                <ChevronLeft class="h-6 w-6" aria-hidden="true" />
              </button>
              <Text as="h2" size="h4" class="text-center" aria-live="polite">{{
                agenda.title.value
              }}</Text>
              <button
                type="button"
                :aria-label="t(`agenda.nav.next.${tab.value}`)"
                class="p-1 text-foreground outline-none hover:text-primary focus-visible:ring-2 focus-visible:ring-ring"
                @click="agenda.next()"
              >
                <ChevronRight class="h-6 w-6" aria-hidden="true" />
              </button>
            </div>

            <div class="flex items-end gap-4">
              <div class="flex flex-1 flex-col gap-1">
                <label
                  :for="`agenda-employee-${tab.value}`"
                  class="font-sans text-[14px] text-foreground"
                >
                  {{ t('agenda.filter.label') }}
                </label>
                <Select
                  :id="`agenda-employee-${tab.value}`"
                  v-model="employeeFilter"
                  :options="employeeOptions"
                />
              </div>
              <Button size="sm" variant="secondary" @click="agenda.goToday()">{{
                t('agenda.nav.today')
              }}</Button>
            </div>

            <div :aria-busy="agenda.loading.value">
              <Text
                v-if="agenda.failed.value"
                size="paragraph"
                role="alert"
                class="py-8 text-center text-destructive"
              >
                {{ t('agenda.errors.load') }}
              </Text>
              <Text
                v-else-if="agenda.loading.value && agenda.groups.value.length === 0"
                size="paragraph"
                role="status"
                class="py-8 text-center"
              >
                {{ t('ui.list.loading') }}
              </Text>
              <Text
                v-else-if="agenda.groups.value.length === 0"
                size="paragraph"
                class="py-8 text-center text-muted-foreground"
              >
                {{ t('agenda.empty') }}
              </Text>
              <section v-for="group in agenda.groups.value" v-else :key="group.date" class="pb-4">
                <Text
                  v-if="agenda.view.value !== 'day'"
                  as="h3"
                  size="h4"
                  class="bg-highlight px-3 py-2"
                >
                  {{ group.label }}
                </Text>
                <ul class="border-b border-border">
                  <li
                    v-for="item in group.items"
                    :key="item.id"
                    class="border-t border-border first:border-t-0"
                  >
                    <button
                      type="button"
                      class="flex w-full items-center justify-between gap-4 px-3 py-3 text-left outline-none transition-colors hover:bg-accent/50 focus-visible:ring-2 focus-visible:ring-ring"
                      @click="openDetail(item)"
                    >
                      <span class="flex min-w-0 flex-col gap-1">
                        <Text as="span" size="h4">{{ hours(item) }} · {{ item.customerName }}</Text>
                        <Text as="span" size="notes" class="text-muted-foreground">
                          {{ t('agenda.detail.employee', { name: item.employeeName }) }}
                        </Text>
                      </span>
                      <Badge :variant="STATUS_BADGE[item.status]">{{
                        statusLabel(item.status)
                      }}</Badge>
                    </button>
                  </li>
                </ul>
              </section>
            </div>
          </div>
        </template>
      </Tabs>
    </main>

    <AppointmentDetailDialog
      v-model:open="detailOpen"
      :appointment="detail"
      :status-variant="detail ? STATUS_BADGE[detail.status] : 'default'"
      :status-label="detail ? statusLabel(detail.status) : ''"
      :transitions="detailTransitions"
      :can-modify="detail ? canModify(detail.status) : false"
      :can-cancel="detail ? canCancel(detail.status) : false"
      :busy="busy"
      @transition="onTransition"
      @modify="onModify"
      @cancel="cancelOpen = true"
    />

    <CancelAppointmentDialog
      v-model:open="cancelOpen"
      :when="detailWhen"
      :reasons="staffReasons"
      :busy="busy"
      @confirm="confirmCancel"
    />
  </div>
</template>
