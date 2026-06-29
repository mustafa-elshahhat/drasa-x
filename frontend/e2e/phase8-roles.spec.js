import { test, expect, request as pwRequest } from '@playwright/test'

// Phase 8 §12.A (3-7) — cross-role protection of the Student Portal, proven live
// against the real backend + route guards. Each non-student role logs in with a
// deterministic seed account and is denied the student routes (the guard
// redirects role mismatches to /forbidden). All seed accounts share the local
// dev password.

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const PASSWORD = process.env.E2E_PASSWORD
const RUN = Boolean(process.env.E2E_LOGIN_CODE && PASSWORD)

const ROLES = [
  { code: 'TEACH-T1', label: 'Teacher' },
  { code: 'PARENT-T1', label: 'Parent' },
  { code: 'ADMIN-T1', label: 'SchoolAdmin' },
  { code: 'SYS-1', label: 'SystemAdmin' },
]

async function backendUp() {
  try {
    const ctx = await pwRequest.newContext()
    const res = await ctx.get(`${BACKEND}/health/live`, { timeout: 3000 })
    await ctx.dispose()
    return res.ok()
  } catch {
    return false
  }
}

async function loginAs(page, code) {
  await page.goto('/login')
  await page.getByLabel(/login code/i).fill(code)
  await page.getByLabel(/^password/i).fill(PASSWORD)
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page).not.toHaveURL(/\/login$/)
}

test.describe('Phase 8 cross-role rejection of student routes', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!RUN, 'Set E2E_LOGIN_CODE / E2E_PASSWORD to run')

  test.beforeAll(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
  })

  for (const r of ROLES) {
    test(`${r.label} (${r.code}) is denied the Student Portal`, async ({ browser }) => {
      const context = await browser.newContext()
      const page = await context.newPage()
      try {
        await loginAs(page, r.code)
        // Hard navigation forces a clean guard evaluation (no SPA race with the
        // role's own post-login redirect). A non-student must NEVER land on a
        // student route, and student content must never render.
        for (const route of ['/app/student', '/app/student/attendance', '/app/student/lessons/PH8-LESSON-T1']) {
          await page.goto(route)
          await expect(page).not.toHaveURL(/\/app\/student/)
          await expect(page.getByRole('heading', { name: /student dashboard/i })).toHaveCount(0)
        }
      } finally {
        await context.close()
      }
    })
  }
})
