<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { formatDateSpain } from '@lib/utils/date.utils';
import {
  CONSENT_TYPES,
  type ConsentType,
  type CustomerConsent,
} from '@features/customers/types/customer.types';

/**
 * Consentimientos de una clienta ya dada de alta (4.2b): las cuatro finalidades del
 * piloto con su estado y fecha, y «Dar» o «Retirar» en cada una. Presentacional:
 * emite `change`; la página confirma antes de retirar el de datos (H-47).
 */
const props = withDefaults(defineProps<{ consents: CustomerConsent[]; busy?: boolean }>(), {
  busy: false,
});

const emit = defineEmits<{ change: [consentType: ConsentType, granted: boolean] }>();

const { t } = useI18n();

const find = (type: ConsentType) => props.consents.find((c) => c.consentType === type);

function state(type: ConsentType): string {
  const consent = find(type);
  if (consent?.isGranted && consent.grantedAt) {
    return t('customers.form.consents.granted', {
      date: formatDateSpain(new Date(consent.grantedAt)),
    });
  }
  if (consent?.revokedAt) {
    return t('customers.form.consents.revoked', {
      date: formatDateSpain(new Date(consent.revokedAt)),
    });
  }
  return t('customers.form.consents.notGranted');
}
</script>

<template>
  <section class="flex flex-col gap-3">
    <Text as="h3" size="h4">{{ t('customers.form.consents.legend') }}</Text>
    <ul class="flex flex-col border-b border-border" data-testid="customer-consents">
      <li
        v-for="type in CONSENT_TYPES"
        :key="type"
        class="flex items-center justify-between gap-4 border-t border-border py-3"
      >
        <div class="flex min-w-0 flex-col gap-1">
          <Text as="span" size="paragraph">{{ t(`customers.form.consents.${type}`) }}</Text>
          <Text as="span" size="notes" class="text-muted-foreground">{{ state(type) }}</Text>
        </div>
        <Button
          size="xs"
          variant="secondary"
          class="shrink-0"
          :disabled="busy"
          :aria-label="
            t(
              find(type)?.isGranted
                ? 'customers.form.consents.revokeLabel'
                : 'customers.form.consents.grantLabel',
              { consent: t(`customers.form.consents.${type}`) }
            )
          "
          @click="emit('change', type, !find(type)?.isGranted)"
        >
          {{
            find(type)?.isGranted
              ? t('customers.form.consents.revoke')
              : t('customers.form.consents.grant')
          }}
        </Button>
      </li>
    </ul>
  </section>
</template>
