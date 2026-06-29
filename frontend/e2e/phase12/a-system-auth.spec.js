import { test, expect } from '@playwright/test'
import { CODES12, HAS_CREDS, backendUp, login, nav, attachGuards } from '../support/ph12.js'

test.describe('Phase 12 — system-admin auth & role guards', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH12-A1 system admin login lands on the Platform administration dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await expect(page).toHaveURL(/\/app\/system$/)
    await expect(page.getByRole('heading', { name: 'Platform administration' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-A2 a student cannot reach the system portal (role guard → forbidden)', async ({ page }) => {
    await login(page, CODES12.student)
    await expect(page).toHaveURL(/\/app\/student$/)
    await nav(page, '/app/system')
    await expect(page).toHaveURL(/\/forbidden$/)
  })

  test('PH12-A3 a school admin cannot reach the system portal (role guard → forbidden)', async ({ page }) => {
    await login(page, CODES12.schoolAdmin)
    await expect(page).toHaveURL(/\/app\/school$/)
    await nav(page, '/app/system')
    await expect(page).toHaveURL(/\/forbidden$/)
  })

  test('PH12-A4 an unauthenticated visitor is redirected to login', async ({ page }) => {
    await page.goto('/app/system')
    await expect(page).toHaveURL(/\/login/)
  })
})
