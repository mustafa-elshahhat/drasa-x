// Testing gaps T-13/T-14/T-15 (route/detail/RBAC audit, Phase 5 instruction):
// prove a Teacher can actually SEE and CLICK the Homework/Competitions/Vision
// sidebar links (P0-5/D1/D2 fixed these being missing from nav entirely), and
// a Student can do the same for Vision (D1) — via a real sidebar link click,
// not the `nav()` SPA-history-push helper every other phase8/9 spec uses
// (which bypasses the sidebar and would not have caught the original bug).
import { test, expect } from '@playwright/test'
import { CODES9, HAS_CREDS, backendUp, resetE2E, login, attachGuards } from '../support/ph9.js'

const h1 = (page) => page.getByRole('heading', { level: 1 })
const sidebar = (page) => page.locator('.app-sidebar__nav')

test.describe('Phase 9 / T-13,T-14,T-15 — nav visibility for previously-orphaned surfaces', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    await resetE2E()
  })

  test('T-13 teacher sidebar exposes Homework and clicking it opens the real page', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES9.teacher)
    const link = sidebar(page).getByRole('link', { name: 'Homework' })
    await expect(link).toBeVisible()
    await link.click()
    await expect(page).toHaveURL(/\/app\/teacher\/homework$/)
    await expect(h1(page)).toHaveText('Homework')
    guards.assertNoForbidden()
  })

  test('T-14 teacher sidebar exposes Competitions and clicking it opens the real page', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES9.teacher)
    const link = sidebar(page).getByRole('link', { name: 'Competitions' })
    await expect(link).toBeVisible()
    await link.click()
    await expect(page).toHaveURL(/\/app\/teacher\/competitions$/)
    await expect(h1(page)).toHaveText('Competitions')
    guards.assertNoForbidden()
  })

  test('T-15a teacher sidebar exposes Computer-vision attendance and clicking it opens the real page', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES9.teacher)
    const link = sidebar(page).getByRole('link', { name: 'Computer-vision attendance' })
    await expect(link).toBeVisible()
    await link.click()
    await expect(page).toHaveURL(/\/app\/vision$/)
    await expect(h1(page)).toHaveText('Computer-vision attendance')
    guards.assertNoForbidden()
  })

  test('T-15b student sidebar exposes My engagement (vision) and clicking it opens the real page', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES9.studentT1)
    const link = sidebar(page).getByRole('link', { name: 'My engagement' })
    await expect(link).toBeVisible()
    await link.click()
    await expect(page).toHaveURL(/\/app\/student\/vision$/)
    await expect(h1(page)).toHaveText('My engagement')
    guards.assertNoForbidden()
  })
})
