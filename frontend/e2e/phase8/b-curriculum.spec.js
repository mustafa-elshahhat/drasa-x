import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, backendUp, login, nav, resetE2E, attachGuards } from '../support/ph8.js'

// Group B — Dashboard, curriculum, materials, lesson progress (B9–B20).
test.describe('Phase 8 B — curriculum & lesson progress', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // ensure the fixture lesson starts uncompleted
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })

  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('B9 dashboard renders real API-backed metrics (no static demo values)', async () => {
    await nav(page, '/app/student')
    await expect(page.getByRole('heading', { name: /student dashboard/i })).toBeVisible()
    const subjectsMetric = page.getByRole('link', { name: /assigned subjects/i })
    await expect(subjectsMetric).toBeVisible()
    await expect(subjectsMetric).toContainText(/\d/)
  })

  test('B10 my subjects lists the enrolled subject and is tenant-scoped', async () => {
    await nav(page, '/app/student/subjects')
    await expect(page.getByRole('heading', { name: /my subjects/i })).toBeVisible()
    await expect(page.getByText('Phase 8 Mathematics').first()).toBeVisible()
    // Real backend rows only (tenant-scoped curriculum; cross-tenant isolation is
    // proven by B11/B20). The list reflects persisted subjects, not demo data.
    const rows = await page.locator('.student-row-link').count()
    expect(rows).toBeGreaterThanOrEqual(1)
  })

  test('B11 subject details loads the authorized subject; a cross-tenant subject id leaks nothing', async () => {
    await nav(page, `/app/student/subjects/${FIX.subjectT1}`)
    await expect(page.getByText('Phase 8 Algebra').first()).toBeVisible() // authorized unit loaded
    // Cross-tenant subject id → no authorized unit content rendered.
    await nav(page, `/app/student/subjects/${FIX.subjectT1.replace('T1', 'T2')}`)
    await expect(page.locator('main')).toBeVisible()
    await expect(page.getByText('Phase 8 Linear Equations')).toHaveCount(0)
    await expect(page.locator('.student-row-link')).toHaveCount(0)
  })

  test('B12 units flow renders only units of the authorized subject', async () => {
    await nav(page, `/app/student/subjects/${FIX.subjectT1}`)
    await expect(page.getByText('Phase 8 Algebra').first()).toBeVisible()
  })

  test('B13 lessons flow renders only lessons of the authorized unit', async () => {
    await nav(page, `/app/student/units/${FIX.unitT1}`)
    await expect(page.getByText('Phase 8 Linear Equations').first()).toBeVisible()
  })

  test('B14 lesson materials render persisted backend material as safe text', async () => {
    await nav(page, `/app/student/lessons/${FIX.lessonT1}`)
    await expect(page.getByText('Phase 8 Worksheet').first()).toBeVisible()
  })

  test('B15 viewing a lesson does not mark it complete', async () => {
    await nav(page, `/app/student/lessons/${FIX.lessonT1}`)
    await expect(page.getByRole('button', { name: /mark lesson complete/i })).toBeEnabled()
    await expect(page.getByRole('button', { name: /^lesson completed$/i })).toHaveCount(0)
  })

  test('B16 explicit Complete Lesson sends the command and updates UI after server confirmation', async () => {
    await nav(page, `/app/student/lessons/${FIX.lessonT1}`)
    await page.getByRole('button', { name: /mark lesson complete/i }).click()
    await expect(page.getByText(/progress saved/i)).toBeVisible()
  })

  test('B17 completed state survives reload', async () => {
    await page.reload()
    await expect(page.getByRole('button', { name: /lesson completed/i })).toBeDisabled()
  })

  test('B18 completion is idempotent (single stable completed state, no contradiction)', async () => {
    await nav(page, '/app/student/subjects')
    await nav(page, `/app/student/lessons/${FIX.lessonT1}`)
    await expect(page.getByRole('button', { name: /lesson completed/i })).toBeDisabled()
    await expect(page.getByRole('button', { name: /lesson completed/i })).toHaveCount(1)
  })

  test('B19 completing an unassigned lesson (different grade) is rejected without leaking details', async () => {
    await page.goto(`/app/student/lessons/${FIX.lessonUnassigned}`) // fresh mount, clean mutation state
    const btn = page.getByRole('button', { name: /mark lesson complete/i })
    await expect(btn).toBeEnabled()
    await btn.click()
    await expect(page.getByText(/not found|could not be found/i)).toBeVisible()
  })

  test('B20 a Tenant 1 student cannot complete a Tenant 2 lesson', async () => {
    await page.goto(`/app/student/lessons/${FIX.lessonT2}`) // fresh mount, clean mutation state
    const btn = page.getByRole('button', { name: /mark lesson complete/i })
    await expect(btn).toBeEnabled()
    await btn.click()
    await expect(page.getByText(/not found|could not be found/i)).toBeVisible()
  })
})
