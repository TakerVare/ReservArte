<script setup lang="ts">
import { ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { Button } from '@components/ui/button';
import { Dialog } from '@components/ui/dialog';
import { Input } from '@components/ui/input';
import { Text } from '@components/ui/text';

/** Alta rápida de una categoría del catálogo (RA-869d7fc6b): solo el nombre. */
withDefaults(defineProps<{ busy?: boolean }>(), { busy: false });

const open = defineModel<boolean>('open', { default: false });
const emit = defineEmits<{ create: [name: string] }>();

const { t } = useI18n();

const name = ref('');
const error = ref<string | null>(null);

watch(open, (isOpen) => {
  if (!isOpen) return;
  name.value = '';
  error.value = null;
});

function submit() {
  const value = name.value.trim();
  if (!value) {
    error.value = t('services.category.required');
    return;
  }
  if (value.length > 100) {
    error.value = t('services.category.tooLong');
    return;
  }
  error.value = null;
  emit('create', value);
}
</script>

<template>
  <Dialog v-model:open="open" :title="t('services.category.title')">
    <form id="category-form" class="flex flex-col gap-1" novalidate @submit.prevent="submit">
      <label for="category-name" class="font-sans text-[14px] text-foreground">
        {{ t('services.category.name') }}
      </label>
      <Input
        id="category-name"
        v-model="name"
        maxlength="100"
        :invalid="!!error"
        :disabled="busy"
      />
      <Text v-if="error" size="notes" class="text-destructive" role="alert">{{ error }}</Text>
    </form>
    <template #footer>
      <Button size="sm" variant="secondary" :disabled="busy" @click="open = false">
        {{ t('services.category.cancel') }}
      </Button>
      <Button size="sm" variant="primary" type="submit" form="category-form" :disabled="busy">
        {{ t('services.category.save') }}
      </Button>
    </template>
  </Dialog>
</template>
