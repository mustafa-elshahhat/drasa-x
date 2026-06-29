import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, backendUp, login, nav, attachGuards } from '../support/ph8.js'

// Group A — Authentication, role boundaries, and session security (A1–A8).
test.describe('Phase 8 A — authentication & role boundaries', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD (and E2E_LOGIN_CODE) to run the live matrix')

  test.beforeAll(async () => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  })

  test('A1 anonymous user is redirected to login and sees no protected student data', async ({ browser }) => {
    const context = await browser.newContext()
    const page = await context.newPage()
    try {
      await page.goto('/app/student')
      await expect(page).toHaveURL(/\/login/)
      await expect(page.getByRole('heading', { name: /student dashboard/i })).toHaveCount(0)
    } finally {
      await context.close()
    }
  })

  test('A2 a valid student logs in and reaches the Student Portal dashboard', async ({ browser }) => {
    const context = await browser.newContext()
    const page = await context.newPage()
    const guards = attachGuards(page)
    try {
      await login(page, CODES.studentT1)
      await nav(page, '/app/student')
      await expect(page.getByRole('heading', { name: /student dashboard/i })).toBeVisible()
      await expect(page.getByRole('navigation', { name: /main navigation/i })).toBeVisible()
      guards.assertNoForbidden()
    } finally {
      await context.close()
    }
  })

  for (const r of [
    { code: CODES.teacher, label: 'A3 Teacher' },
    { code: CODES.parent, label: 'A4 Parent' },
    { code: CODES.schoolAdmin, label: 'A5 SchoolAdmin' },
    { code: CODES.systemAdmin, label: 'A6 SystemAdmin' },
  ]) {
    test(`${r.label} (${r.code}) is denied every Student Portal route`, async ({ browser }) => {
      const context = await browser.newContext()
      const page = await context.newPage()
      try {
        await login(page, r.code)
        for (const route of ['/app/student', '/app/student/subjects', `/app/student/lessons/${FIX.lessonT1}`, '/app/student/quizzes']) {
          await page.goto(route)
          await expect(page).not.toHaveURL(/\/app\/student/)
          await expect(page.getByRole('heading', { name: /student dashboard/i })).toHaveCount(0)
        }
        // No student navigation surfaced for a non-student role.
        await expect(page.getByRole('link', { name: /my subjects/i })).toHaveCount(0)
      } finally {
        await context.close()
      }
    })
  }

  test('A7 a student cannot reach another tenant\'s resource by tampering route params, and no client identity is sent', async ({ browser }) => {
    const context = await browser.newContext()
    const page = await context.newPage()
    const identityHeaders = []
    page.on('request', (req) => {
      const h = req.headers()
      for (const key of ['x-tenant-id', 'x-tenantid', 'tenantid', 'x-user-id', 'x-student-id', 'x-role']) {
        if (h[key] !== undefined) identityHeaders.push(`${key}=${h[key]}`)
      }
    })
    try {
      await login(page, CODES.studentT1)
      // Cross-tenant subject id must not leak the foreign subject.
      await nav(page, `/app/student/subjects/${FIX.subjectT1.replace('T1', 'T2')}`)
      await expect(page.locator('main')).toBeVisible()
      await expect(page.locator('body')).not.toContainText('Phase 8 Tenant2')
      // The client supplies NO tenant/student/role identity header (identity is from the JWT only).
      expect(identityHeaders, `client identity headers leaked: ${identityHeaders.join(', ')}`).toEqual([])
      // Access token is held in memory, never in localStorage (storage is not authoritative).
      const lsToken = await page.evaluate(() => JSON.stringify(window.localStorage))
      expect(lsToken).not.toMatch(/eyJ[A-Za-z0-9_-]+\./) // no JWT in localStorage
    } finally {
      await context.close()
    }
  })

  test('A8 logout clears the session and re-protects student routes', async ({ browser }) => {
    const context = await browser.newContext()
    const page = await context.newPage()
    try {
      await login(page, CODES.studentT1)
      await nav(page, '/app/student')
      await expect(page.getByRole('heading', { name: /student dashboard/i })).toBeVisible()
      await page.getByRole('button', { name: /sign out/i }).click()
      await expect(page).toHaveURL(/\/login|\/$/, { timeout: 15000 })
      // A previously protected route is no longer reachable after logout.
      await page.goto('/app/student')
      await expect(page).toHaveURL(/\/login/)
      await expect(page.getByRole('heading', { name: /student dashboard/i })).toHaveCount(0)
    } finally {
      await context.close()
    }
  })
})
