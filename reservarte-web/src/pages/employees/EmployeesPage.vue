<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Banner } from '@components/ui/banner';
import { DataList } from '@components/ui/data-list';
import { Select } from '@components/ui/select';
import { ConfirmDialog } from '@components/ui/confirm-dialog';
import { useDataList } from '@lib/composables/useDataList';
import { ApiRequestError } from '@lib/api/request';
import { useUiStore } from '@stores/uiStore';
import { deactivateEmployee, getEmployees } from '@features/employees/api/employees.api';
import type { Employee } from '@features/employees/types/employee.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Gestión de empleados (RA-869d7fbyt), según Figma «CRUD» (387:56720) con la foto a
 * la izquierda de cada nombre: buscador, «Nuevo empleado», filtro de activos o de
 * baja, y editar, dar de baja y ver. Editar y ver abren la ficha.
 */

const { t } = useI18n();
const router = useRouter();
const ui = useUiStore();

type Status = 'active' | 'inactive';

const list = useDataList<Employee, { status: Status }>(
  ({ search, page, pageSize, filters }) =>
    getEmployees({ search, page, pageSize, isActive: filters.status === 'active' }),
  { filters: { status: 'active' } }
);

const statusOptions = computed(() => [
  { value: 'active', label: t('employees.filter.active') },
  { value: 'inactive', label: t('employees.filter.inactive') },
]);
const status = computed({
  get: () => list.filters.value.status,
  set: (value: string | undefined) => {
    list.filters.value = { status: value === 'inactive' ? 'inactive' : 'active' };
  },
});

const detail = (employee: Employee) =>
  employee.isActive
    ? t(`employees.roles.${employee.rol}`)
    : `${t(`employees.roles.${employee.rol}`)} · ${t('employees.inactive')}`;

function open(employee: Employee) {
  void router.push({ name: 'employee-detail', params: { id: String(employee.id) } });
}

// ── Baja ──────────────────────────────────────────────────────────────────
const confirmOpen = ref(false);
const target = ref<Employee | null>(null);
const busy = ref(false);

function askDeactivate(employee: Employee) {
  target.value = employee;
  confirmOpen.value = true;
}

async function deactivate() {
  if (!target.value) return;
  busy.value = true;
  try {
    await deactivateEmployee(target.value.id);
    ui.addToast(t('employees.done.deactivated', { name: target.value.fullName }), 'success');
    confirmOpen.value = false;
    void list.reload();
  } catch (err) {
    // 403 con motivo de negocio («No puedes darte de baja a ti mismo»): se enseña tal cual.
    const forbidden = err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN';
    ui.addToast(forbidden ? err.message : t('employees.errors.save'), 'error');
  } finally {
    busy.value = false;
  }
}

function goBack() {
  void router.push({ name: 'account' });
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="w-full">
      <DataList
        v-model:search="list.search.value"
        :title="t('employees.title')"
        :items="list.items.value"
        :item-key="(e) => e.id"
        :item-label="(e) => e.fullName"
        :item-photo="(e) => e.profileImageUrl"
        :item-detail="detail"
        :item-deletable="(e) => e.isActive"
        :pagination="list.pagination.value"
        :loading="list.loading.value"
        :failed="!!list.error.value"
        :create-label="t('employees.new')"
        :empty-message="t('employees.empty')"
        @back="goBack"
        @create="router.push({ name: 'employee-new' })"
        @edit="open"
        @view="open"
        @delete="askDeactivate"
        @page="list.goToPage"
        @retry="list.reload"
      >
        <template #filters>
          <div class="flex w-full flex-col gap-1">
            <label for="employees-status" class="font-sans text-[14px] text-foreground">
              {{ t('employees.filter.label') }}
            </label>
            <Select id="employees-status" v-model="status" :options="statusOptions" />
          </div>
        </template>
      </DataList>
    </main>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="t('employees.confirmDeactivate.title')"
      :description="t('employees.confirmDeactivate.description', { name: target?.fullName ?? '' })"
      :confirm-label="t('employees.confirmDeactivate.confirm')"
      :cancel-label="t('employees.confirmDeactivate.cancel')"
      :busy="busy"
      @confirm="deactivate"
    />
  </div>
</template>
