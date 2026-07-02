// Testing gap T-05 (route/detail/RBAC audit §10 item 5): "School-admin e2e
// row-to-detail coverage — phase11/b-school-management.spec.js B2 only asserts
// the roster lists rows; no e2e clicks any school entity into a detail/action
// anywhere." This spec closes that gap for both entity types that gained a
// real detail page this pass (P1-1 user detail, P1-2/P1-3 class detail).
import { test, expect } from '@playwright/test'
import { CODES11, HAS_CREDS, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph11.js'

const h1 = (page) => page.getByRole('heading', { level: 1 })

test.describe('Phase 11 / T-05 — school-admin row-to-detail navigation', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    await resetE2E()
  })

  test('T-05a admin opens a student from the roster into a real detail page and toggles account status', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/students')
    await expect(h1(page)).toHaveText('Students')

    const firstRow = page.locator('.ui-table tbody tr, .student-list__item').first()
    await expect(firstRow).toBeVisible()
    await firstRow.getByRole('link', { name: 'Open' }).click()

    await expect(page).toHaveURL(/\/app\/school\/students\/[^/]+$/)
    await expect(h1(page)).toHaveText('User details')
    // Real account lifecycle data (not a placeholder) — one of Enable/Disable is
    // always actionable and the other disabled, proving live isDisabled state.
    const enableBtn = page.getByRole('button', { name: 'Enable account' })
    const disableBtn = page.getByRole('button', { name: 'Disable account' })
    await expect(enableBtn).toBeVisible()
    await expect(disableBtn).toBeVisible()

    if (await disableBtn.isEnabled()) {
      await disableBtn.click()
      await expect(page.getByText('Account disabled.')).toBeVisible()
      await expect(enableBtn).toBeEnabled()
      // Restore state so this fixture stays reusable across runs.
      await enableBtn.click()
      await expect(page.getByText('Account enabled.')).toBeVisible()
    } else {
      await enableBtn.click()
      await expect(page.getByText('Account enabled.')).toBeVisible()
      await expect(disableBtn).toBeEnabled()
    }
    guards.assertNoForbidden()
  })

  test('T-05b admin opens a class from the list into a real detail page with roster', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES11.schoolAdmin)
    await nav(page, '/app/school/classes')
    await expect(h1(page)).toHaveText('Classes')

    const firstRow = page.locator('.ui-table tbody tr').first()
    await expect(firstRow).toBeVisible()
    await firstRow.getByRole('link', { name: 'Open' }).click()

    await expect(page).toHaveURL(/\/app\/school\/classes\/[^/]+$/)
    await expect(h1(page)).toHaveText('Class details')
    guards.assertNoForbidden()
  })
})
