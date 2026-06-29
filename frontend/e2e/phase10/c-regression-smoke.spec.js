// Phase 10 — regression smoke: Student (Phase 8) and Teacher (Phase 9) portals
// must still load and serve real backend data after the Parent Portal work.
import { test, expect } from '@playwright/test'
import { CODES10, HAS_CREDS, backendUp, login, nav, attachGuards } from '../support/ph10.js'

test.describe('Phase 10 — Phase 8/9 regression smoke', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH10-C1 Student Portal (Phase 8) still loads its dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.studentT1)
    await expect(page).toHaveURL(/\/app\/student$/)
    await expect(page.getByRole('heading', { name: 'Student dashboard' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-C2 Teacher Portal (Phase 9) still loads its dashboard and classes', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.teacher)
    await expect(page).toHaveURL(/\/app\/teacher$/)
    await expect(page.getByRole('heading', { name: 'Teacher dashboard' })).toBeVisible()
    await nav(page, '/app/teacher/classes')
    await expect(page.getByRole('heading', { name: 'My classes' })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-C3 the parent portal never calls the AI service or archived backend', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.parentLinked)
    await nav(page, '/app/parent')
    await nav(page, '/app/parent/children')
    await nav(page, '/app/parent/announcements')
    await nav(page, '/app/parent/notifications')
    guards.assertNoForbidden()
  })
})
