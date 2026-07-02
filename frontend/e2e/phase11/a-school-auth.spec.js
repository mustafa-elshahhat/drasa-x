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

// SchoolAdmin Teacher-portal removal: SchoolAdmin must see and use ONLY the School
// Admin portal — no Teacher-portal nav items, and no direct-URL access to any
// /app/teacher/* route or the Teacher-only /app/vision surface. A previous pass
// deliberately shared these with SchoolAdmin; that is no longer the desired
// product behavior.
test.describe('Phase 11 — SchoolAdmin Teacher-portal removal', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  const sidebar = (page) => page.locator('.app-sidebar__nav')

  test('PH11-A5 school admin sidebar has no Teacher-portal items', async ({ page }) => {
    await login(page, CODES11.schoolAdmin)
    await expect(page).toHaveURL(/\/app\/school$/)
    const nav = sidebar(page)
    for (const label of [
      'Teacher dashboard', 'My classes', 'My subjects', 'Generate AI quiz draft', 'Computer-vision attendance',
    ]) {
      await expect(nav.getByRole('link', { name: label, exact: true })).toHaveCount(0)
    }
    // Sanity: the school admin's own portal links are still present.
    await expect(nav.getByRole('link', { name: 'School administration', exact: true })).toBeVisible()
  })

  for (const path of [
    '/app/teacher',
    '/app/teacher/classes',
    '/app/teacher/subjects',
    '/app/teacher/quizzes',
    '/app/teacher/quiz-generate',
    '/app/teacher/homework',
    '/app/teacher/competitions',
    '/app/teacher/office-hours',
    '/app/teacher/communities',
    '/app/teacher/settings',
    '/app/vision',
  ]) {
    test(`PH11-A6 school admin direct URL to ${path} is forbidden`, async ({ page }) => {
      await login(page, CODES11.schoolAdmin)
      await nav(page, path)
      await expect(page).toHaveURL(/\/forbidden$/)
    })
  }

  test('PH11-A7 school admin header settings link points to /app/school/settings, never /app/teacher/settings', async ({ page }) => {
    await login(page, CODES11.schoolAdmin)
    await page.getByRole('button', { name: 'Account menu' }).click()
    const settingsLink = page.getByRole('menuitem', { name: /settings/i })
    await expect(settingsLink).toHaveAttribute('href', '/app/school/settings')
  })

  test('PH11-A8 teacher portal still works for a real Teacher after the removal', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.teacher)
    await expect(page).toHaveURL(/\/app\/teacher$/)
    await expect(page.getByRole('heading', { name: 'Teacher dashboard' })).toBeVisible()
    await expect(sidebar(page).getByRole('link', { name: 'My classes', exact: true })).toBeVisible()
    guards.assertNoForbidden()
  })
})
