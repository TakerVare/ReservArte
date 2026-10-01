<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

const props = withDefaults(defineProps<{ class?: string }>(), { class: '' });
const classes = computed(() => cn('border-b border-input', props.class));
</script>

<template>
  <thead :class="classes">
    <slot />
  </thead>
</template>
