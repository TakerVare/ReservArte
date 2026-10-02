<script setup lang="ts">
import { computed, onMounted, ref, shallowRef } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Plus } from 'lucide-vue-next';
import { Banner } from '@components/ui/banner';
import { DataList } from '@components/ui/data-list';
import { Select } from '@components/ui/select';
import { ConfirmDialog } from '@components/ui/confirm-dialog';
import { useDataList } from '@lib/composables/useDataList';
import { ApiRequestError } from '@lib/api/request';
import { formatCurrencyEur } from '@lib/utils/currency.utils';
import { useUiStore } from '@stores/uiStore';
import { deactivateService, getCategories, getServices } from '@features/services/api/services.api';
import type { Service, ServiceCategory } from '@features/services/types/service.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Gestión de servicios (RA-869d7fc6b), con el patrón de los listados de gestión
 * (Figma «CRUD» 387:56720): buscador, «Nuevo servicio», filtros de estado y
 * categoría, y editar, dar de baja y ver. En cada fila, categoría, duración y precio.
 */

const { t } = useI18n();
const router = useRouter();
const ui = useUiStore();

type Status = 'active' | 'inactive';
type Filters = { status: Status; categoryId: string };
const ALL = 'all';

const list = useDataList<Service, Filters>(
  ({ search, page, pageSize, filters }) =>
    getServices({
      search,
      page,
      pageSize,
      isActive: filters.status === 'active',
      categoryId: filters.categoryId === ALL ? undefined : Number(filters.categoryId),
    }),
  { filters: { status: 'active', categoryId: ALL } }
);

const categories = shallowRef<ServiceCategory[]>([]);
onMounted(async () => {
  try {
    categories.value = await getCategories();
  } catch {
    // Sin categorías el filtro solo ofrece «Todas»; la lista sigue funcionando.
  }
});

const statusOptions = computed(() => [
  { value: 'active', label: t('services.filter.active') },
  { value: 'inactive', label: t('services.filter.inactive') },
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
  { value: ALL, label: t('services.filter.all') },
  ...categories.value
    .filter((c) => c.isActive)
    .map((c) => ({ value: String(c.id), label: c.name })),
]);
const category = computed({
  get: () => list.filters.value.categoryId,
  set: (value: string | undefined) => {
    list.filters.value = { ...list.filters.value, categoryId: value ?? ALL };
  },
});

const detail = (service: Service) =>
  [
    service.categoryName ?? t('services.noCategory'),
    t('services.summary', {
      minutes: service.durationMinutes,
      price: formatCurrencyEur(service.basePrice),
    }),
    service.isActive ? null : t('services.inactive'),
  ]
    .filter(Boolean)
    .join(' · ');

function open(service: Service) {
  void router.push({ name: 'service-detail', params: { id: String(service.id) } });
}

// ── Baja ──────────────────────────────────────────────────────────────────
const confirmOpen = ref(false);
const target = ref<Service | null>(null);
const busy = ref(false);

function askDeactivate(service: Service) {
  target.value = service;
  confirmOpen.value = true;
}

async function deactivate() {
  if (!target.value) return;
  busy.value = true;
  try {
    await deactivateService(target.value.id);
    ui.addToast(t('services.done.deactivated', { name: target.value.name }), 'success');
    confirmOpen.value = false;
    void list.reload();
  } catch (err) {
    const forbidden = err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN';
    ui.addToast(forbidden ? err.message : t('services.errors.save'), 'error');
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
        :title="t('services.title')"
        :items="list.items.value"
        :item-key="(s) => s.id"
        :item-label="(s) => s.name"
        :item-detail="detail"
        :item-deletable="(s) => s.isActive"
        :pagination="list.pagination.value"
        :loading="list.loading.value"
        :failed="!!list.error.value"
        :create-label="t('services.new')"
        :create-icon="Plus"
        :empty-message="t('services.empty')"
        @back="router.push({ name: 'account' })"
        @create="router.push({ name: 'service-new' })"
        @edit="open"
        @view="open"
        @delete="askDeactivate"
        @page="list.goToPage"
        @retry="list.reload"
      >
        <template #filters>
          <div class="flex w-full gap-4">
            <div class="flex min-w-0 flex-1 flex-col gap-1">
              <label for="services-status" class="font-sans text-[14px] text-foreground">
                {{ t('services.filter.status') }}
              </label>
              <Select id="services-status" v-model="status" :options="statusOptions" />
            </div>
            <div class="flex min-w-0 flex-1 flex-col gap-1">
              <label for="services-category" class="font-sans text-[14px] text-foreground">
                {{ t('services.filter.category') }}
              </label>
              <Select id="services-category" v-model="category" :options="categoryOptions" />
            </div>
          </div>
        </template>
      </DataList>
    </main>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="t('services.confirmDeactivate.title')"
      :description="t('services.confirmDeactivate.description', { name: target?.name ?? '' })"
      :confirm-label="t('services.confirmDeactivate.confirm')"
      :cancel-label="t('services.confirmDeactivate.cancel')"
      :busy="busy"
      @confirm="deactivate"
    />
  </div>
</template>
