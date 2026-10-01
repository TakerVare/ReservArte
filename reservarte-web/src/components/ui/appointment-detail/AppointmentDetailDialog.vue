<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Dialog } from '@components/ui/dialog';
import { Badge, type BadgeVariant } from '@components/ui/badge';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { formatCurrencyEur } from '@lib/utils/currency.utils';
import { formatAppointmentDateTime } from '@lib/utils/date.utils';
import type {
  AppointmentDetail,
  AppointmentTransition,
} from '@features/appointments/types/appointment.types';

/**
 * Detalle de una cita en el listado del personal (RA-869fajn7g, recoge 3.11):
 * clienta, fecha y hora, empleada, servicios, precio, estado y avisos. Las acciones
 * las decide quien lo usa (según estado y rol) y llegan como eventos.
 */
const props = withDefaults(
  defineProps<{
    appointment: AppointmentDetail | null;
    statusVariant: BadgeVariant;
    statusLabel: string;
    transitions: AppointmentTransition[];
    canModify: boolean;
    canCancel?: boolean;
    busy?: boolean;
  }>(),
  { busy: false, canCancel: false }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{
  transition: [action: AppointmentTransition];
  modify: [];
  cancel: [];
}>();

const { t } = useI18n();

const when = computed(() =>
  props.appointment
    ? `${formatAppointmentDateTime(props.appointment.appointmentDate, props.appointment.startTime)} – ${props.appointment.endTime.slice(0, 5)}h`
    : ''
);
</script>

<template>
  <Dialog v-model:open="open" :title="appointment?.customerName ?? ''" :description="when">
    <div v-if="appointment" class="flex flex-col gap-4">
      <div class="flex items-center justify-between gap-4">
        <Text size="paragraph">{{
          t('agenda.detail.employee', { name: appointment.employeeName })
        }}</Text>
        <Badge :variant="statusVariant">{{ statusLabel }}</Badge>
      </div>

      <ul class="flex flex-col border-y border-border" :aria-label="t('agenda.detail.services')">
        <li
          v-for="item in appointment.items ?? []"
          :key="item.order"
          class="flex items-center justify-between gap-4 py-2"
        >
          <Text as="span" size="paragraph">
            {{ item.serviceName
            }}<template v-if="item.serviceVariationName">
              · {{ item.serviceVariationName }}</template
            >
            <span class="text-muted-foreground"> · {{ item.durationMinutes }} min</span>
          </Text>
          <Text as="span" size="paragraph">{{ formatCurrencyEur(item.price) }}</Text>
        </li>
      </ul>

      <div class="flex items-center justify-between">
        <Text size="h4">{{ t('agenda.detail.total') }}</Text>
        <Text size="h4">{{ formatCurrencyEur(appointment.totalPrice) }}</Text>
      </div>

      <Text
        v-for="warning in appointment.warnings ?? []"
        :key="warning.code + (warning.serviceId ?? '')"
        size="notes"
        role="note"
        class="text-destructive"
      >
        {{ warning.message }}
      </Text>
    </div>

    <template #footer>
      <Button v-if="canCancel" variant="secondary" :disabled="busy" @click="emit('cancel')">
        {{ t('agenda.actions.cancel') }}
      </Button>
      <Button v-if="canModify" variant="secondary" :disabled="busy" @click="emit('modify')">
        {{ t('agenda.actions.modify') }}
      </Button>
      <Button
        v-for="action in transitions"
        :key="action"
        :variant="action === 'no-show' ? 'secondary' : 'primary'"
        :disabled="busy"
        @click="emit('transition', action)"
      >
        {{ t(`agenda.actions.${action}`) }}
      </Button>
    </template>
  </Dialog>
</template>
