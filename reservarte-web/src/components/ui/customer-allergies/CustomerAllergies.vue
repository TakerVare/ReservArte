<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Pencil, Trash2 } from 'lucide-vue-next';
import { Badge } from '@components/ui/badge';
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';
import { Input } from '@components/ui/input';
import { Select } from '@components/ui/select';
import { Text } from '@components/ui/text';
import {
  ALLERGY_SEVERITIES,
  type AllergySeverity,
  type CustomerAllergy,
} from '@features/customers/types/customer.types';
import type { AllergyInput } from '@features/customers/api/customers.api';

/**
 * Alergias conocidas de una clienta (4.2b): lista con su gravedad, y alta, edición y
 * baja con un diálogo. Presentacional: emite `save` (con el id si es una edición) y
 * `remove`; quien lo usa cierra el diálogo con `open` al guardar bien.
 */
withDefaults(defineProps<{ allergies: CustomerAllergy[]; busy?: boolean }>(), { busy: false });

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{
  save: [input: AllergyInput, allergyId: number | null];
  remove: [allergy: CustomerAllergy];
}>();

const { t } = useI18n();

const editing = ref<CustomerAllergy | null>(null);
const description = ref('');
const severity = ref<AllergySeverity>('medium');
const error = ref<string | null>(null);

watch(open, (isOpen) => {
  if (!isOpen) return;
  description.value = editing.value?.allergyDescription ?? '';
  severity.value = editing.value?.severity ?? 'medium';
  error.value = null;
});

function startAdd() {
  editing.value = null;
  open.value = true;
}

function startEdit(allergy: CustomerAllergy) {
  editing.value = allergy;
  open.value = true;
}

const severityOptions = computed(() =>
  ALLERGY_SEVERITIES.map((value) => ({ value, label: t(`customers.allergy.severity.${value}`) }))
);
const severityModel = computed({
  get: () => severity.value,
  set: (value: string | undefined) => {
    severity.value = (value as AllergySeverity) ?? 'medium';
  },
});

const SEVERITY_BADGE = { low: 'default', medium: 'outline', high: 'destructive' } as const;

function submit() {
  const text = description.value.trim();
  if (!text) {
    error.value = t('customers.allergy.descriptionRequired');
    return;
  }
  if (text.length > 500) {
    error.value = t('customers.allergy.descriptionTooLong');
    return;
  }
  error.value = null;
  emit('save', { allergyDescription: text, severity: severity.value }, editing.value?.id ?? null);
}

const iconButtonClasses =
  'flex h-11 w-11 shrink-0 items-center justify-center text-primary outline-none transition-colors hover:text-primary-hover focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50';
</script>

<template>
  <section class="flex flex-col gap-3">
    <Text as="h3" size="h4">{{ t('customers.allergy.allergiesTitle') }}</Text>
    <Text v-if="allergies.length === 0" size="paragraph" class="text-muted-foreground">
      {{ t('customers.allergy.noAllergies') }}
    </Text>
    <ul v-else class="flex flex-col border-b border-border" data-testid="customer-allergies">
      <li
        v-for="allergy in allergies"
        :key="allergy.id"
        class="flex items-center justify-between gap-2 border-t border-border py-2"
      >
        <div class="flex min-w-0 flex-col gap-1">
          <Text as="span" size="paragraph" class="break-words">{{
            allergy.allergyDescription
          }}</Text>
          <Badge :variant="SEVERITY_BADGE[allergy.severity]" class="self-start">
            {{ t(`customers.allergy.severity.${allergy.severity}`) }}
          </Badge>
        </div>
        <div class="flex shrink-0">
          <button
            type="button"
            :class="iconButtonClasses"
            :disabled="busy"
            :aria-label="t('customers.allergy.editAllergy', { name: allergy.allergyDescription })"
            @click="startEdit(allergy)"
          >
            <Pencil class="h-5 w-5" aria-hidden="true" />
          </button>
          <button
            type="button"
            :class="iconButtonClasses"
            :disabled="busy"
            :aria-label="t('customers.allergy.removeAllergy', { name: allergy.allergyDescription })"
            @click="emit('remove', allergy)"
          >
            <Trash2 class="h-5 w-5" aria-hidden="true" />
          </button>
        </div>
      </li>
    </ul>
    <Button size="sm" variant="secondary" class="self-start" :disabled="busy" @click="startAdd">
      {{ t('customers.allergy.addAllergy') }}
    </Button>

    <Dialog
      v-model:open="open"
      :title="
        editing ? t('customers.allergy.allergyDialogEdit') : t('customers.allergy.allergyDialogNew')
      "
    >
      <form id="allergy-edit-form" class="flex flex-col gap-4" novalidate @submit.prevent="submit">
        <div class="flex flex-col gap-1">
          <label for="allergy-description" class="font-sans text-[14px] text-foreground">
            {{ t('customers.allergy.description') }}
          </label>
          <Input
            id="allergy-description"
            v-model="description"
            maxlength="500"
            :invalid="!!error"
            :disabled="busy"
          />
        </div>
        <div class="flex flex-col gap-1">
          <label for="allergy-severity" class="font-sans text-[14px] text-foreground">
            {{ t('customers.allergy.severityLabel') }}
          </label>
          <Select
            id="allergy-severity"
            v-model="severityModel"
            :options="severityOptions"
            :disabled="busy"
          />
        </div>
        <Text v-if="error" size="notes" class="text-destructive" role="alert">{{ error }}</Text>
      </form>
      <template #footer>
        <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
          {{ t('customers.allergy.cancel') }}
        </Button>
        <Button size="sm" variant="primary" type="submit" form="allergy-edit-form" :disabled="busy">
          {{ t('customers.allergy.save') }}
        </Button>
      </template>
    </Dialog>
  </section>
</template>
