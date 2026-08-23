<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    /** Dirección del local. Configurable por organización (módulo Configuración, futuro). */
    address: string;
    title?: string;
  }>(),
  {
    title: 'Mapa de ubicación',
  }
);

// Embed de Google Maps sin API key (https://www.google.com/maps?q=...&output=embed).
const embedSrc = computed(
  () => `https://www.google.com/maps?q=${encodeURIComponent(props.address)}&output=embed`
);
</script>

<template>
  <iframe
    :src="embedSrc"
    :title="title"
    class="h-[197px] w-full border-0 md:h-[250px] lg:h-[300px]"
    loading="lazy"
    referrerpolicy="no-referrer-when-downgrade"
    allowfullscreen
  />
</template>
