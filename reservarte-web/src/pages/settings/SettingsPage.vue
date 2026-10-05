<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Banner } from '@components/ui/banner';
import { PageTitle } from '@components/ui/page-title';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { SettingsForm } from '@components/ui/settings-form';
import { ApiRequestError } from '@lib/api/request';
import { useOrganizationStore } from '@stores/organizationStore';
import { useUiStore } from '@stores/uiStore';
import type { SettingsFormValues } from '@features/organization/validation/settings.schema';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Configuración del centro (RA-869f6r71x), para Admin y Manager: zona horaria,
 * umbral de cancelación y máximo de no presentaciones. Sin diseño propio en
 * Figma: el patrón de las fichas. Quién puede guardar lo decide la API (403).
 */

const { t } = useI18n();
const router = useRouter();
const ui = useUiStore();
const organization = useOrganizationStore();

const loadState = ref<'loading' | 'ready' | 'failed'>('loading');
const busy = ref(false);
const serverErrors = ref<Partial<Record<keyof SettingsFormValues, string>>>();

const initial = computed<SettingsFormValues | null>(() =>
  organization.settings
    ? {
        timeZone: organization.settings.timeZone,
        cancellationHoursThreshold: organization.settings.cancellationHoursThreshold,
        maxNoShowsBeforeBlock: organization.settings.maxNoShowsBeforeBlock,
      }
    : null
);

// Siempre se vuelve a pedir: otra persona del centro puede haberla cambiado.
async function load() {
  loadState.value = 'loading';
  try {
    await organization.load();
    loadState.value = 'ready';
  } catch {
    loadState.value = 'failed';
  }
}
onMounted(load);

async function save(values: SettingsFormValues) {
  busy.value = true;
  serverErrors.value = undefined;
  try {
    await organization.save(values);
    ui.addToast(t('settings.done'), 'success');
  } catch (err) {
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
    ui.addToast(t('settings.errors.save'), 'error');
  } finally {
    busy.value = false;
  }
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <main class="flex w-full max-w-[393px] flex-col md:max-w-[600px] xl:max-w-[800px]">
      <PageTitle :label="t('settings.title')" />

      <div class="flex items-center px-7 py-[18px]">
        <Button size="sm" variant="primary" @click="router.push({ name: 'account' })">
          <template #icon-start><ArrowLeftIcon class="h-4 w-4" aria-hidden="true" /></template>
          {{ t('ui.list.back') }}
        </Button>
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
        <div
          v-else-if="loadState === 'failed' || !initial"
          role="alert"
          class="flex flex-col items-center gap-4 py-8"
        >
          <Text size="paragraph" class="text-center">{{ t('settings.errors.load') }}</Text>
          <Button size="sm" variant="secondary" @click="load">{{ t('settings.retry') }}</Button>
        </div>
        <SettingsForm
          v-else
          :initial="initial"
          :busy="busy"
          :server-errors="serverErrors"
          @submit="save"
        />
      </div>
    </main>
  </div>
</template>
