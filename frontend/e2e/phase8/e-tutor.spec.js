import { test, expect } from '@playwright/test'
import {
  HAS_CREDS, CODES, backendUp, login, attachGuards, captureRequest, ensureTutorDoc,
} from '../support/ph8.js'

// Group E — AI Tutor (E39–E47). The browser calls the backend (/api/v1/ai/tutor); the
// backend mediates school-ai-rag. A deterministic curriculum document is ingested
// through the real teacher → backend → RAG flow so a known question is grounded.
test.describe('Phase 8 E — AI tutor (backend-mediated, grounded)', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await ensureTutorDoc() // idempotent teacher-mediated curriculum ingest
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  async function ask(question, contextValue) {
    await page.goto('/app/student/ai-tutor') // fresh form per query
    await expect(page.getByRole('heading', { name: /ai tutor/i })).toBeVisible()
    if (contextValue) await page.getByLabel(/subject or lesson context/i).fill(contextValue)
    await page.getByLabel(/^question/i).fill(question)
    await page.getByRole('button', { name: /send question/i }).click()
  }

  test('E39 a grounded question renders an answer from the backend-mediated tutor', async () => {
    await ask('What is a linear equation and how do I solve one?')
    await expect(page.getByRole('heading', { name: /grounded answer/i })).toBeVisible({ timeout: 30000 })
    await expect(page.getByText(/linear equation/i).first()).toBeVisible()
  })

  test('E40 the question is sent same-origin to the backend; no browser call to the AI service', async () => {
    const calls = []
    const listener = (req) => { if (req.method() === 'POST' && /\/api\/v1\/ai\/tutor/.test(req.url())) calls.push(req.url()) }
    page.on('request', listener)
    await ask('Explain how to solve 2x + 3 = 7 step by step.')
    await expect(page.getByRole('heading', { name: /grounded answer/i })).toBeVisible({ timeout: 30000 })
    page.off('request', listener)
    expect(calls.length).toBeGreaterThanOrEqual(1)
    for (const u of calls) expect(u).toContain(':5155') // backend origin, never :8000
    expect(guards.forbidden, `forbidden AI/archived calls: ${guards.forbidden.join(', ')}`).toEqual([])
  })

  test('E41 the outbound request carries only the question content (no client identity/usage)', async () => {
    await page.goto('/app/student/ai-tutor')
    await page.getByLabel(/^question/i).fill('Define a linear equation.')
    const reqP = captureRequest(page, '/api/v1/ai/tutor', 'POST')
    await page.getByRole('button', { name: /send question/i }).click()
    const req = await reqP
    const body = req.body || {}
    expect(Object.keys(body).sort()).toEqual(['language', 'message'])
    for (const forbidden of ['tenantId', 'studentId', 'userId', 'role', 'usage', 'studentName', 'student_name', 'grade']) {
      expect(body, `request leaked ${forbidden}`).not.toHaveProperty(forbidden)
    }
  })

  test('E42 the answer and citations render as safe escaped text (no HTML injection)', async () => {
    await ask('What is a linear equation and how do I solve one?')
    const card = page.locator('.ui-card', { hasText: /grounded answer/i })
    await expect(card).toBeVisible({ timeout: 30000 })
    // The model answer contains markdown (e.g. "**"); React escapes it, so it is
    // shown as literal text rather than interpreted as markup. There must be no
    // injected <script> anywhere on the page.
    await expect(page.locator('script:not([src])')).toHaveCount(0)
    await expect(card.getByText(/linear equation/i).first()).toBeVisible()
  })

  test('E43 the grounded answer is the backend-mediated grounded response (with a real answer body)', async () => {
    const respP = page.waitForResponse((r) => r.url().includes('/api/v1/ai/tutor') && r.request().method() === 'POST')
    await ask('What is the standard form of a linear equation?')
    const resp = await respP
    const json = await resp.json().catch(() => ({}))
    const data = json?.data ?? json
    expect(data.grounded, 'backend reported grounded=true').toBe(true)
    expect((data.answer || '').length).toBeGreaterThan(0)
    expect(data.citationCount).toBeGreaterThanOrEqual(1)
    await expect(page.getByRole('heading', { name: /grounded answer/i })).toBeVisible({ timeout: 30000 })
  })

  test('E44 citations are rendered from the grounded response', async () => {
    await ask('How do I isolate the variable in a linear equation?')
    await expect(page.getByRole('heading', { name: /grounded answer/i })).toBeVisible({ timeout: 30000 })
    await expect(page.getByRole('heading', { name: /^citations$/i })).toBeVisible()
    // A real citation (the ingested reference) is shown — not the empty state.
    await expect(page.getByText(/no citations returned/i)).toHaveCount(0)
    await expect(page.getByText(/E2E-PH8-DOC-T1|Algebra Reference/i).first()).toBeVisible()
  })

  test('E45 the correlation id is surfaced for traceability', async () => {
    await ask('Give the definition of a linear equation.')
    await expect(page.getByRole('heading', { name: /grounded answer/i })).toBeVisible({ timeout: 30000 })
    await expect(page.getByText(/reference id/i)).toBeVisible()
    await expect(page.locator('code').filter({ hasText: /^[a-f0-9]{16,}$/ }).first()).toBeVisible()
  })

  test('E46 an off-syllabus question returns the documented no-answer state (no fabrication)', async () => {
    const respP = page.waitForResponse((r) => r.url().includes('/api/v1/ai/tutor') && r.request().method() === 'POST')
    await ask('What year did the French Revolution begin?', 'history')
    const resp = await respP
    const json = await resp.json().catch(() => ({}))
    const data = json?.data ?? json
    expect(data.grounded, 'no grounding for off-syllabus subject').toBe(false)
    expect(data.citationCount).toBe(0)
    await expect(page.getByRole('heading', { name: /no-answer response/i })).toBeVisible({ timeout: 30000 })
  })

  test('E47 an over-long question is rejected by the backend and shows a usable error (no fabricated answer)', async () => {
    await page.goto('/app/student/ai-tutor')
    // Set a >4000-char message programmatically (the backend hard limit is 4000);
    // this exercises the real validation/error path, not a mocked failure.
    const huge = 'a '.repeat(2100) // ~4200 chars
    await page.locator('textarea').evaluate((el, v) => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLTextAreaElement.prototype, 'value').set
      setter.call(el, v)
      el.dispatchEvent(new Event('input', { bubbles: true }))
    }, huge)
    const respP = page.waitForResponse((r) => r.url().includes('/api/v1/ai/tutor') && r.request().method() === 'POST')
    await page.getByRole('button', { name: /send question/i }).click()
    const resp = await respP
    expect(resp.status()).toBe(400) // real backend rejection, not 5xx
    await expect(page.locator('.ui-alert, [role="alert"]').first()).toBeVisible()
    await expect(page.getByRole('heading', { name: /grounded answer/i })).toHaveCount(0)
  })
})
