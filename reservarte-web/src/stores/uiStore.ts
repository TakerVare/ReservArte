import { defineStore } from 'pinia';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastMessage {
  id: number;
  message: string;
  type: ToastType;
}

export const useUiStore = defineStore('ui', {
  state: () => ({
    isLoading: false,
    sidebarOpen: true,
    toasts: [] as ToastMessage[],
  }),

  actions: {
    addToast(message: string, type: ToastType = 'info') {
      this.toasts.push({
        id: Date.now() + this.toasts.length,
        message,
        type,
      });
    },

    removeToast(id: number) {
      this.toasts = this.toasts.filter((toast) => toast.id !== id);
    },

    toggleSidebar() {
      this.sidebarOpen = !this.sidebarOpen;
    },
  },
});
