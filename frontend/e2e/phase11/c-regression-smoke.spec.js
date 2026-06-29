// Phase 11 — regression smoke: the Student (Phase 8), Teacher (Phase 9) and Parent
// (Phase 10) portals must still load and serve real backend data after the School
// Admin Portal work, and the school portal must never call the AI/archived services.
import { test, expect } from '@playwright/test'
import { CODES11, HAS_CREDS, backendUp, login, nav, attachGuards } from '../support/ph11.js'

test.describe('Phase 11 — Phase 8/9/10 regression smoke', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH11-C1 Student Portal (Phase 8) still loads its dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.student)
    await expect(page).toHaveURL(/\/app\/student$/)
    await expect(page.getByRole('heading', { name: 'Student dashboard' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-C2 Teacher Portal (Phase 9) still loads its dashboard and classes', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.teacher)
    await expect(page).toHaveURL(/\/app\/teacher$/)
    await expect(page.getByRole('heading', { name: 'Teacher dashboard' })).toBeVisible()
    await nav(page, '/app/teacher/classes')
    await expect(page.getByRole('heading', { name: 'My classes' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-C3 Parent Portal (Phase 10) still loads its dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.parentLinked)
    await expect(page).toHaveURL(/\/app\/parent$/)
    await expect(page.getByRole('heading', { name: 'Parent dashboard' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-C4 the school portal never calls the AI service or archived backend', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school')
    await nav(page, '/app/school/students')
    await nav(page, '/app/school/relationships')
    await nav(page, '/app/school/reports')
    await nav(page, '/app/school/ai-usage')
    await nav(page, '/app/school/audit-log')
    guards.assertNoForbidden()
  })
})
