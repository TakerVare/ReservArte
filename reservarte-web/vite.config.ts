import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import path from 'path';
export default defineConfig({
  plugins: [vue()],
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
        target: 'http://localhost:5218',
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
});
