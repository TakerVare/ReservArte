<script setup lang="ts">
import { Text } from '@components/ui/text';

defineProps<{
  label: string;
}>();
</script>

<template>
  <div class="w-full bg-primary-hover p-2 md:p-6 xl:p-12">
    <Text as="h1" size="h3" class="px-3 md:text-[36px] xl:text-[48px]">
      {{ label }}
    </Text>
  </div>
</template>
