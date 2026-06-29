// Phase 13 D — regression smoke for the Phase 8/9/10/11/12 portals. Each prior-
// phase actor logs in and its landing portal renders with no forbidden (AI/
// archived) request, 5xx, or page error — proving Phase 13 did not regress them.
import { test, expect } from '@playwright/test'
import { BACKEND, HAS_CREDS, CODES, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph13.js'

const PORTALS = [
  { phase: 8, code: CODES.studentT1, path: '/app/student', heading: /dashboard|my learning|student/i },
  { phase: 9, code: CODES.teacher, path: '/app/teacher', heading: /teacher|dashboard/i },
  { phase: 10, code: CODES.parent, path: '/app/parent', heading: /parent|dashboard/i },
  { phase: 11, code: CODES.schoolAdmin, path: '/app/school', heading: /school|dashboard|administration/i },
  { phase: 12, code: CODES.systemAdmin, path: '/app/system', heading: /platform|system|administration/i },
]

test.describe('Phase 13 D — prior-phase regression smoke', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run live E2E')

  test.beforeAll(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    await resetE2E()
  })

  for (const p of PORTALS) {
    test(`Phase ${p.phase} portal still loads for ${p.code}`, async ({ browser }) => {
      const ctx = await browser.newContext()
      const page = await ctx.newPage()
      const guards = attachGuards(page)
      try {
        await login(page, p.code)
        await nav(page, p.path)
        await expect(page.getByRole('heading').first()).toBeVisible({ timeout: 15000 })
        guards.assertNoForbidden()
      } finally {
        await ctx.close()
      }
    })
  }
})
