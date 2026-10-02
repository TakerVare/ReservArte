<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { cn } from '@lib/utils/cn.utils';

export type AvatarSize = 'sm' | 'md' | 'lg';

/**
 * Foto de una persona en círculo (RA-869d7fbyt), o sus iniciales sobre `accent` si
 * no tiene foto o la imagen no carga. Decorativa (`alt=""`): el nombre va siempre
 * al lado, en texto. Tallas: `sm` 44 px (filas de listado), `md` 64 px y `lg`
 * 96 px (cabecera de la ficha, Figma «Detalle usuario» 387:56778).
 */
const props = withDefaults(
  defineProps<{
    name: string;
    src?: string | null;
    size?: AvatarSize;
    class?: string;
  }>(),
  {
    src: undefined,
    size: 'sm',
    class: '',
  }
);

const sizeClasses: Record<AvatarSize, string> = {
  sm: 'h-11 w-11 text-[16px]',
  md: 'h-16 w-16 text-[20px]',
  lg: 'h-24 w-24 text-[32px]',
};

const failed = ref(false);
watch(
  () => props.src,
  () => {
    failed.value = false;
  }
);

const initials = computed(() =>
  props.name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join('')
);

const classes = computed(() => cn('shrink-0 rounded-full', sizeClasses[props.size], props.class));
</script>

<template>
  <img
    v-if="src && !failed"
    :src="src"
    alt=""
    :class="cn(classes, 'object-cover')"
    data-testid="avatar-photo"
    @error="failed = true"
  />
  <span
    v-else
    aria-hidden="true"
    :class="
      cn(classes, 'flex items-center justify-center bg-accent font-sans font-bold text-foreground')
    "
    data-testid="avatar-initials"
  >
    {{ initials }}
  </span>
</template>
