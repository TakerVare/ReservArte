import js from '@eslint/js';
import pluginVue from 'eslint-plugin-vue';
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript';
import prettierRecommended from 'eslint-plugin-prettier/recommended';

export default defineConfigWithVueTs(
  {
    ignores: [
      'dist/**',
      // Config de Tailwind 3: usa require() (interop CJS que resuelve jiti);
      // se excluye del lint para no declarar globals de Node solo para él
      'tailwind.config.js',
    ],
  },

  js.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  vueTsConfigs.recommended,

  // Prettier SIEMPRE al final: desactiva reglas en conflicto y reporta
  // el formato como warnings de la regla prettier/prettier
  prettierRecommended,

  {
    rules: {
      'prettier/prettier': 'warn',
      '@typescript-eslint/no-explicit-any': 'warn',
      'vue/multi-word-component-names': 'off',
    },
  }
);
