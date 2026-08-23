<script setup lang="ts">
import type { Component } from 'vue';
import { computed } from 'vue';
import { Text } from '@components/ui/text';

const props = withDefaults(
  defineProps<{
    icon: Component;
    value: string;
    /** tel:, mailto:, https://instagram.com/... — si se omite, se muestra como texto sin enlace. */
    href?: string;
  }>(),
  {
    href: undefined,
  }
);

const tag = computed(() => (props.href ? 'a' : 'div'));
const isExternal = computed(() => props.href?.startsWith('http') ?? false);
</script>

<template>
  <component
    :is="tag"
    :href="href"
    :target="isExternal ? '_blank' : undefined"
    :rel="isExternal ? 'noopener noreferrer' : undefined"
    class="flex w-full items-center gap-6 py-4 text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
  >
    <component :is="icon" class="h-10 w-10 shrink-0" />
    <Text size="h3">{{ value }}</Text>
  </component>
</template>
