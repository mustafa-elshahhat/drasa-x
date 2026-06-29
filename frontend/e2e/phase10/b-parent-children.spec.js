// Phase 10 — Parent Portal child monitoring + relationship/tenant scoping (live).
import { test, expect } from '@playwright/test'
import { CODES10, CHILD_NAME, HAS_CREDS, backendUp, login, nav, attachGuards, apiLoginId } from '../support/ph10.js'

test.describe('Phase 10 — parent child monitoring', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
  })

  test('PH10-B1 parent sees only the linked child in the children list', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.parentLinked)
    await nav(page, '/app/parent/children')
    await expect(page.getByText(new RegExp(CHILD_NAME))).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-B2 parent opens the linked child profile (real backend data)', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.parentLinked)
    await nav(page, '/app/parent/children')
    await page.getByText(new RegExp(CHILD_NAME)).click()
    await expect(page).toHaveURL(/\/app\/parent\/children\/[^/]+$/)
    await expect(page.getByText('Progress summary')).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-B3 parent views the child academic progress from the backend', async ({ page }) => {
    const guards = attachGuards(page)
    const childId = await apiLoginId(CODES10.studentT1)
    await login(page, CODES10.parentLinked)
    await nav(page, `/app/parent/children/${childId}/progress`)
    await expect(page.getByRole('heading', { name: 'Academic progress' })).toBeVisible()
    await expect(page.getByText('Quiz results')).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-B4 parent views the child attendance read model', async ({ page }) => {
    const guards = attachGuards(page)
    const childId = await apiLoginId(CODES10.studentT1)
    await login(page, CODES10.parentLinked)
    await nav(page, `/app/parent/children/${childId}/attendance`)
    await expect(page.getByText('Attendance summary')).toBeVisible()
    // STU-T1 has seeded attendance — at least the summary labels render.
    await expect(page.getByText('Total sessions')).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-B5 a parent with no children sees the empty state', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.parentNoChild)
    await nav(page, '/app/parent/children')
    await expect(page.getByText('No children are linked to your account.')).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH10-B6 parent cannot open a same-tenant UNLINKED child (denied, no profile leak)', async ({ page }) => {
    const guards = attachGuards(page)
    const otherId = await apiLoginId(CODES10.otherT1)
    await login(page, CODES10.parentLinked)
    await nav(page, `/app/parent/children/${otherId}`)
    // The backend returns 403; the UI renders an error state, never the child profile.
    await expect(page.getByText('Something went wrong')).toBeVisible()
    await expect(page.getByText('Progress summary')).toHaveCount(0)
    guards.assertNoForbidden() // 4xx is expected; only 5xx/AI/archived would fail here
  })

  test('PH10-B7 parent cannot open a CROSS-TENANT child (hidden as not found)', async ({ page }) => {
    const guards = attachGuards(page)
    const crossId = await apiLoginId(CODES10.studentT2)
    await login(page, CODES10.parentLinked)
    await nav(page, `/app/parent/children/${crossId}`)
    await expect(page.getByText('Something went wrong')).toBeVisible()
    await expect(page.getByText('Progress summary')).toHaveCount(0)
    guards.assertNoForbidden()
  })

  test('PH10-B8 parent submits a document request for a linked child', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES10.parentLinked)
    await nav(page, '/app/parent/documents')
    await expect(page.getByRole('heading', { name: 'Document requests' })).toBeVisible()
    await page.getByLabel('Child').selectOption({ label: CHILD_NAME })
    await page.getByLabel('Subject').fill('Official transcript request')
    await page.getByLabel('Details').fill('Please issue an official transcript for the term.')
    await page.getByRole('button', { name: /submit request/i }).click()
    await expect(page.getByText('Request submitted')).toBeVisible()
    guards.assertNoForbidden()
  })
})
