import { test, expect, request as pwRequest } from '@playwright/test'
import { HAS_CREDS, CODES, BACKEND, PASSWORD, backendUp, login, nav, attachGuards } from '../support/ph8.js'

// Phase 14 closure patch — live coverage for the four closed gaps:
//  1. teacher manual-award UI control,
//  2. durable competition submissions schema (API + student submit UI),
//  3. parent read-only gamification view (linked child only),
//  4. community grade-eligibility join gate.
// Real browser → vite preview → DerasaX-backend → PostgreSQL. No mocking.

// STU-T1 is seeded in grade G7-ID; G8-ID is a different tenant-1 grade.
const STUDENT_GRADE = 'G7-ID'
const OTHER_GRADE = 'G8-ID'

async function rawLogin(code) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: PASSWORD },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`login ${code}: ${res.status()}`)
    const m = await res.json()
    return { token: m.token, id: m.id }
  } finally {
    await ctx.dispose()
  }
}

function authCtx(token) {
  return pwRequest.newContext({ extraHTTPHeaders: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } })
}

const FUTURE = { startsAt: '2030-01-01T00:00:00Z', endsAt: '2035-01-01T00:00:00Z' }

test.describe('Phase 14 B — closure gaps', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let stuId

  test.beforeAll(async () => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    stuId = (await rawLogin(CODES.studentT1)).id
  })

  test('C1 competition submissions: entry required, durable resubmission, staff-only listing (API)', async () => {
    const teacher = await rawLogin(CODES.teacher)
    const student = await rawLogin(CODES.studentT1)
    const tctx = await authCtx(teacher.token)
    const sctx = await authCtx(student.token)
    try {
      const create = await tctx.post(`${BACKEND}/api/v1/competitions`, { data: { title: `E2E-PH14-SUB-${Date.now()}`, ...FUTURE } })
      expect(create.status()).toBe(201)
      const compId = (await create.json()).data.id
      expect((await tctx.post(`${BACKEND}/api/v1/competitions/${compId}/publish`)).status()).toBe(200)

      // Submitting before entering is rejected.
      expect((await sctx.post(`${BACKEND}/api/v1/competitions/${compId}/submissions`, { data: { content: 'early' } })).status()).toBe(409)

      // Enter, then submit (201), then resubmit (200, updated in place).
      await sctx.post(`${BACKEND}/api/v1/competitions/${compId}/entries`)
      expect((await sctx.post(`${BACKEND}/api/v1/competitions/${compId}/submissions`, { data: { content: 'first' } })).status()).toBe(201)
      expect((await sctx.post(`${BACKEND}/api/v1/competitions/${compId}/submissions`, { data: { content: 'revised' } })).status()).toBe(200)

      // Student reads own submission; staff list it; a student cannot list all (staff-only).
      const mine = await sctx.get(`${BACKEND}/api/v1/competitions/${compId}/submissions/me`)
      expect((await mine.json()).data.content).toBe('revised')
      const list = await tctx.get(`${BACKEND}/api/v1/competitions/${compId}/submissions`)
      expect(list.status()).toBe(200)
      expect((await list.json()).data.some((s) => s.studentId === stuId && s.content === 'revised')).toBe(true)
      expect((await sctx.get(`${BACKEND}/api/v1/competitions/${compId}/submissions`)).status()).toBe(403)
    } finally {
      await tctx.dispose(); await sctx.dispose()
    }
  })

  test('C2 community grade-eligibility gate admits matching grade and rejects others (API)', async () => {
    const teacher = await rawLogin(CODES.teacher)
    const student = await rawLogin(CODES.studentT1)
    const tctx = await authCtx(teacher.token)
    const sctx = await authCtx(student.token)
    try {
      const okCreate = await tctx.post(`${BACKEND}/api/v1/communities`, { data: { name: `E2E-PH14-GOK-${Date.now()}`, visibility: 1, eligibleGradeId: STUDENT_GRADE } })
      expect(okCreate.status()).toBe(201)
      const okId = (await okCreate.json()).data.id
      expect((await sctx.post(`${BACKEND}/api/v1/communities/${okId}/join`)).status()).toBe(200)

      const noCreate = await tctx.post(`${BACKEND}/api/v1/communities`, { data: { name: `E2E-PH14-GNO-${Date.now()}`, visibility: 1, eligibleGradeId: OTHER_GRADE } })
      const noId = (await noCreate.json()).data.id
      expect((await sctx.post(`${BACKEND}/api/v1/communities/${noId}/join`)).status()).toBe(403)
    } finally {
      await tctx.dispose(); await sctx.dispose()
    }
  })

  test('C3 a teacher awards points to an assigned student through the portal UI', async ({ browser }) => {
    const context = await browser.newContext()
    const page = await context.newPage()
    const guards = attachGuards(page)
    try {
      await login(page, CODES.teacher)
      await page.waitForURL(/\/app\/teacher/, { timeout: 15000 }) // let the post-login redirect settle before SPA nav
      const award = page.waitForResponse((r) => r.url().includes(`/students/${stuId}/points`) && r.request().method() === 'POST')
      await nav(page, `/app/teacher/students/${stuId}`)
      await expect(page.getByRole('heading', { name: /gamification points/i })).toBeVisible()
      await page.getByLabel('Points', { exact: true }).fill('7')
      await page.getByLabel('Reason', { exact: true }).fill('Phase 14 closure E2E award')
      await page.getByRole('button', { name: /^award points$/i }).click()
      expect([200, 201]).toContain((await award).status())
      await expect(page.getByText(/points awarded/i)).toBeVisible()
      guards.assertNoForbidden()
    } finally {
      await context.close()
    }
  })

  test('C4 a parent sees a linked child read-only gamification points (UI)', async ({ browser }) => {
    const context = await browser.newContext()
    const page = await context.newPage()
    const guards = attachGuards(page)
    try {
      await login(page, 'PH10-PARENT-T1')
      await page.waitForURL(/\/app\/parent/, { timeout: 15000 })
      const pointsResp = page.waitForResponse((r) => r.url().includes(`/students/${stuId}/points`) && r.request().method() === 'GET')
      await nav(page, `/app/parent/children/${stuId}/points`)
      expect((await pointsResp).status()).toBe(200)
      await expect(page.getByRole('heading', { name: /gamification points/i }).first()).toBeVisible()
      guards.assertNoForbidden()
    } finally {
      await context.close()
    }
  })

  test('C5 a student submits durable competition work through the portal UI', async ({ browser }) => {
    // Seed an open competition and pre-enter the student via the API so the UI test focuses on submission.
    const teacher = await rawLogin(CODES.teacher)
    const tctx = await authCtx(teacher.token)
    let compId
    try {
      const create = await tctx.post(`${BACKEND}/api/v1/competitions`, { data: { title: `E2E-PH14-UISUB-${Date.now()}`, ...FUTURE } })
      compId = (await create.json()).data.id
      await tctx.post(`${BACKEND}/api/v1/competitions/${compId}/publish`)
    } finally {
      await tctx.dispose()
    }
    const student = await rawLogin(CODES.studentT1)
    const sctx = await authCtx(student.token)
    try {
      await sctx.post(`${BACKEND}/api/v1/competitions/${compId}/entries`)
    } finally {
      await sctx.dispose()
    }

    const context = await browser.newContext()
    const page = await context.newPage()
    const guards = attachGuards(page)
    try {
      await login(page, CODES.studentT1)
      await page.waitForURL(/\/app\/student/, { timeout: 15000 })
      const submitResp = page.waitForResponse((r) => r.url().includes(`/competitions/${compId}/submissions`) && r.request().method() === 'POST')
      await nav(page, `/app/student/competitions/${compId}`)
      await expect(page.getByRole('heading', { name: /your submission/i })).toBeVisible()
      await page.getByLabel('Submission', { exact: true }).fill('My durable competition entry — E2E')
      await page.getByRole('button', { name: /submit work/i }).click()
      expect([200, 201]).toContain((await submitResp).status())
      await expect(page.getByText(/submission saved/i)).toBeVisible()
      guards.assertNoForbidden()
    } finally {
      await context.close()
    }
  })
})
