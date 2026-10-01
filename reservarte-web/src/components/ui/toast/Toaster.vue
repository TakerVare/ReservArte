<script setup lang="ts">
import type { Component } from 'vue';
import { ToastClose, ToastDescription, ToastProvider, ToastRoot, ToastViewport } from 'reka-ui';
import { AlertTriangle, CheckCircle2, Info, X, XCircle } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';
import { useUiStore, type ToastType } from '@stores/uiStore';

/**
 * Avisos emergentes (RA-869d7fbuf) sobre Reka UI, alimentados por
 * `uiStore.addToast`. Se monta una vez en `App.vue`. Los errores se anuncian
 * como `assertive` y el resto como `polite`. No hay tokens de éxito ni de
 * aviso: el tipo se distingue por el icono y el borde (`destructive` para los
 * errores y `primary` para el resto).
 */

const DURATION_MS = 5000;

const { t } = useI18n();
const ui = useUiStore();

const icons: Record<ToastType, Component> = {
  success: CheckCircle2,
  error: XCircle,
  warning: AlertTriangle,
  info: Info,
};

function onOpenChange(id: number, open: boolean) {
  if (!open) ui.removeToast(id);
}
</script>

<template>
  <ToastProvider :duration="DURATION_MS" :label="t('ui.toast.region')">
    <ToastRoot
      v-for="toast in ui.toasts"
      :key="toast.id"
      :type="toast.type === 'error' ? 'foreground' : 'background'"
      :class="[
        'flex items-start gap-3 border border-l-4 border-input bg-background p-4 shadow-[0_10px_30px_hsl(var(--foreground)/10%)]',
        toast.type === 'error' ? 'border-l-destructive' : 'border-l-primary',
      ]"
      :data-type="toast.type"
      @update:open="onOpenChange(toast.id, $event)"
    >
      <component
        :is="icons[toast.type]"
        :class="[
          'mt-0.5 h-5 w-5 shrink-0',
          toast.type === 'error' ? 'text-destructive' : 'text-primary',
        ]"
        aria-hidden="true"
      />
      <ToastDescription class="flex-1 font-sans text-[16px] leading-[normal] text-foreground">
        {{ toast.message }}
      </ToastDescription>
      <ToastClose
        :aria-label="t('ui.toast.close')"
        class="shrink-0 text-muted-foreground outline-none transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
      >
        <X class="h-4 w-4" aria-hidden="true" />
      </ToastClose>
    </ToastRoot>
    <ToastViewport
      class="fixed bottom-[116px] left-1/2 z-[100] flex w-[calc(100%-2rem)] max-w-[420px] -translate-x-1/2 flex-col gap-2 outline-none"
    />
  </ToastProvider>
</template>
