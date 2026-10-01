<script setup lang="ts">
import { Text } from '@components/ui/text';
import { cn } from '@lib/utils/cn.utils';

withDefaults(
  defineProps<{
    label: string;
    /** Para ajustar el espaciado por posición (en Figma, el segundo título lleva 48 px). */
    class?: string;
  }>(),
  {
    class: '',
  }
);
</script>

<template>
  <div :class="cn('w-full py-8', $props.class)">
    <Text size="h2">{{ label }}</Text>
  </div>
</template>
