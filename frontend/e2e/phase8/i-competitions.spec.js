import { test, expect, request as pwRequest } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, BACKEND, backendUp, login, nav, resetE2E, attachGuards, captureRequest, apiLogin } from '../support/ph8.js'

// Group I — Competitions & leaderboards (I63–I67).
test.describe('Phase 8 I — competitions & leaderboards', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // clear STU-T1 entries
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('I63 eligible (tenant-scoped) competitions are listed', async () => {
    await nav(page, '/app/student/competitions')
    await expect(page.getByRole('heading', { name: /^competitions$/i })).toBeVisible()
    await expect(page.getByText('Phase 8 Math Olympiad').first()).toBeVisible()
    // Tenant scoping is the real contract: the seeded same-tenant competition is listed and the
    // cross-tenant competition never leaks in. (An exact total count is not deterministic — a
    // later phase, e.g. Phase 14, seeds additional same-tenant competitions in the shared run.)
    await expect(page.getByText('Phase 8 Tenant2 Competition')).toHaveCount(0)
  })

  test('I64 competition details render state and the entry action', async () => {
    await nav(page, `/app/student/competitions/${FIX.compT1}`)
    await expect(page.getByRole('heading', { name: /phase 8 math olympiad/i })).toBeVisible()
    await expect(page.getByText(/leaderboard/i)).toBeVisible()
    await expect(page.getByRole('button', { name: /enter competition/i })).toBeVisible()
  })

  test('I65 entering a competition is server-confirmed and sends no client authority', async () => {
    await page.goto(`/app/student/competitions/${FIX.compT1}`)
    const reqP = captureRequest(page, `/competitions/${FIX.compT1}/entries`, 'POST')
    const respP = page.waitForResponse((r) => r.url().includes(`/competitions/${FIX.compT1}/entries`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /enter competition/i }).click()
    expect((await respP).status()).toBe(201)
    const req = await reqP
    const body = req.body || {}
    for (const forbidden of ['rank', 'points', 'score', 'studentId', 'tenantId']) {
      expect(body, `entry request leaked ${forbidden}`).not.toHaveProperty(forbidden)
    }
  })

  test('I66 the leaderboard shows server-owned rank and points', async () => {
    await page.goto(`/app/student/leaderboard/${FIX.compT1}`)
    const board = page.locator('.ui-card', { hasText: /leaderboard/i })
    await expect(board).toBeVisible()
    await expect(board.getByText(/no leaderboard rows/i)).toHaveCount(0)
    // Exact match: the rival's persisted score is "95"; a non-exact match also hits any random
    // student-ref GUID that happens to contain "95" (e.g. "...149515..."), tripping strict mode.
    await expect(board.getByText('95', { exact: true })).toBeVisible() // OTHER-T1's persisted score
  })

  test('I67 a duplicate entry carries no client authority and a cross-tenant id is denied', async () => {
    // Duplicate entry: still no client-supplied rank/points; rejected by the server.
    await page.goto(`/app/student/competitions/${FIX.compT1}`)
    const reqP = captureRequest(page, `/competitions/${FIX.compT1}/entries`, 'POST')
    const respP = page.waitForResponse((r) => r.url().includes(`/competitions/${FIX.compT1}/entries`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /enter competition/i }).click()
    expect((await respP).status()).toBe(409)
    const body = (await reqP).body || {}
    for (const forbidden of ['rank', 'points', 'score', 'studentId']) {
      expect(body).not.toHaveProperty(forbidden)
    }
    // Cross-tenant competition id leaks nothing (404).
    const token = await apiLogin(CODES.studentT1)
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/api/v1/competitions/${FIX.compT2}`, { headers: { Authorization: `Bearer ${token}` } })
      expect(res.status()).toBe(404)
    } finally {
      await ctx.dispose()
    }
  })
})
