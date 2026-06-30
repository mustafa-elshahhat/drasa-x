import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

// =============================================================================
// frontend build/test configuration.
//
// PWA safety (Phase 7 §17/§19) — UNCHANGED by the TS/Tailwind migration:
//   * Only the static app shell is precached.
//   * Authenticated backend API responses (/api/**) are NEVER cached — the
//     service worker uses NetworkOnly for the backend origin so no token or
//     per-user/per-tenant payload can be served from cache to another user.
//   * No cross-origin AI-service caching (the browser never calls the AI service).
//
// Tailwind is additive (tokens-backed, no preflight reset — see styles/tailwind.css).
// =============================================================================
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'prompt', // user-controlled update; no silent activation
      includeAssets: ['derasax-logo.png', 'apple-touch-icon.png'],
      manifest: {
        name: 'DerasaX',
        short_name: 'DerasaX',
        description: 'DerasaX — AI-assisted learning platform',
        theme_color: '#0f9ba8',
        background_color: '#0f0f10',
        display: 'standalone',
        start_url: '/',
        scope: '/',
        icons: [
          { src: 'pwa-192x192.png', sizes: '192x192', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png' },
          { src: 'pwa-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Precache only static build assets — never API responses.
        globPatterns: ['**/*.{js,css,html,svg,png,woff2}'],
        navigateFallbackDenylist: [/^\/api\//],
        runtimeCaching: [
          {
            // Any same-origin or cross-origin /api/ call is NEVER cached.
            urlPattern: ({ url }) => url.pathname.startsWith('/api/'),
            handler: 'NetworkOnly',
            method: 'GET',
          },
        ],
        // Do not fall back to index.html for API routes.
        navigateFallback: 'index.html',
      },
      devOptions: {
        // The SW stays disabled in dev to avoid stale-cache confusion locally.
        enabled: false,
      },
    }),
  ],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.js'],
    css: false,
    exclude: ['e2e/**', 'node_modules/**', 'dist/**'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json-summary'],
      include: ['src/**/*.{js,jsx,ts,tsx}'],
      exclude: ['src/**/*.test.{js,jsx,ts,tsx}', 'src/test/**', 'src/main.tsx'],
    },
  },
})
