<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { Trash2 } from 'lucide-vue-next';
import { Badge } from '@components/ui/badge';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import type { Absence } from '@features/employees/types/employee.types';
import { describeAbsence } from '@features/employees/utils/absence-dates';

/**
 * Vacaciones y ausencias de un empleado (RA-869d7fc0h), en hora del centro: tipo,
 * fechas, motivo y «quitar». Presentacional: el alta la abre quien lo usa (`add`).
 */
const props = withDefaults(
  defineProps<{
    absences: Absence[];
    /** Zona del centro (IANA), en la que se muestran las fechas. */
    timeZone: string;
    busy?: boolean;
  }>(),
  { busy: false }
);

const emit = defineEmits<{ add: []; remove: [absence: Absence] }>();

const { t } = useI18n();

function when(absence: Absence): string {
  const w = describeAbsence(absence, props.timeZone);
  const values = { from: w.fromDay, to: w.toDay, start: w.startTime, end: w.endTime };
  if (w.allDay) return t(`employees.absences.when.${w.singleDay ? 'day' : 'days'}`, values);
  return t(`employees.absences.when.${w.singleDay ? 'hours' : 'span'}`, values);
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
      <Text as="h2" size="h3">{{ t('employees.absences.title') }}</Text>
      <Text size="notes" class="text-muted-foreground">{{ t('employees.absences.hint') }}</Text>
    </div>

    <Button size="sm" variant="primary" class="self-start" :disabled="busy" @click="emit('add')">
      {{ t('employees.absences.add') }}
    </Button>

    <Text v-if="absences.length === 0" size="paragraph" class="py-4 text-muted-foreground">
      {{ t('employees.absences.empty') }}
    </Text>
    <ul v-else class="flex flex-col border-b border-border">
      <li
        v-for="absence in absences"
        :key="absence.id"
        class="flex items-center justify-between gap-4 border-t border-border py-3"
      >
        <div class="flex min-w-0 flex-col gap-1">
          <Badge :variant="absence.type === 'vacation' ? 'primary' : 'default'" class="self-start">
            {{ t(`employees.absences.types.${absence.type}`) }}
          </Badge>
          <Text as="p" size="h4">{{ when(absence) }}</Text>
          <Text v-if="absence.reason" as="p" size="notes" class="text-muted-foreground">
            {{ absence.reason }}
          </Text>
        </div>
        <button
          type="button"
          class="flex h-11 w-11 shrink-0 items-center justify-center text-primary outline-none transition-colors hover:text-primary-hover focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
          :disabled="busy"
          :aria-label="t('employees.absences.remove', { what: when(absence) })"
          @click="emit('remove', absence)"
        >
          <Trash2 class="h-5 w-5" aria-hidden="true" />
        </button>
      </li>
    </ul>
    <Text size="notes" class="text-muted-foreground">{{ t('employees.absences.range') }}</Text>
  </div>
</template>
