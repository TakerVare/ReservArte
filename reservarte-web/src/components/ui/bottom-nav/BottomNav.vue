<script setup lang="ts">
import type { Component } from 'vue';
import type { RouteLocationRaw } from 'vue-router';
import { RouterLink } from 'vue-router';

export interface BottomNavItem {
  label: string;
  to: RouteLocationRaw;
  icon: Component;
}

defineProps<{
  items: BottomNavItem[];
}>();
</script>

<template>
  <nav
    class="flex h-[100px] w-full items-center justify-center border-t border-border bg-background"
  >
    <div class="flex h-[72px] w-full max-w-[393px] items-center">
      <RouterLink
        v-for="item in items"
        :key="item.label"
        v-slot="{ isActive, href, navigate }"
        :to="item.to"
        custom
      >
        <a
          :href="href"
          :aria-label="item.label"
          :aria-current="isActive ? 'page' : undefined"
          :class="isActive ? 'text-primary' : 'text-muted-foreground'"
          class="flex h-full flex-1 items-center justify-center outline-none transition-colors focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
          @click="navigate"
        >
          <component :is="item.icon" class="h-[60px] w-[60px]" />
        </a>
      </RouterLink>
    </div>
  </nav>
</template>
