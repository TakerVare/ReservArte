import { describe, expect, it } from 'vitest';
import { defineComponent } from 'vue';
import { mount } from '@vue/test-utils';
import { useI18n } from 'vue-i18n';
import { i18n } from '../index';

/**
 * Integración de vue-i18n (11, RA-869f6r6dk) tal como la configura la app:
 * Composition API, locale `es` y `$t` inyectado en las plantillas.
 */
describe('i18n', () => {
  it('usa la Composition API con español como locale y como respaldo', () => {
    expect(i18n.mode).toBe('composition');
    expect(i18n.global.locale.value).toBe('es');
    expect(i18n.global.fallbackLocale.value).toBe('es');
  });

  it('traduce claves anidadas del locale es', () => {
    expect(i18n.global.t('auth.login.title')).toBe('Iniciar sesión');
    expect(i18n.global.t('common.errorUnexpected')).toBe(
      'Ha ocurrido un error. Inténtelo de nuevo.'
    );
  });

  it('una clave inexistente devuelve la propia clave', () => {
    expect(i18n.global.t('no.existe')).toBe('no.existe');
  });

  it('una plantilla traduce con $t (globalInjection) y con useI18n', () => {
    const Title = defineComponent({
      setup() {
        const { t } = useI18n();
        return { t };
      },
      template: '<h1>{{ $t("auth.login.title") }}</h1><p>{{ t("common.errorUnexpected") }}</p>',
    });

    const wrapper = mount(Title, { global: { plugins: [i18n] } });

    expect(wrapper.find('h1').text()).toBe('Iniciar sesión');
    expect(wrapper.find('p').text()).toBe('Ha ocurrido un error. Inténtelo de nuevo.');
  });
});
