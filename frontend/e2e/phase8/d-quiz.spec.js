import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, backendUp, login, nav, resetE2E, attachGuards, captureRequest } from '../support/ph8.js'

// Group D — Quiz lifecycle (D26–D38): list, details, attempt, save (option + text),
// restore, payload hygiene, submit lock, repeat-submit safety, result, history.
test.describe('Phase 8 D — quiz lifecycle', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  let attemptId

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // clean attempts for the deterministic lifecycle
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('D26 only assigned quizzes are listed (self-scoped)', async () => {
    await nav(page, '/app/student/quizzes')
    await expect(page.getByRole('heading', { name: /^quizzes$/i })).toBeVisible()
    await expect(page.getByText('Phase 8 Algebra Quiz').first()).toBeVisible()
    expect(await page.locator('.student-row-link').count()).toBe(1)
  })

  test('D27 quiz details expose no correct answers or score before submission', async () => {
    await nav(page, `/app/student/quizzes/${FIX.quizT1}`)
    await expect(page.getByRole('heading', { name: /phase 8 algebra quiz/i })).toBeVisible()
    const body = await page.locator('main').innerText()
    for (const leak of ['isCorrect', 'correctOption', 'correctAnswer', 'correctOptionId']) {
      expect(body, `details leaked ${leak}`).not.toContain(leak)
    }
  })

  test('D28 starting a quiz creates an attempt and navigates to the attempt route', async () => {
    await page.goto(`/app/student/quizzes/${FIX.quizT1}`)
    const respP = page.waitForResponse((r) => /\/quizzes\/.+\/attempts$/.test(r.url()) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /start attempt/i }).click()
    const resp = await respP
    expect(resp.status()).toBe(201)
    const json = await resp.json().catch(() => ({}))
    attemptId = (json?.data ?? json)?.id
    expect(attemptId, 'attempt id returned').toBeTruthy()
    await expect(page).toHaveURL(new RegExp(`/quiz-attempts/${attemptId}$`))
  })

  test('D29 starting again resumes the same attempt (no duplicate)', async () => {
    await page.goto(`/app/student/quizzes/${FIX.quizT1}`)
    const respP = page.waitForResponse((r) => /\/quizzes\/.+\/attempts$/.test(r.url()) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /start attempt/i }).click()
    const resp = await respP
    const json = await resp.json().catch(() => ({}))
    expect((json?.data ?? json)?.id).toBe(attemptId) // resumed, not duplicated
  })

  test('D30 the attempt renders the real questions and options', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await expect(page.getByText('2 + 2 = ?')).toBeVisible()
    await expect(page.getByText(/a linear equation graphs a straight line/i)).toBeVisible()
    await expect(page.getByText(/explain how to solve x \+ 1 = 3/i)).toBeVisible()
    expect(await page.locator('input[type="radio"]').count()).toBeGreaterThanOrEqual(5) // 3 MCQ + 2 T/F
  })

  test('D31 a selected option answer is saved (server-confirmed)', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await page.locator('input[type="radio"][value="E2E-PH8-Q1-B"]').check()
    await page.locator('input[type="radio"][value="E2E-PH8-Q2-T"]').check()
    const respP = page.waitForResponse((r) => r.url().includes(`/attempts/${attemptId}/answers`) && r.request().method() === 'PUT')
    await page.getByRole('button', { name: /save answers/i }).click()
    expect((await respP).status()).toBe(200)
  })

  test('D32 a free-text answer is saved as answer text (not as an option id)', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await page.locator('input[type="radio"][value="E2E-PH8-Q1-B"]').check()
    await page.locator('input[type="radio"][value="E2E-PH8-Q2-T"]').check()
    await page.getByLabel(/^answer/i).fill('Subtract 1 from both sides to get x = 2.')
    const reqP = captureRequest(page, `/attempts/${attemptId}/answers`, 'PUT')
    const respP = page.waitForResponse((r) => r.url().includes(`/attempts/${attemptId}/answers`) && r.request().method() === 'PUT')
    await page.getByRole('button', { name: /save answers/i }).click()
    expect((await respP).status()).toBe(200)
    const req = await reqP
    const essay = (req.body.answers || []).find((a) => a.questionId === 'E2E-PH8-Q3')
    expect(essay, 'essay answer present').toBeTruthy()
    expect(essay.answerText).toBe('Subtract 1 from both sides to get x = 2.')
    expect(essay).not.toHaveProperty('selectedOptionId')
  })

  test('D33 saved answers are restored on reload', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await expect(page.locator('input[type="radio"][value="E2E-PH8-Q1-B"]')).toBeChecked()
    await expect(page.locator('input[type="radio"][value="E2E-PH8-Q2-T"]')).toBeChecked()
    await expect(page.getByLabel(/^answer/i)).toHaveValue(/subtract 1 from both sides/i)
  })

  test('D34 the answers payload carries no score/correctness/rank/identity', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await page.locator('input[type="radio"][value="E2E-PH8-Q1-B"]').check()
    const reqP = captureRequest(page, `/attempts/${attemptId}/answers`, 'PUT')
    await page.getByRole('button', { name: /save answers/i }).click()
    const req = await reqP
    expect(Object.keys(req.body).sort()).toEqual(['answers'])
    for (const a of req.body.answers) {
      for (const k of Object.keys(a)) expect(['questionId', 'selectedOptionId', 'answerText']).toContain(k)
      for (const forbidden of ['score', 'isCorrect', 'rank', 'tenantId', 'studentId', 'pointsEarned']) {
        expect(a).not.toHaveProperty(forbidden)
      }
    }
  })

  test('D35 submitting the attempt locks editing and shows the result', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await page.getByRole('button', { name: /submit attempt/i }).click()
    await expect(page).toHaveURL(new RegExp(`/quiz-attempts/${attemptId}/result$`))
    // Editing is locked server-side after submission.
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    await page.locator('input[type="radio"][value="E2E-PH8-Q1-A"]').check()
    const respP = page.waitForResponse((r) => r.url().includes(`/attempts/${attemptId}/answers`) && r.request().method() === 'PUT')
    await page.getByRole('button', { name: /save answers/i }).click()
    expect((await respP).status()).toBe(409)
  })

  test('D36 re-submitting a submitted attempt is safely rejected (no duplicate grade)', async () => {
    await page.goto(`/app/student/quiz-attempts/${attemptId}`)
    const respP = page.waitForResponse((r) => r.url().includes(`/attempts/${attemptId}/submit`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /submit attempt/i }).click()
    expect((await respP).status()).toBe(409)
    await expect(page.locator('[role="alert"]').first()).toBeVisible()
  })

  test('D37 the result is the server-owned score and status', async () => {
    const respP = page.waitForResponse((r) => r.url().includes(`/attempts/${attemptId}/result`) && r.request().method() === 'GET')
    await page.goto(`/app/student/quiz-attempts/${attemptId}/result`)
    const json = await (await respP).json().catch(() => ({}))
    const data = json?.data ?? json
    // Auto-graded MCQ + true/false = 2 of 4; essay awaits manual grading.
    expect(data.totalScore).toBe(4)
    expect(data.achievedScore).toBe(2)
    await expect(page.getByRole('heading', { name: /attempt result/i })).toBeVisible()
  })

  test('D38 attempt history shows the persisted attempt; a foreign attempt id is denied', async () => {
    await page.goto(`/app/student/quizzes/${FIX.quizT1}`)
    await expect(page.getByText(/previous attempts/i)).toBeVisible()
    await expect(page.locator('.student-row-link').first()).toBeVisible()
    // A foreign / non-existent attempt id leaks nothing (404 → error state).
    const respP = page.waitForResponse((r) => r.url().includes('/attempts/00000000-0000-0000-0000-000000000999') && r.request().method() === 'GET')
    await page.goto('/app/student/quiz-attempts/00000000-0000-0000-0000-000000000999')
    expect((await respP).status()).toBe(404)
    await expect(page.locator('[role="alert"]').first()).toBeVisible()
  })
})
