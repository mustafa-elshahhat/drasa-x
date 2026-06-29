// =============================================================================
// Phase 18 — SECURITY smoke (live).
//
// Re-proves the platform's runtime security posture end-to-end, mixing real
// browser UI (session protection, no token leakage to web storage) with
// browser-origin HTTP (Playwright request) → DerasaX-backend exactly as the SPA
// would call it. No mocking; honest skips when the backend / creds are absent.
//
// Covers: anonymous→login redirect for every role dashboard; no auth token in
// localStorage/sessionStorage (anonymous AND after a real login); backend HTTP
// security headers on a normal and an unauthorized response; sanitized error
// bodies (no stack traces / SQL / secrets); cross-role 403 matrix. The malware
// scanner runs in the default "Disabled" (NotScanned) mode in the live stack, so
// infected-file rejection is proven at the backend-test layer (ScannerStubFactory),
// not here — documented in PHASE18_REMAINING_GAPS.md, not faked.
// =============================================================================
import { test, expect, request as pwRequest } from '@playwright/test'

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const PASSWORD = process.env.E2E_PASSWORD || ''
const HAS_CREDS = Boolean(PASSWORD)

const JWT_RE = /^ey[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+$/
const SENSITIVE_KEY_RE = /(token|jwt|secret|refresh|password|bearer)/i

async function backendUp() {
  try {
    const ctx = await pwRequest.newContext()
    const res = await ctx.get(`${BACKEND}/health/live`, { timeout: 3000 })
    await ctx.dispose()
    return res.ok()
  } catch {
    return false
  }
}

async function dumpStorage(page) {
  return page.evaluate(() => {
    const grab = (s) => Object.keys(s).map((k) => ({ key: k, value: s.getItem(k) }))
    return { local: grab(window.localStorage), session: grab(window.sessionStorage) }
  })
}

function assertNoTokens(storage) {
  for (const scope of ['local', 'session']) {
    for (const { key, value } of storage[scope]) {
      expect(SENSITIVE_KEY_RE.test(key), `web storage key "${key}" looks token-like`).toBe(false)
      expect(JWT_RE.test((value || '').trim()), `web storage value for "${key}" looks like a JWT`).toBe(false)
    }
  }
}

async function login(code) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: PASSWORD },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`login ${code}: ${res.status()}`)
    return await res.json()
  } finally {
    await ctx.dispose()
  }
}

const authCtx = (token) => pwRequest.newContext({ extraHTTPHeaders: { Authorization: `Bearer ${token}` } })

// ---------------------------------------------------------------------------
// Backend-INDEPENDENT: session protection + no token in web storage (no creds).
// ---------------------------------------------------------------------------
test.describe('Phase 18 — session protection (no backend required)', () => {
  for (const route of ['/app/student', '/app/teacher', '/app/parent', '/app/school', '/app/system']) {
    test(`anonymous is redirected from ${route} to login`, async ({ page }) => {
      await page.goto(route)
      await expect(page).toHaveURL(/\/login$/)
    })
  }

  test('anonymous app stores no auth token in localStorage/sessionStorage', async ({ page }) => {
    await page.goto('/login')
    assertNoTokens(await dumpStorage(page))
  })
})

// ---------------------------------------------------------------------------
// Backend HTTP security headers + sanitized errors (needs backend, NO creds).
// ---------------------------------------------------------------------------
test.describe('Phase 18 — backend security headers (no creds)', () => {
  test.beforeAll(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
  })

  test('normal response carries the security headers', async () => {
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/health/live`)
      expect(res.ok()).toBeTruthy()
      const h = res.headers()
      expect(h['x-content-type-options']).toBe('nosniff')
      expect(h['x-frame-options']).toBe('DENY')
      expect(h['referrer-policy']).toBe('no-referrer')
      expect(h['content-security-policy']).toContain("frame-ancestors 'none'")
    } finally {
      await ctx.dispose()
    }
  })

  test('unauthorized API response is 401, carries headers, and leaks no internals', async () => {
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/api/v1/audit`) // protected → 401
      expect(res.status()).toBe(401)
      expect(res.headers()['x-content-type-options']).toBe('nosniff')
      const body = (await res.text()).toLowerCase()
      // No stack traces, SQL, connection strings, or server fingerprints in the body.
      for (const leak of ['exception', 'stack trace', 'npgsql', 'select * from', 'password=', 'host=']) {
        expect(body.includes(leak), `error body must not contain "${leak}"`).toBe(false)
      }
    } finally {
      await ctx.dispose()
    }
  })
})

// ---------------------------------------------------------------------------
// Live matrix (needs the stack + E2E_PASSWORD).
// ---------------------------------------------------------------------------
test.describe('Phase 18 — live security matrix', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live Phase 18 security matrix')

  let admin, sys, student
  test.beforeAll(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    admin = await login('ADMIN-T1')
    sys = await login('SYS-1')
    student = await login('STU-T1')
  })

  test('cross-role access is forbidden (403)', async () => {
    const a = await authCtx(admin.token)
    const s = await authCtx(sys.token)
    const stu = await authCtx(student.token)
    try {
      // Each admin reaches its own surface...
      expect((await a.get(`${BACKEND}/api/v1/school-admin/dashboard`)).status()).toBe(200)
      expect((await s.get(`${BACKEND}/api/v1/system-admin/dashboard`)).status()).toBe(200)
      // ...but never the other's.
      expect((await stu.get(`${BACKEND}/api/v1/school-admin/dashboard`)).status()).toBe(403)
      expect((await a.get(`${BACKEND}/api/v1/system-admin/dashboard`)).status()).toBe(403)
    } finally {
      await a.dispose(); await s.dispose(); await stu.dispose()
    }
  })

  test('after a real UI login no auth token is persisted to web storage', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel(/login code/i).fill('ADMIN-T1')
    await page.getByLabel(/^password/i).fill(PASSWORD)
    await page.getByRole('button', { name: /sign in/i }).click()
    await expect(page).toHaveURL(/\/app/)
    // The access token lives in memory only; the refresh token is an HttpOnly cookie
    // (invisible to JS). Neither must appear in localStorage/sessionStorage.
    assertNoTokens(await dumpStorage(page))
  })
})
