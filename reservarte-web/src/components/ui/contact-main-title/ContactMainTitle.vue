<script setup lang="ts">
import { Text } from '@components/ui/text';

defineProps<{
  label: string;
}>();
</script>

<template>
  <div class="w-full py-8">
    <Text size="h2">{{ label }}</Text>
  </div>
</template>
