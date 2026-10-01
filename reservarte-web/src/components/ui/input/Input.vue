<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

/**
 * Campo de texto base (RA-869d7fbuf). Estilo de `styles-reference.html` §4.2,
 * el mismo que ya usan los formularios: borde `input`, ángulos rectos y foco
 * rosa con halo. `invalid` pinta el error y marca `aria-invalid`; el mensaje lo
 * pone quien lo usa, enlazado con `aria-describedby`.
 */
const props = withDefaults(
  defineProps<{
    invalid?: boolean;
    class?: string;
  }>(),
  {
    invalid: false,
    class: '',
  }
);

const model = defineModel<string | number>();

const classes = computed(() =>
  cn(
    'w-full border border-input bg-background px-4 py-3 font-sans text-foreground outline-none',
    'placeholder:text-muted-foreground',
    'focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]',
    'disabled:cursor-not-allowed disabled:opacity-60',
    props.invalid &&
      'border-destructive focus:border-destructive focus:shadow-[0_0_0_3px_hsl(var(--destructive)/20%)]',
    props.class
  )
);
</script>

<template>
  <input v-model="model" :class="classes" :aria-invalid="invalid || undefined" />
</template>
