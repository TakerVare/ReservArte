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
  <!-- Medidas de Figma («Contact-Page», 387:57554) en sus cortes 576/768/992/1200:
       a todo el ancho en móvil y, desde 576, centrado con ancho fijo. -->
  <div class="flex w-full justify-center">
    <iframe
      :src="embedSrc"
      :title="title"
      class="block h-[197px] w-full border-0 min-[576px]:w-[393px] md:h-[250px] md:w-[600px] min-[992px]:h-[300px] min-[1200px]:w-[800px]"
      loading="lazy"
      referrerpolicy="no-referrer-when-downgrade"
      allowfullscreen
    />
  </div>
</template>
