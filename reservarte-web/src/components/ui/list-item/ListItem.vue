<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { Text, type TextSize } from '@components/ui/text';
import { Avatar } from '@components/ui/avatar';
import { cn } from '@lib/utils/cn.utils';
import Pencil from '@assets/icons/action-pencil.svg';
import Trash from '@assets/icons/action-trash.svg';
import Eye from '@assets/icons/action-eye.svg';

export type ListItemSize = 'lg' | 'md' | 'sm' | 'xs';

const props = withDefaults(
  defineProps<{
    label: string;
    size?: ListItemSize;
    /** Foto (o iniciales) a la izquierda del nombre (RA-869d7fbyt). */
    avatar?: boolean;
    photoUrl?: string | null;
    /** Línea secundaria bajo el nombre (rol, «De baja»…). */
    detail?: string;
    /** Sin «Eliminar» (p. ej., una ficha que ya está de baja). */
    deletable?: boolean;
  }>(),
  {
    size: 'md',
    avatar: false,
    photoUrl: undefined,
    detail: undefined,
    deletable: true,
  }
);

defineEmits<{
  edit: [];
  delete: [];
  view: [];
}>();

// Tamaños tal como están definidos en Figma: LG/MD comparten icono grande
// (56px) y separación de 20px; SM/XS comparten icono más compacto (44px)
// y separación de 4px. El texto usa directamente los estilos H2/H3/H4 de
// Text (H4 ya es negrita por definición, coherente con SM/XS aquí).
const TEXT_SIZE: Record<ListItemSize, TextSize> = {
  lg: 'h2',
  md: 'h3',
  sm: 'h4',
  xs: 'h4',
};

const rowPadding: Record<ListItemSize, string> = {
  lg: 'py-8',
  md: 'py-6',
  sm: 'py-[11px]',
  xs: 'py-[11px]',
};

const iconSizeClasses: Record<ListItemSize, string> = {
  lg: 'h-14 w-14',
  md: 'h-14 w-14',
  sm: 'h-11 w-11',
  xs: 'h-11 w-11',
};

const actionsGapClasses: Record<ListItemSize, string> = {
  lg: 'gap-5',
  md: 'gap-5',
  sm: 'gap-1',
  xs: 'gap-1',
};

const { t } = useI18n();

const textSize = computed(() => TEXT_SIZE[props.size]);
const rowClasses = computed(() =>
  cn('flex w-full items-center justify-between border-t border-border', rowPadding[props.size])
);
const iconClasses = computed(() => iconSizeClasses[props.size]);
const actionsGapClass = computed(() => actionsGapClasses[props.size]);

const actionButtonClasses =
  'flex items-center justify-center text-primary transition-colors hover:text-primary-hover outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background';
</script>

<template>
  <div :class="rowClasses">
    <div class="flex min-w-0 items-center gap-4 pr-4">
      <Avatar v-if="avatar" :name="label" :src="photoUrl" />
      <!-- Como en Figma («CRUD», 387:56720): el nombre parte en dos líneas, no se corta. -->
      <div class="flex min-w-0 flex-col gap-1">
        <Text as="p" :size="textSize" class="break-words">{{ label }}</Text>
        <Text v-if="detail" as="p" size="notes" class="text-muted-foreground">{{ detail }}</Text>
      </div>
    </div>
    <div class="flex shrink-0 items-center" :class="actionsGapClass">
      <button
        type="button"
        :class="actionButtonClasses"
        :aria-label="t('ui.list.edit', { name: label })"
        @click="$emit('edit')"
      >
        <Pencil :class="iconClasses" />
      </button>
      <button
        v-if="deletable"
        type="button"
        :class="actionButtonClasses"
        :aria-label="t('ui.list.delete', { name: label })"
        @click="$emit('delete')"
      >
        <Trash :class="iconClasses" />
      </button>
      <button
        type="button"
        :class="actionButtonClasses"
        :aria-label="t('ui.list.view', { name: label })"
        @click="$emit('view')"
      >
        <Eye :class="iconClasses" />
      </button>
    </div>
  </div>
</template>
