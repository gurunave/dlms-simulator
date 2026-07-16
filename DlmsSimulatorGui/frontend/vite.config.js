import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// During `npm run dev` the UI is served from :5173 and proxies API + SignalR
// to the ASP.NET Core backend on :5100. `npm run build` emits into the
// backend's wwwroot so the backend can serve the whole app on its own.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5100',
      '/hub': { target: 'http://localhost:5100', ws: true },
    },
  },
  build: {
    outDir: '../backend/wwwroot',
    emptyOutDir: true,
  },
});
