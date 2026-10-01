<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Dialog } from '@components/ui/dialog';
import { Select } from '@components/ui/select';
import { Input } from '@components/ui/input';
import { Button } from '@components/ui/button';

/**
 * Confirmación de cancelación de una cita (RA-869d7fcfy, CancelModal): selector de
 * motivo y, si es «Otro motivo», un texto libre. El motivo es opcional. En el piloto
 * cancelar no tiene penalización (`869f7axdq`, Fase 7). Emite `confirm` con el motivo
 * ya resuelto; la llamada a la API la hace quien lo usa.
 */
const props = withDefaults(
  defineProps<{
    /** Fecha y hora de la cita, ya formateadas, para la descripción. */
    when: string;
    /** Motivos predefinidos: los de la clienta o los del personal. */
    reasons: string[];
    busy?: boolean;
  }>(),
  { busy: false }
);

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{ confirm: [reason: string | undefined] }>();

const { t } = useI18n();

const OTHER = 'other';
const choice = ref<string>();
const other = ref('');

const options = computed(() => [
  ...props.reasons.map((reason) => ({ value: reason, label: reason })),
  { value: OTHER, label: t('cancel.other') },
]);

// Cada vez que se abre, empieza en blanco.
watch(open, (isOpen) => {
  if (isOpen) {
    choice.value = undefined;
    other.value = '';
  }
});

function confirm() {
  const reason = choice.value === OTHER ? other.value.trim() || undefined : choice.value;
  emit('confirm', reason);
}
</script>

<template>
  <Dialog
    v-model:open="open"
    :title="t('cancel.title')"
    :description="t('cancel.description', { when })"
  >
    <div class="flex flex-col gap-2">
      <label for="cancel-reason" class="font-sans text-foreground">{{ t('cancel.reason') }}</label>
      <Select
        id="cancel-reason"
        v-model="choice"
        :options="options"
        :placeholder="t('cancel.optional')"
      />
      <template v-if="choice === OTHER">
        <label for="cancel-reason-other" class="sr-only">{{ t('cancel.otherLabel') }}</label>
        <Input
          id="cancel-reason-other"
          v-model="other"
          maxlength="500"
          :placeholder="t('cancel.otherLabel')"
        />
      </template>
    </div>
    <template #footer>
      <Button variant="secondary" :disabled="busy" @click="open = false">{{
        t('cancel.back')
      }}</Button>
      <Button :disabled="busy" @click="confirm">{{ t('cancel.confirm') }}</Button>
    </template>
  </Dialog>
</template>
