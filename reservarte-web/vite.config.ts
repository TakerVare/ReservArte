import { defineConfig, loadEnv } from 'vite';
import vue from '@vitejs/plugin-vue';
import svgLoader from 'vite-svg-loader';
import path from 'path';
// La SPA llama a la API con rutas relativas (`/api/...`); en desarrollo este
// proxy las reenvía a la API (RA-869f6r69b). El destino sale de
// API_PROXY_TARGET (.env.development o variable de entorno); sin prefijo VITE_
// a propósito, para que no llegue al código del navegador. Por defecto, el
// puerto convenido de la API (ADR-018).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiProxyTarget = env.API_PROXY_TARGET || 'http://localhost:5555';

  return {
    plugins: [vue(), svgLoader()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
        '@components': path.resolve(__dirname, './src/components'),
        '@features': path.resolve(__dirname, './src/features'),
        '@pages': path.resolve(__dirname, './src/pages'),
        '@lib': path.resolve(__dirname, './src/lib'),
        '@stores': path.resolve(__dirname, './src/stores'),
        '@types': path.resolve(__dirname, './src/types'),
        '@assets': path.resolve(__dirname, './src/assets'),
      },
    },
    server: {
      port: 3000,
      proxy: {
        '/api': {
          target: apiProxyTarget,
          // La API ve su propio Host: el redirect_uri de OAuth no cambia.
          changeOrigin: true,
        },
      },
    },
    build: {
      outDir: 'dist',
      sourcemap: true,
      rollupOptions: {
        output: {
          // Vite 8 (Rolldown) solo tipa manualChunks en su forma función.
          // Traducción fiel del objeto { vendor: [...] } del script (era Rollup):
          // vue, @vue/*, vue-router y pinia van al chunk "vendor".
          manualChunks(id) {
            if (/node_modules[\\/](vue|@vue|vue-router|pinia)[\\/]/.test(id)) {
              return 'vendor';
            }
          },
        },
      },
    },
  };
});
