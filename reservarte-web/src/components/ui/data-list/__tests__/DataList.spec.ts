import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import DataList from '../DataList.vue';

interface Client {
  id: number;
  name: string;
}

const clients: Client[] = [
  { id: 1, name: 'Marcos Aguilar Fernández' },
  { id: 2, name: 'Elena Torres Delgado' },
];

function mountList(props: Record<string, unknown> = {}) {
  return mount(DataList<Client>, {
    props: {
      title: 'Gestión de Clientes',
      items: clients,
      itemKey: (c: Client) => c.id,
      itemLabel: (c: Client) => c.name,
      pagination: { page: 1, pageSize: 20, totalCount: 2, totalPages: 1 },
      ...props,
    },
    global: { plugins: [i18n] },
  });
}

describe('DataList', () => {
  it('pinta el título, el buscador con nombre y una fila por elemento', () => {
    const wrapper = mountList();
    expect(wrapper.get('h1').text()).toBe('Gestión de Clientes');
    expect(wrapper.get('input[type="search"]').attributes('aria-label')).toBe('Buscar');
    expect(wrapper.findAll('li').map((li) => li.text())).toEqual([
      'Marcos Aguilar Fernández',
      'Elena Torres Delgado',
    ]);
  });

  it('cada acción emite su elemento y su botón lleva el nombre', async () => {
    const wrapper = mountList();
    await wrapper.get('[aria-label="Editar Elena Torres Delgado"]').trigger('click');
    await wrapper.get('[aria-label="Eliminar Marcos Aguilar Fernández"]').trigger('click');
    await wrapper.get('[aria-label="Ver Elena Torres Delgado"]').trigger('click');

    expect(wrapper.emitted('edit')).toEqual([[clients[1]]]);
    expect(wrapper.emitted('delete')).toEqual([[clients[0]]]);
    expect(wrapper.emitted('view')).toEqual([[clients[1]]]);
  });

  it('«Volver» y «Nuevo» emiten sus eventos', async () => {
    const wrapper = mountList();
    const [back, create] = wrapper
      .findAll('button')
      .filter((b) => ['Volver', 'Nuevo'].includes(b.text()));
    await back.trigger('click');
    await create.trigger('click');
    expect(wrapper.emitted('back')).toHaveLength(1);
    expect(wrapper.emitted('create')).toHaveLength(1);
  });

  it('la búsqueda va por v-model:search', async () => {
    const wrapper = mountList();
    await wrapper.get('input[type="search"]').setValue('elena');
    expect(wrapper.emitted('update:search')?.at(-1)).toEqual(['elena']);
  });

  it('sin elementos muestra el mensaje vacío', () => {
    const wrapper = mountList({ items: [], emptyMessage: 'No hay clientas.' });
    expect(wrapper.text()).toContain('No hay clientas.');
    expect(wrapper.find('ul').exists()).toBe(false);
  });

  it('cargando sin elementos lo anuncia', () => {
    const wrapper = mountList({ items: [], loading: true });
    expect(wrapper.get('[role="status"]').text()).toBe('Cargando…');
  });

  it('con error lo avisa y «Reintentar» emite retry', async () => {
    const wrapper = mountList({ failed: true });
    expect(wrapper.get('[role="alert"]').text()).toContain('No se ha podido cargar el listado.');
    await wrapper.get('[role="alert"] button').trigger('click');
    expect(wrapper.emitted('retry')).toHaveLength(1);
  });

  it('con una sola página no hay paginación', () => {
    expect(mountList().find('nav').exists()).toBe(false);
  });

  it('con varias páginas pide la anterior y la siguiente, y desactiva los extremos', async () => {
    const wrapper = mountList({
      pagination: { page: 1, pageSize: 20, totalCount: 45, totalPages: 3 },
    });
    const nav = wrapper.get('nav');
    expect(nav.text()).toContain('Página 1 de 3');
    const [previous, next] = nav.findAll('button');
    expect(previous.attributes('disabled')).toBeDefined();

    await next.trigger('click');
    expect(wrapper.emitted('page')).toEqual([[2]]);

    await wrapper.setProps({
      pagination: { page: 3, pageSize: 20, totalCount: 45, totalPages: 3 },
    });
    expect(wrapper.get('nav').findAll('button')[1].attributes('disabled')).toBeDefined();
  });
});
