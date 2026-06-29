// Phase 9 — Group C: AI quiz lifecycle (draft → review → edit → publish → assign)
// plus live backend-mediated AI draft generation. The draft is seeded; the reset
// restores it so this is repeatable.
import { test, expect } from '@playwright/test'
import { CODES9, FIX9, PASSWORD, backendUp, resetE2E, login, nav, attachGuards, ensurePh9CurriculumDoc } from '../support/ph9.js'

test.describe.configure({ mode: 'serial' })

let context, page, guards

test.beforeAll(async ({ browser }) => {
  test.skip(!PASSWORD, 'E2E_PASSWORD not set')
  test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  await resetE2E()
  // Ground the curriculum so live AI draft generation is in-scope (real RAG path).
  await ensurePh9CurriculumDoc()
  context = await browser.newContext()
  page = await context.newPage()
  guards = attachGuards(page)
  await login(page, CODES9.teacher)
})

test.afterAll(async () => { await context?.close() })

test('PH9-C1 the seeded AI quiz appears as a DRAFT only (not published)', async () => {
  await nav(page, `/app/teacher/quizzes/${FIX9.draftQuiz}`)
  await expect(page.getByText(/this is a draft/i)).toBeVisible()
  await expect(page.getByRole('button', { name: /publish quiz/i })).toBeVisible()
  // No assignment surface before publishing.
  await expect(page.getByRole('button', { name: /assign quiz/i })).toHaveCount(0)
  guards.assertNoForbidden()
})

test('PH9-C2 teacher edits a draft question and the change persists', async () => {
  await nav(page, `/app/teacher/quizzes/${FIX9.draftQuiz}`)
  await page.getByRole('button', { name: /edit question/i }).first().click()
  const editor = page.getByLabel(/question text/i)
  await editor.fill('What is 5 + 7? (reviewed)')
  await page.getByRole('button', { name: /^save$/i }).click()
  await expect(page.getByText(/What is 5 \+ 7\? \(reviewed\)/)).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-C3 teacher publishes the reviewed draft', async () => {
  await nav(page, `/app/teacher/quizzes/${FIX9.draftQuiz}`)
  await page.getByRole('button', { name: /publish quiz/i }).click()
  await expect(page.getByText(/quiz published/i)).toBeVisible()
  // Once published, the assignment surface appears and Publish disappears.
  await expect(page.getByRole('heading', { name: /assign to a class/i })).toBeVisible()
})

test('PH9-C4 teacher assigns the published quiz to an assigned class', async () => {
  await nav(page, `/app/teacher/quizzes/${FIX9.draftQuiz}`)
  await expect(page.getByRole('heading', { name: /assign to a class/i })).toBeVisible()
  await page.getByLabel(/target class/i).selectOption(FIX9.classT1)
  await page.getByRole('button', { name: /assign quiz/i }).click()
  await expect(page.getByText(/quiz assigned/i)).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-C5 teacher generates a new AI quiz draft through the backend (draft-only)', async () => {
  test.slow() // first AI generation can take a while
  await nav(page, '/app/teacher/quiz-generate')
  await expect(page.getByText(/draft-only generation/i)).toBeVisible()
  await page.getByLabel(/^subject/i).selectOption(FIX9.subjectT1)
  await page.getByLabel(/topic/i).fill('linear equations')
  await page.getByLabel(/number of questions/i).fill('1')
  await page.getByRole('button', { name: /generate draft/i }).click()
  // On success the app navigates to the new draft quiz detail (draft messaging).
  await expect(page.getByText(/this is a draft/i)).toBeVisible({ timeout: 90000 })
  // The browser must never have called the AI service directly.
  guards.assertNoForbidden()
})
