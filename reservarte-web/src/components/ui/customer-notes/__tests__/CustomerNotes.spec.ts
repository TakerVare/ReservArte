import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import { i18n } from '@/i18n';
import CustomerNotes from '../CustomerNotes.vue';

const notes = [
  {
    id: 1,
    note: 'Con autora',
    employeeId: 2,
    employeeName: 'Lucía',
    createdAt: '2026-09-30T15:00:00Z',
  },
  {
    id: 2,
    note: 'Sin autora',
    employeeId: 3,
    employeeName: null,
    createdAt: '2026-09-29T15:00:00Z',
  },
];

const mountNotes = () => mount(CustomerNotes, { props: { notes }, global: { plugins: [i18n] } });

describe('CustomerNotes', () => {
  it('cada nota lleva su autora si la hay', () => {
    const items = mountNotes().findAll('li');
    expect(items[0]!.text()).toContain('Lucía · 30/09/2026');
    expect(items[1]!.text()).not.toContain('·');
  });

  it('emite la nota sin espacios y no deja mandar una vacía o demasiado larga', async () => {
    const wrapper = mountNotes();
    const submit = () => wrapper.get('form').trigger('submit');

    await wrapper.get('textarea').setValue('   ');
    await submit();
    expect(wrapper.emitted('add')).toBeUndefined();

    await wrapper.get('textarea').setValue('x'.repeat(2001));
    expect(wrapper.text()).toContain('La nota no puede superar los 2000 caracteres.');
    await submit();
    expect(wrapper.emitted('add')).toBeUndefined();

    await wrapper.get('textarea').setValue('  Trae su tinte  ');
    await submit();
    expect(wrapper.emitted('add')![0]).toEqual(['Trae su tinte']);
  });

  it('«clear» vacía el borrador (lo llama la página al guardar bien)', async () => {
    const wrapper = mountNotes();
    await wrapper.get('textarea').setValue('Borrador');
    (wrapper.vm as unknown as { clear: () => void }).clear();
    await wrapper.vm.$nextTick();
    expect((wrapper.get('textarea').element as HTMLTextAreaElement).value).toBe('');
  });
});
