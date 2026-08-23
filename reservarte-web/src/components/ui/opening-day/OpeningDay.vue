<script setup lang="ts">
import { Text } from '@components/ui/text';

defineProps<{
  label: string;
}>();
</script>

<template>
  <div class="w-full py-4">
    <Text size="paragraph">{{ label }}</Text>
  </div>
</template>
