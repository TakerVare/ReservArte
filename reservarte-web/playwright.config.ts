import { defineConfig, devices } from '@playwright/test';

/**
 * Configuración de Playwright para los tests E2E del frontend.
 * - Tres navegadores: Chromium, Firefox, WebKit.
 * - webServer: reutiliza el dev server de Vite (puerto 3000) si ya está
 *   corriendo; si no, lo arranca. En el equipo Windows (Torre-Maria) el
 *   puerto 3000 debe estar libre (parar WAHA antes).
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  reporter: 'html',

  use: {
    baseURL: 'http://localhost:3000',
    trace: 'on-first-retry',
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],

  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});