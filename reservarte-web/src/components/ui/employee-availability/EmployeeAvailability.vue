<script setup lang="ts">
import { Text } from '@components/ui/text';
import { Button } from '@components/ui/button';

export interface EmployeeAvailabilityEntry {
  name: string;
  slots: string[];
}

withDefaults(
  defineProps<{
    title?: string;
    employees: EmployeeAvailabilityEntry[];
  }>(),
  {
    title: 'Citas disponibles:',
  }
);

const emit = defineEmits<{
  'select-slot': [payload: { employee: string; slot: string }];
}>();
</script>

<template>
  <div class="flex w-full flex-col gap-6 py-16">
    <Text size="h2">{{ title }}</Text>

    <div v-for="employee in employees" :key="employee.name" class="flex flex-col gap-4">
      <Text size="h3">{{ employee.name }}</Text>
      <div class="flex flex-wrap gap-4 gap-x-12 pl-4 xl:gap-x-16">
        <Button
          v-for="slot in employee.slots"
          :key="slot"
          type="button"
          variant="primary"
          size="xl"
          @click="emit('select-slot', { employee: employee.name, slot })"
        >
          {{ slot }}
        </Button>
      </div>
    </div>
  </div>
</template>
