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
// Georgia, mismo tracking (0.01em) y color, sin line-height explícito
// (se deja el valor por defecto del navegador). H4 es el único en negrita;
// "big-message" es el único centrado (mensaje grande de estado vacío).
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
const classes = computed(() =>
  cn('font-sans text-foreground tracking-[0.01em]', sizeClasses[props.size], props.class)
);
</script>

<template>
  <component :is="tag" :class="classes">
    <slot />
  </component>
</template>
