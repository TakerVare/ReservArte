<script setup lang="ts">
import { computed } from 'vue';
import { cn } from '@lib/utils/cn.utils';

export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'xxl';
export type ButtonVariant = 'primary' | 'secondary' | 'primary-nav' | 'secondary-nav';

const props = withDefaults(
  defineProps<{
    size?: ButtonSize;
    variant?: ButtonVariant;
    type?: 'button' | 'submit' | 'reset';
    class?: string;
  }>(),
  {
    size: 'md',
    variant: 'primary',
    type: 'button',
    class: '',
  }
);

const sizeClasses: Record<ButtonSize, string> = {
  xs: 'px-2 py-1 text-[12px] leading-[12px] tracking-[0.0458em]',
  sm: 'px-4 py-2 text-[16px] leading-[20px] tracking-[0.0344em]',
  md: 'px-6 py-3 text-[16px] leading-[20px] tracking-[0.0344em]',
  lg: 'px-8 py-4 text-[20px] leading-[20px] tracking-[0.0275em]',
  xl: 'px-12 py-6 text-[20px] leading-[20px] tracking-[0.0275em]',
  xxl: 'px-16 py-8 text-[20px] leading-[20px] tracking-[0.0275em]',
};

// Los botones de navegación tienen ancho/alto/padding fijos por tamaño (a
// diferencia de los demás, que se ajustan al contenido) y una única talla de
// texto (20px) para las 6 tallas, tal como está definido en Figma (fila
// "Enabled"). El gap icono-texto sí difiere entre primary-nav y
// secondary-nav en MD/SM/XS (así lo especifica cada componente en Figma).
const NAV_TEXT = 'text-[20px] leading-[20px] tracking-[0.0275em]';
const navDimensionClasses: Record<ButtonSize, string> = {
  xs: 'w-[155px] h-[55px] px-2 py-1',
  sm: 'w-[170px] h-[60px] px-4 py-2',
  md: 'w-[185px] h-[65px] px-6 py-3',
  lg: 'w-[200px] h-[70px] px-8 py-4',
  xl: 'w-[215px] h-[75px] px-16 py-8',
  xxl: 'w-[230px] h-[75px] px-16 py-8',
};
const navGapClasses: Record<'primary-nav' | 'secondary-nav', Record<ButtonSize, string>> = {
  'primary-nav': { xs: 'gap-1', sm: 'gap-2', md: 'gap-3', lg: 'gap-4', xl: 'gap-4', xxl: 'gap-4' },
  'secondary-nav': {
    xs: 'gap-2',
    sm: 'gap-4',
    md: 'gap-6',
    lg: 'gap-4',
    xl: 'gap-4',
    xxl: 'gap-4',
  },
};

const variantClasses: Record<ButtonVariant, string> = {
  primary: cn(
    'bg-primary border border-primary text-primary-foreground',
    'hover:-translate-y-0.5 hover:bg-primary-hover hover:border-primary-hover hover:shadow-[0_4px_12px_0_hsl(var(--primary)/40%)]',
    'active:translate-y-0 active:shadow-[0_4px_20px_0_hsl(var(--primary)/40%)]',
    'disabled:opacity-60'
  ),
  secondary: cn(
    'bg-transparent border-2 border-primary text-primary',
    'hover:bg-primary hover:text-primary-foreground',
    'active:bg-primary active:text-primary-foreground active:shadow-[0_4px_20px_0_hsl(var(--primary)/40%)]',
    'disabled:bg-primary disabled:text-primary-foreground disabled:opacity-60'
  ),
  // Mismos colores/interacción que "primary"/"secondary": en Figma los
  // botones de nav instancian el mismo componente base, solo cambian
  // tamaño e icono.
  'primary-nav': '',
  'secondary-nav': '',
};
variantClasses['primary-nav'] = variantClasses.primary;
variantClasses['secondary-nav'] = variantClasses.secondary;

const NAV_VARIANTS = new Set<ButtonVariant>(['primary-nav', 'secondary-nav']);

const classes = computed(() => {
  const isNav = NAV_VARIANTS.has(props.variant);
  const navClasses = isNav
    ? cn(
        navDimensionClasses[props.size],
        navGapClasses[props.variant as 'primary-nav' | 'secondary-nav'][props.size],
        NAV_TEXT
      )
    : '';
  return cn(
    'inline-flex items-center justify-center gap-2 whitespace-nowrap select-none',
    'font-sans font-bold',
    'transition-all duration-300 ease-in-out',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
    'disabled:pointer-events-none disabled:cursor-not-allowed',
    variantClasses[props.variant],
    isNav ? navClasses : sizeClasses[props.size],
    props.class
  );
});
</script>

<template>
  <button :type="type" :class="classes">
    <span v-if="$slots['icon-start']" class="inline-flex shrink-0"><slot name="icon-start" /></span>
    <slot />
    <span v-if="$slots['icon-end']" class="inline-flex shrink-0"><slot name="icon-end" /></span>
  </button>
</template>
