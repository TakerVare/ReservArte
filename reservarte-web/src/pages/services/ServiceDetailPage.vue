<script setup lang="ts">
import { computed, ref, shallowRef, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Trash2 } from 'lucide-vue-next';
import { Banner } from '@components/ui/banner';
import { PageTitle } from '@components/ui/page-title';
import { Button } from '@components/ui/button';
import { Tabs } from '@components/ui/tabs';
import { Text } from '@components/ui/text';
import { ConfirmDialog } from '@components/ui/confirm-dialog';
import { ServiceForm } from '@components/ui/service-form';
import { ServiceVariations } from '@components/ui/service-variations';
import { CategoryDialog } from '@components/ui/category-dialog';
import { ApiRequestError } from '@lib/api/request';
import { formatCurrencyEur } from '@lib/utils/currency.utils';
import { useUiStore } from '@stores/uiStore';
import {
  addVariation,
  createCategory,
  createService,
  deactivateService,
  deleteVariation,
  getCategories,
  getService,
  reactivateService,
  updateService,
  updateVariation,
} from '@features/services/api/services.api';
import type {
  ServiceCategory,
  ServiceDetail,
  ServiceInput,
  ServiceVariation,
  VariationInput,
} from '@features/services/types/service.types';
import type { ServiceFormValues } from '@features/services/validation/service.schema';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Ficha de servicio (RA-869d7fc6b), con el patrón de las fichas de la app: banda con
 * el título, «Volver» y «Dar de baja», y dos pestañas, Datos y Variaciones (esta solo
 * al editar). La pestaña va en la URL (`?tab=`). Las tarifas por nivel de empleada no
 * se gestionan aquí: el piloto no las aplica.
 */

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const ui = useUiStore();

type Tab = 'data' | 'variations';
const TABS: Tab[] = ['data', 'variations'];

const creating = computed(() => route.name === 'service-new');
const serviceId = computed(() => (creating.value ? null : Number(route.params.id)));

const service = shallowRef<ServiceDetail | null>(null);
const categories = shallowRef<ServiceCategory[]>([]);
const loadState = ref<'loading' | 'ready' | 'missing' | 'failed'>('loading');
const busy = ref(false);

const tabs = computed(() => TABS.map((value) => ({ value, label: t(`services.tabs.${value}`) })));
const tabModel = computed({
  get: () => (TABS.includes(route.query.tab as Tab) ? (route.query.tab as Tab) : 'data'),
  set: (value: string | undefined) => {
    const next = TABS.includes(value as Tab) ? (value as Tab) : 'data';
    void router.replace({ query: { ...route.query, tab: next === 'data' ? undefined : next } });
  },
});

async function load() {
  loadState.value = 'loading';
  try {
    const [found, all] = await Promise.all([
      serviceId.value === null ? Promise.resolve(null) : getService(serviceId.value),
      getCategories(),
    ]);
    service.value = found;
    categories.value = all;
    loadState.value = 'ready';
  } catch (err) {
    loadState.value =
      err instanceof ApiRequestError && err.code === 'GEN_NOT_FOUND' ? 'missing' : 'failed';
  }
}

// La clave es texto: un array nuevo en cada lectura dispararía también con `?tab=`.
watch(
  () => `${String(route.name)}:${String(route.params.id ?? '')}`,
  () => void load(),
  { immediate: true }
);

async function refresh() {
  if (serviceId.value === null) return;
  service.value = await getService(serviceId.value);
}

// ── Datos ─────────────────────────────────────────────────────────────────
const formRef = ref<InstanceType<typeof ServiceForm> | null>(null);
const formInitial = computed<Partial<ServiceFormValues> | undefined>(() =>
  service.value
    ? {
        name: service.value.name,
        description: service.value.description ?? '',
        categoryId: service.value.categoryId ? String(service.value.categoryId) : 'none',
        durationMinutes: service.value.durationMinutes,
        basePrice: service.value.basePrice,
        requiresAllergyTest: service.value.requiresAllergyTest,
        allergyTestHoursBefore: service.value.allergyTestHoursBefore,
        isActive: service.value.isActive,
      }
    : undefined
);
const serverErrors = ref<Partial<Record<keyof ServiceFormValues, string>>>();

function toInput(values: ServiceFormValues): ServiceInput {
  return {
    name: values.name.trim(),
    description: values.description?.trim() || null,
    categoryId: values.categoryId === 'none' ? null : Number(values.categoryId),
    durationMinutes: values.durationMinutes,
    basePrice: values.basePrice,
    // La imagen no se edita aquí todavía (fotos, RA-869d7ee5t): se conserva.
    imageUrl: service.value?.imageUrl ?? null,
    requiresAllergyTest: values.requiresAllergyTest,
    // Sin prueba, la API no mira la antelación; se conserva la que hubiera.
    allergyTestHoursBefore: values.requiresAllergyTest
      ? values.allergyTestHoursBefore
      : (service.value?.allergyTestHoursBefore ?? 48),
  };
}

function showError(err: unknown, fallback: string) {
  if (err instanceof ApiRequestError) {
    if (err.code === 'GEN_VALIDATION_FAILED' && err.details.some((d) => d.field)) {
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
  ui.addToast(fallback, 'error');
}

async function save(values: ServiceFormValues) {
  busy.value = true;
  serverErrors.value = undefined;
  try {
    if (creating.value) {
      const created = await createService(toInput(values));
      ui.addToast(t('services.done.created'), 'success');
      await router.replace({ name: 'service-detail', params: { id: String(created.id) } });
      return;
    }
    const id = serviceId.value!;
    const saved = await updateService(id, toInput(values));
    if (values.isActive !== saved.isActive) {
      await (values.isActive ? reactivateService(id) : deactivateService(id));
    }
    await refresh();
    ui.addToast(t('services.done.updated'), 'success');
  } catch (err) {
    showError(err, t('services.errors.save'));
  } finally {
    busy.value = false;
  }
}

// ── Categoría nueva ───────────────────────────────────────────────────────
const categoryOpen = ref(false);

async function addCategory(name: string) {
  busy.value = true;
  try {
    const created = await createCategory(name);
    categories.value = [...categories.value, created];
    formRef.value?.selectCategory(created.id);
    categoryOpen.value = false;
    ui.addToast(t('services.category.created', { name: created.name }), 'success');
  } catch (err) {
    const forbidden = err instanceof ApiRequestError && err.code === 'GEN_FORBIDDEN';
    ui.addToast(forbidden ? err.message : t('services.category.error'), 'error');
  } finally {
    busy.value = false;
  }
}

// ── Baja y reactivación ───────────────────────────────────────────────────
const confirmOpen = ref(false);

async function setActive(active: boolean) {
  if (!service.value) return;
  busy.value = true;
  try {
    const saved = active
      ? await reactivateService(service.value.id)
      : await deactivateService(service.value.id);
    confirmOpen.value = false;
    await refresh();
    ui.addToast(
      t(active ? 'services.done.reactivated' : 'services.done.deactivated', { name: saved.name }),
      'success'
    );
  } catch (err) {
    showError(err, t('services.errors.save'));
  } finally {
    busy.value = false;
  }
}

// ── Variaciones ───────────────────────────────────────────────────────────
const variationOpen = ref(false);
const variationError = ref<string | null>(null);

watch(variationOpen, (isOpen) => {
  if (isOpen) variationError.value = null;
});

async function saveVariation(input: VariationInput, variationId: number | null) {
  if (serviceId.value === null) return;
  busy.value = true;
  variationError.value = null;
  try {
    await (variationId === null
      ? addVariation(serviceId.value, input)
      : updateVariation(serviceId.value, variationId, input));
    variationOpen.value = false;
    await refresh();
    ui.addToast(t('services.variations.saved'), 'success');
  } catch (err) {
    // El 400 de la API (p. ej., la duración resultante) se enseña dentro del diálogo.
    if (err instanceof ApiRequestError && err.code === 'GEN_VALIDATION_FAILED') {
      variationError.value = err.details[0]?.message ?? err.message;
    } else {
      showError(err, t('services.variations.error'));
    }
  } finally {
    busy.value = false;
  }
}

async function removeVariation(variation: ServiceVariation) {
  if (serviceId.value === null) return;
  busy.value = true;
  try {
    await deleteVariation(serviceId.value, variation.id);
    await refresh();
    ui.addToast(t('services.variations.removed'), 'success');
  } catch (err) {
    showError(err, t('services.variations.error'));
  } finally {
    busy.value = false;
  }
}

function goBack() {
  void router.push({ name: 'services' });
}

const title = computed(() =>
  creating.value ? t('services.detail.newTitle') : t('services.detail.editTitle')
);
const summary = computed(() => {
  if (!service.value) return '';
  return [
    service.value.categoryName ?? t('services.noCategory'),
    t('services.summary', {
      minutes: service.value.durationMinutes,
      price: formatCurrencyEur(service.value.basePrice),
    }),
    service.value.isActive ? null : t('services.inactive'),
  ]
    .filter(Boolean)
    .join(' · ');
});
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex w-full max-w-[393px] flex-col md:max-w-[600px] xl:max-w-[800px]">
      <PageTitle :label="title" />

      <div class="flex items-center justify-between px-7 py-[18px]">
        <Button size="sm" variant="primary" @click="goBack">
          <template #icon-start><ArrowLeftIcon class="h-4 w-4" aria-hidden="true" /></template>
          {{ t('ui.list.back') }}
        </Button>
        <template v-if="service">
          <Button
            v-if="service.isActive"
            size="sm"
            variant="secondary"
            :disabled="busy"
            @click="confirmOpen = true"
          >
            {{ t('services.detail.deactivate') }}
            <template #icon-end><Trash2 class="h-4 w-4" aria-hidden="true" /></template>
          </Button>
          <Button v-else size="sm" variant="secondary" :disabled="busy" @click="setActive(true)">
            {{ t('services.detail.reactivate') }}
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
          {{ t('services.detail.notFound') }}
        </Text>
        <Text
          v-else-if="loadState === 'failed'"
          size="paragraph"
          role="alert"
          class="py-8 text-center text-destructive"
        >
          {{ t('services.detail.loadError') }}
        </Text>

        <template v-else>
          <div v-if="service" class="flex flex-col gap-1 py-2.5">
            <Text as="p" size="h3" class="break-words">{{ service.name }}</Text>
            <Text as="p" size="notes" class="text-muted-foreground">{{ summary }}</Text>
          </div>

          <ServiceForm
            v-if="creating"
            ref="formRef"
            creating
            :categories="categories"
            :busy="busy"
            :server-errors="serverErrors"
            @submit="save"
            @cancel="goBack"
            @new-category="categoryOpen = true"
          />

          <Tabs v-else v-model="tabModel" :tabs="tabs" :label="t('services.tabs.label')">
            <template #data>
              <ServiceForm
                ref="formRef"
                :initial="formInitial"
                :categories="categories"
                :busy="busy"
                :server-errors="serverErrors"
                @submit="save"
                @cancel="goBack"
                @new-category="categoryOpen = true"
              />
            </template>
            <template #variations>
              <ServiceVariations
                v-if="service"
                v-model:open="variationOpen"
                :variations="service.variations"
                :base-price="service.basePrice"
                :duration-minutes="service.durationMinutes"
                :busy="busy"
                :error="variationError"
                @save="saveVariation"
                @remove="removeVariation"
              />
            </template>
          </Tabs>
        </template>
      </div>
    </main>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="t('services.confirmDeactivate.title')"
      :description="t('services.confirmDeactivate.description', { name: service?.name ?? '' })"
      :confirm-label="t('services.confirmDeactivate.confirm')"
      :cancel-label="t('services.confirmDeactivate.cancel')"
      :busy="busy"
      @confirm="setActive(false)"
    />
    <CategoryDialog v-model:open="categoryOpen" :busy="busy" @create="addCategory" />
  </div>
</template>
