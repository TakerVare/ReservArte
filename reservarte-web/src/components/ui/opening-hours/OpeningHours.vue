<script setup lang="ts">
import { Text } from '@components/ui/text';

defineProps<{
  hours: string;
}>();
</script>

<template>
  <div class="w-full py-4 pl-8">
    <Text size="paragraph">· {{ hours }}</Text>
  </div>
</template>
