// Phase 11 — School Admin Portal core management flows (live, real backend).
import { test, expect } from '@playwright/test'
import { CODES11, NAMES11, HAS_CREDS, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph11.js'

// The page title is the single <h1>; section cards are <h2>. Assert the <h1> to
// avoid an h1/h2 strict-mode collision when a section repeats the page title.
const h1 = (page) => page.getByRole('heading', { level: 1 })

test.describe('Phase 11 — school-admin management', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    await resetE2E() // clears Phase 11 admin-created links/assignments → repeatable
  })

  test('PH11-B1 dashboard shows a real tenant summary (no fabricated data)', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await expect(page).toHaveURL(/\/app\/school$/)
    await expect(page.getByText('Main School')).toBeVisible() // real tenant name
    await expect(h1(page)).toHaveText('School administration')
    guards.assertNoForbidden()
  })

  test('PH11-B2 students roster lists real tenant students', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/students')
    await expect(h1(page)).toHaveText('Students')
    await expect(page.locator('.student-list__item').first()).toBeVisible() // real roster rows
    guards.assertNoForbidden()
  })

  test('PH11-B3 admin creates an academic year', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/academic-years')
    await expect(h1(page)).toHaveText('Academic years')
    const stamp = String(Date.now()).slice(-6)
    const card = page.locator('.ui-card', { hasText: 'Create' }).first()
    await card.getByLabel('Name', { exact: true }).fill(`E2E Year ${stamp}`)
    await card.getByLabel('Code', { exact: true }).fill(`E2EY${stamp}`)
    await card.getByLabel('Start date', { exact: true }).fill('2031-09-01')
    await card.getByLabel('End date', { exact: true }).fill('2032-06-30')
    await card.getByRole('button', { name: 'Create', exact: true }).click()
    await expect(page.getByText('Created', { exact: true })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-B4 admin links a parent to a student', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/relationships')
    await expect(h1(page)).toBeVisible()
    const card = page.locator('.ui-card', { hasText: 'Link parent to student' })
    // The dedicated PH11 parent (reset clears its links each run → repeatable); any
    // tenant student works as the link target, so pick the first real option (the
    // roster picker is capped at the API page size, not every seeded student).
    await card.getByLabel('Parent', { exact: true }).selectOption({ label: NAMES11.parentUnlinked })
    await card.getByLabel('Student', { exact: true }).selectOption({ index: 1 })
    await card.getByRole('button', { name: 'Link parent to student' }).click()
    await expect(page.getByText(/link was created/i)).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-B5 admin assigns a teacher to a class', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/teacher-assignments')
    await expect(h1(page)).toHaveText('Teacher assignments')
    const card = page.locator('.ui-card', { hasText: 'Assign teacher to a class' })
    await card.getByLabel('Teacher', { exact: true }).selectOption({ label: NAMES11.teacherUnassigned })
    await card.getByLabel('Class', { exact: true }).selectOption({ label: NAMES11.classFixture })
    await card.getByRole('button', { name: 'Assign teacher to a class' }).click()
    await expect(page.getByText('Created', { exact: true })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-B6 admin publishes an announcement', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/announcements')
    await expect(h1(page)).toHaveText('Announcements')
    const stamp = String(Date.now()).slice(-6)
    const card = page.locator('.ui-card', { hasText: 'New announcement' })
    await card.getByLabel('Title', { exact: true }).fill(`E2E Notice ${stamp}`)
    await card.getByLabel('Message', { exact: true }).fill('Phase 11 live announcement.')
    await card.getByRole('button', { name: 'New announcement' }).click()
    await expect(page.getByText('Created', { exact: true })).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH11-B7 admin views the tenant audit log', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/audit-log')
    await expect(h1(page)).toHaveText('Audit log')
    guards.assertNoForbidden()
  })
})
