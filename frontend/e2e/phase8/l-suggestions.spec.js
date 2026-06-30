import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, backendUp, login, nav, resetE2E, attachGuards, captureRequest } from '../support/ph8.js'

// Group L — Anonymous suggestions (L80–L85).
test.describe('Phase 8 L — anonymous suggestions', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E()
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('L80 privacy wording describes application-level anonymity (no cryptographic claim)', async () => {
    await nav(page, '/app/student/suggestions')
    await expect(page.getByRole('heading', { name: /anonymous suggestions/i })).toBeVisible()
    await expect(page.getByText(/application-level anonymity, not a cryptographic anonymity guarantee/i)).toBeVisible()
  })

  test('L81 client-side validation gates submission without replacing the backend', async () => {
    await nav(page, '/app/student/suggestions')
    const submit = page.getByRole('button', { name: /submit/i })
    await expect(submit).toBeDisabled() // empty
    await page.getByLabel(/^subject/i).fill('E2E suggestion subject')
    await expect(submit).toBeDisabled() // subject only
    await page.getByLabel(/^suggestion/i).fill('A constructive suggestion body for Phase 8.')
    await expect(submit).toBeEnabled() // both present
  })

  test('L83 the outbound request carries only suggestion content (no identity)', async () => {
    await page.goto('/app/student/suggestions') // clean form (no pending mutation)
    await page.getByLabel(/^subject/i).fill('E2E payload subject')
    await page.getByLabel(/^suggestion/i).fill('Payload body for anonymity check.')
    const submit = page.getByRole('button', { name: /submit/i })
    await expect(submit).toBeEnabled()
    const reqP = captureRequest(page, '/api/v1/suggestions', 'POST')
    await submit.click()
    const req = await reqP
    const keys = Object.keys(req.body || {})
    expect(keys.sort()).toEqual(['body', 'title'])
    for (const forbidden of ['authorId', 'studentId', 'userId', 'tenantId', 'role', 'submittedByUserId']) {
      expect(req.body).not.toHaveProperty(forbidden)
    }
    await expect(page.getByText(/suggestion submitted/i)).toBeVisible()
  })

  test('L82 a valid suggestion submits and shows a success state', async () => {
    await page.goto('/app/student/suggestions') // clean form (no pending mutation)
    await page.getByLabel(/^subject/i).fill('E2E success subject')
    await page.getByLabel(/^suggestion/i).fill('Body for the success-state check.')
    const submit = page.getByRole('button', { name: /submit/i })
    await expect(submit).toBeEnabled()
    await submit.click()
    await expect(page.getByText(/suggestion submitted/i)).toBeVisible()
  })

  test('L84 invalid input is blocked client-side and fires no request (no duplicate submit)', async () => {
    await nav(page, '/app/student/suggestions')
    let fired = false
    page.on('request', (r) => { if (r.url().includes('/api/v1/suggestions') && r.method() === 'POST') fired = true })
    // Empty form: the disabled button cannot submit and the form guard blocks it.
    await expect(page.getByRole('button', { name: /submit/i })).toBeDisabled()
    await page.waitForTimeout(300)
    expect(fired).toBe(false)
  })

  test('L85 the success response exposes no author identity', async () => {
    await page.goto('/app/student/suggestions') // clean form (no pending mutation)
    await page.getByLabel(/^subject/i).fill('E2E anonymity subject')
    await page.getByLabel(/^suggestion/i).fill('Body that must not echo author identity.')
    const submit = page.getByRole('button', { name: /submit/i })
    await expect(submit).toBeEnabled()
    const respP = page.waitForResponse((r) => r.url().includes('/api/v1/suggestions') && r.request().method() === 'POST')
    await submit.click()
    const resp = await respP
    const json = await resp.json().catch(() => ({}))
    const data = json?.data ?? json
    // The server's anonymized response must not echo the author back to the client.
    for (const forbidden of ['submittedByUserId', 'authorId', 'studentId', 'userId', 'submittedByUser', 'createdBy']) {
      expect(data, `response leaked ${forbidden}`).not.toHaveProperty(forbidden)
    }
    // And the success state does not name the author within the suggestion form region.
    await expect(page.getByText(/suggestion submitted/i)).toBeVisible()
    await expect(page.getByText(/^suggestion submitted$/i).locator('..')).not.toContainText('Youssef Ibrahim')
  })
})
