import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api/products': {
        target: 'https://localhost:7200',
        changeOrigin: true,
        secure: false,
      },
      '/api/orders': {
        target: 'https://localhost:7153',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
