<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

const props = withDefaults(defineProps<{ class?: string }>(), { class: '' });
const classes = computed(() =>
  cn('px-4 py-3 align-middle font-sans text-[16px] text-foreground', props.class)
);
</script>

<template>
  <td :class="classes"><slot /></td>
</template>
