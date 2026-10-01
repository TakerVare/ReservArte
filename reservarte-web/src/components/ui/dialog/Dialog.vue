<script setup lang="ts">
import {
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogOverlay,
  DialogPortal,
  DialogRoot,
  DialogTitle,
  DialogTrigger,
} from 'reka-ui';
import { X } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';
import { Text } from '@components/ui/text';

/**
 * Diálogo modal (RA-869d7fbuf) sobre Reka UI: foco atrapado, Escape y clic
 * fuera para cerrar, y `aria-labelledby`/`aria-describedby` con el título y la
 * descripción. Estilo de `styles-reference.html` §6.2: fondo blanco, sombra
 * suave, ángulos rectos y hasta 500 px de ancho; los botones van en `footer`,
 * alineados a la derecha.
 */
withDefaults(
  defineProps<{
    title: string;
    description?: string;
  }>(),
  {
    description: undefined,
  }
);

const open = defineModel<boolean>('open', { default: false });

const { t } = useI18n();
</script>

<template>
  <DialogRoot v-model:open="open">
    <DialogTrigger v-if="$slots.trigger" as-child>
      <slot name="trigger" />
    </DialogTrigger>
    <DialogPortal>
      <DialogOverlay class="fixed inset-0 z-50 bg-foreground/40" />
      <DialogContent
        class="fixed left-1/2 top-1/2 z-50 flex max-h-[85vh] w-[calc(100%-2rem)] max-w-[500px] -translate-x-1/2 -translate-y-1/2 flex-col gap-4 overflow-y-auto bg-background p-8 shadow-[0_10px_30px_hsl(var(--foreground)/10%)] focus:outline-none"
      >
        <DialogTitle as-child>
          <Text size="h3" as="h2" class="pr-8">{{ title }}</Text>
        </DialogTitle>
        <DialogDescription v-if="description" as-child>
          <Text size="paragraph" class="text-muted-foreground">{{ description }}</Text>
        </DialogDescription>

        <slot />

        <div v-if="$slots.footer" class="mt-2 flex flex-wrap justify-end gap-4">
          <slot name="footer" />
        </div>

        <DialogClose
          :aria-label="t('ui.dialog.close')"
          class="absolute right-4 top-4 text-muted-foreground outline-none transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
        >
          <X class="h-5 w-5" aria-hidden="true" />
        </DialogClose>
      </DialogContent>
    </DialogPortal>
  </DialogRoot>
</template>
