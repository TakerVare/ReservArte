<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

export type BadgeVariant = 'default' | 'primary' | 'outline' | 'muted' | 'destructive';

/**
 * Etiqueta corta de estado o categoría (RA-869d7fbuf). Ángulos rectos y solo
 * tokens. Los colores por estado de cita llegan con `869d7fca1`.
 */
const props = withDefaults(
  defineProps<{
    variant?: BadgeVariant;
    class?: string;
  }>(),
  {
    variant: 'default',
    class: '',
  }
);

const variantClasses: Record<BadgeVariant, string> = {
  default: 'bg-accent text-foreground border-accent',
  primary: 'bg-primary text-primary-foreground border-primary',
  outline: 'bg-transparent text-foreground border-primary',
  muted: 'bg-muted text-muted-foreground border-muted',
  destructive: 'bg-destructive text-destructive-foreground border-destructive',
};

const classes = computed(() =>
  cn(
    'inline-flex items-center gap-1 whitespace-nowrap border px-2 py-0.5 font-sans text-[12px] leading-[normal] tracking-[0.01em]',
    variantClasses[props.variant],
    props.class
  )
);
</script>

<template>
  <span :class="classes"><slot /></span>
</template>
