<script setup lang="ts">
import { computed } from 'vue';
import { useI18n } from 'vue-i18n';
import { useAuthStore } from '@stores/authStore';
import { BottomNav, type BottomNavItem } from '@components/ui/bottom-nav';
import HomeIcon from '@assets/icons/nav-home.svg';
import MapPinIcon from '@assets/icons/nav-map-pin.svg';
import UserIcon from '@assets/icons/nav-user.svg';

const { t } = useI18n();
const authStore = useAuthStore();

// Los 3 destinos son fijos y SIEMPRE visibles (patrón de navegación global).
// El Home es condicional: con sesión → citas del cliente; sin sesión → login.
// «Mi cuenta» (antes «Cuenta») nombra igual el destino y el título de la
// pantalla (RA-869faaunu).
const navItems = computed<BottomNavItem[]>(() => [
  {
    label: t('nav.home'),
    to: authStore.isAuthenticated ? { name: 'my-appointments' } : { name: 'login' },
    icon: HomeIcon,
  },
  { label: t('nav.contact'), to: { name: 'contact' }, icon: MapPinIcon },
  { label: t('nav.account'), to: { name: 'account' }, icon: UserIcon },
]);
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <div class="flex-1">
      <router-view />
    </div>
    <BottomNav :items="navItems" class="sticky bottom-0 z-40" />
  </div>
</template>
