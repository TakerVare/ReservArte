<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { Banner } from '@components/ui/banner';
import { Menu, type MenuSection } from '@components/ui/menu';
import { useAuthStore } from '@stores/authStore';
import type { UserRole } from '@features/auth/types/auth.types';
import logo from '@assets/images/Logo_Recto_More_Than_Brows_SIN_fondo.png';

/**
 * Pantalla de Usuario (destino «Cuenta» del BottomNav, Figma «User (admin)»):
 * el acceso a la gestión (RA-869ep9p36). No hay Sidebar: el personal entra a
 * cada módulo desde el «Área de administración»; todos, a lo suyo desde el
 * «Área de usuario».
 */

const STAFF_ROLES: readonly UserRole[] = ['Admin', 'Manager', 'Employee'];

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();

// Sin usuario cargado (p. ej. tras recargar: la rehidratación llega con
// 869f6r6hc) no se sabe el rol y el área de administración no se muestra.
const isStaff = computed(() => {
  const role = authStore.user?.rol;
  return role !== undefined && STAFF_ROLES.includes(role);
});

const sections = computed<MenuSection[]>(() => {
  const user: MenuSection = {
    key: 'user',
    label: t('account.user.title'),
    items: [
      { key: 'profile', label: t('account.user.profile'), to: { name: 'account-profile' } },
      {
        key: 'payment-methods',
        label: t('account.user.paymentMethods'),
        to: { name: 'account-payment-methods' },
      },
      {
        key: 'notifications',
        label: t('account.user.notifications'),
        to: { name: 'account-notifications' },
      },
      { key: 'settings', label: t('account.user.settings'), to: { name: 'account-settings' } },
      { key: 'privacy', label: t('account.user.privacy'), to: { name: 'account-privacy' } },
      { key: 'about', label: t('account.user.about'), to: { name: 'about' } },
      // No está en Figma: sin el Header retirado, era el único sitio para salir.
      { key: 'logout', label: t('account.user.logout') },
    ],
  };

  if (!isStaff.value) {
    return [user];
  }

  const admin: MenuSection = {
    key: 'admin',
    label: t('account.admin.title'),
    items: [
      { key: 'appointments', label: t('account.admin.appointments'), to: { name: 'appointments' } },
      { key: 'users', label: t('account.admin.users'), to: { name: 'customers' } },
      { key: 'services', label: t('account.admin.services'), to: { name: 'services' } },
      { key: 'employees', label: t('account.admin.employees'), to: { name: 'employees' } },
      { key: 'settings', label: t('account.admin.settings'), to: { name: 'settings' } },
    ],
  };

  return [admin, user];
});

function onSelect(key: string) {
  if (key === 'logout') {
    authStore.logout();
    router.push({ name: 'login' });
  }
}
</script>

<template>
  <div class="flex w-full flex-col items-center">
    <Banner :logo-src="logo" logo-alt="More Than Brows" />
    <h1 class="sr-only">{{ t('account.title') }}</h1>
    <div class="w-full max-w-[393px]">
      <Menu :sections="sections" @select="onSelect" />
    </div>
  </div>
</template>
