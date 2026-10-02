<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';
import { Text } from '@components/ui/text';

/**
 * Bloqueo de una clienta (4.2b): estado con el motivo, «Bloquear» (pide motivo, 500
 * caracteres como mucho) y «Desbloquear». Presentacional: emite `block` y `unblock`;
 * quien lo usa cierra el diálogo con `open` al guardar bien.
 */
withDefaults(
  defineProps<{ name: string; blocked: boolean; reason?: string | null; busy?: boolean }>(),
  { reason: null, busy: false }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{ block: [reason: string]; unblock: [] }>();

const { t } = useI18n();

const draft = ref('');
const error = ref<string | null>(null);

watch(open, (isOpen) => {
  if (!isOpen) return;
  draft.value = '';
  error.value = null;
});

function submit() {
  const reason = draft.value.trim();
  if (!reason) {
    error.value = t('customers.block.reasonRequired');
    return;
  }
  if (reason.length > 500) {
    error.value = t('customers.block.reasonTooLong');
    return;
  }
  error.value = null;
  emit('block', reason);
}
</script>

<template>
  <section class="flex flex-col gap-3">
    <Text as="h3" size="h4">{{ t('customers.block.title') }}</Text>
    <Text size="notes" class="text-muted-foreground">{{ t('customers.block.hint') }}</Text>
    <Text
      as="p"
      size="paragraph"
      :class="blocked ? 'text-destructive' : 'text-muted-foreground'"
      data-testid="customer-block-state"
    >
      {{ blocked ? t('customers.block.reason', { reason }) : t('customers.block.notBlocked') }}
    </Text>
    <Button
      v-if="blocked"
      size="sm"
      variant="secondary"
      class="self-start"
      :disabled="busy"
      @click="emit('unblock')"
    >
      {{ t('customers.block.unblock') }}
    </Button>
    <Button
      v-else
      size="sm"
      variant="secondary"
      class="self-start"
      :disabled="busy"
      @click="open = true"
    >
      {{ t('customers.block.block') }}
    </Button>

    <Dialog v-model:open="open" :title="t('customers.block.dialogTitle', { name })">
      <form id="block-form" class="flex flex-col gap-1" novalidate @submit.prevent="submit">
        <label for="block-reason" class="font-sans text-[14px] text-foreground">
          {{ t('customers.block.reasonLabel') }}
        </label>
        <textarea
          id="block-reason"
          v-model="draft"
          rows="3"
          :disabled="busy"
          :aria-invalid="!!error || undefined"
          class="w-full resize-y border border-input bg-background px-4 py-3 font-sans text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)]"
        />
        <Text v-if="error" size="notes" class="text-destructive" role="alert">{{ error }}</Text>
      </form>
      <template #footer>
        <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
          {{ t('customers.block.cancel') }}
        </Button>
        <Button size="sm" variant="primary" type="submit" form="block-form" :disabled="busy">
          {{ t('customers.block.confirm') }}
        </Button>
      </template>
    </Dialog>
  </section>
</template>
