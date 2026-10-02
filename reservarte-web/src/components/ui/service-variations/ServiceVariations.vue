<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Pencil, Trash2 } from 'lucide-vue-next';
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';
import { Input } from '@components/ui/input';
import { Text } from '@components/ui/text';
import { formatCurrencyEur } from '@lib/utils/currency.utils';
import type { ServiceVariation, VariationInput } from '@features/services/types/service.types';

/**
 * Variaciones de un servicio (RA-869d7fc6b): cada una con su ajuste de precio y
 * duración y el total resultante, que es lo que cobra y ocupa la cita. Alta, edición
 * y baja con un diálogo. Presentacional: emite `save` (con el id si es una edición) y
 * `remove`; quien lo usa cierra el diálogo con `open` al guardar bien y puede pasar
 * el error de la API en `error`.
 */
const props = withDefaults(
  defineProps<{
    variations: ServiceVariation[];
    basePrice: number;
    durationMinutes: number;
    busy?: boolean;
    error?: string | null;
  }>(),
  { busy: false, error: null }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{
  save: [input: VariationInput, variationId: number | null];
  remove: [variation: ServiceVariation];
}>();

const { t } = useI18n();

const editing = ref<ServiceVariation | null>(null);
const name = ref('');
const price = ref<string | number>('0');
const minutes = ref<string | number>('0');
const localError = ref<string | null>(null);

watch(open, (isOpen) => {
  if (!isOpen) return;
  name.value = editing.value?.name ?? '';
  price.value = String(editing.value?.priceModifier ?? 0);
  minutes.value = String(editing.value?.durationModifier ?? 0);
  localError.value = null;
});

function startAdd() {
  editing.value = null;
  open.value = true;
}

function startEdit(variation: ServiceVariation) {
  editing.value = variation;
  open.value = true;
}

const signed = (value: number, text: string) => (value > 0 ? `+${text}` : text);

function modifiers(v: ServiceVariation) {
  return t('services.variations.modifiers', {
    price: signed(v.priceModifier, formatCurrencyEur(v.priceModifier)),
    minutes: signed(v.durationModifier, String(v.durationModifier)),
  });
}

function total(v: ServiceVariation) {
  return t('services.variations.result', {
    price: formatCurrencyEur(props.basePrice + v.priceModifier),
    minutes: props.durationMinutes + v.durationModifier,
  });
}

function submit() {
  const text = name.value.trim();
  if (!text) {
    localError.value = t('services.variations.required');
    return;
  }
  if (text.length > 100) {
    localError.value = t('services.variations.tooLong');
    return;
  }
  const priceModifier = Number(price.value);
  const durationModifier = Number(minutes.value);
  if (
    price.value === '' ||
    minutes.value === '' ||
    !Number.isFinite(priceModifier) ||
    !Number.isInteger(durationModifier)
  ) {
    localError.value = t('services.variations.numbers');
    return;
  }
  // Mismo control que la API: la duración resultante tiene que ser positiva.
  if (props.durationMinutes + durationModifier <= 0) {
    localError.value = t('services.variations.tooShort');
    return;
  }
  localError.value = null;
  emit('save', { name: text, priceModifier, durationModifier }, editing.value?.id ?? null);
}

const iconButtonClasses =
  'flex h-11 w-11 shrink-0 items-center justify-center text-primary outline-none transition-colors hover:text-primary-hover focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50';
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
      <Text as="h2" size="h3">{{ t('services.variations.title') }}</Text>
      <Text size="notes" class="text-muted-foreground">{{ t('services.variations.hint') }}</Text>
    </div>

    <Text v-if="variations.length === 0" size="paragraph" class="text-muted-foreground">
      {{ t('services.variations.empty') }}
    </Text>
    <ul v-else class="flex flex-col border-b border-border" data-testid="service-variations">
      <li
        v-for="variation in variations"
        :key="variation.id"
        class="flex items-center justify-between gap-2 border-t border-border py-2"
      >
        <div class="flex min-w-0 flex-col gap-1">
          <Text as="span" size="h4" class="break-words">{{ variation.name }}</Text>
          <Text as="span" size="notes" class="text-muted-foreground">{{
            modifiers(variation)
          }}</Text>
          <Text as="span" size="notes">{{ total(variation) }}</Text>
        </div>
        <div class="flex shrink-0">
          <button
            type="button"
            :class="iconButtonClasses"
            :disabled="busy"
            :aria-label="t('services.variations.edit', { name: variation.name })"
            @click="startEdit(variation)"
          >
            <Pencil class="h-5 w-5" aria-hidden="true" />
          </button>
          <button
            type="button"
            :class="iconButtonClasses"
            :disabled="busy"
            :aria-label="t('services.variations.remove', { name: variation.name })"
            @click="emit('remove', variation)"
          >
            <Trash2 class="h-5 w-5" aria-hidden="true" />
          </button>
        </div>
      </li>
    </ul>

    <Button size="sm" variant="primary" class="self-start" :disabled="busy" @click="startAdd">
      {{ t('services.variations.add') }}
    </Button>

    <Dialog
      v-model:open="open"
      :title="editing ? t('services.variations.dialogEdit') : t('services.variations.dialogNew')"
    >
      <form id="variation-form" class="flex flex-col gap-4" novalidate @submit.prevent="submit">
        <div class="flex flex-col gap-1">
          <label for="variation-name" class="font-sans text-[14px] text-foreground">
            {{ t('services.variations.name') }}
          </label>
          <Input id="variation-name" v-model="name" maxlength="100" :disabled="busy" />
        </div>
        <div class="flex gap-4">
          <div class="flex min-w-0 flex-1 flex-col gap-1">
            <label for="variation-price" class="font-sans text-[14px] text-foreground">
              {{ t('services.variations.price') }}
            </label>
            <Input
              id="variation-price"
              v-model="price"
              type="number"
              step="0.5"
              inputmode="decimal"
              aria-describedby="variation-price-hint"
              :disabled="busy"
            />
            <Text id="variation-price-hint" size="notes" class="text-muted-foreground">
              {{ t('services.variations.priceHint') }}
            </Text>
          </div>
          <div class="flex min-w-0 flex-1 flex-col gap-1">
            <label for="variation-duration" class="font-sans text-[14px] text-foreground">
              {{ t('services.variations.duration') }}
            </label>
            <Input
              id="variation-duration"
              v-model="minutes"
              type="number"
              step="5"
              inputmode="numeric"
              :disabled="busy"
            />
          </div>
        </div>
        <Text v-if="localError || error" size="notes" class="text-destructive" role="alert">
          {{ localError ?? error }}
        </Text>
      </form>
      <template #footer>
        <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
          {{ t('services.variations.cancel') }}
        </Button>
        <Button size="sm" variant="primary" type="submit" form="variation-form" :disabled="busy">
          {{ t('services.variations.save') }}
        </Button>
      </template>
    </Dialog>
  </div>
</template>
