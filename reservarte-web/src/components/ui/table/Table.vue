<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

/**
 * Tabla base (RA-869d7fbuf): piezas nativas (`Table`, `TableHeader`,
 * `TableBody`, `TableRow`, `TableHead`, `TableCell`, `TableCaption`) con los
 * tokens del proyecto. El contenedor hace scroll horizontal en pantallas
 * estrechas. El DataTable (`869d7fbxn`) se monta sobre ellas.
 */
const props = withDefaults(defineProps<{ class?: string }>(), { class: '' });
const classes = computed(() => cn('w-full caption-bottom border-collapse', props.class));
</script>

<template>
  <div class="relative w-full overflow-x-auto">
    <table :class="classes">
      <slot />
    </table>
  </div>
</template>
