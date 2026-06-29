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
  // The ~245-spec backend-dependent matrix drives ONE shared backend on a single worker. After
  // relaxing local rate limits and widening timeouts (below), residual failures are purely
  // transient latency: an occasional page's data fetch exceeds even the widened timeout under
  // peak serial load, then renders fine on the next attempt. Retries re-run only those transient
  // flakes; they NEVER skip, delete, or relax an assertion — a genuinely broken spec fails all
  // attempts. This is Playwright's standard handling for live-stack timing flakiness.
  retries: 2,
  // The whole backend-dependent matrix drives ONE shared local backend on a single worker;
  // under that serial load a live page's data fetch can legitimately take longer than
  // Playwright's 5s default, producing intermittent "element not found" timeouts that are pure
  // latency (the element renders, just not within 5s) — not assertion or product defects. Give
  // the live stack realistic timeouts. No assertion is changed or relaxed by this.
  expect: { timeout: 15000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    actionTimeout: 15000,
    navigationTimeout: 20000,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: `npm run preview -- --port ${PORT} --strictPort`,
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 60_000,
  },
})
