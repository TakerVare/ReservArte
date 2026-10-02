<script setup lang="ts">
import { computed, ref, shallowRef, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Banner } from '@components/ui/banner';
import { PageTitle } from '@components/ui/page-title';
import { Button } from '@components/ui/button';
import { Avatar } from '@components/ui/avatar';
import { Tabs } from '@components/ui/tabs';
import { Text } from '@components/ui/text';
import { ConfirmDialog } from '@components/ui/confirm-dialog';
import { EmployeeForm } from '@components/ui/employee-form';
import { ScheduleEditor } from '@components/ui/schedule-editor';
import { AbsenceDialog, AbsenceList } from '@components/ui/absences';
import { EmployeeServicesEditor } from '@components/ui/employee-services';
import { ApiRequestError } from '@lib/api/request';
import { useUiStore } from '@stores/uiStore';
import {
  addAbsence,
  createEmployee,
  deactivateEmployee,
  deleteAbsence,
  getAvailability,
  getEmployee,
  getEmployeeServices,
  reactivateEmployee,
  replaceEmployeeServices,
  replaceSchedule,
  resendInvitation,
  updateEmployee,
} from '@features/employees/api/employees.api';
import type {
  Absence,
  AbsenceInput,
  Employee,
  EmployeeAvailability,
  EmployeeInput,
} from '@features/employees/types/employee.types';
import type { EmployeeFormValues } from '@features/employees/validation/employee.schema';
import { fromWeek, toWeek, type ScheduleDay } from '@features/employees/utils/schedule';
import { getActiveServices, type ServiceOption } from '@features/services/api/services.api';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';
import { Trash2 } from 'lucide-vue-next';

/**
 * Ficha de empleado (RA-869d7fbyt + RA-869d7fc0h), según Figma «Detalle usuario»
 * (387:56778): banda con el título, «Volver» y «Dar de baja», foto y «Datos de
 * usuario». Al editar, tres pestañas más con el estilo de la app: los servicios que
 * presta (4.1b), el horario semanal y las vacaciones y ausencias. El alta lleva a la
 * ficha nueva, en «Servicios»: sin servicios ni horario no sale en la reserva.
 */

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const ui = useUiStore();

type Tab = 'data' | 'services' | 'schedule' | 'absences';
const TABS: Tab[] = ['data', 'services', 'schedule', 'absences'];

const creating = computed(() => route.name === 'employee-new');
const employeeId = computed(() => (creating.value ? null : Number(route.params.id)));

const employee = shallowRef<Employee | null>(null);
const loadState = ref<'loading' | 'ready' | 'missing' | 'failed'>('loading');
const busy = ref(false);

// La pestaña va en la URL (`?tab=`): al recargar o volver atrás se queda donde estaba.
const tabs = computed(() => TABS.map((value) => ({ value, label: t(`employees.tabs.${value}`) })));
const tabModel = computed({
  get: () => (TABS.includes(route.query.tab as Tab) ? (route.query.tab as Tab) : 'data'),
  set: (value: string | undefined) => {
    const next = TABS.includes(value as Tab) ? (value as Tab) : 'data';
    void router.replace({ query: { ...route.query, tab: next === 'data' ? undefined : next } });
  },
});

// ── Carga ─────────────────────────────────────────────────────────────────
const schedule = ref<ScheduleDay[]>(toWeek([]));
const absences = shallowRef<Absence[]>([]);
const catalog = shallowRef<ServiceOption[]>([]);
const assignedServices = ref<number[]>([]);

/** Las ausencias de hoy a un año vista (la API, por defecto, da 90 días). */
function absenceRange() {
  const from = new Date();
  from.setUTCHours(0, 0, 0, 0);
  const to = new Date(from);
  to.setUTCFullYear(to.getUTCFullYear() + 1);
  return { from: from.toISOString(), to: to.toISOString() };
}

function applyAvailability(availability: EmployeeAvailability) {
  schedule.value = toWeek(availability.weeklySchedule);
  absences.value = availability.exceptions
    .filter((absence) => absence.isActive)
    .sort((a, b) => a.startDateTime.localeCompare(b.startDateTime));
}

async function load() {
  if (employeeId.value === null) {
    employee.value = null;
    loadState.value = 'ready';
    return;
  }
  loadState.value = 'loading';
  try {
    const id = employeeId.value;
    const [found, availability, services, active] = await Promise.all([
      getEmployee(id),
      getAvailability(id, absenceRange()),
      getEmployeeServices(id),
      getActiveServices(),
    ]);
    employee.value = found;
    applyAvailability(availability);
    assignedServices.value = services.map((service) => service.serviceId);
    catalog.value = active;
    loadState.value = 'ready';
  } catch (err) {
    loadState.value =
      err instanceof ApiRequestError && err.code === 'GEN_NOT_FOUND' ? 'missing' : 'failed';
  }
}

// Solo al cambiar de ficha (o del alta a la ficha nueva), no al cambiar de pestaña.
watch(
  () => [route.name, route.params.id],
  () => void load(),
  { immediate: true }
);

// ── Datos ─────────────────────────────────────────────────────────────────
const formInitial = computed<Partial<EmployeeFormValues> | undefined>(() =>
  employee.value
    ? {
        firstName: employee.value.firstName,
        lastName: employee.value.lastName,
        email: employee.value.email,
        phone: employee.value.phone ?? '',
        rol: employee.value.rol,
        hireDate: employee.value.hireDate ?? '',
        isActive: employee.value.isActive,
      }
    : undefined
);
const serverErrors = ref<Partial<Record<keyof EmployeeFormValues, string>>>();

function toInput(values: EmployeeFormValues): EmployeeInput {
  return {
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    email: values.email.trim(),
    phone: values.phone?.trim() || null,
    rol: values.rol,
    hireDate: values.hireDate || null,
    // La foto no se edita aquí todavía (subida de fotos, RA-869d7ee5t): se conserva.
    profileImageUrl: employee.value?.profileImageUrl ?? null,
  };
}

function showSaveError(err: unknown) {
  if (err instanceof ApiRequestError) {
    if (err.code === 'GEN_CONFLICT') {
      serverErrors.value = { email: t('employees.errors.emailTaken') };
      return;
    }
    if (err.code === 'GEN_VALIDATION_FAILED' && err.details.length > 0) {
      serverErrors.value = Object.fromEntries(
        err.details.filter((d) => d.field).map((d) => [d.field, d.message])
      );
      return;
    }
    if (err.code === 'GEN_FORBIDDEN') {
      ui.addToast(err.message, 'error');
      return;
    }
  }
  ui.addToast(t('employees.errors.save'), 'error');
}

async function save(values: EmployeeFormValues) {
  busy.value = true;
  serverErrors.value = undefined;
  try {
    if (creating.value) {
      const created = await createEmployee(toInput(values));
      ui.addToast(t('employees.done.created'), 'success');
      await router.replace({
        name: 'employee-detail',
        params: { id: String(created.id) },
        query: { tab: 'services' },
      });
      return;
    }
    const id = employeeId.value!;
    let saved = await updateEmployee(id, toInput(values));
    if (values.isActive !== saved.isActive) {
      saved = values.isActive ? await reactivateEmployee(id) : await deactivateEmployee(id);
    }
    employee.value = saved;
    ui.addToast(t('employees.done.updated'), 'success');
  } catch (err) {
    showSaveError(err);
  } finally {
    busy.value = false;
  }
}

async function sendInvitation() {
  if (!employee.value) return;
  busy.value = true;
  try {
    await resendInvitation(employee.value.id);
    ui.addToast(t('employees.done.invitation'), 'success');
  } catch {
    ui.addToast(t('employees.errors.invitation'), 'error');
  } finally {
    busy.value = false;
  }
}

// ── Baja y reactivación ───────────────────────────────────────────────────
const confirmOpen = ref(false);

async function deactivate() {
  if (!employee.value) return;
  busy.value = true;
  try {
    employee.value = await deactivateEmployee(employee.value.id);
    confirmOpen.value = false;
    ui.addToast(t('employees.done.deactivated', { name: employee.value.fullName }), 'success');
  } catch (err) {
    showSaveError(err);
  } finally {
    busy.value = false;
  }
}

async function reactivate() {
  if (!employee.value) return;
  busy.value = true;
  try {
    employee.value = await reactivateEmployee(employee.value.id);
    ui.addToast(t('employees.done.reactivated', { name: employee.value.fullName }), 'success');
  } catch (err) {
    showSaveError(err);
  } finally {
    busy.value = false;
  }
}

// ── Servicios que presta (4.1b) ───────────────────────────────────────────
async function saveServices(serviceIds: number[]) {
  if (employeeId.value === null) return;
  busy.value = true;
  try {
    const saved = await replaceEmployeeServices(employeeId.value, serviceIds);
    assignedServices.value = saved.map((service) => service.serviceId);
    ui.addToast(t('employees.services.saved'), 'success');
  } catch (err) {
    const forbidden = err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN';
    ui.addToast(forbidden ? err.message : t('employees.services.saveError'), 'error');
  } finally {
    busy.value = false;
  }
}

// ── Horario ───────────────────────────────────────────────────────────────
async function saveSchedule(days: ScheduleDay[]) {
  if (employeeId.value === null) return;
  busy.value = true;
  try {
    applyAvailability(await replaceSchedule(employeeId.value, fromWeek(days)));
    ui.addToast(t('employees.schedule.saved'), 'success');
  } catch (err) {
    if (err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN') {
      ui.addToast(err.message, 'error');
    } else {
      ui.addToast(t('employees.schedule.saveError'), 'error');
    }
  } finally {
    busy.value = false;
  }
}

// ── Ausencias ─────────────────────────────────────────────────────────────
const absenceOpen = ref(false);

async function reloadAbsences() {
  if (employeeId.value === null) return;
  applyAvailability(await getAvailability(employeeId.value, absenceRange()));
}

async function saveAbsence(input: AbsenceInput) {
  if (employeeId.value === null) return;
  busy.value = true;
  try {
    await addAbsence(employeeId.value, input);
    absenceOpen.value = false;
    ui.addToast(t('employees.absences.saved'), 'success');
    await reloadAbsences();
  } catch (err) {
    const forbidden = err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN';
    ui.addToast(forbidden ? err.message : t('employees.absences.saveError'), 'error');
  } finally {
    busy.value = false;
  }
}

async function removeAbsence(absence: Absence) {
  if (employeeId.value === null) return;
  busy.value = true;
  try {
    await deleteAbsence(employeeId.value, absence.id);
    ui.addToast(t('employees.absences.removed'), 'success');
    await reloadAbsences();
  } catch {
    ui.addToast(t('employees.absences.removeError'), 'error');
  } finally {
    busy.value = false;
  }
}

function goBack() {
  void router.push({ name: 'employees' });
}

const title = computed(() =>
  creating.value ? t('employees.detail.newTitle') : t('employees.detail.editTitle')
);
const displayName = computed(() => employee.value?.fullName ?? '');
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex w-full max-w-[393px] flex-col md:max-w-[600px] xl:max-w-[800px]">
      <PageTitle :label="title" />

      <!-- Como en Figma: «Volver» a la izquierda y la baja a la derecha. -->
      <div class="flex items-center justify-between px-7 py-[18px]">
        <Button size="sm" variant="primary" @click="goBack">
          <template #icon-start><ArrowLeftIcon class="h-4 w-4" aria-hidden="true" /></template>
          {{ t('ui.list.back') }}
        </Button>
        <template v-if="employee">
          <Button
            v-if="employee.isActive"
            size="sm"
            variant="secondary"
            :disabled="busy"
            @click="confirmOpen = true"
          >
            {{ t('employees.detail.deactivate') }}
            <template #icon-end><Trash2 class="h-4 w-4" aria-hidden="true" /></template>
          </Button>
          <Button v-else size="sm" variant="secondary" :disabled="busy" @click="reactivate">
            {{ t('employees.detail.reactivate') }}
          </Button>
        </template>
      </div>

      <div class="flex flex-col px-[22px] pb-10">
        <Text
          v-if="loadState === 'loading'"
          size="paragraph"
          role="status"
          class="py-8 text-center"
        >
          {{ t('ui.list.loading') }}
        </Text>
        <Text
          v-else-if="loadState === 'missing'"
          size="paragraph"
          role="alert"
          class="py-8 text-center"
        >
          {{ t('employees.detail.notFound') }}
        </Text>
        <Text
          v-else-if="loadState === 'failed'"
          size="paragraph"
          role="alert"
          class="py-8 text-center text-destructive"
        >
          {{ t('employees.detail.loadError') }}
        </Text>

        <template v-else>
          <div v-if="employee" class="flex items-center gap-6 py-2.5">
            <Avatar :name="displayName" :src="employee.profileImageUrl" size="lg" />
            <div class="flex min-w-0 flex-col gap-1">
              <Text as="p" size="h3" class="break-words">{{ displayName }}</Text>
              <Text as="p" size="notes" class="text-muted-foreground">
                {{ t(`employees.roles.${employee.rol}`) }}
                <template v-if="!employee.isActive"> · {{ t('employees.inactive') }}</template>
              </Text>
            </div>
          </div>

          <EmployeeForm
            v-if="creating"
            creating
            :busy="busy"
            :server-errors="serverErrors"
            @submit="save"
            @cancel="goBack"
          />

          <Tabs v-else v-model="tabModel" :tabs="tabs" :label="t('employees.tabs.label')">
            <template #data>
              <EmployeeForm
                :initial="formInitial"
                :busy="busy"
                :server-errors="serverErrors"
                @submit="save"
                @cancel="goBack"
              />
              <Button
                v-if="employee?.isActive"
                size="xs"
                variant="secondary"
                class="mt-6"
                :disabled="busy"
                @click="sendInvitation"
              >
                {{ t('employees.form.resendInvitation') }}
              </Button>
            </template>
            <template #services>
              <EmployeeServicesEditor
                v-model="assignedServices"
                :services="catalog"
                :busy="busy"
                @save="saveServices"
              />
            </template>
            <template #schedule>
              <ScheduleEditor v-model="schedule" :busy="busy" @save="saveSchedule" />
            </template>
            <template #absences>
              <AbsenceList
                :absences="absences"
                :busy="busy"
                @add="absenceOpen = true"
                @remove="removeAbsence"
              />
            </template>
          </Tabs>
        </template>
      </div>
    </main>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="t('employees.confirmDeactivate.title')"
      :description="t('employees.confirmDeactivate.description', { name: displayName })"
      :confirm-label="t('employees.confirmDeactivate.confirm')"
      :cancel-label="t('employees.confirmDeactivate.cancel')"
      :busy="busy"
      @confirm="deactivate"
    />
    <AbsenceDialog v-model:open="absenceOpen" :busy="busy" @confirm="saveAbsence" />
  </div>
</template>
