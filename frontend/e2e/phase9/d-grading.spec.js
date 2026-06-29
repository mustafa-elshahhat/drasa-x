// Phase 9 — Group D: submission → grading → feedback, and the student seeing it.
// A real student attempt is created via the API, then the teacher grades it in the UI.
import { test, expect } from '@playwright/test'
import { CODES9, FIX9, PASSWORD, backendUp, resetE2E, login, nav, attachGuards, studentSubmitQuiz } from '../support/ph9.js'

test.describe.configure({ mode: 'serial' })

let context, page, guards, attemptId

test.beforeAll(async ({ browser }) => {
  test.skip(!PASSWORD, 'E2E_PASSWORD not set')
  test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  await resetE2E()
  attemptId = await studentSubmitQuiz(FIX9.publishedQuiz)
  context = await browser.newContext()
  page = await context.newPage()
  guards = attachGuards(page)
  await login(page, CODES9.teacher)
})

test.afterAll(async () => { await context?.close() })

test('PH9-D1 teacher sees the student submission for an assigned quiz', async () => {
  await nav(page, `/app/teacher/quizzes/${FIX9.publishedQuiz}`)
  await expect(page.getByRole('heading', { name: /submissions/i })).toBeVisible()
  // The submitted attempt appears in the submissions list.
  await expect(page.getByText(/Submitted|Graded/).first()).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-D2 teacher opens the submission and grades it', async () => {
  await nav(page, `/app/teacher/submissions/${attemptId}`)
  await expect(page.getByRole('heading', { name: /^submission$/i }).or(page.getByRole('heading', { name: /grading|submission/i })).first()).toBeVisible()
  await page.getByRole('button', { name: /save grade/i }).click()
  await expect(page.getByText(/grade saved/i)).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-D3 teacher adds feedback to the submission', async () => {
  await nav(page, `/app/teacher/submissions/${attemptId}`)
  await page.getByLabel(/feedback to the student/i).fill('Good reasoning on the linear equation steps.')
  await page.getByRole('button', { name: /send feedback/i }).click()
  await expect(page.getByText(/feedback sent/i)).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-D4 the student can see the graded result with the teacher feedback', async () => {
  const sContext = await page.context().browser().newContext()
  const sPage = await sContext.newPage()
  try {
    await login(sPage, CODES9.studentT1)
    await nav(sPage, `/app/student/quiz-attempts/${attemptId}/result`)
    await expect(sPage.getByText(/Good reasoning on the linear equation steps\.|feedback/i).first()).toBeVisible()
  } finally {
    await sContext.close()
  }
})
