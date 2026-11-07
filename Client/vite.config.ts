import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'


export default defineConfig({
  plugins: [vue()],
  build: {
    rollupOptions: {
      input: {
        main: fileURLToPath(new URL('./index.html', import.meta.url)),
        visualPreview: fileURLToPath(new URL('./visual-preview.html', import.meta.url)),
      },
    },
  },
  resolve: {
    // Keep asset paths stable in Windows sandboxes and junction-based checkouts.
    preserveSymlinks: true,
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  // server: {
  //   proxy: {
  //     '/api':  { target: BACKEND, changeOrigin: true, secure: false },
  //     '/hubs': { target: BACKEND, changeOrigin: true, secure: false, ws: true },
  //   },
  // },

  server: {
  host: '0.0.0.0',
  port: 5173,
  strictPort: true,
  proxy: {
    '/api': {
      target: 'http://127.0.0.1:5018',
      changeOrigin: true,
    },
    '/hubs': {
      target: 'http://127.0.0.1:5018',
      changeOrigin: true,
      ws: true,
    },
  },
},
})
