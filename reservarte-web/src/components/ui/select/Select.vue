<script setup lang="ts">
import { computed } from 'vue';
import {
  SelectContent,
  SelectIcon,
  SelectItem,
  SelectItemIndicator,
  SelectItemText,
  SelectPortal,
  SelectRoot,
  SelectTrigger,
  SelectValue,
  SelectViewport,
} from 'reka-ui';
import { Check, ChevronDown } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';
import { cn } from '@lib/utils/cn.utils';

export interface SelectOption {
  /** No puede ser cadena vacía (Reka UI la reserva para «sin valor»). */
  value: string;
  label: string;
  disabled?: boolean;
}

/**
 * Desplegable (RA-869d7fbuf) sobre Reka UI: teclado, lector de pantalla y
 * posición gestionados por la librería. El disparador tiene el aspecto del
 * `Input` (`styles-reference.html` §4.2). Las etiquetas: `id` en el
 * disparador para un `<label for>`, o `aria-label`.
 */
const props = withDefaults(
  defineProps<{
    options: SelectOption[];
    placeholder?: string;
    id?: string;
    invalid?: boolean;
    disabled?: boolean;
    class?: string;
  }>(),
  {
    placeholder: undefined,
    id: undefined,
    invalid: false,
    disabled: false,
    class: '',
  }
);

const model = defineModel<string>();

const { t } = useI18n();

const triggerClasses = computed(() =>
  cn(
    'flex w-full items-center justify-between gap-2 border border-input bg-background px-4 py-3 text-left font-sans text-foreground outline-none',
    'focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]',
    'disabled:cursor-not-allowed disabled:opacity-60 data-[placeholder]:text-muted-foreground',
    props.invalid &&
      'border-destructive focus:border-destructive focus:shadow-[0_0_0_3px_hsl(var(--destructive)/20%)]',
    props.class
  )
);
</script>

<template>
  <SelectRoot v-model="model" :disabled="disabled">
    <SelectTrigger :id="id" :class="triggerClasses" :aria-invalid="invalid || undefined">
      <SelectValue :placeholder="placeholder ?? t('ui.select.placeholder')" />
      <SelectIcon as-child>
        <ChevronDown class="h-4 w-4 shrink-0 text-primary" aria-hidden="true" />
      </SelectIcon>
    </SelectTrigger>
    <SelectPortal>
      <SelectContent
        position="popper"
        :side-offset="4"
        class="z-50 max-h-[300px] min-w-[var(--reka-select-trigger-width)] overflow-hidden border border-input bg-background shadow-[0_10px_30px_hsl(var(--foreground)/10%)]"
      >
        <SelectViewport class="p-1">
          <SelectItem
            v-for="option in options"
            :key="option.value"
            :value="option.value"
            :disabled="option.disabled"
            class="relative flex cursor-pointer select-none items-center py-2 pl-8 pr-4 font-sans text-foreground outline-none data-[disabled]:pointer-events-none data-[highlighted]:bg-accent data-[disabled]:opacity-50"
          >
            <SelectItemIndicator class="absolute left-2 inline-flex items-center">
              <Check class="h-4 w-4 text-primary" aria-hidden="true" />
            </SelectItemIndicator>
            <SelectItemText>{{ option.label }}</SelectItemText>
          </SelectItem>
        </SelectViewport>
      </SelectContent>
    </SelectPortal>
  </SelectRoot>
</template>
