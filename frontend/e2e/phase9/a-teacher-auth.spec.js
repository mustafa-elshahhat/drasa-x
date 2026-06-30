// Phase 9 — Group A: teacher authentication, dashboard, route guards, cross-tenant.
import { test, expect } from '@playwright/test'
import { CODES9, FIX9, PASSWORD, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph9.js'

test.describe.configure({ mode: 'serial' })

test.beforeAll(async () => {
  test.skip(!PASSWORD, 'E2E_PASSWORD not set')
  test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  await resetE2E()
})

test('PH9-A1 teacher logs in and the dashboard loads assignment-scoped data', async ({ browser }) => {
  const context = await browser.newContext()
  const page = await context.newPage()
  const guards = attachGuards(page)
  try {
    await login(page, CODES9.teacher)
    await nav(page, '/app/teacher')
    await expect(page.getByRole('heading', { name: /teacher dashboard/i })).toBeVisible()
    await expect(page.getByText(/assigned classes/i).first()).toBeVisible()
    await expect(page.getByText(/students/i).first()).toBeVisible()
    guards.assertNoForbidden()
  } finally {
    await context.close()
  }
})

test('PH9-A2 a student is denied every teacher route', async ({ browser }) => {
  const context = await browser.newContext()
  const page = await context.newPage()
  try {
    await login(page, CODES9.studentT1)
    for (const route of ['/app/teacher', '/app/teacher/classes', '/app/teacher/quizzes', '/app/teacher/quiz-generate']) {
      await page.goto(route)
      await expect(page).not.toHaveURL(/\/app\/teacher/)
    }
  } finally {
    await context.close()
  }
})

test('PH9-A3 parent and system admin are denied teacher routes', async ({ browser }) => {
  for (const code of [CODES9.parent, CODES9.systemAdmin]) {
    const context = await browser.newContext()
    const page = await context.newPage()
    try {
      await login(page, code)
      await page.goto('/app/teacher')
      await expect(page).not.toHaveURL(/\/app\/teacher$/)
    } finally {
      await context.close()
    }
  }
})

test('PH9-A4 direct navigation to a protected teacher URL while anonymous redirects to login', async ({ browser }) => {
  const context = await browser.newContext()
  const page = await context.newPage()
  try {
    await page.goto('/app/teacher/quizzes')
    await expect(page).toHaveURL(/\/login/)
  } finally {
    await context.close()
  }
})

test('PH9-A5 an unassigned teacher sees an empty class list (no data leak)', async ({ browser }) => {
  const context = await browser.newContext()
  const page = await context.newPage()
  const guards = attachGuards(page)
  try {
    await login(page, CODES9.teacherUnassigned)
    await nav(page, '/app/teacher/classes')
    await expect(page.getByText(/no assigned classes/i)).toBeVisible()
    guards.assertNoForbidden()
  } finally {
    await context.close()
  }
})

test('PH9-A6 a cross-tenant teacher cannot read another tenant\'s class students', async ({ browser }) => {
  const context = await browser.newContext()
  const page = await context.newPage()
  const guards = attachGuards(page)
  try {
    await login(page, CODES9.teacherT2)
    // TEACH-T2 is tenant-2; PH8-CLASS-T1 is tenant-1 → backend returns 404, UI shows error, no roster leak.
    await nav(page, `/app/teacher/classes/${FIX9.classT1}`)
    await expect(page.getByText(/Youssef Ibrahim|Salma Adel/)).toHaveCount(0)
    guards.assertNoForbidden() // 4xx is allowed; only 5xx/AI/archived calls are defects
  } finally {
    await context.close()
  }
})
