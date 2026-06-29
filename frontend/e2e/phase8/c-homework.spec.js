import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, backendUp, login, nav, resetE2E, attachGuards } from '../support/ph8.js'

// Group C — Homework lifecycle (C21–C25).
test.describe('Phase 8 C — homework', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // clean submission state for the deterministic submit flow
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('C21 only assigned homework is listed (self-scoped)', async () => {
    await nav(page, '/app/student/homework')
    await expect(page.getByRole('heading', { name: /^homework$/i })).toBeVisible()
    await expect(page.getByText('Phase 8 Open Homework').first()).toBeVisible()
    // Unassigned and cross-tenant homework never appear in the student's list.
    await expect(page.getByText(/unassigned/i)).toHaveCount(0)
    const rows = await page.locator('.student-row-link').count()
    expect(rows).toBe(1)
  })

  test('C22 homework details show backend instructions and status', async () => {
    await nav(page, `/app/student/homework/${FIX.hwOpen}`)
    await expect(page.getByRole('heading', { name: /phase 8 open homework/i })).toBeVisible()
    await expect(page.getByText(/submission history/i)).toBeVisible()
  })

  test('C23 submitting an answer is confirmed by the server and shown', async () => {
    await page.goto(`/app/student/homework/${FIX.hwOpen}`) // fresh, clean mutation state
    const answer = 'Phase 8 homework answer: a linear equation graphs as a straight line.'
    await page.getByLabel(/answer text/i).fill(answer)
    const respP = page.waitForResponse((r) => r.url().includes(`/homework/${FIX.hwOpen}/submit`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /submit homework/i }).click()
    const resp = await respP
    expect(resp.status()).toBe(201)
    // The persisted submission now renders (the empty state is gone).
    await expect(page.getByText(answer).first()).toBeVisible()
    await expect(page.getByText(/no submission yet/i)).toHaveCount(0)
  })

  test('C24 the submission persists across reload', async () => {
    await page.goto(`/app/student/homework/${FIX.hwOpen}`)
    await expect(page.getByText(/linear equation graphs as a straight line/i).first()).toBeVisible()
    await expect(page.getByText(/no submission yet/i)).toHaveCount(0)
  })

  test('C25 invalid submissions are rejected accurately and leave the prior submission intact', async () => {
    const trySubmit = async (id, expectedStatus) => {
      await page.goto(`/app/student/homework/${id}`)
      await page.getByLabel(/answer text/i).fill('Attempted invalid submission.')
      const respP = page.waitForResponse((r) => r.url().includes(`/homework/${id}/submit`) && r.request().method() === 'POST')
      await page.getByRole('button', { name: /submit homework/i }).click()
      const resp = await respP
      expect(resp.status(), `submit ${id} expected ${expectedStatus}`).toBe(expectedStatus)
      // No fabricated success: an error state is shown.
      await expect(page.locator('[role="alert"]').first()).toBeVisible()
    }
    await trySubmit(FIX.hwOpen, 409)        // duplicate (already submitted in C23)
    await trySubmit(FIX.hwUnassigned, 403)  // not targeted to this student
    await trySubmit(FIX.hwT2, 404)          // cross-tenant id leaks nothing

    // The original submission is still intact and unchanged.
    await page.goto(`/app/student/homework/${FIX.hwOpen}`)
    await expect(page.getByText(/linear equation graphs as a straight line/i).first()).toBeVisible()
  })
})
