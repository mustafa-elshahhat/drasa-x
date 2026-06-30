import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, backendUp, login, nav, resetE2E, attachGuards } from '../support/ph8.js'

// Group M — Badges, streaks, profile, settings (M86–M91).
test.describe('Phase 8 M — badges / streaks / profile / settings', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E()
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('M86 the badge catalog renders backend records', async () => {
    await nav(page, '/app/student/badges')
    await expect(page.getByRole('heading', { name: /^badges$/i })).toBeVisible()
    const catalog = page.locator('.ui-card', { hasText: /badge catalog/i })
    await expect(catalog.getByText('First Lesson').first()).toBeVisible()
    await expect(catalog.getByText('3-Day Streak').first()).toBeVisible()
  })

  test('M87 only awarded badges are shown and survive reload', async () => {
    await page.goto('/app/student/badges')
    const earned = page.locator('.ui-card', { hasText: /earned badges/i })
    await expect(earned.getByText(/no badges yet/i)).toHaveCount(0)
    await expect(earned.getByText(/phase 8 fixture award|E2E-PH8-BADGE-1/i).first()).toBeVisible()
    await page.reload()
    await expect(earned.getByText(/phase 8 fixture award|E2E-PH8-BADGE-1/i).first()).toBeVisible()
  })

  test('M88 the streak is the backend streak (not client-computed)', async ({ browser }) => {
    // The deterministic streak fixture belongs to the supporting PH8-OTHER-T1
    // student (STU-T1's streak slot is reserved for a backend integration test).
    const ctx = await browser.newContext()
    const p = await ctx.newPage()
    try {
      await login(p, 'PH8-OTHER-T1')
      await p.goto('/app/student/streaks')
      const streakCard = p.locator('.ui-card', { hasText: /learning streaks/i })
      await expect(streakCard).toBeVisible()
      await expect(streakCard.getByText('3').first()).toBeVisible() // currentCount
      await expect(streakCard.getByText('5').first()).toBeVisible() // longestCount
    } finally {
      await ctx.close()
    }
  })

  test('M89 the profile shows the real signed-in identity; localStorage is not authoritative', async () => {
    await page.goto('/app/student/profile')
    await expect(page.getByText('STU-T1').first()).toBeVisible()
    await expect(page.getByText('Youssef Ibrahim').first()).toBeVisible()
    // No access token / identity authority is persisted in localStorage.
    const dump = await page.evaluate(() => JSON.stringify(window.localStorage))
    expect(dump).not.toMatch(/eyJ[A-Za-z0-9_-]+\./) // no JWT
    expect(dump.toLowerCase()).not.toContain('accesstoken')
    expect(dump.toLowerCase()).not.toContain('"token"')
  })

  test('M90 settings link to the single existing security implementation', async () => {
    await nav(page, '/app/student/settings')
    await expect(page.getByRole('heading', { name: /account settings/i })).toBeVisible()
    const link = page.getByRole('link', { name: /change password/i })
    await expect(link).toHaveAttribute('href', /\/app\/security$/)
  })

  test('M91 badge/streak/profile state is read-only from the server (no client authority sent)', async () => {
    const mutating = []
    const listener = (req) => {
      const u = req.url()
      const m = req.method()
      if (m !== 'GET' && /\/(badges|streak|students)\b/.test(u)) mutating.push(`${m} ${u}`)
    }
    page.on('request', listener)
    await page.goto('/app/student/badges')
    await expect(page.locator('.ui-card', { hasText: /badge catalog/i })).toBeVisible()
    await page.goto('/app/student/streaks')
    await expect(page.locator('.ui-card', { hasText: /learning streaks/i })).toBeVisible()
    page.off('request', listener)
    // Badges/streak are server-owned: the client never POSTs awards/points/streaks.
    expect(mutating, `unexpected mutating badge/streak requests: ${mutating.join(', ')}`).toEqual([])
    // And no client-side authority is stashed anywhere in web storage.
    const storage = await page.evaluate(() => JSON.stringify({ ls: window.localStorage, ss: window.sessionStorage }))
    for (const k of ['role', 'tenantId', 'studentId', 'rank', 'points']) {
      expect(storage.toLowerCase()).not.toContain(`"${k.toLowerCase()}"`)
    }
  })
})
