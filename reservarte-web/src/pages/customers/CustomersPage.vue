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
import { deactivateCustomer, getCustomers } from '@features/customers/api/customers.api';
import {
  CUSTOMER_CATEGORIES,
  type Customer,
  type CustomerCategory,
} from '@features/customers/types/customer.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Gestión de clientes (RA-869d7fc34), con el patrón de Empleados (Figma «CRUD»
 * 387:56720 con la foto a la izquierda): buscador, «Nuevo cliente», filtros de
 * estado y categoría, y editar, dar de baja y ver. Editar y ver abren la ficha.
 */

const { t } = useI18n();
const router = useRouter();
const ui = useUiStore();

type Status = 'active' | 'inactive';
type Filters = { status: Status; category: CustomerCategory | 'all' };

const list = useDataList<Customer, Filters>(
  ({ search, page, pageSize, filters }) =>
    getCustomers({
      search,
      page,
      pageSize,
      isActive: filters.status === 'active',
      category: filters.category === 'all' ? undefined : filters.category,
    }),
  { filters: { status: 'active', category: 'all' } }
);

const statusOptions = computed(() => [
  { value: 'active', label: t('customers.filter.active') },
  { value: 'inactive', label: t('customers.filter.inactive') },
]);
const status = computed({
  get: () => list.filters.value.status,
  set: (value: string | undefined) => {
    list.filters.value = {
      ...list.filters.value,
      status: value === 'inactive' ? 'inactive' : 'active',
    };
  },
});
const categoryOptions = computed(() => [
  { value: 'all', label: t('customers.filter.all') },
  ...CUSTOMER_CATEGORIES.map((value) => ({ value, label: t(`customers.categories.${value}`) })),
]);
const category = computed({
  get: () => list.filters.value.category,
  set: (value: string | undefined) => {
    list.filters.value = {
      ...list.filters.value,
      category: (value as CustomerCategory | 'all' | undefined) ?? 'all',
    };
  },
});

const detail = (customer: Customer) =>
  [
    t(`customers.categories.${customer.category}`),
    customer.isBlocked ? t('customers.blocked') : null,
    customer.isActive ? null : t('customers.inactive'),
  ]
    .filter(Boolean)
    .join(' · ');

function open(customer: Customer) {
  void router.push({ name: 'customer-detail', params: { id: String(customer.id) } });
}

// ── Baja ──────────────────────────────────────────────────────────────────
const confirmOpen = ref(false);
const target = ref<Customer | null>(null);
const busy = ref(false);

function askDeactivate(customer: Customer) {
  target.value = customer;
  confirmOpen.value = true;
}

async function deactivate() {
  if (!target.value) return;
  busy.value = true;
  try {
    await deactivateCustomer(target.value.id);
    ui.addToast(t('customers.done.deactivated', { name: target.value.fullName }), 'success');
    confirmOpen.value = false;
    void list.reload();
  } catch (err) {
    const forbidden = err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN';
    ui.addToast(forbidden ? err.message : t('customers.errors.save'), 'error');
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="w-full">
      <DataList
        v-model:search="list.search.value"
        :title="t('customers.title')"
        :items="list.items.value"
        :item-key="(c) => c.id"
        :item-label="(c) => c.fullName"
        :item-photo="(c) => c.profileImageUrl"
        :item-detail="detail"
        :item-deletable="(c) => c.isActive"
        :pagination="list.pagination.value"
        :loading="list.loading.value"
        :failed="!!list.error.value"
        :create-label="t('customers.new')"
        :empty-message="t('customers.empty')"
        @back="router.push({ name: 'account' })"
        @create="router.push({ name: 'customer-new' })"
        @edit="open"
        @view="open"
        @delete="askDeactivate"
        @page="list.goToPage"
        @retry="list.reload"
      >
        <template #filters>
          <div class="flex w-full gap-4">
            <div class="flex min-w-0 flex-1 flex-col gap-1">
              <label for="customers-status" class="font-sans text-[14px] text-foreground">
                {{ t('customers.filter.status') }}
              </label>
              <Select id="customers-status" v-model="status" :options="statusOptions" />
            </div>
            <div class="flex min-w-0 flex-1 flex-col gap-1">
              <label for="customers-category" class="font-sans text-[14px] text-foreground">
                {{ t('customers.filter.category') }}
              </label>
              <Select id="customers-category" v-model="category" :options="categoryOptions" />
            </div>
          </div>
        </template>
      </DataList>
    </main>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="t('customers.confirmDeactivate.title')"
      :description="t('customers.confirmDeactivate.description', { name: target?.fullName ?? '' })"
      :confirm-label="t('customers.confirmDeactivate.confirm')"
      :cancel-label="t('customers.confirmDeactivate.cancel')"
      :busy="busy"
      @confirm="deactivate"
    />
  </div>
</template>
