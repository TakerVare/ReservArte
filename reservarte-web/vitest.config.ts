import { defineConfig, mergeConfig } from 'vitest/config';
import viteConfig from './vite.config';

// Tests unitarios y de componente (RA-869eqxm8z). Reutiliza la configuración de
// Vite (plugins y alias) para que los tests resuelvan los imports igual que la
// app. Los E2E de Playwright viven en e2e/ y no pasan por aquí.
export default defineConfig((env) =>
  mergeConfig(
    viteConfig(env),
    defineConfig({
      test: {
        // happy-dom: DOM, localStorage y window.location sin navegador real.
        environment: 'happy-dom',
        // Convención: cada módulo guarda sus tests en una carpeta __tests__ a su lado.
        include: ['src/**/__tests__/**/*.spec.ts'],
        restoreMocks: true,
        unstubGlobals: true,
      },
    })
  )
);
