import { beforeEach, describe, expect, it } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { createMemoryHistory, createRouter, type Router } from 'vue-router';
import { i18n } from '@/i18n';
import { router as appRouter } from '@/router';
import { useAuthStore, type AuthUser } from '@stores/authStore';
import type { UserRole } from '@features/auth/types/auth.types';
import AccountPage from '../AccountPage.vue';

/**
 * Pantalla de Usuario (RA-869ep9p36): el acceso a la gestión según el rol,
 * con las rutas reales de la app.
 */

function userWith(rol: UserRole): AuthUser {
  return { id: 1, email: 'a@b.com', firstName: 'Ana', lastName: 'López', rol };
}

let router: Router;

async function mountAs(rol: UserRole | null) {
  const store = useAuthStore();
  store.login({ user: rol ? userWith(rol) : null, accessToken: 'jwt' });
  router = createRouter({ history: createMemoryHistory(), routes: appRouter.getRoutes() });
  await router.push('/cuenta');
  const wrapper = mount(AccountPage, { global: { plugins: [router, i18n] } });
  await flushPromises();
  return wrapper;
}

function sectionTitles(wrapper: Awaited<ReturnType<typeof mountAs>>) {
  return wrapper.findAll('h2').map((h) => h.text());
}

beforeEach(() => {
  localStorage.clear();
  setActivePinia(createPinia());
});

describe('AccountPage', () => {
  it.each(['Admin', 'Manager', 'Employee'] as const)(
    '%s ve el área de administración y la de usuario',
    async (rol) => {
      const wrapper = await mountAs(rol);

      expect(sectionTitles(wrapper)).toEqual(['Área de administración', 'Área de usuario']);
    }
  );

  it('una clienta solo ve el área de usuario', async () => {
    const wrapper = await mountAs('Customer');

    expect(sectionTitles(wrapper)).toEqual(['Área de usuario']);
    expect(wrapper.text()).not.toContain('Empleados');
  });

  it('sin usuario cargado (rol desconocido) no muestra el área de administración', async () => {
    const wrapper = await mountAs(null);

    expect(sectionTitles(wrapper)).toEqual(['Área de usuario']);
  });

  it('cada opción de gestión enlaza con su pantalla', async () => {
    const wrapper = await mountAs('Admin');
    const hrefs = Object.fromEntries(
      wrapper.findAll('a').map((a) => [a.text(), a.attributes('href')])
    );

    expect(hrefs).toMatchObject({
      Citas: '/citas',
      Usuarios: '/usuarios',
      Servicios: '/servicios',
      Empleados: '/empleados',
      'Datos de usuario': '/cuenta/datos',
      'Métodos de pago': '/cuenta/metodos-pago',
      Notificaciones: '/cuenta/notificaciones',
      Privacidad: '/cuenta/privacidad',
      'Acerca de More Than Brows': '/acerca-de',
    });
  });

  it('«Cerrar sesión» cierra la sesión y vuelve a login', async () => {
    const wrapper = await mountAs('Employee');
    const store = useAuthStore();

    await wrapper.get('button').trigger('click');
    await flushPromises();

    expect(wrapper.get('button').text()).toBe('Cerrar sesión');
    expect(store.isAuthenticated).toBe(false);
    expect(localStorage.getItem('authToken')).toBeNull();
    expect(router.currentRoute.value.name).toBe('login');
  });
});
