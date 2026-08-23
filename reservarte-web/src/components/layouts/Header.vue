<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { Menu, LogOut } from 'lucide-vue-next';
import { Text } from '@components/ui/text';
import { useAuthStore } from '@stores/authStore';
import { useUiStore } from '@stores/uiStore';

const router = useRouter();
const authStore = useAuthStore();
const uiStore = useUiStore();

const displayName = computed(() => {
  const user = authStore.user;
  if (!user) return '';
  const fullName = `${user.firstName} ${user.lastName}`.trim();
  return fullName || user.email;
});

function handleLogout() {
  authStore.logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <header
    class="relative z-50 flex h-16 shrink-0 items-center justify-between border-b border-border bg-background px-4"
  >
    <button
      type="button"
      aria-label="Abrir menú"
      class="rounded-full p-2 text-foreground outline-none transition-colors hover:text-primary focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background md:hidden"
      @click="uiStore.toggleSidebar()"
    >
      <Menu class="h-6 w-6" />
    </button>

    <Text size="paragraph" class="min-w-0 flex-1 truncate px-2 text-center md:text-left">{{
      displayName
    }}</Text>

    <button
      type="button"
      aria-label="Cerrar sesión"
      class="flex items-center gap-2 rounded-full p-2 text-foreground outline-none transition-colors hover:text-primary focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background"
      @click="handleLogout"
    >
      <LogOut class="h-5 w-5" />
      <Text size="notes" class="hidden sm:block">Cerrar sesión</Text>
    </button>
  </header>
</template>
