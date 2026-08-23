<script setup lang="ts">
import { computed } from 'vue';
import { Text, type TextSize } from '@components/ui/text';
import { cn } from '@lib/utils/cn.utils';
import Pencil from '@assets/icons/action-pencil.svg';
import Trash from '@assets/icons/action-trash.svg';
import Eye from '@assets/icons/action-eye.svg';

export type ListItemSize = 'lg' | 'md' | 'sm' | 'xs';

const props = withDefaults(
  defineProps<{
    label: string;
    size?: ListItemSize;
  }>(),
  {
    size: 'md',
  }
);

defineEmits<{
  edit: [];
  delete: [];
  view: [];
}>();

// Tamaños tal como están definidos en Figma: LG/MD comparten icono grande
// (56px) y separación de 20px; SM/XS comparten icono más compacto (44px)
// y separación de 4px. El texto usa directamente los estilos H2/H3/H4 de
// Text (H4 ya es negrita por definición, coherente con SM/XS aquí).
const TEXT_SIZE: Record<ListItemSize, TextSize> = {
  lg: 'h2',
  md: 'h3',
  sm: 'h4',
  xs: 'h4',
};

const rowPadding: Record<ListItemSize, string> = {
  lg: 'py-8',
  md: 'py-6',
  sm: 'py-[11px]',
  xs: 'py-[11px]',
};

const iconSizeClasses: Record<ListItemSize, string> = {
  lg: 'h-14 w-14',
  md: 'h-14 w-14',
  sm: 'h-11 w-11',
  xs: 'h-11 w-11',
};

const actionsGapClasses: Record<ListItemSize, string> = {
  lg: 'gap-5',
  md: 'gap-5',
  sm: 'gap-1',
  xs: 'gap-1',
};

const textSize = computed(() => TEXT_SIZE[props.size]);
const rowClasses = computed(() =>
  cn('flex w-full items-center justify-between border-t border-border', rowPadding[props.size])
);
const iconClasses = computed(() => iconSizeClasses[props.size]);
const actionsGapClass = computed(() => actionsGapClasses[props.size]);

const actionButtonClasses =
  'flex items-center justify-center text-primary transition-colors hover:text-primary-hover outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background';
</script>

<template>
  <div :class="rowClasses">
    <Text as="p" :size="textSize" class="min-w-0 truncate">{{ label }}</Text>
    <div class="flex shrink-0 items-center" :class="actionsGapClass">
      <button type="button" :class="actionButtonClasses" aria-label="Editar" @click="$emit('edit')">
        <Pencil :class="iconClasses" />
      </button>
      <button
        type="button"
        :class="actionButtonClasses"
        aria-label="Eliminar"
        @click="$emit('delete')"
      >
        <Trash :class="iconClasses" />
      </button>
      <button type="button" :class="actionButtonClasses" aria-label="Ver" @click="$emit('view')">
        <Eye :class="iconClasses" />
      </button>
    </div>
  </div>
</template>
