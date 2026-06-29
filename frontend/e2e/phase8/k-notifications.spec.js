import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, backendUp, login, resetE2E, attachGuards } from '../support/ph8.js'

// Group K — Notifications & announcements (K73–K79).
test.describe('Phase 8 K — notifications & announcements', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // re-create fresh unread E2E notifications
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  // Read the count the UI shows (kept in sync with the server's unread-count).
  async function bannerCount() {
    const banner = page.locator('.ui-alert', { hasText: /unread count/i })
    const txt = await banner.innerText()
    const m = txt.match(/\d+/)
    return m ? Number(m[0]) : null
  }
  // Read the server's current unread count via the page's authenticated GET.
  async function serverUnread() {
    const resp = await page.waitForResponse(
      (r) => r.url().includes('/api/v1/notifications/unread-count') && r.request().method() === 'GET',
    )
    const json = await resp.json().catch(() => ({}))
    return (json?.data ?? json)?.unreadCount
  }

  test('K73 the inbox lists the student\'s own notifications', async () => {
    await page.goto('/app/student/notifications')
    await expect(page.getByRole('heading', { name: /^notifications$/i })).toBeVisible()
    await expect(page.getByText(/phase 8 notification/i).first()).toBeVisible({ timeout: 15000 })
  })

  test('K74 the unread count shown matches the server', async () => {
    const countP = serverUnread()
    await page.goto('/app/student/notifications')
    const count = await countP
    expect(typeof count).toBe('number')
    await expect(page.locator('.ui-alert', { hasText: /unread count/i })).toContainText(String(count))
  })

  test('K75 marking one notification read decrements the count', async () => {
    await page.goto('/app/student/notifications')
    await expect.poll(bannerCount).not.toBeNull()
    const before = await bannerCount()
    await page.getByRole('button', { name: /^mark read$/i }).first().click()
    await expect.poll(bannerCount, { timeout: 10000 }).toBe(before - 1)
  })

  test('K76 marking all read drops the unread count to zero', async () => {
    await page.goto('/app/student/notifications')
    await expect.poll(bannerCount).not.toBeNull()
    await page.getByRole('button', { name: /mark all read/i }).click()
    await expect.poll(bannerCount, { timeout: 10000 }).toBe(0)
  })

  test('K77 the read state persists across reload', async () => {
    await page.goto('/app/student/notifications')
    await expect.poll(bannerCount, { timeout: 10000 }).toBe(0)
  })

  test('K78 announcements show tenant-targeted records', async () => {
    await page.goto('/app/student/announcements')
    await expect(page.getByRole('heading', { name: /^announcements$/i })).toBeVisible()
    await expect(page.getByText('Phase 8 School Announcement').first()).toBeVisible()
  })

  test('K79 the inbox exposes no external or foreign-tenant deep links', async () => {
    await page.goto('/app/student/notifications')
    await expect(page.getByText(/phase 8 notification/i).first()).toBeVisible()
    // Notification action targets are rendered as inert text (deep-link navigation
    // is not wired); any anchor that does exist must be an internal /app route —
    // never an external origin or a cross-tenant absolute URL.
    const hrefs = await page.locator('main a[href]').evaluateAll((els) => els.map((e) => e.getAttribute('href')))
    for (const href of hrefs) {
      // No external origin and no protocol-relative/cross-origin target.
      expect(/^https?:\/\//i.test(href), `external link in inbox: ${href}`).toBe(false)
      expect(href.startsWith('//'), `protocol-relative link in inbox: ${href}`).toBe(false)
      // Internal links are same-origin app routes (tenant is enforced by the token).
      if (href.startsWith('/')) expect(/^\/app(\/|$)|^\/login(\/|$)/.test(href), `unexpected route: ${href}`).toBe(true)
    }
  })
})
