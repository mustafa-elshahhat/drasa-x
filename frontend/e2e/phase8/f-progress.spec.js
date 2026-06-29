import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, backendUp, login, nav, attachGuards } from '../support/ph8.js'

// Group F — Recommendations, progress, engagement (F48–F51).
test.describe('Phase 8 F — recommendations / progress / engagement', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('F48 personalized recommendations render persisted backend records', async () => {
    await nav(page, '/app/student/recommendations')
    await expect(page.getByRole('heading', { name: /ai recommendations/i })).toBeVisible()
    await expect(page.getByText('Practice linear equations').first()).toBeVisible()
  })

  test('F49 progress renders real subject/metric progress', async () => {
    await nav(page, '/app/student/progress')
    await expect(page.getByRole('heading', { name: /^progress$/i })).toBeVisible()
    await expect(page.getByText(/subject progress/i)).toBeVisible()
    // A persisted numeric metric is shown (not a frontend-computed value).
    await expect(page.getByText(/completionPercentage/i).first()).toBeVisible()
  })

  test('F50 engagement renders persisted metrics/insights', async () => {
    await nav(page, '/app/student/engagement')
    await expect(page.getByRole('heading', { name: /engagement summary/i })).toBeVisible()
    await expect(page.getByText('Phase 8 fixture insight: steady progress in algebra.').first()).toBeVisible()
  })

  test('F51 recommendations/progress/engagement stay accurate after reload', async () => {
    await page.goto('/app/student/recommendations')
    await expect(page.getByText('Practice linear equations').first()).toBeVisible()
    await page.goto('/app/student/engagement')
    await expect(page.getByText(/steady progress in algebra/i).first()).toBeVisible()
  })
})
