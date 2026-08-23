<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink } from 'vue-router';
import {
  LayoutDashboard,
  Users,
  User,
  Scissors,
  Calendar,
  CreditCard,
  Bell,
  Settings,
  PanelLeftClose,
  PanelLeftOpen,
} from 'lucide-vue-next';
import { useUiStore } from '@stores/uiStore';

const uiStore = useUiStore();
const collapsed = ref(false);

const navItems = [
  { label: 'Dashboard', to: { name: 'dashboard' }, icon: LayoutDashboard },
  { label: 'Empleados', to: { name: 'employees' }, icon: Users },
  { label: 'Clientes', to: { name: 'customers' }, icon: User },
  { label: 'Servicios', to: { name: 'services' }, icon: Scissors },
  { label: 'Citas', to: { name: 'appointments' }, icon: Calendar },
  { label: 'Pagos', to: { name: 'payments' }, icon: CreditCard },
  { label: 'Recordatorios', to: { name: 'reminders' }, icon: Bell },
  { label: 'Configuración', to: { name: 'settings' }, icon: Settings },
];
</script>

<template>
  <aside
    class="fixed inset-y-0 left-0 z-40 flex -translate-x-full flex-col border-r border-border bg-background transition-all duration-300 md:static md:translate-x-0"
    :class="[uiStore.sidebarOpen ? 'translate-x-0' : '', collapsed ? 'w-20' : 'w-64']"
  >
    <div class="flex items-center justify-end p-4">
      <button
        type="button"
        :aria-label="collapsed ? 'Expandir menú' : 'Colapsar menú'"
        class="rounded-full p-2 text-muted-foreground outline-none transition-colors hover:text-primary focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
        @click="collapsed = !collapsed"
      >
        <component :is="collapsed ? PanelLeftOpen : PanelLeftClose" class="h-5 w-5" />
      </button>
    </div>

    <nav class="flex flex-1 flex-col gap-1 overflow-y-auto px-2 pb-4">
      <RouterLink
        v-for="item in navItems"
        :key="item.label"
        :to="item.to"
        class="flex items-center gap-3 px-3 py-2 font-sans text-foreground outline-none transition-colors hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring [&.router-link-exact-active]:bg-primary [&.router-link-exact-active]:text-primary-foreground"
      >
        <component :is="item.icon" class="h-5 w-5 shrink-0" />
        <span v-if="!collapsed" class="truncate">{{ item.label }}</span>
      </RouterLink>
    </nav>
  </aside>
</template>
