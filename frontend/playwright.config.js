import { defineConfig, devices } from '@playwright/test'

// Playwright E2E configuration (Phase 7 §22). E2E runs against the production
// build served by `vite preview`. Backend-dependent specs detect the local
// DerasaX-backend at runtime and skip cleanly when it is not running, so the
// backend-independent specs (redirect, RTL, no-direct-AI) always produce
// evidence.
const PORT = 4173
const BASE_URL = `http://localhost:${PORT}`

export default defineConfig({
  testDir: './e2e',
  // The backend-dependent specs (authenticated, phase8-student) all drive ONE
  // shared local DerasaX-backend whose anonymous auth surface is (correctly)
  // rate-limited and whose progress reads fan out into several concurrent
  // requests. Running them across many parallel workers makes login throttling
  // and read-model timing nondeterministic. A single worker keeps the live
  // acceptance evidence reproducible; the suite is fast (~10s) so there is no
  // material cost. Backend-independent specs remain unaffected.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: `npm run preview -- --port ${PORT} --strictPort`,
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
