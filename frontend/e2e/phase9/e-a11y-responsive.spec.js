// Phase 9 — Group E: accessibility + responsive/RTL smoke on representative pages.
import { test, expect } from '@playwright/test'
import { CODES9, PASSWORD, backendUp, resetE2E, login, nav, setLang, attachGuards, axeSeriousOrCritical } from '../support/ph9.js'

test.describe.configure({ mode: 'serial' })

let context, page, guards

test.beforeAll(async ({ browser }) => {
  test.skip(!PASSWORD, 'E2E_PASSWORD not set')
  test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  await resetE2E()
  context = await browser.newContext()
  page = await context.newPage()
  guards = attachGuards(page)
  await login(page, CODES9.teacher)
})

test.afterAll(async () => { await context?.close() })

const ROUTES = ['/app/teacher', '/app/teacher/classes', '/app/teacher/quizzes', '/app/teacher/quiz-generate']

test('PH9-E1 representative teacher pages have no serious/critical a11y violations (EN)', async () => {
  await setLang(page, 'en')
  for (const route of ROUTES) {
    await nav(page, route)
    await expect(page.locator('main')).toBeVisible()
    const violations = await axeSeriousOrCritical(page)
    expect(violations, `a11y violations on ${route}: ${violations.map((v) => v.id).join(', ')}`).toEqual([])
  }
  guards.assertNoForbidden()
})

test('PH9-E2 Arabic flips direction to RTL on the teacher dashboard', async ({ browser }) => {
  const arContext = await browser.newContext()
  const arPage = await arContext.newPage()
  try {
    await login(arPage, CODES9.teacher)
    await nav(arPage, '/app/teacher')
    await setLang(arPage, 'ar')
    await expect(arPage.locator('html')).toHaveAttribute('dir', 'rtl')
    const violations = await axeSeriousOrCritical(arPage)
    expect(violations).toEqual([])
  } finally {
    await arContext.close()
  }
})

test('PH9-E3 mobile viewport renders the teacher dashboard', async ({ browser }) => {
  const mContext = await browser.newContext({ viewport: { width: 390, height: 844 } })
  const mPage = await mContext.newPage()
  try {
    await login(mPage, CODES9.teacher)
    await nav(mPage, '/app/teacher')
    await expect(mPage.getByRole('heading', { name: /teacher dashboard/i })).toBeVisible()
  } finally {
    await mContext.close()
  }
})
