<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { Badge } from '@components/ui/badge';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { formatAppointmentDateTime } from '@lib/utils/date.utils';
import { STATUS_BADGE } from '@features/appointments/utils/appointment-status';
import type { AppointmentDetail } from '@features/appointments/types/appointment.types';

/**
 * Historial de citas de una clienta (RA-869d7fc34), de la más reciente a la más
 * antigua: fecha y hora, servicios, empleada y estado en color, como el listado de
 * citas. «Ver más» pide la página siguiente. Presentacional.
 */
withDefaults(
  defineProps<{
    appointments: AppointmentDetail[];
    hasMore?: boolean;
    loading?: boolean;
    failed?: boolean;
  }>(),
  { hasMore: false, loading: false, failed: false }
);

const emit = defineEmits<{ more: [] }>();

const { t } = useI18n();

const services = (a: AppointmentDetail) =>
  (a.items ?? []).map((item) => item.serviceName).join(', ');
</script>

<template>
  <div class="flex flex-col gap-6">
    <Text as="h2" size="h3">{{ t('customers.history.title') }}</Text>

    <Text v-if="failed" size="paragraph" role="alert" class="text-destructive">
      {{ t('customers.history.error') }}
    </Text>
    <Text v-else-if="loading && appointments.length === 0" size="paragraph" role="status">
      {{ t('ui.list.loading') }}
    </Text>
    <Text v-else-if="appointments.length === 0" size="paragraph" class="text-muted-foreground">
      {{ t('customers.history.empty') }}
    </Text>
    <ul v-else class="flex flex-col border-b border-border" data-testid="customer-history">
      <li
        v-for="appointment in appointments"
        :key="appointment.id"
        class="flex items-start justify-between gap-4 border-t border-border py-3"
      >
        <div class="flex min-w-0 flex-col gap-1">
          <Text as="p" size="h4">
            {{ formatAppointmentDateTime(appointment.appointmentDate, appointment.startTime) }}
          </Text>
          <Text v-if="services(appointment)" as="p" size="paragraph">
            {{ services(appointment) }}
          </Text>
          <Text as="p" size="notes" class="text-muted-foreground">
            {{ t('customers.history.employee', { name: appointment.employeeName }) }}
          </Text>
        </div>
        <Badge :variant="STATUS_BADGE[appointment.status]" class="shrink-0">
          {{ t(`agenda.status.${appointment.status}`) }}
        </Badge>
      </li>
    </ul>

    <Button
      v-if="hasMore"
      size="sm"
      variant="secondary"
      class="self-start"
      :disabled="loading"
      @click="emit('more')"
    >
      {{ t('customers.history.more') }}
    </Button>
  </div>
</template>
