<script setup lang="ts">
import { TabsContent, TabsList, TabsRoot, TabsTrigger } from 'reka-ui';

export interface TabItem {
  value: string;
  label: string;
  disabled?: boolean;
}

/**
 * Pestañas (RA-869d7fbuf) sobre Reka UI: roles ARIA y flechas del teclado de
 * la librería. Cada panel es el slot con el `value` de su pestaña. La activa se
 * subraya en `primary`, como el foco del resto de la interfaz.
 */
defineProps<{
  tabs: TabItem[];
  /** Nombre accesible de la lista de pestañas. */
  label: string;
}>();

const model = defineModel<string>();
</script>

<template>
  <TabsRoot v-model="model" :default-value="tabs[0]?.value" class="flex w-full flex-col">
    <TabsList :aria-label="label" class="flex w-full overflow-x-auto border-b border-input">
      <TabsTrigger
        v-for="tab in tabs"
        :key="tab.value"
        :value="tab.value"
        :disabled="tab.disabled"
        class="-mb-px whitespace-nowrap border-b-2 border-transparent px-4 py-3 font-sans text-[16px] leading-[normal] tracking-[0.01em] text-muted-foreground outline-none transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:pointer-events-none disabled:opacity-50 data-[state=active]:border-primary data-[state=active]:text-foreground"
      >
        {{ tab.label }}
      </TabsTrigger>
    </TabsList>
    <TabsContent
      v-for="tab in tabs"
      :key="tab.value"
      :value="tab.value"
      class="pt-6 outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <slot :name="tab.value" />
    </TabsContent>
  </TabsRoot>
</template>
