// Phase 10 — Parent Portal auth + role guards (live).
import { test, expect } from '@playwright/test'
import { CODES10, HAS_CREDS, backendUp, login, nav, attachGuards } from '../support/ph10.js'

test.describe('Phase 10 — parent auth & role guards', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH10-A1 parent login lands on the Parent Portal dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.parentLinked)
    await expect(page).toHaveURL(/\/app\/parent$/)
    await expect(page.getByRole('heading', { name: 'Parent dashboard' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-A2 a student cannot reach the parent portal (role guard → forbidden)', async ({ page }) => {
    await login(page, CODES10.studentT1)
    // Wait for the post-login landing to settle before navigating, so the SPA
    // role guard (not a race with the login redirect) is what denies access.
    await expect(page).toHaveURL(/\/app\/student$/)
    await nav(page, '/app/parent')
    await expect(page).toHaveURL(/\/forbidden$/)
  })

  test('PH10-A3 an unauthenticated visitor is redirected to login', async ({ page }) => {
    await page.goto('/app/parent')
    await expect(page).toHaveURL(/\/login/)
  })
})
