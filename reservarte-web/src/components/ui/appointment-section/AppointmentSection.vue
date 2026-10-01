<script setup lang="ts">
// En móvil, como en Figma (Home con y sin cita, RA-869faaunu): «Modificar» y
// «Cancelar» se reparten el ancho y «Reservar Cita» es ancho.
import { Text } from '@components/ui/text';
import { Button } from '@components/ui/button';

withDefaults(
  defineProps<{
    title?: string;
    /** Fecha/hora ya formateada (ej. "24 Dic - 10:00h"). Ausente = sin cita asignada. */
    dateTime?: string;
    emptyMessage?: string;
    modifyLabel?: string;
    cancelLabel?: string;
    bookLabel?: string;
  }>(),
  {
    title: 'Próxima cita:',
    dateTime: undefined,
    emptyMessage: 'No hay citas asignadas',
    modifyLabel: 'Modificar',
    cancelLabel: 'Cancelar',
    bookLabel: 'Reservar Cita',
  }
);

const emit = defineEmits<{
  modify: [];
  cancel: [];
  book: [];
}>();
</script>

<template>
  <div class="flex w-full flex-col items-center gap-8 py-8 text-center">
    <Text size="h3" class="md:text-[48px]">{{ title }}</Text>

    <template v-if="dateTime">
      <Text size="h3" class="md:text-[36px]">{{ dateTime }}</Text>
      <div class="flex w-full items-center justify-between gap-4">
        <Button size="md" variant="primary" class="flex-1 md:hidden" @click="emit('modify')">
          {{ modifyLabel }}
        </Button>
        <Button size="xxl" variant="primary" class="hidden md:inline-flex" @click="emit('modify')">
          {{ modifyLabel }}
        </Button>
        <Button size="md" variant="secondary" class="flex-1 md:hidden" @click="emit('cancel')">
          {{ cancelLabel }}
        </Button>
        <Button
          size="xxl"
          variant="secondary"
          class="hidden md:inline-flex"
          @click="emit('cancel')"
        >
          {{ cancelLabel }}
        </Button>
      </div>
    </template>

    <template v-else>
      <Text size="big-message" class="max-w-[485px] text-[48px] md:text-[64px]">
        {{ emptyMessage }}
      </Text>
      <Button
        size="md"
        variant="primary"
        class="w-full max-w-[270px] md:hidden"
        @click="emit('book')"
      >
        {{ bookLabel }}
      </Button>
      <Button size="xxl" variant="primary" class="hidden md:inline-flex" @click="emit('book')">
        {{ bookLabel }}
      </Button>
    </template>
  </div>
</template>
