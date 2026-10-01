import { afterEach, describe, expect, it } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { i18n } from '@/i18n';
import Dialog from '../Dialog.vue';

let wrapper: VueWrapper | undefined;

function mountDialog(open: boolean) {
  wrapper = mount(Dialog, {
    props: {
      open,
      title: 'Cancelar cita',
      description: '¿Seguro que quieres cancelarla?',
      'onUpdate:open': (v: boolean) => wrapper!.setProps({ open: v }),
    },
    slots: { default: '<p>Cuerpo</p>', footer: '<button>Confirmar</button>' },
    global: { plugins: [i18n] },
    attachTo: document.body,
  });
  return wrapper;
}

afterEach(() => {
  wrapper?.unmount();
  document.body.innerHTML = '';
});

describe('Dialog', () => {
  it('cerrado no pinta nada', async () => {
    mountDialog(false);
    await flushPromises();
    expect(document.querySelector('[role="dialog"]')).toBeNull();
  });

  it('abierto es un diálogo con nombre y descripción accesibles', async () => {
    mountDialog(true);
    await flushPromises();

    const dialog = document.querySelector('[role="dialog"]')!;
    expect(dialog).not.toBeNull();
    const title = document.getElementById(dialog.getAttribute('aria-labelledby')!);
    const description = document.getElementById(dialog.getAttribute('aria-describedby')!);
    expect(title?.textContent).toContain('Cancelar cita');
    expect(description?.textContent).toContain('¿Seguro que quieres cancelarla?');
    expect(dialog.textContent).toContain('Cuerpo');
    expect(dialog.textContent).toContain('Confirmar');
  });

  it('el botón «Cerrar» lo cierra', async () => {
    const w = mountDialog(true);
    await flushPromises();

    (document.querySelector('[aria-label="Cerrar"]') as HTMLButtonElement).click();
    await flushPromises();

    expect(w.emitted('update:open')?.at(-1)).toEqual([false]);
  });
});
