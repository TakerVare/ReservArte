<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

const props = withDefaults(defineProps<{ class?: string }>(), { class: '' });
const classes = computed(() => cn('mt-4 font-sans text-[14px] text-muted-foreground', props.class));
</script>

<template>
  <caption :class="classes">
    <slot />
  </caption>
</template>
