<script setup lang="ts">
import { computed, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import { Trash2 } from 'lucide-vue-next';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import { formatDateSpain, formatTimeSpain } from '@lib/utils/date.utils';
import type { CustomerNote } from '@features/customers/types/customer.types';

/**
 * Notas internas del personal sobre una clienta (RA-869d7fc34): alta con texto
 * libre (2000 caracteres como mucho, lo que admite la API) y lista de la más
 * reciente a la más antigua, con su autora. Presentacional: emite `add` y `remove`.
 */
const props = withDefaults(defineProps<{ notes: CustomerNote[]; busy?: boolean }>(), {
  busy: false,
});

const emit = defineEmits<{ add: [note: string]; remove: [note: CustomerNote] }>();

const { t } = useI18n();

const MAX = 2000;
const draft = ref('');
const tooLong = computed(() => draft.value.trim().length > MAX);

function when(note: CustomerNote) {
  const date = new Date(note.createdAt);
  return `${formatDateSpain(date)} ${formatTimeSpain(date)}`;
}

function byline(note: CustomerNote) {
  return note.employeeName
    ? t('customers.notes.by', { name: note.employeeName, date: when(note) })
    : t('customers.notes.byUnknown', { date: when(note) });
}

function add() {
  const text = draft.value.trim();
  if (!text || tooLong.value || props.busy) return;
  emit('add', text);
}

/** Quien lo usa lo llama al guardar bien la nota. */
function clear() {
  draft.value = '';
}

defineExpose({ clear });
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex flex-col gap-2">
      <Text as="h2" size="h3">{{ t('customers.notes.title') }}</Text>
      <Text size="notes" class="text-muted-foreground">{{ t('customers.notes.hint') }}</Text>
    </div>

    <form class="flex flex-col gap-2" novalidate @submit.prevent="add">
      <label for="customer-note" class="font-sans text-[14px] text-foreground">
        {{ t('customers.notes.label') }}
      </label>
      <textarea
        id="customer-note"
        v-model="draft"
        rows="3"
        :disabled="busy"
        :aria-invalid="tooLong || undefined"
        :aria-describedby="tooLong ? 'customer-note-error' : undefined"
        class="w-full resize-y border border-input bg-background px-4 py-3 font-sans text-foreground outline-none focus:border-primary focus:shadow-[0_0_0_3px_hsl(var(--primary)/20%)] disabled:cursor-not-allowed disabled:opacity-60"
      />
      <Text v-if="tooLong" id="customer-note-error" size="notes" class="text-destructive">
        {{ t('customers.notes.tooLong') }}
      </Text>
      <Button
        type="submit"
        size="sm"
        variant="primary"
        class="self-start"
        :disabled="busy || !draft.trim() || tooLong"
      >
        {{ t('customers.notes.add') }}
      </Button>
    </form>

    <Text v-if="notes.length === 0" size="paragraph" class="py-4 text-muted-foreground">
      {{ t('customers.notes.empty') }}
    </Text>
    <ul v-else class="flex flex-col border-b border-border">
      <li
        v-for="note in notes"
        :key="note.id"
        class="flex items-start justify-between gap-4 border-t border-border py-3"
      >
        <div class="flex min-w-0 flex-col gap-1">
          <Text as="p" size="paragraph" class="whitespace-pre-line break-words">{{
            note.note
          }}</Text>
          <Text as="p" size="notes" class="text-muted-foreground">{{ byline(note) }}</Text>
        </div>
        <button
          type="button"
          class="flex h-11 w-11 shrink-0 items-center justify-center text-primary outline-none transition-colors hover:text-primary-hover focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
          :disabled="busy"
          :aria-label="t('customers.notes.remove', { date: when(note) })"
          @click="emit('remove', note)"
        >
          <Trash2 class="h-5 w-5" aria-hidden="true" />
        </button>
      </li>
    </ul>
  </div>
</template>
