<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

export type TextSize = 'h1' | 'h2' | 'h3' | 'h4' | 'paragraph' | 'notes' | 'big-message';
export type TextTag = 'h1' | 'h2' | 'h3' | 'h4' | 'p' | 'span' | 'div';

const DEFAULT_TAG: Record<TextSize, TextTag> = {
  h1: 'h1',
  h2: 'h2',
  h3: 'h3',
  h4: 'h4',
  paragraph: 'p',
  notes: 'p',
  'big-message': 'p',
};

const props = withDefaults(
  defineProps<{
    size?: TextSize;
    as?: TextTag;
    class?: string;
  }>(),
  {
    size: 'paragraph',
    as: undefined,
    class: '',
  }
);

// Tamaños tal como están definidos en Figma ("Text-Label"): todos en
// Georgia, mismo tracking (0.01em) y color, y altura de línea `normal`, la
// «auto» de Figma (≈ 1,14 en Georgia). Hay que fijarla: el preflight de
// Tailwind pone 1,5 en `html` y los textos salían más altos (RA-869d7fbuf).
// H4 es el único en negrita; "big-message" es el único centrado (mensaje
// grande de estado vacío).
const sizeClasses: Record<TextSize, string> = {
  h1: 'text-[48px] font-normal',
  h2: 'text-[36px] font-normal',
  h3: 'text-[24px] font-normal',
  h4: 'text-[18px] font-bold',
  paragraph: 'text-[16px] font-normal',
  notes: 'text-[14px] font-normal',
  'big-message': 'text-[64px] font-normal text-center',
};

const tag = computed(() => props.as ?? DEFAULT_TAG[props.size]);
// La altura de línea se añade tras combinar: tailwind-merge descarta `leading-*`
// si detrás llega un tamaño de letra (p. ej. `text-[48px]` desde fuera). Solo
// se omite si quien usa el componente pone la suya sin prefijo.
const HAS_LEADING = /(^|\s)leading-/;
const classes = computed(() => {
  const merged = cn(
    'font-sans text-foreground tracking-[0.01em]',
    sizeClasses[props.size],
    props.class
  );
  return HAS_LEADING.test(merged) ? merged : `${merged} leading-[normal]`;
});
</script>

<template>
  <component :is="tag" :class="classes">
    <slot />
  </component>
</template>
