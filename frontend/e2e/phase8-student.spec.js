import { test, expect, request as pwRequest } from '@playwright/test'

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const LOGIN_CODE = process.env.E2E_LOGIN_CODE
const PASSWORD = process.env.E2E_PASSWORD
const RESET_KEY = process.env.E2E_RESET_KEY || 'ph8-e2e-local'

async function backendUp() {
  try {
    const ctx = await pwRequest.newContext()
    const res = await ctx.get(`${BACKEND}/health/live`, { timeout: 3000 })
    await ctx.dispose()
    return res.ok()
  } catch {
    return false
  }
}

// Restore STU-T1's deterministic fixture baseline (Development-only backend endpoint). Phase 15's
// CV-confirmation flow runs earlier in the shared file order and adds a ComputerVision attendance
// record for STU-T1; resetting first keeps the attendance summary/percentage order-independent.
async function resetE2E() {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/dev/e2e/reset`, {
      headers: { 'X-E2E-Reset-Key': RESET_KEY },
      timeout: 8000,
    })
    if (!res.ok()) throw new Error(`reset failed: ${res.status()}`)
  } finally {
    await ctx.dispose()
  }
}

async function login(page) {
  await page.goto('/login')
  await page.getByLabel(/login code/i).fill(LOGIN_CODE)
  await page.getByLabel(/^password/i).fill(PASSWORD)
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page).toHaveURL(/\/app/)
}

async function navigateInApp(page, path) {
  await page.evaluate((target) => {
    window.history.pushState({}, '', target)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, path)
}

test.describe('Phase 8 student portal live contracts', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!LOGIN_CODE || !PASSWORD, 'Set E2E_LOGIN_CODE / E2E_PASSWORD to run')

  let context
  let studentPage

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    await resetE2E() // deterministic attendance baseline (clears Phase 15 CV-confirmed records)

    context = await browser.newContext()
    studentPage = await context.newPage()
    await login(studentPage)
  })

  test.afterAll(async () => {
    await context?.close()
  })

  test.beforeEach(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
  })

  test('student sees persisted attendance summary and records with no CV claim', async () => {
    const page = studentPage
    await navigateInApp(page, '/app/student/attendance')
    await expect(page.getByRole('heading', { name: 'Attendance', exact: true })).toBeVisible()
    await expect(page.getByText(/attendance summary/i)).toBeVisible()
    await expect(page.getByText('66.67%')).toBeVisible()
    await expect(page.getByLabel(/attendance status: present/i)).toBeVisible()
    await expect(page.locator('body')).not.toContainText(/facial recognition produced/i)
    await expect(page.locator('body')).not.toContainText(/computer vision produced/i)
  })

  test('student explicitly completes an assigned lesson and completion survives reload', async () => {
    const page = studentPage
    await navigateInApp(page, '/app/student/lessons/PH8-LESSON-T1')
    const button = page.getByRole('button', { name: /mark lesson complete|lesson completed/i })
    await expect(button).toBeVisible()
    if (await page.getByRole('button', { name: /mark lesson complete/i }).isVisible().catch(() => false)) {
      await page.getByRole('button', { name: /mark lesson complete/i }).click()
      await expect(page.getByText(/progress saved/i)).toBeVisible()
    }
    await page.reload()
    await expect(page.getByRole('button', { name: /lesson completed/i })).toBeDisabled()
  })

  test('viewing a lesson alone does not create progress for an uncompleted cross-tenant lesson', async () => {
    const page = studentPage
    await navigateInApp(page, '/app/student/lessons/PH8-LESSON-T2')
    await page.getByRole('button', { name: /mark lesson complete/i }).click()
    await expect(page.getByText(/not found|could not be found/i)).toBeVisible()
  })

  test('representative mobile/tablet/desktop student routes have no horizontal page overflow', async () => {
    const page = studentPage
    for (const size of [
      { width: 390, height: 844 },
      { width: 820, height: 1180 },
      { width: 1366, height: 900 },
    ]) {
      await page.setViewportSize(size)
      for (const route of ['/app/student', '/app/student/attendance', '/app/student/lessons/PH8-LESSON-T1', '/app/student/suggestions']) {
        await navigateInApp(page, route)
        await expect(page.locator('main')).toBeVisible()
        const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1)
        expect(overflow, `${route} overflows at ${size.width}`).toBe(false)
      }
    }
  })

  test('Arabic RTL attendance remains usable', async () => {
    const page = studentPage
    await navigateInApp(page, '/app/student/attendance')
    await page.getByLabel(/language/i).selectOption('ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.getByText(/ملخص الحضور/)).toBeVisible()
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1)
    expect(overflow).toBe(false)
  })
})
