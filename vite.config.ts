import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// 實價通 — Vite + React build
export default defineConfig({
  plugins: [react()],
  base: '/Actual-Price-Registration/',
});
