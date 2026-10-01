<script setup lang="ts">
import { Text } from '@components/ui/text';
import { Button } from '@components/ui/button';

export interface EmployeeAvailabilityEntry {
  /** Para reservar con esa empleada (RA-869fagpyg). */
  id: number;
  name: string;
  slots: string[];
}

withDefaults(
  defineProps<{
    title?: string;
    employees: EmployeeAvailabilityEntry[];
    /** Mientras se guarda una reserva, los huecos no se pueden pulsar. */
    disabled?: boolean;
  }>(),
  {
    title: 'Citas disponibles:',
    disabled: false,
  }
);

const emit = defineEmits<{
  'select-slot': [payload: { employeeId: number; employee: string; slot: string }];
}>();
</script>

<template>
  <div class="flex w-full flex-col gap-6 py-16">
    <Text size="h2">{{ title }}</Text>

    <div v-for="employee in employees" :key="employee.id" class="flex flex-col gap-4">
      <Text size="h3">{{ employee.name }}</Text>
      <!-- Como en Figma (387:56629): dos huecos por fila en móvil. -->
      <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
        <Button
          v-for="slot in employee.slots"
          :key="slot"
          type="button"
          variant="primary"
          size="lg"
          class="w-full"
          :disabled="disabled"
          @click="emit('select-slot', { employeeId: employee.id, employee: employee.name, slot })"
        >
          {{ slot }}
        </Button>
      </div>
    </div>
  </div>
</template>
