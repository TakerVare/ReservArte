<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router';
import { RouterLink } from 'vue-router';
import { Text } from '@components/ui/text';
import ChevronIcon from '@assets/icons/menu-chevron.svg';

export interface MenuItem {
  key: string;
  label: string;
  /** Destino de la opción. Sin él, la opción es una acción: emite `select`. */
  to?: RouteLocationRaw;
}

export interface MenuSection {
  key: string;
  label: string;
  items: MenuItem[];
}

defineProps<{
  sections: MenuSection[];
}>();

defineEmits<{
  select: [key: string];
}>();

// Menú de secciones de la pantalla de Usuario (Figma «Menu», Material 3):
// cabecera de sección con fondo `highlight` y separador `accent`; opciones de
// 44 px con sangría de 20 px, texto H4 y chevron en `primary`. Sombra de
// elevación 3, con el color del texto como base para no usar negros literales.
const itemClasses =
  'flex h-11 w-full items-center gap-2 rounded p-3 text-left outline-none transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring';
</script>

<template>
  <nav
    class="flex w-full flex-col rounded-b-2xl bg-background shadow-[0_4px_4px_hsl(var(--foreground)/15%),0_1px_1.5px_hsl(var(--foreground)/30%)]"
  >
    <section v-for="section in sections" :key="section.key" class="w-full py-0.5">
      <div class="flex min-h-8 w-full flex-col gap-0.5 bg-highlight px-1">
        <Text as="h2" size="h3" class="px-3 py-2.5 text-muted-foreground">{{ section.label }}</Text>
        <div class="mx-2 mb-[2px] border-t border-accent" aria-hidden="true" />
      </div>
      <ul class="w-full">
        <li v-for="item in section.items" :key="item.key" class="w-full px-1 py-0.5">
          <RouterLink v-if="item.to" :to="item.to" :class="itemClasses">
            <span class="size-5 shrink-0" aria-hidden="true" />
            <Text as="span" size="h4" class="min-w-0 flex-1 truncate">{{ item.label }}</Text>
            <ChevronIcon class="size-5 shrink-0 text-primary" aria-hidden="true" />
          </RouterLink>
          <button v-else type="button" :class="itemClasses" @click="$emit('select', item.key)">
            <span class="size-5 shrink-0" aria-hidden="true" />
            <Text as="span" size="h4" class="min-w-0 flex-1 truncate">{{ item.label }}</Text>
            <ChevronIcon class="size-5 shrink-0 text-primary" aria-hidden="true" />
          </button>
        </li>
      </ul>
    </section>
  </nav>
</template>
