<script setup lang="ts">
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';

/**
 * Confirmación de una acción con consecuencias (RA-869d7fbyt), como dar de baja una
 * ficha: título, explicación y dos botones. Presentacional.
 */
withDefaults(
  defineProps<{
    title: string;
    description: string;
    confirmLabel: string;
    cancelLabel: string;
    busy?: boolean;
  }>(),
  { busy: false }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{ confirm: [] }>();
</script>

<template>
  <Dialog v-model:open="open" :title="title" :description="description">
    <template #footer>
      <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
        {{ cancelLabel }}
      </Button>
      <Button size="sm" variant="primary" :disabled="busy" @click="emit('confirm')">
        {{ confirmLabel }}
      </Button>
    </template>
  </Dialog>
</template>
