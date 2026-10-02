<script setup lang="ts" generic="T">
import type { Component } from 'vue';
import { useI18n } from 'vue-i18n';
import { HeroBanner } from '@components/ui/hero-banner';
import { ListItem } from '@components/ui/list-item';
import { Button } from '@components/ui/button';
import { Text } from '@components/ui/text';
import type { Pagination } from '@lib/composables/useDataList';
import ArrowLeftIcon from '@assets/icons/arrow-left.svg';
import UserPlusIcon from '@assets/icons/user-plus.svg';

/**
 * Listado de gestión (RA-869d7fbxn), según Figma «CRUD» (387:56720): cabecera
 * con título, «Volver», «Nuevo» y buscador (`HeroBanner`), y una fila
 * `ListItem` por elemento con editar, eliminar y ver. Presentacional: el estado
 * lo lleva `useDataList`. La paginación y la ranura de filtros no están en
 * Figma: siguen el mismo estilo y solo aparecen si hacen falta.
 */
const props = withDefaults(
  defineProps<{
    title: string;
    items: T[];
    itemKey: (item: T) => string | number;
    itemLabel: (item: T) => string;
    pagination: Pagination;
    loading?: boolean;
    /** Hay error de carga: se muestra el aviso y «Reintentar». */
    failed?: boolean;
    createLabel?: string;
    createIcon?: Component;
    emptyMessage?: string;
    /** Con foto: muestra el avatar de cada fila (RA-869d7fbyt). */
    itemPhoto?: (item: T) => string | null | undefined;
    itemDetail?: (item: T) => string | undefined;
    itemDeletable?: (item: T) => boolean;
  }>(),
  {
    loading: false,
    failed: false,
    createLabel: undefined,
    createIcon: undefined,
    emptyMessage: undefined,
    itemPhoto: undefined,
    itemDetail: undefined,
    itemDeletable: undefined,
  }
);

const search = defineModel<string>('search', { default: '' });

const emit = defineEmits<{
  back: [];
  create: [];
  edit: [item: T];
  delete: [item: T];
  view: [item: T];
  page: [page: number];
  retry: [];
}>();

const { t } = useI18n();
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <!-- Como en Figma: la banda del título va de borde a borde y las filas llevan ~18 px dentro. -->
    <div class="w-full max-w-[393px] md:max-w-[600px] xl:max-w-[800px]">
      <HeroBanner
        v-model:search="search"
        :title="title"
        searchable
        :search-placeholder="t('ui.list.search')"
      >
        <template #primary-button>
          <Button size="sm" variant="primary" @click="emit('back')">
            <template #icon-start><ArrowLeftIcon class="h-4 w-4" aria-hidden="true" /></template>
            {{ t('ui.list.back') }}
          </Button>
        </template>
        <template #secondary-button>
          <Button size="sm" variant="secondary" @click="emit('create')">
            {{ createLabel ?? t('ui.list.create') }}
            <template #icon-end>
              <component
                :is="props.createIcon ?? UserPlusIcon"
                class="h-4 w-4"
                aria-hidden="true"
              />
            </template>
          </Button>
        </template>
      </HeroBanner>

      <div
        v-if="$slots.filters"
        class="flex flex-wrap gap-4 border-b border-border px-[18px] py-2.5"
      >
        <slot name="filters" />
      </div>

      <div :aria-busy="loading" class="w-full">
        <Text v-if="failed" size="paragraph" class="py-8 text-center text-destructive" role="alert">
          {{ t('ui.list.error') }}
          <Button size="sm" variant="secondary" class="ml-4" @click="emit('retry')">
            {{ t('ui.list.retry') }}
          </Button>
        </Text>
        <Text
          v-else-if="loading && items.length === 0"
          size="paragraph"
          class="py-8 text-center"
          role="status"
        >
          {{ t('ui.list.loading') }}
        </Text>
        <Text
          v-else-if="items.length === 0"
          size="paragraph"
          class="py-8 text-center text-muted-foreground"
        >
          {{ emptyMessage ?? t('ui.list.empty') }}
        </Text>
        <ul v-else class="w-full border-b border-border">
          <li v-for="item in items" :key="itemKey(item)">
            <ListItem
              size="sm"
              class="px-[18px]"
              :label="itemLabel(item)"
              :avatar="!!props.itemPhoto"
              :photo-url="props.itemPhoto?.(item)"
              :detail="props.itemDetail?.(item)"
              :deletable="props.itemDeletable?.(item) ?? true"
              @edit="emit('edit', item)"
              @delete="emit('delete', item)"
              @view="emit('view', item)"
            />
          </li>
        </ul>
      </div>

      <nav
        v-if="pagination.totalPages > 1"
        :aria-label="t('ui.list.page', { page: pagination.page, total: pagination.totalPages })"
        class="flex items-center justify-between gap-4 px-[18px] py-6"
      >
        <Button
          size="sm"
          variant="secondary"
          :disabled="pagination.page <= 1 || loading"
          @click="emit('page', pagination.page - 1)"
        >
          {{ t('ui.list.previous') }}
        </Button>
        <Text size="notes" class="text-muted-foreground">
          {{ t('ui.list.page', { page: pagination.page, total: pagination.totalPages }) }}
        </Text>
        <Button
          size="sm"
          variant="secondary"
          :disabled="pagination.page >= pagination.totalPages || loading"
          @click="emit('page', pagination.page + 1)"
        >
          {{ t('ui.list.next') }}
        </Button>
      </nav>
    </div>
  </div>
</template>
