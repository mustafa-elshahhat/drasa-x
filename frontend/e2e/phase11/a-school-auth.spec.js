// Phase 11 — School Admin Portal auth + role guards (live).
import { test, expect } from '@playwright/test'
import { CODES11, HAS_CREDS, backendUp, login, nav, attachGuards } from '../support/ph11.js'

test.describe('Phase 11 — school-admin auth & role guards', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH11-A1 school admin login lands on the School Admin dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await expect(page).toHaveURL(/\/app\/school$/)
    await expect(page.getByRole('heading', { name: 'School administration' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-A2 a student cannot reach the school portal (role guard → forbidden)', async ({ page }) => {
    await login(page, CODES11.student)
    await expect(page).toHaveURL(/\/app\/student$/)
    await nav(page, '/app/school')
    await expect(page).toHaveURL(/\/forbidden$/)
  })

  test('PH11-A3 a parent cannot reach the school portal (role guard → forbidden)', async ({ page }) => {
    await login(page, CODES11.parent)
    await expect(page).toHaveURL(/\/app\/parent$/)
    await nav(page, '/app/school')
    await expect(page).toHaveURL(/\/forbidden$/)
  })

  test('PH11-A4 an unauthenticated visitor is redirected to login', async ({ page }) => {
    await page.goto('/app/school')
    await expect(page).toHaveURL(/\/login/)
  })
})
