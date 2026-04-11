import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    host: true,
    allowedHosts: [
      'app.neuroscan.online',
      'localhost',
      '127.0.0.1'
    ]
  }
});
