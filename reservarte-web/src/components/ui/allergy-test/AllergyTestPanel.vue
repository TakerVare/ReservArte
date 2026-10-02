<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Badge } from '@components/ui/badge';
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';
import { Input } from '@components/ui/input';
import { Text } from '@components/ui/text';
import type { CustomerAllergy } from '@features/customers/types/customer.types';
import { centerToUtc, utcToCenter } from '@features/employees/utils/absence-dates';
import { formatDateSpain } from '@lib/utils/date.utils';

/**
 * Prueba de alergia y alergias conocidas de una clienta (RA-869d7fc34): la última
 * prueba en hora del centro y «Registrar prueba» con día y hora (por defecto, ahora).
 * Emite `record` con el instante en UTC. Las alergias se muestran sin editar: la API
 * todavía no tiene operación para ellas.
 */
const props = withDefaults(
  defineProps<{
    lastTestAt?: string | null;
    allergies: CustomerAllergy[];
    busy?: boolean;
    /** Para pruebas: el instante «ahora». */
    now?: () => Date;
  }>(),
  { lastTestAt: null, busy: false, now: () => new Date() }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{ record: [testedAt: string] }>();

const { t } = useI18n();

const date = ref('');
const time = ref('');
const error = ref<string | null>(null);

watch(open, (isOpen) => {
  if (!isOpen) return;
  const current = utcToCenter(props.now().toISOString());
  date.value = current.date;
  time.value = current.time;
  error.value = null;
});

const last = computed(() => {
  if (!props.lastTestAt) return null;
  const moment = utcToCenter(props.lastTestAt);
  const [y, m, d] = moment.date.split('-').map(Number) as [number, number, number];
  return `${formatDateSpain(new Date(y, m - 1, d))} ${moment.time}`;
});

const SEVERITY_BADGE = { low: 'default', medium: 'outline', high: 'destructive' } as const;

function submit() {
  if (!date.value || !time.value) {
    error.value = t('customers.allergy.required');
    return;
  }
  const testedAt = centerToUtc(`${date.value}T${time.value}`);
  if (Date.parse(testedAt) > props.now().getTime()) {
    error.value = t('customers.allergy.future');
    return;
  }
  error.value = null;
  emit('record', testedAt);
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
      <Text as="h2" size="h3">{{ t('customers.allergy.title') }}</Text>
      <Text size="notes" class="text-muted-foreground">{{ t('customers.allergy.hint') }}</Text>
    </div>

    <Text as="p" size="h4" data-testid="allergy-last">
      {{ last ? t('customers.allergy.last', { date: last }) : t('customers.allergy.never') }}
    </Text>

    <Button size="sm" variant="primary" class="self-start" :disabled="busy" @click="open = true">
      {{ t('customers.allergy.record') }}
    </Button>

    <div class="flex flex-col gap-3">
      <Text as="h3" size="h4">{{ t('customers.allergy.allergiesTitle') }}</Text>
      <Text v-if="allergies.length === 0" size="paragraph" class="text-muted-foreground">
        {{ t('customers.allergy.noAllergies') }}
      </Text>
      <ul v-else class="flex flex-col border-b border-border">
        <li
          v-for="allergy in allergies"
          :key="allergy.id"
          class="flex items-center justify-between gap-4 border-t border-border py-3"
        >
          <Text as="span" size="paragraph">{{ allergy.allergyDescription }}</Text>
          <Badge :variant="SEVERITY_BADGE[allergy.severity]">
            {{ t(`customers.allergy.severity.${allergy.severity}`) }}
          </Badge>
        </li>
      </ul>
    </div>

    <Dialog v-model:open="open" :title="t('customers.allergy.dialogTitle')">
      <form id="allergy-form" class="flex gap-4" novalidate @submit.prevent="submit">
        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <label for="allergy-date" class="font-sans text-[14px] text-foreground">
            {{ t('customers.allergy.date') }}
          </label>
          <Input id="allergy-date" v-model="date" type="date" class="px-2" :disabled="busy" />
        </div>
        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <label for="allergy-time" class="font-sans text-[14px] text-foreground">
            {{ t('customers.allergy.time') }}
          </label>
          <Input id="allergy-time" v-model="time" type="time" class="px-2" :disabled="busy" />
        </div>
      </form>
      <Text v-if="error" size="notes" class="text-destructive" role="alert">{{ error }}</Text>
      <template #footer>
        <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
          {{ t('customers.allergy.cancel') }}
        </Button>
        <Button size="sm" variant="primary" type="submit" form="allergy-form" :disabled="busy">
          {{ t('customers.allergy.save') }}
        </Button>
      </template>
    </Dialog>
  </div>
</template>
