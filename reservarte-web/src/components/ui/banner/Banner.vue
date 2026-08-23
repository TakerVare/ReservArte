<script setup lang="ts">
withDefaults(
  defineProps<{
    /** URL del logo del tenant. Configurable por organización (módulo Configuración, futuro). */
    logoSrc: string;
    logoAlt?: string;
  }>(),
  {
    logoAlt: 'Logo',
  }
);
</script>

<template>
  <header class="flex h-[170px] w-full items-center justify-center bg-background p-2.5">
    <img :src="logoSrc" :alt="logoAlt" class="h-auto w-full max-w-[393px]" />
  </header>
</template>
