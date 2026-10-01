import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { defineComponent } from 'vue';
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from '..';

const Example = defineComponent({
  // Registrados con prefijo: `Table` es un nombre reservado de HTML.
  components: {
    UiTable: Table,
    UiTableBody: TableBody,
    UiTableCaption: TableCaption,
    UiTableCell: TableCell,
    UiTableHead: TableHead,
    UiTableHeader: TableHeader,
    UiTableRow: TableRow,
  },
  template: `
    <UiTable>
      <UiTableCaption>Citas de hoy</UiTableCaption>
      <UiTableHeader><UiTableRow><UiTableHead>Clienta</UiTableHead></UiTableRow></UiTableHeader>
      <UiTableBody><UiTableRow><UiTableCell class="font-bold">Laura</UiTableCell></UiTableRow></UiTableBody>
    </UiTable>`,
});

describe('Table', () => {
  it('monta una tabla nativa con su estructura', () => {
    const wrapper = mount(Example);
    expect(wrapper.find('table caption').text()).toBe('Citas de hoy');
    expect(wrapper.find('table thead tr th').text()).toBe('Clienta');
    expect(wrapper.find('table tbody tr td').text()).toBe('Laura');
  });

  it('envuelve la tabla en un contenedor con scroll horizontal', () => {
    const wrapper = mount(Example);
    expect(wrapper.classes()).toContain('overflow-x-auto');
  });

  it('combina las clases del llamador', () => {
    const wrapper = mount(Example);
    expect(wrapper.find('td').classes()).toEqual(expect.arrayContaining(['font-bold', 'px-4']));
  });
});
