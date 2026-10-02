import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import Avatar from '../Avatar.vue';

describe('Avatar', () => {
  it('sin foto, enseña las dos primeras iniciales, oculto al lector de pantalla', () => {
    const wrapper = mount(Avatar, { props: { name: 'lucía martínez sanz' } });
    const initials = wrapper.get('[data-testid="avatar-initials"]');
    expect(initials.text()).toBe('LM');
    expect(initials.attributes('aria-hidden')).toBe('true');
  });

  it('con foto, la pinta decorativa; si no carga, vuelve a las iniciales', async () => {
    const wrapper = mount(Avatar, { props: { name: 'María García', src: '/m.png', size: 'lg' } });
    const img = wrapper.get('img');
    expect(img.attributes('alt')).toBe('');
    expect(img.classes()).toContain('h-24');

    await img.trigger('error');
    expect(wrapper.find('img').exists()).toBe(false);
    expect(wrapper.get('[data-testid="avatar-initials"]').text()).toBe('MG');

    // Una foto nueva se vuelve a intentar.
    await wrapper.setProps({ src: '/m2.png' });
    expect(wrapper.get('img').attributes('src')).toBe('/m2.png');
  });
});
