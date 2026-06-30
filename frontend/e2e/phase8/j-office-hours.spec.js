import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, BACKEND, backendUp, login, nav, resetE2E, attachGuards, apiLogin } from '../support/ph8.js'
import { request as pwRequest } from '@playwright/test'

// Group J — Office hours booking (J68–J72).
test.describe('Phase 8 J — office hours', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // clear prior bookings
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('J68 available sessions render with capacity state; full sessions are not offered', async () => {
    await nav(page, '/app/student/office-hours')
    await expect(page.getByRole('heading', { name: /office hours/i })).toBeVisible()
    const availableCard = page.locator('.ui-card', { hasText: /available sessions/i })
    await expect(availableCard.getByText('Mathematics Office Hour').first()).toBeVisible()
    // The capacity-reached session is correctly excluded from the available list.
    await expect(availableCard.getByText(/full office hour/i)).toHaveCount(0)
  })

  test('J69 booking a session is confirmed by the server', async () => {
    await page.goto('/app/student/office-hours')
    const availableCard = page.locator('.ui-card', { hasText: /available sessions/i })
    const respP = page.waitForResponse((r) => /\/office-hours\/.+\/bookings$/.test(r.url()) && r.request().method() === 'POST')
    await availableCard.getByRole('button', { name: /^book$/i }).first().click()
    expect((await respP).status()).toBe(201)
    // It now appears under "My bookings".
    const mineCard = page.locator('.ui-card', { hasText: /my bookings/i })
    await expect(mineCard.getByText('Mathematics Office Hour').first()).toBeVisible()
  })

  test('J70 the booking persists across reload', async () => {
    await page.goto('/app/student/office-hours')
    const mineCard = page.locator('.ui-card', { hasText: /my bookings/i })
    await expect(mineCard.getByText('Mathematics Office Hour').first()).toBeVisible()
    await expect(mineCard.getByText(/no bookings yet/i)).toHaveCount(0)
  })

  test('J71 booking the same session again is rejected (no duplicate)', async () => {
    await page.goto('/app/student/office-hours')
    const availableCard = page.locator('.ui-card', { hasText: /available sessions/i })
    const respP = page.waitForResponse((r) => r.url().includes(`/office-hours/${FIX.ohOpen}/bookings`) && r.request().method() === 'POST')
    await availableCard.getByRole('button', { name: /^book$/i }).first().click()
    expect((await respP).status()).toBe(409)
    await expect(page.locator('[role="alert"]').first()).toBeVisible()
  })

  test('J72 a capacity-reached session cannot be overbooked (server authoritative)', async () => {
    // The UI never offers a full session (J68). The capacity rule is enforced
    // server-side: a direct booking on the full session is rejected with 409.
    const token = await apiLogin(CODES.studentT1)
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.post(`${BACKEND}/api/v1/office-hours/${FIX.ohFull}/bookings`, {
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        data: { notes: 'overbook attempt' },
      })
      expect(res.status()).toBe(409)
    } finally {
      await ctx.dispose()
    }
  })
})
