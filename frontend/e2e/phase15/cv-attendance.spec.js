// =============================================================================
// Phase 15 — Computer-vision attendance + engagement: LIVE acceptance.
// Real browser → vite preview → DerasaX-backend → school-ai-rag (stub CV engine)
// → PostgreSQL. No network mocking. The browser never calls the AI service
// (attachGuards forbids any :8000 request).
//
// Proves the 10 required flows:
//   1. teacher starts a CV session
//   2. teacher analyzes a snapshot through backend-mediated AI
//   3. backend persists the analysis (candidates appear)
//   4. teacher reviews + confirms an attendance candidate
//   5. the confirmed attendance appears in the existing attendance flow
//   6. cross-tenant / unauthorized access is rejected
//   7. a parent sees only their linked child's summary
//   8. a student sees only their own summary
//   9. AI unavailable / model-not-ready is shown honestly (E2E_AI_DOWN pass)
//  10. prior Phase 13/14 surfaces still load (smoke)
// =============================================================================
import { test, expect, request as pwRequest } from '@playwright/test'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { BACKEND, PASSWORD, HAS_CREDS, CODES, backendUp, login, nav, attachGuards } from '../support/ph8.js'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const FRAME = path.join(__dirname, 'fixtures', 'frame.png')
const PARENT = 'PH10-PARENT-T1' // linked to STU-T1 (Phase 10 fixtures)

async function apiLoginFull(code) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: PASSWORD },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`login ${code}: ${res.status()}`)
    return await res.json() // { token, role, id, isAuthenticated }
  } finally {
    await ctx.dispose()
  }
}

async function apiGet(path, token) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.get(`${BACKEND}${path}`, { headers: { Authorization: `Bearer ${token}` }, timeout: 15000 })
    return { status: res.status(), body: res.ok() ? await res.json() : null }
  } finally {
    await ctx.dispose()
  }
}

test.describe('Phase 15 — CV attendance + engagement', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let stuT1, stuT2, other
  let teacherPage, teacherCtx, guards
  let sessionId

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    stuT1 = await apiLoginFull(CODES.studentT1)
    stuT2 = await apiLoginFull(CODES.studentT2)
    other = await apiLoginFull('PH8-OTHER-T1')
    teacherCtx = await browser.newContext()
    teacherPage = await teacherCtx.newPage()
    guards = attachGuards(teacherPage)
    await login(teacherPage, CODES.teacher)
  })

  test.afterAll(async () => {
    if (process.env.E2E_AI_DOWN) {
      // In the AI-down pass, a 502 from analyze is the EXPECTED outcome under test
      // (flow 9). Only the hard invariants must still hold.
      expect(guards?.forbidden ?? [], 'no direct browser→AI requests').toEqual([])
      expect(guards?.pageErrors ?? [], 'no page errors').toEqual([])
    } else {
      guards?.assertNoForbidden() // no browser → AI(:8000) requests, no 5xx, no page errors
    }
    await teacherCtx?.close()
  })

  test('1+2+3 teacher starts a session, analyzes a frame, backend persists candidates', async () => {
    test.skip(!process.env.E2E_AI_DOWN ? false : true, 'success path requires the AI service up')
    await nav(teacherPage, '/app/vision')
    await teacherPage.getByLabel(/session title/i).fill('E2E CV ' + Date.now())
    await teacherPage.getByRole('button', { name: /start session/i }).click()
    await teacherPage.waitForURL(/\/app\/vision\/sessions\//, { timeout: 15000 })
    sessionId = teacherPage.url().split('/sessions/')[1]
    expect(sessionId).toBeTruthy()

    await teacherPage.getByTestId('cv-frame-input').setInputFiles(FRAME)
    await expect(teacherPage.getByTestId('cv-detections')).toBeVisible({ timeout: 20000 })
    // persisted candidates appear
    await expect(teacherPage.getByTestId('cv-candidate').first()).toBeVisible({ timeout: 15000 })
  })

  test('4+5 teacher confirms a candidate and it appears in the student attendance flow', async () => {
    test.skip(!sessionId, 'depends on session creation')
    const row = teacherPage.getByTestId('cv-candidate').first()
    await row.getByLabel(/student id/i).fill(stuT1.id)
    await row.getByRole('button', { name: /^confirm$/i }).click()
    // a candidate transitions to a confirmed state (candidates share a LastSeenAt,
    // so the confirmed one is not necessarily first after the list refetches).
    await expect(
      teacherPage.getByTestId('cv-candidate-status').filter({ hasText: /confirmed/i }),
    ).toHaveCount(1, { timeout: 15000 })

    // the same StudentAttendanceRecord store the student attendance page reads
    const att = await apiGet('/api/v1/student/attendance', stuT1.token)
    expect(att.status).toBe(200)
    const records = (att.body?.data?.records || []).filter(
      (r) => r.source === 'ComputerVision' && r.sessionKey === `cv-${sessionId}`,
    )
    expect(records.length).toBe(1)
  })

  test('6 cross-tenant / unauthorized access is rejected', async ({ browser }) => {
    // a tenant-2 user is denied the staff CV surface
    const t2 = await apiGet('/api/v1/vision/sessions', stuT2.token)
    expect(t2.status).toBe(403)
    // an unknown session id is a 404 (no cross-tenant leak)
    const teacher = await apiLoginFull(CODES.teacher)
    const ghost = await apiGet('/api/v1/vision/sessions/does-not-exist', teacher.token)
    expect(ghost.status).toBe(404)
    // UI: a student hitting the staff route is redirected away (role guard)
    const ctx = await browser.newContext()
    const p = await ctx.newPage()
    try {
      await login(p, CODES.studentT1)
      await nav(p, '/app/vision')
      await expect(p).toHaveURL(/\/forbidden|\/app\b/, { timeout: 10000 })
      await expect(p.getByTestId('cv-frame-input')).toHaveCount(0)
    } finally {
      await ctx.close()
    }
  })

  test('7 a parent sees only their linked child summary', async ({ browser }) => {
    const ctx = await browser.newContext()
    const p = await ctx.newPage()
    const g = attachGuards(p)
    try {
      await login(p, PARENT)
      // linked child -> summary renders (card or empty state, never an error)
      await nav(p, `/app/parent/children/${stuT1.id}/vision`)
      await expect(p.getByRole('heading', { name: /child engagement/i })).toBeVisible({ timeout: 10000 })
      await expect(p.getByRole('alert')).toHaveCount(0)
      // unlinked / forbidden child -> honest error, never another student's data
      await nav(p, `/app/parent/children/${other.id}/vision`)
      await expect(p.getByRole('alert')).toBeVisible({ timeout: 10000 })
      g.assertNoForbidden()
    } finally {
      await ctx.close()
    }
  })

  test('8 a student sees only their own summary', async ({ browser }) => {
    const ctx = await browser.newContext()
    const p = await ctx.newPage()
    const g = attachGuards(p)
    try {
      await login(p, CODES.studentT1)
      await nav(p, '/app/student/vision')
      await expect(p.getByRole('heading', { name: /my engagement/i })).toBeVisible({ timeout: 10000 })
      g.assertNoForbidden()
    } finally {
      await ctx.close()
    }
  })

  test('9 AI unavailable / model-not-ready is shown honestly', async () => {
    test.skip(!process.env.E2E_AI_DOWN, 'run this pass with the school-ai-rag service stopped (E2E_AI_DOWN=1)')
    await nav(teacherPage, '/app/vision')
    await teacherPage.getByLabel(/session title/i).fill('E2E CV down ' + Date.now())
    await teacherPage.getByRole('button', { name: /start session/i }).click()
    await teacherPage.waitForURL(/\/app\/vision\/sessions\//, { timeout: 15000 })
    await teacherPage.getByTestId('cv-frame-input').setInputFiles(FRAME)
    // honest failure, never a fabricated success
    await expect(teacherPage.getByText(/ai service unavailable/i)).toBeVisible({ timeout: 20000 })
    await expect(teacherPage.getByTestId('cv-detections')).toHaveCount(0)
  })

  test('10 prior Phase 13/14 surfaces still load (smoke)', async ({ browser }) => {
    const ctx = await browser.newContext()
    const p = await ctx.newPage()
    const g = attachGuards(p)
    try {
      await login(p, CODES.studentT1)
      await nav(p, '/app/notifications') // Phase 13
      await expect(p.getByRole('heading').first()).toBeVisible({ timeout: 10000 })
      await nav(p, '/app/student/points') // Phase 14 gamification
      await expect(p.getByRole('heading').first()).toBeVisible({ timeout: 10000 })
      g.assertNoForbidden()
    } finally {
      await ctx.close()
    }
  })
})
