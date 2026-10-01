import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { i18n } from '@/i18n';
import { useUiStore } from '@stores/uiStore';
import Toaster from '../Toaster.vue';

let wrapper: VueWrapper | undefined;

beforeEach(() => {
  setActivePinia(createPinia());
});

afterEach(() => {
  wrapper?.unmount();
  document.body.innerHTML = '';
});

describe('Toaster', () => {
  it('muestra los avisos del uiStore', async () => {
    wrapper = mount(Toaster, { global: { plugins: [i18n] }, attachTo: document.body });
    const ui = useUiStore();

    ui.addToast('Cita cancelada', 'success');
    ui.addToast('No se ha podido guardar', 'error');
    await flushPromises();

    expect(document.body.textContent).toContain('Cita cancelada');
    expect(document.body.textContent).toContain('No se ha podido guardar');
    const error = document.querySelector('[data-type="error"]');
    expect(error?.className).toContain('border-l-destructive');
  });

  it('«Cerrar aviso» lo quita del store', async () => {
    wrapper = mount(Toaster, { global: { plugins: [i18n] }, attachTo: document.body });
    const ui = useUiStore();
    ui.addToast('Cita cancelada', 'success');
    await flushPromises();

    (document.querySelector('[aria-label="Cerrar aviso"]') as HTMLButtonElement).click();
    await flushPromises();

    expect(ui.toasts).toHaveLength(0);
  });
});
