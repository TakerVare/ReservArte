<script setup lang="ts">
import { useI18n } from 'vue-i18n';
import { Dialog } from '@components/ui/dialog';
import { Input } from '@components/ui/input';
import { Text } from '@components/ui/text';
import { Avatar } from '@components/ui/avatar';

export interface CustomerPickerItem {
  id: number;
  name: string;
  photoUrl?: string | null;
}

/**
 * Buscador de clientas del personal en la pantalla de reserva (RA-869fagpyg): foto
 * y nombre de cada una; al elegir, emite `select` y se cierra. Presentacional: la
 * búsqueda la lleva quien lo usa (v-model `search`).
 */
defineProps<{
  customers: CustomerPickerItem[];
  loading?: boolean;
}>();

const open = defineModel<boolean>('open', { default: false });
const search = defineModel<string>('search', { default: '' });

const emit = defineEmits<{ select: [customer: CustomerPickerItem] }>();

const { t } = useI18n();

function choose(customer: CustomerPickerItem) {
  emit('select', customer);
  open.value = false;
}
</script>

<template>
  <Dialog v-model:open="open" :title="t('booking.customer.pickerTitle')">
    <Input
      v-model="search"
      type="search"
      :aria-label="t('booking.customer.search')"
      :placeholder="t('booking.customer.search')"
    />
    <Text
      v-if="loading && customers.length === 0"
      size="paragraph"
      role="status"
      class="py-4 text-center"
    >
      {{ t('ui.list.loading') }}
    </Text>
    <Text
      v-else-if="customers.length === 0"
      size="paragraph"
      class="py-4 text-center text-muted-foreground"
    >
      {{ t('booking.customer.none') }}
    </Text>
    <ul v-else class="flex max-h-[50vh] flex-col overflow-y-auto">
      <li
        v-for="customer in customers"
        :key="customer.id"
        class="border-b border-border last:border-0"
      >
        <button
          type="button"
          class="flex w-full items-center gap-4 px-2 py-3 text-left outline-none transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring"
          @click="choose(customer)"
        >
          <Avatar :name="customer.name" :src="customer.photoUrl" />
          <Text as="span" size="h4">{{ customer.name }}</Text>
        </button>
      </li>
    </ul>
  </Dialog>
</template>
