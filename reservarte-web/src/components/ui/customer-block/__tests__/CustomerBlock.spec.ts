import { afterEach, describe, expect, it } from 'vitest';
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils';
import { i18n } from '@/i18n';
import CustomerBlock from '../CustomerBlock.vue';

let wrapper: VueWrapper | undefined;

afterEach(() => {
  wrapper?.unmount();
  document.body.innerHTML = '';
});

async function mountOpen() {
  wrapper = mount(CustomerBlock, {
    props: {
      name: 'Carmen',
      blocked: false,
      open: false,
      'onUpdate:open': (v: boolean) => wrapper!.setProps({ open: v }),
    },
    global: { plugins: [i18n] },
    attachTo: document.body,
  });
  await wrapper.setProps({ open: true });
  await flushPromises();
  return wrapper;
}

const textarea = () => document.getElementById('block-reason') as HTMLTextAreaElement;
async function submitWith(value: string) {
  textarea().value = value;
  textarea().dispatchEvent(new Event('input'));
  await flushPromises();
  document.getElementById('block-form')!.dispatchEvent(new Event('submit'));
  await flushPromises();
}

describe('CustomerBlock', () => {
  it('bloqueada, enseña el motivo y ofrece desbloquear', () => {
    const w = mount(CustomerBlock, {
      props: { name: 'Carmen', blocked: true, reason: 'Impagos' },
      global: { plugins: [i18n] },
    });
    expect(w.get('[data-testid="customer-block-state"]').text()).toBe('Bloqueado: Impagos');
    expect(w.findAll('button').map((b) => b.text())).toEqual(['Desbloquear']);
  });

  it('exige motivo, lo limita a 500 caracteres y lo emite sin espacios', async () => {
    const w = await mountOpen();
    await submitWith('   ');
    expect(document.querySelector('[role="alert"]')?.textContent).toBe(
      'Indica el motivo del bloqueo.'
    );
    await submitWith('x'.repeat(501));
    expect(document.querySelector('[role="alert"]')?.textContent).toBe(
      'El motivo no puede superar los 500 caracteres.'
    );
    expect(w.emitted('block')).toBeUndefined();
    await submitWith('  Impagos  ');
    expect(w.emitted('block')![0]).toEqual(['Impagos']);
  });
});
