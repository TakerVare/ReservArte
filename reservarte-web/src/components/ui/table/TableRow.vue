<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

const props = withDefaults(defineProps<{ class?: string }>(), { class: '' });
const classes = computed(() =>
  cn(
    'border-b border-input transition-colors hover:bg-accent/50 data-[state=selected]:bg-accent',
    props.class
  )
);
</script>

<template>
  <tr :class="classes">
    <slot />
  </tr>
</template>
