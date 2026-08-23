<script setup lang="ts">
import { Search, X } from 'lucide-vue-next';
import { PageTitle } from '@components/ui/page-title';

withDefaults(
  defineProps<{
    title: string;
    searchable?: boolean;
    searchPlaceholder?: string;
  }>(),
  {
    searchable: false,
    searchPlaceholder: 'Buscar',
  }
);

const search = defineModel<string>('search', { default: '' });
</script>

<template>
  <div class="flex w-full flex-col items-center gap-2.5">
    <PageTitle :label="title" />

    <div
      v-if="$slots['primary-button'] || $slots['secondary-button']"
      class="flex w-full items-center justify-between py-[26px]"
    >
      <div><slot name="primary-button" /></div>
      <div><slot name="secondary-button" /></div>
    </div>

    <div v-if="searchable" class="w-full border-y border-border py-2.5">
      <div class="flex w-full items-center gap-2 rounded-full bg-foreground/10 px-4 py-2.5">
        <Search class="h-4 w-4 shrink-0 text-muted-foreground" />
        <input
          v-model="search"
          type="search"
          :placeholder="searchPlaceholder"
          class="w-full bg-transparent font-sans text-foreground outline-none placeholder:text-muted-foreground [&::-webkit-search-cancel-button]:appearance-none"
        />
        <button
          v-if="search"
          type="button"
          aria-label="Borrar búsqueda"
          class="shrink-0 rounded-full text-muted-foreground outline-none transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
          @click="search = ''"
        >
          <X class="h-4 w-4" />
        </button>
      </div>
    </div>
  </div>
</template>
