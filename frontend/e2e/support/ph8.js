// =============================================================================
// Phase 8 live-acceptance shared support (A1–M91). Real browser → real frontend
// (vite preview) → real DerasaX-backend → PostgreSQL → school-ai-rag.
// No network mocking. Credentials come from env (never hard-coded secrets).
// =============================================================================
import { expect, request as pwRequest } from '@playwright/test'

export const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
export const PASSWORD = process.env.E2E_PASSWORD || ''
export const RESET_KEY = process.env.E2E_RESET_KEY || 'ph8-e2e-local'

// Deterministic seed login codes (DataSeederService Phase 3 + Phase 8 fixtures).
export const CODES = {
  studentT1: process.env.E2E_LOGIN_CODE || 'STU-T1',
  studentT2: 'STU-T2',
  teacher: 'TEACH-T1',
  parent: 'PARENT-T1',
  schoolAdmin: 'ADMIN-T1',
  systemAdmin: 'SYS-1',
}

// Deterministic Phase 8 fixture identifiers.
export const FIX = {
  subjectT1: 'PH8-SUBJECT-T1',
  unitT1: 'PH8-UNIT-T1',
  lessonT1: 'PH8-LESSON-T1',
  lessonT2: 'PH8-LESSON-T2',
  lessonUnassigned: 'E2E-PH8-LESSON-UNASSIGNED-T1',
  materialT1: 'E2E-PH8-MAT-T1',
  hwOpen: 'E2E-PH8-HW-OPEN',
  hwUnassigned: 'E2E-PH8-HW-UNASSIGNED',
  hwT2: 'E2E-PH8-HW-T2',
  quizT1: 'E2E-PH8-QUIZ-T1',
  quizT2: 'E2E-PH8-QUIZ-T2',
  commT1: 'E2E-PH8-COMM-T1',
  commT2: 'E2E-PH8-COMM-T2',
  compT1: 'E2E-PH8-COMP-T1',
  compT2: 'E2E-PH8-COMP-T2',
  ohOpen: 'E2E-PH8-OH-OPEN-T1',
  ohFull: 'E2E-PH8-OH-FULL-T1',
}

export const HAS_CREDS = Boolean(PASSWORD)

export async function backendUp() {
  try {
    const ctx = await pwRequest.newContext()
    const res = await ctx.get(`${BACKEND}/health/live`, { timeout: 3000 })
    await ctx.dispose()
    return res.ok()
  } catch {
    return false
  }
}

// Reset the mutable per-run fixture state (Development-only backend endpoint).
export async function resetE2E() {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/dev/e2e/reset`, {
      headers: { 'X-E2E-Reset-Key': RESET_KEY },
      timeout: 8000,
    })
    if (!res.ok()) throw new Error(`reset failed: ${res.status()}`)
  } finally {
    await ctx.dispose()
  }
}

// Authenticate over the real backend account contract and return the access
// token (for legitimate API-driven setup such as teacher curriculum ingestion).
export async function apiLogin(code, password = PASSWORD) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: password },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`apiLogin ${code} failed: ${res.status()}`)
    const model = await res.json()
    if (!model?.token) throw new Error(`apiLogin ${code}: no token in response`)
    return model.token
  } finally {
    await ctx.dispose()
  }
}

// Deterministic curriculum document for the AI-tutor grounded path (Group E).
// Ingested through the REAL teacher → backend → school-ai-rag flow (never the
// browser, never the AI service directly). Ingestion is idempotent server-side
// (identical checksum at an equal version is a no-op), so this is safe to call
// before every run. Tenant is derived from the teacher's signed token.
export const TUTOR_DOC = {
  documentId: 'E2E-PH8-DOC-T1',
  version: 1,
  title: 'Algebra Reference',
  materialType: 'textbook',
  language: 'en',
  grade: 7,
  subject: 'mathematics',
  content:
    'A linear equation is an algebraic equation in which each term is either a ' +
    'constant or the product of a constant and a single variable raised to the ' +
    'first power. The graph of a linear equation in two variables is a straight ' +
    'line. The standard form of a linear equation is ax + b = c, where a, b, and ' +
    'c are constants and x is the variable. To solve a linear equation, isolate ' +
    'the variable on one side of the equation by performing the same operation on ' +
    'both sides. For example, to solve 2x + 3 = 7, subtract 3 from both sides to ' +
    'get 2x = 4, then divide both sides by 2 to get x = 2. Linear equations have ' +
    'exactly one solution when the variable has a single value that makes the ' +
    'equation true.',
}

export async function ensureTutorDoc() {
  const token = await apiLogin(CODES.teacher)
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/ai/documents`, {
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      data: TUTOR_DOC,
      timeout: 90000,
    })
    if (!res.ok()) throw new Error(`ensureTutorDoc failed: ${res.status()} ${await res.text()}`)
    return await res.json()
  } finally {
    await ctx.dispose()
  }
}

export async function login(page, code = CODES.studentT1) {
  await page.goto('/login')
  await page.getByLabel(/login code/i).fill(code)
  await page.getByLabel(/^password/i).fill(PASSWORD)
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page).not.toHaveURL(/\/login$/, { timeout: 15000 })
}

// SPA navigation that keeps the in-memory access token (no full reload).
export async function nav(page, path) {
  await page.evaluate((target) => {
    window.history.pushState({}, '', target)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, path)
}

export async function setLang(page, lang) {
  await page.getByLabel(/language/i).selectOption(lang)
}

// Attach guards: no browser request may go to the AI service or an archived
// backend; collect console + page errors for end-of-test assertions.
export function attachGuards(page) {
  const consoleErrors = []
  const pageErrors = []
  const forbidden = []
  const serverErrors = []
  page.on('console', (m) => {
    if (m.type() === 'error') consoleErrors.push(m.text())
  })
  page.on('pageerror', (e) => pageErrors.push(String(e)))
  page.on('request', (req) => {
    const url = req.url()
    if (/:8000(\/|$)/.test(url) || /archived/i.test(url)) forbidden.push(url)
  })
  page.on('response', (res) => {
    // 5xx from the application backend is always a defect; 4xx is part of normal
    // flows (empty submission, rejected cross-tenant id) and is asserted per-test.
    if (res.status() >= 500 && res.url().includes('/api/')) serverErrors.push(`${res.status()} ${res.url()}`)
  })
  return {
    // Core invariants every Phase 8 page must satisfy.
    assertNoForbidden() {
      expect(forbidden, `forbidden browser requests (AI/archived): ${forbidden.join(', ')}`).toEqual([])
      expect(serverErrors, `5xx backend responses: ${serverErrors.join(', ')}`).toEqual([])
      expect(pageErrors, `page errors: ${pageErrors.join(' | ')}`).toEqual([])
    },
    assertClean(allow = []) {
      this.assertNoForbidden()
      const ce = consoleErrors.filter((t) => !allow.some((a) => t.includes(a)))
      expect(ce, `console errors: ${ce.join(' | ')}`).toEqual([])
    },
    consoleErrors,
    pageErrors,
    forbidden,
    serverErrors,
  }
}

// Capture the outbound request body for a path (for payload-whitelist assertions).
export function captureRequest(page, pathMatch, method = 'POST') {
  return new Promise((resolve) => {
    page.on('request', (req) => {
      if (req.method() === method && req.url().includes(pathMatch)) {
        let body = null
        try {
          body = req.postDataJSON()
        } catch {
          body = req.postData()
        }
        resolve({ url: req.url(), body, headers: req.headers() })
      }
    })
  })
}

export async function axeSeriousOrCritical(page) {
  const { default: AxeBuilder } = await import('@axe-core/playwright')
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  return results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')
}
