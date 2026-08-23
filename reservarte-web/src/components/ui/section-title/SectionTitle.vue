<script setup lang="ts">
import { Text } from '@components/ui/text';

defineProps<{
  label: string;
}>();
</script>

<template>
  <div class="w-full bg-accent p-2 md:p-6">
    <Text as="h2" size="h3" class="px-3 md:text-[36px]">
      {{ label }}
    </Text>
  </div>
</template>
