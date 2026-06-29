// Phase 13 B — announcement targeting (recipients-only), preference suppression,
// and SignalR real-time delivery without a page refresh.
import { test, expect } from '@playwright/test'
import { BACKEND, PASSWORD, HAS_CREDS, CODES13, backendUp, resetE2E, login, nav, attachGuards, apiLoginFull, publishAnnouncement, unreadCount } from '../support/ph13.js'

test.describe('Phase 13 B — announcements, preferences, real-time', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run live E2E')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    await resetE2E()
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
  })
  test.afterAll(async () => { await context?.close() })

  test('B1 SignalR delivers an announcement notification in real time (no refresh)', async () => {
    await login(page, CODES13.studentA)
    await nav(page, '/app/notifications')
    // Confirm the real-time hub is actually connected (no fake "live" state).
    await expect(page.getByTestId('notifications-realtime')).toHaveText(/live/i, { timeout: 20000 })

    const { token: adminToken } = await apiLoginFull(CODES13.admin)
    const { token: aToken } = await apiLoginFull(CODES13.studentA)
    const before = await unreadCount(aToken)

    // The admin publishes a Students-targeted announcement (real backend contract).
    await publishAnnouncement(adminToken, { title: 'PH13 E2E live notice', body: 'Live broadcast to students.', targetAudience: 1 })

    // The new notification appears on the page WITHOUT navigating/reloading (SignalR push →
    // query invalidation → refetch → render). The page is never reloaded between trigger and assert.
    await expect(page.getByText(/PH13 E2E live notice/)).toBeVisible({ timeout: 15000 })
    expect(await unreadCount(aToken)).toBeGreaterThan(before)
    guards.assertNoForbidden()
  })

  test('B2 a non-targeted role does not receive the announcement (recipients-only)', async () => {
    const { token: teacherToken } = await apiLoginFull(CODES13.teacher)
    // Teacher is not in a Students-only audience.
    const teacherCtx = await page.context().browser().newContext()
    const tp = await teacherCtx.newPage()
    await login(tp, CODES13.teacher)
    await nav(tp, '/app/notifications')
    await expect(tp.getByRole('heading', { name: /^notifications$/i })).toBeVisible()
    await expect(tp.getByText(/PH13 E2E live notice/)).toHaveCount(0)
    void teacherToken
    await teacherCtx.close()
  })

  test('B3 preferences suppress an optional category but lock mandatory ones', async () => {
    const bCtx = await page.context().browser().newContext()
    const bp = await bCtx.newPage()
    await login(bp, CODES13.studentB)

    // Disable the optional Announcement category in-app.
    await nav(bp, '/app/notifications/preferences')
    const annRow = bp.getByTestId('pref-Announcement')
    await expect(annRow).toBeVisible({ timeout: 15000 })
    const annToggle = annRow.locator('input[type="checkbox"]').first()
    // Controlled checkbox bound to the persisted preference: click, then wait for the saved state
    // (the PUT → query refetch reflects inAppEnabled:false).
    if (await annToggle.isChecked()) {
      await annToggle.click()
      await expect(annToggle).not.toBeChecked({ timeout: 10000 })
    }

    // Mandatory categories cannot be turned off (locked in the UI).
    await expect(bp.getByTestId('pref-Warning').locator('input[type="checkbox"]').first()).toBeDisabled()

    const { token: bToken } = await apiLoginFull(CODES13.studentB)
    const before = await unreadCount(bToken)
    const { token: adminToken } = await apiLoginFull(CODES13.admin)
    await publishAnnouncement(adminToken, { title: 'PH13 suppressed notice', body: 'Should not reach B.', targetAudience: 1 })

    // Give routing a moment; B's count must NOT increase (optional category suppressed).
    await bp.waitForTimeout(2500)
    expect(await unreadCount(bToken)).toBe(before)
    await bCtx.close()
  })
})
