<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

const props = withDefaults(defineProps<{ class?: string }>(), { class: '' });
const classes = computed(() =>
  cn(
    'h-12 px-4 text-left align-middle font-sans text-[14px] font-bold text-foreground',
    props.class
  )
);
</script>

<template>
  <th :class="classes"><slot /></th>
</template>
