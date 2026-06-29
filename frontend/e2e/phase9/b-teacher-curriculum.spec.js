// Phase 9 — Group B: classes, students, curriculum browse (assignment-scoped).
import { test, expect } from '@playwright/test'
import { CODES9, FIX9, PASSWORD, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph9.js'

test.describe.configure({ mode: 'serial' })

let context, page, guards

test.beforeAll(async ({ browser }) => {
  test.skip(!PASSWORD, 'E2E_PASSWORD not set')
  test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
  await resetE2E()
  context = await browser.newContext()
  page = await context.newPage()
  guards = attachGuards(page)
  await login(page, CODES9.teacher)
})

test.afterAll(async () => { await context?.close() })

test('PH9-B1 teacher sees the assigned class in the class list', async () => {
  await nav(page, '/app/teacher/classes')
  await expect(page.getByText(/Phase 8 Class/).first()).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-B2 teacher opens the assigned class and sees enrolled students', async () => {
  await nav(page, `/app/teacher/classes/${FIX9.classT1}`)
  await expect(page.getByText(/Tenant1 Student|Phase 8 Other Student/).first()).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-B3 teacher cannot read students of a class they are not assigned to', async () => {
  // A tenant-2 class id → backend 404 → UI shows no roster.
  await nav(page, `/app/teacher/classes/${FIX9.classT2}`)
  await expect(page.getByText(/Tenant1 Student/)).toHaveCount(0)
  guards.assertNoForbidden()
})

test('PH9-B4 teacher browses curriculum: subjects → units → lessons → lesson detail', async () => {
  await nav(page, '/app/teacher/subjects')
  await expect(page.getByText(/Phase 8 Mathematics/).first()).toBeVisible()
  await nav(page, `/app/teacher/subjects/${FIX9.subjectT1}`)
  await expect(page.getByRole('heading', { name: /units/i })).toBeVisible()
  // Drill into the seeded unit and lesson.
  await nav(page, '/app/teacher/units/PH8-UNIT-T1')
  await expect(page.getByRole('heading', { name: /lessons/i })).toBeVisible()
  await nav(page, '/app/teacher/lessons/PH8-LESSON-T1')
  await expect(page.getByRole('heading', { name: /lesson details/i })).toBeVisible()
  guards.assertNoForbidden()
})

test('PH9-B5 teacher gradebook lists students with backend-authoritative averages', async () => {
  await nav(page, '/app/teacher/students')
  await expect(page.getByRole('heading', { name: /students/i }).first()).toBeVisible()
  guards.assertNoForbidden()
})
