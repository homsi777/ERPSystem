import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  resolve: {
    preserveSymlinks: true
  },
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['icon.svg', 'pwa-192x192.png', 'pwa-512x512.png'],
      workbox: {
        navigateFallbackDenylist: [/^\/api\//],
        // Network-first HTML so deploys are not stuck on a stale index.html → old hashed JS.
        // SWR for static assets: hashed filenames change each build; avoid month-long CacheFirst.
        runtimeCaching: [
          {
            urlPattern: ({ request, url }) =>
              url.origin === self.location.origin &&
              !url.pathname.startsWith('/api/') &&
              request.destination === 'document',
            handler: 'NetworkFirst',
            options: {
              cacheName: 'alamal-ab-pages',
              networkTimeoutSeconds: 4,
              expiration: {
                maxEntries: 20,
                maxAgeSeconds: 60 * 60 * 24
              }
            }
          },
          {
            urlPattern: ({ request, url }) =>
              url.origin === self.location.origin &&
              !url.pathname.startsWith('/api/') &&
              request.destination !== 'document',
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'alamal-ab-static',
              expiration: {
                maxEntries: 80,
                maxAgeSeconds: 60 * 60 * 24 * 7
              }
            }
          }
        ]
      },
      manifest: {
        name: 'الأمل.AB — تجارة أقمشة الجينز',
        short_name: 'الأمل.AB',
        description:
          'الأمل.AB لتجارة الأقمشة — استيراد وبيع جملة أفخر أنواع أقمشة الجينز (الدنيم) من المصنع إلى منتجك.',
        lang: 'ar',
        dir: 'rtl',
        theme_color: '#185FA5',
        background_color: '#F4F7FB',
        display: 'standalone',
        start_url: '/inventory',
        scope: '/',
        icons: [
          {
            src: '/pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png',
            purpose: 'any maskable'
          },
          {
            src: '/pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable'
          }
        ]
      }
    })
  ],
  server: {
    host: '0.0.0.0',
    port: 5173,
    strictPort: true,
    allowedHosts: true
  },
  preview: {
    host: '0.0.0.0',
    port: 5174,
    strictPort: true,
    allowedHosts: true
  }
});
