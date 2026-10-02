<script setup lang="ts">
import { computed, ref, shallowRef, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Trash2 } from 'lucide-vue-next';
import { Banner } from '@components/ui/banner';
import { PageTitle } from '@components/ui/page-title';
import { Button } from '@components/ui/button';
import { Avatar } from '@components/ui/avatar';
import { Tabs } from '@components/ui/tabs';
import { Text } from '@components/ui/text';
import { ConfirmDialog } from '@components/ui/confirm-dialog';
import { CustomerForm } from '@components/ui/customer-form';
import { CustomerNotes } from '@components/ui/customer-notes';
import { AllergyTestPanel } from '@components/ui/allergy-test';
import { CustomerHistory } from '@components/ui/customer-history';
import { CustomerConsents } from '@components/ui/customer-consents';
import { CustomerAllergies } from '@components/ui/customer-allergies';
import { CustomerBlock } from '@components/ui/customer-block';
import { ApiRequestError } from '@lib/api/request';
import { useUiStore } from '@stores/uiStore';
import {
  addAllergy,
  addCustomerNote,
  blockCustomer,
  deleteAllergy,
  setConsent,
  unblockCustomer,
  updateAllergy,
  type AllergyInput,
  createCustomer,
  deactivateCustomer,
  deleteCustomerNote,
  getCustomer,
  getCustomerHistory,
  reactivateCustomer,
  recordAllergyTest,
  updateCustomer,
} from '@features/customers/api/customers.api';
import type {
  ConsentType,
  CustomerAllergy,
  CustomerDetail,
  CustomerInput,
  CustomerNote,
} from '@features/customers/types/customer.types';
import type { CustomerFormValues } from '@features/customers/validation/customer.schema';
import type { AppointmentDetail } from '@features/appointments/types/appointment.types';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Ficha de cliente (RA-869d7fc34 + RA-869d7fc51), con el patrón de la ficha de
 * empleado (Figma «Detalle usuario» 387:56778): banda con el título, «Volver» y
 * «Dar de baja», foto y «Datos de usuario». Al editar, Datos lleva además los
 * consentimientos y el bloqueo (4.2b), y hay tres pestañas más: notas del personal,
 * prueba de alergia y alergias, e historial de citas. La pestaña va en la URL
 * (`?tab=`), como en la de empleado.
 */

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const ui = useUiStore();

type Tab = 'data' | 'notes' | 'allergy' | 'history';
const TABS: Tab[] = ['data', 'notes', 'allergy', 'history'];

const creating = computed(() => route.name === 'customer-new');
const customerId = computed(() => (creating.value ? null : Number(route.params.id)));

const customer = shallowRef<CustomerDetail | null>(null);
const loadState = ref<'loading' | 'ready' | 'missing' | 'failed'>('loading');
const busy = ref(false);

const tabs = computed(() => TABS.map((value) => ({ value, label: t(`customers.tabs.${value}`) })));
const tabModel = computed({
  get: () => (TABS.includes(route.query.tab as Tab) ? (route.query.tab as Tab) : 'data'),
  set: (value: string | undefined) => {
    const next = TABS.includes(value as Tab) ? (value as Tab) : 'data';
    void router.replace({ query: { ...route.query, tab: next === 'data' ? undefined : next } });
  },
});

// ── Historial ─────────────────────────────────────────────────────────────
const history = shallowRef<AppointmentDetail[]>([]);
const historyPage = ref(1);
const historyTotalPages = ref(0);
const historyLoading = ref(false);
const historyFailed = ref(false);

async function loadHistory(page: number) {
  if (customerId.value === null) return;
  historyLoading.value = true;
  historyFailed.value = false;
  try {
    const result = await getCustomerHistory(customerId.value, page);
    history.value = page === 1 ? result.items : [...history.value, ...result.items];
    historyPage.value = result.pagination.page;
    historyTotalPages.value = result.pagination.totalPages;
  } catch {
    historyFailed.value = true;
  } finally {
    historyLoading.value = false;
  }
}

// ── Carga ─────────────────────────────────────────────────────────────────
async function load() {
  history.value = [];
  if (customerId.value === null) {
    customer.value = null;
    loadState.value = 'ready';
    return;
  }
  loadState.value = 'loading';
  try {
    customer.value = await getCustomer(customerId.value);
    loadState.value = 'ready';
    void loadHistory(1);
  } catch (err) {
    loadState.value =
      err instanceof ApiRequestError && err.code === 'GEN_NOT_FOUND' ? 'missing' : 'failed';
  }
}

// Solo al cambiar de ficha (o del alta a la ficha nueva), no al cambiar de pestaña.
// La clave es texto: un array nuevo en cada lectura dispararía también con `?tab=`.
watch(
  () => `${String(route.name)}:${String(route.params.id ?? '')}`,
  () => void load(),
  { immediate: true }
);

/** Recarga el perfil sin pasar por «Cargando…» (tras guardar algo). */
async function refresh() {
  if (customerId.value === null) return;
  customer.value = await getCustomer(customerId.value);
}

// ── Datos ─────────────────────────────────────────────────────────────────
const formInitial = computed<Partial<CustomerFormValues> | undefined>(() =>
  customer.value
    ? {
        firstName: customer.value.firstName,
        lastName: customer.value.lastName,
        email: customer.value.email,
        phone: customer.value.phone ?? '',
        birthDate: customer.value.birthDate ?? '',
        category: customer.value.category,
        preferredContactMethod: customer.value.preferredContactMethod,
        isActive: customer.value.isActive,
      }
    : undefined
);
const serverErrors = ref<Partial<Record<keyof CustomerFormValues, string>>>();

function toInput(values: CustomerFormValues): CustomerInput {
  return {
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    email: values.email.trim(),
    phone: values.phone?.trim() || null,
    birthDate: values.birthDate || null,
    category: values.category,
    preferredContactMethod: values.preferredContactMethod,
    // La foto no se edita aquí todavía (RA-869faz11u): se conserva.
    profileImageUrl: customer.value?.profileImageUrl ?? null,
  };
}

function grantedConsents(values: CustomerFormValues): ConsentType[] {
  const consents: [boolean, ConsentType][] = [
    [values.consentDataProcessing, 'data_processing'],
    [values.consentMarketing, 'marketing'],
    [values.consentPhotos, 'photos'],
    [values.consentWhatsapp, 'whatsapp'],
  ];
  return consents.filter(([granted]) => granted).map(([, type]) => type);
}

function showError(err: unknown, fallback: string) {
  if (err instanceof ApiRequestError) {
    if (err.code === 'GEN_CONFLICT') {
      serverErrors.value = { email: t('customers.errors.emailTaken') };
      return;
    }
    if (err.code === 'GEN_VALIDATION_FAILED' && err.details.some((d) => d.field)) {
      serverErrors.value = Object.fromEntries(
        err.details.filter((d) => d.field).map((d) => [d.field, d.message])
      );
      return;
    }
    // 403 con motivo de negocio (cambiar el email de una cuenta de personal, una
    // empleada que no puede editar…): se enseña tal cual.
    if (err.code === 'GEN_FORBIDDEN') {
      ui.addToast(err.message, 'error');
      return;
    }
  }
  ui.addToast(fallback, 'error');
}

async function save(values: CustomerFormValues) {
  busy.value = true;
  serverErrors.value = undefined;
  try {
    if (creating.value) {
      const created = await createCustomer({
        ...toInput(values),
        grantedConsents: grantedConsents(values),
      });
      ui.addToast(t('customers.done.created'), 'success');
      await router.replace({ name: 'customer-detail', params: { id: String(created.id) } });
      return;
    }
    const id = customerId.value!;
    const saved = await updateCustomer(id, toInput(values));
    if (values.isActive !== saved.isActive) {
      await (values.isActive ? reactivateCustomer(id) : deactivateCustomer(id));
    }
    await refresh();
    ui.addToast(t('customers.done.updated'), 'success');
  } catch (err) {
    showError(err, t('customers.errors.save'));
  } finally {
    busy.value = false;
  }
}

// ── Baja y reactivación ───────────────────────────────────────────────────
const confirmOpen = ref(false);

async function setActive(active: boolean) {
  if (!customer.value) return;
  busy.value = true;
  try {
    const saved = active
      ? await reactivateCustomer(customer.value.id)
      : await deactivateCustomer(customer.value.id);
    confirmOpen.value = false;
    await refresh();
    ui.addToast(
      t(active ? 'customers.done.reactivated' : 'customers.done.deactivated', {
        name: saved.fullName,
      }),
      'success'
    );
  } catch (err) {
    showError(err, t('customers.errors.save'));
  } finally {
    busy.value = false;
  }
}

// ── Notas ─────────────────────────────────────────────────────────────────
const notesRef = ref<InstanceType<typeof CustomerNotes> | null>(null);

async function addNote(note: string) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    await addCustomerNote(customerId.value, note);
    notesRef.value?.clear();
    await refresh();
    ui.addToast(t('customers.notes.added'), 'success');
  } catch (err) {
    showError(err, t('customers.notes.error'));
  } finally {
    busy.value = false;
  }
}

async function removeNote(note: CustomerNote) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    await deleteCustomerNote(customerId.value, note.id);
    await refresh();
    ui.addToast(t('customers.notes.removed'), 'success');
  } catch (err) {
    showError(err, t('customers.notes.error'));
  } finally {
    busy.value = false;
  }
}

// ── Prueba de alergia ─────────────────────────────────────────────────────
const allergyOpen = ref(false);

async function recordTest(testedAt: string) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    await recordAllergyTest(customerId.value, testedAt);
    allergyOpen.value = false;
    await refresh();
    ui.addToast(t('customers.allergy.saved'), 'success');
  } catch (err) {
    showError(err, t('customers.allergy.error'));
  } finally {
    busy.value = false;
  }
}

// ── Consentimientos (4.2b) ─────────────────────────────────────────────────
const confirmDataOpen = ref(false);

function changeConsent(consentType: ConsentType, granted: boolean) {
  // H-47: retirar el de datos da de baja la ficha; se confirma antes.
  if (consentType === 'data_processing' && !granted) {
    confirmDataOpen.value = true;
    return;
  }
  void applyConsent(consentType, granted);
}

async function applyConsent(consentType: ConsentType, granted: boolean) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    const wasActive = customer.value?.isActive;
    customer.value = await setConsent(customerId.value, consentType, granted);
    confirmDataOpen.value = false;
    ui.addToast(
      t(
        wasActive && !customer.value.isActive
          ? 'customers.form.consents.deactivated'
          : 'customers.form.consents.saved'
      ),
      'success'
    );
  } catch (err) {
    showError(err, t('customers.form.consents.error'));
  } finally {
    busy.value = false;
  }
}

// ── Alergias (4.2b) ───────────────────────────────────────────────────────
const allergyEditOpen = ref(false);

async function saveAllergy(input: AllergyInput, allergyId: number | null) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    await (allergyId === null
      ? addAllergy(customerId.value, input)
      : updateAllergy(customerId.value, allergyId, input));
    allergyEditOpen.value = false;
    await refresh();
    ui.addToast(t('customers.allergy.allergySaved'), 'success');
  } catch (err) {
    showError(err, t('customers.allergy.allergyError'));
  } finally {
    busy.value = false;
  }
}

async function removeAllergy(allergy: CustomerAllergy) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    await deleteAllergy(customerId.value, allergy.id);
    await refresh();
    ui.addToast(t('customers.allergy.allergyRemoved'), 'success');
  } catch (err) {
    showError(err, t('customers.allergy.allergyError'));
  } finally {
    busy.value = false;
  }
}

// ── Bloqueo (4.2b) ────────────────────────────────────────────────────────
const blockOpen = ref(false);

async function setBlocked(reason: string | null) {
  if (customerId.value === null) return;
  busy.value = true;
  try {
    const saved =
      reason === null
        ? await unblockCustomer(customerId.value)
        : await blockCustomer(customerId.value, reason);
    blockOpen.value = false;
    await refresh();
    ui.addToast(
      t(reason === null ? 'customers.block.unblocked' : 'customers.block.blocked', {
        name: saved.fullName,
      }),
      'success'
    );
  } catch (err) {
    showError(err, t('customers.block.error'));
  } finally {
    busy.value = false;
  }
}

function goBack() {
  void router.push({ name: 'customers' });
}

const title = computed(() =>
  creating.value ? t('customers.detail.newTitle') : t('customers.detail.editTitle')
);
const displayName = computed(() => customer.value?.fullName ?? '');
const statusLine = computed(() => {
  if (!customer.value) return '';
  return [
    t(`customers.categories.${customer.value.category}`),
    customer.value.isBlocked ? t('customers.blocked') : null,
    customer.value.isActive ? null : t('customers.inactive'),
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
        <template v-if="customer">
          <Button
            v-if="customer.isActive"
            size="sm"
            variant="secondary"
            :disabled="busy"
            @click="confirmOpen = true"
          >
            {{ t('customers.detail.deactivate') }}
            <template #icon-end><Trash2 class="h-4 w-4" aria-hidden="true" /></template>
          </Button>
          <Button v-else size="sm" variant="secondary" :disabled="busy" @click="setActive(true)">
            {{ t('customers.detail.reactivate') }}
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
          {{ t('customers.detail.notFound') }}
        </Text>
        <Text
          v-else-if="loadState === 'failed'"
          size="paragraph"
          role="alert"
          class="py-8 text-center text-destructive"
        >
          {{ t('customers.detail.loadError') }}
        </Text>

        <template v-else>
          <div v-if="customer" class="flex items-center gap-6 py-2.5">
            <Avatar :name="displayName" :src="customer.profileImageUrl" size="lg" />
            <div class="flex min-w-0 flex-col gap-1">
              <Text as="p" size="h3" class="break-words">{{ displayName }}</Text>
              <Text as="p" size="notes" class="text-muted-foreground">{{ statusLine }}</Text>
              <Text
                v-if="customer.isBlocked && customer.blockedReason"
                as="p"
                size="notes"
                class="text-destructive"
              >
                {{ t('customers.detail.blockedReason', { reason: customer.blockedReason }) }}
              </Text>
            </div>
          </div>

          <CustomerForm
            v-if="creating"
            creating
            :busy="busy"
            :server-errors="serverErrors"
            @submit="save"
            @cancel="goBack"
          />

          <Tabs v-else v-model="tabModel" :tabs="tabs" :label="t('customers.tabs.label')">
            <template #data>
              <CustomerForm
                :initial="formInitial"
                :busy="busy"
                :server-errors="serverErrors"
                @submit="save"
                @cancel="goBack"
              />
              <div v-if="customer" class="mt-10 flex flex-col gap-10">
                <CustomerConsents
                  :consents="customer.consents"
                  :busy="busy"
                  @change="changeConsent"
                />
                <CustomerBlock
                  v-model:open="blockOpen"
                  :name="customer.fullName"
                  :blocked="customer.isBlocked"
                  :reason="customer.blockedReason"
                  :busy="busy"
                  @block="setBlocked"
                  @unblock="setBlocked(null)"
                />
              </div>
            </template>
            <template #notes>
              <CustomerNotes
                ref="notesRef"
                :notes="customer?.notes ?? []"
                :busy="busy"
                @add="addNote"
                @remove="removeNote"
              />
            </template>
            <template #allergy>
              <div class="flex flex-col gap-10">
                <AllergyTestPanel
                  v-model:open="allergyOpen"
                  :last-test-at="customer?.lastAllergyTestAt"
                  :busy="busy"
                  @record="recordTest"
                />
                <CustomerAllergies
                  v-model:open="allergyEditOpen"
                  :allergies="customer?.allergies ?? []"
                  :busy="busy"
                  @save="saveAllergy"
                  @remove="removeAllergy"
                />
              </div>
            </template>
            <template #history>
              <CustomerHistory
                :appointments="history"
                :has-more="historyPage < historyTotalPages"
                :loading="historyLoading"
                :failed="historyFailed"
                @more="loadHistory(historyPage + 1)"
              />
            </template>
          </Tabs>
        </template>
      </div>
    </main>

    <ConfirmDialog
      v-model:open="confirmOpen"
      :title="t('customers.confirmDeactivate.title')"
      :description="t('customers.confirmDeactivate.description', { name: displayName })"
      :confirm-label="t('customers.confirmDeactivate.confirm')"
      :cancel-label="t('customers.confirmDeactivate.cancel')"
      :busy="busy"
      @confirm="setActive(false)"
    />
    <ConfirmDialog
      v-model:open="confirmDataOpen"
      :title="t('customers.form.consents.confirmDataTitle')"
      :description="t('customers.form.consents.confirmData', { name: displayName })"
      :confirm-label="t('customers.form.consents.confirmDataOk')"
      :cancel-label="t('customers.form.consents.confirmDataCancel')"
      :busy="busy"
      @confirm="applyConsent('data_processing', false)"
    />
  </div>
</template>
