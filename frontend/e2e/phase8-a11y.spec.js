import { test, expect, request as pwRequest } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

// Phase 8 §13 — representative automated accessibility (axe) + RTL + responsive
// verification on AUTHENTICATED student pages, against the live local stack.
// We fail on any 'serious' or 'critical' violation introduced by / exposed on
// the Phase 8 student routes, in both English (LTR) and Arabic (RTL).

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const LOGIN_CODE = process.env.E2E_LOGIN_CODE
const PASSWORD = process.env.E2E_PASSWORD

const ROUTES = [
  { path: '/app/student', name: 'dashboard' },
  { path: '/app/student/lessons/PH8-LESSON-T1', name: 'lesson' },
  { path: '/app/student/attendance', name: 'attendance' },
  { path: '/app/student/suggestions', name: 'suggestions-form' },
  { path: '/app/student/notifications', name: 'notifications' },
  { path: '/app/student/ai-tutor', name: 'ai-tutor' },
]

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

async function login(page) {
  await page.goto('/login')
  await page.getByLabel(/login code/i).fill(LOGIN_CODE)
  await page.getByLabel(/^password/i).fill(PASSWORD)
  await page.getByRole('button', { name: /sign in/i }).click()
  await expect(page).toHaveURL(/\/app/)
}

async function navigateInApp(page, target) {
  await page.evaluate((t) => {
    window.history.pushState({}, '', t)
    window.dispatchEvent(new PopStateEvent('popstate'))
  }, target)
}

async function seriousOrCritical(page) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  return results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')
}

test.describe('Phase 8 accessibility (axe) on representative authenticated routes', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!LOGIN_CODE || !PASSWORD, 'Set E2E_LOGIN_CODE / E2E_PASSWORD to run')

  let context
  let page

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    context = await browser.newContext()
    page = await context.newPage()
    await login(page)
  })

  test.afterAll(async () => {
    await context?.close()
  })

  test('English (LTR): landmarks/headings present and no serious/critical axe violations', async () => {
    await page.getByLabel(/language/i).selectOption('en')
    await navigateInApp(page, '/app/student')
    // Core landmark + heading structure (keyboard/screen-reader navigability).
    await expect(page.locator('main')).toBeVisible()
    await expect(page.getByRole('navigation', { name: /main navigation/i })).toBeVisible()
    await expect(page.getByRole('heading').first()).toBeVisible()
    for (const route of ROUTES) {
      await navigateInApp(page, route.path)
      await expect(page.locator('main')).toBeVisible()
      const violations = await seriousOrCritical(page)
      expect(violations, `${route.name}: ${violations.map((v) => v.id).join(', ')}`).toEqual([])
    }
  })

  test('Arabic (RTL): direction flips and no serious/critical axe violations', async () => {
    await navigateInApp(page, '/app/student/attendance')
    await page.getByLabel(/language/i).selectOption('ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    for (const route of ROUTES) {
      await navigateInApp(page, route.path)
      await expect(page.locator('main')).toBeVisible()
      const violations = await seriousOrCritical(page)
      expect(violations, `${route.name} (ar): ${violations.map((v) => v.id).join(', ')}`).toEqual([])
    }
  })
})
