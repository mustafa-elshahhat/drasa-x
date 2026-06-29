import { test, expect } from '@playwright/test'
import { HAS_CREDS, CODES, backendUp, login, nav, attachGuards, resetE2E } from '../support/ph8.js'

// Group G — Attendance (G52–G56).
test.describe('Phase 8 G — attendance', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    // Restore STU-T1's deterministic attendance baseline (3 seeded manual/import records).
    // Phase 15's CV-confirmation flow runs earlier in the file order and adds a ComputerVision
    // record for STU-T1; resetE2E removes it so the summary/percentage is order-independent.
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

  test('G52 attendance renders persisted records', async () => {
    await nav(page, '/app/student/attendance')
    await expect(page.getByRole('heading', { name: 'Attendance', exact: true })).toBeVisible()
    await expect(page.getByText('Attendance records')).toBeVisible()
    await expect(page.getByLabel(/attendance status: present/i).first()).toBeVisible()
  })

  test('G53 attendance summary totals and percentage match the server', async () => {
    await nav(page, '/app/student/attendance')
    await expect(page.getByText('66.67%')).toBeVisible()
  })

  test('G54 a student with no attendance sees the correct empty state', async ({ browser }) => {
    const ctx = await browser.newContext()
    const p = await ctx.newPage()
    try {
      await login(p, 'PH8-OTHER-T1') // enrolled student with zero attendance records
      await nav(p, '/app/student/attendance')
      await expect(p.getByRole('heading', { name: 'Attendance', exact: true })).toBeVisible()
      await expect(p.getByText(/no attendance records available/i)).toBeVisible()
    } finally {
      await ctx.close()
    }
  })

  test('G55 no computer-vision claim is made; source is shown truthfully', async () => {
    await nav(page, '/app/student/attendance')
    await expect(page.getByText(/manual\/imported attendance only/i)).toBeVisible()
    await expect(page.locator('body')).not.toContainText(/facial recognition produced/i)
    await expect(page.locator('body')).not.toContainText(/computer vision produced/i)
  })

  test('G56 attendance stays correct after reload', async () => {
    await page.goto('/app/student/attendance')
    await expect(page.getByText('66.67%')).toBeVisible()
    await expect(page.getByLabel(/attendance status: present/i).first()).toBeVisible()
  })
})
