// Phase 12 — regression smoke: the Student (Phase 8), Teacher (Phase 9), Parent
// (Phase 10) and School Admin (Phase 11) portals must still load and serve real
// backend data after the System Admin Portal work, and the system portal must never
// call the AI/archived services.
import { test, expect } from '@playwright/test'
import { CODES12, HAS_CREDS, backendUp, login, nav, attachGuards } from '../support/ph12.js'

test.describe('Phase 12 — Phase 8/9/10/11 regression smoke', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH12-C1 Student Portal (Phase 8) still loads its dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.student)
    await expect(page).toHaveURL(/\/app\/student$/)
    await expect(page.getByRole('heading', { name: 'Student dashboard' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-C2 Teacher Portal (Phase 9) still loads its dashboard and classes', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.teacher)
    await expect(page).toHaveURL(/\/app\/teacher$/)
    await expect(page.getByRole('heading', { name: 'Teacher dashboard' })).toBeVisible()
    await nav(page, '/app/teacher/classes')
    await expect(page.getByRole('heading', { name: 'My classes' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-C3 Parent Portal (Phase 10) still loads its dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.parent)
    await expect(page).toHaveURL(/\/app\/parent$/)
    await expect(page.getByRole('heading', { name: 'Parent dashboard' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-C4 School Admin Portal (Phase 11) still loads its dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.schoolAdmin)
    await expect(page).toHaveURL(/\/app\/school$/)
    await expect(page.getByRole('heading', { name: 'School administration' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-C5 the system portal never calls the AI service or archived backend', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await nav(page, '/app/system')
    await nav(page, '/app/system/tenants')
    await nav(page, '/app/system/usage')
    await nav(page, '/app/system/ai-usage')
    await nav(page, '/app/system/audit')
    await nav(page, '/app/system/health')
    guards.assertNoForbidden()
  })
})
