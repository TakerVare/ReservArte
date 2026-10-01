<script setup lang="ts">
import { Text } from '@components/ui/text';

defineProps<{
  hours: string;
}>();
</script>

<template>
  <!-- Como en Figma: el punto sangrado unos 20 px y el texto a 48 px del borde. -->
  <div class="w-full py-4">
    <Text size="paragraph"
      ><span aria-hidden="true" class="inline-block w-12 pl-5">·</span>{{ hours }}</Text
    >
  </div>
</template>
