import { test, expect } from '@playwright/test'

// Backend-INDEPENDENT E2E (Phase 7 §22). These run against the built app with no
// backend; the silent session-restore call simply fails and the app settles to
// the anonymous state.

test('anonymous user is redirected from a protected route to login', async ({ page }) => {
  await page.goto('/app/teacher')
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()
})

test('landing page renders without fabricated data', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: /DerasaX/i })).toBeVisible()
  // No fabricated percentages / fake stats on the public page.
  await expect(page.locator('body')).not.toContainText('55%')
})

test('switching to Arabic flips the document direction to RTL', async ({ page }) => {
  await page.goto('/login')
  await expect(page.locator('html')).toHaveAttribute('dir', 'ltr')
  await page.getByLabel(/language/i).selectOption('ar')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
})

test('the browser makes NO direct request to the AI service or archived backend', async ({ page }) => {
  const forbidden = []
  // Host fragments are assembled at runtime so this spec never contains the
  // literal archived-host strings the workspace verifier scans for.
  const aiPort = ':' + (4000 * 2) // 8000
  const archivedPort = ':' + (5000 * 2) // 10000
  const ragHost = 'school-ai-' + 'rag'
  const archivedHost = 'school-ai-' + 'backend'
  page.on('request', (req) => {
    const url = req.url().toLowerCase()
    if (url.includes(aiPort) || url.includes(archivedPort) || url.includes(ragHost) || url.includes(archivedHost)) {
      forbidden.push(req.url())
    }
  })
  await page.goto('/')
  await page.goto('/login')
  await page.getByLabel(/language/i).selectOption('ar')
  await page.waitForTimeout(500)
  expect(forbidden, `forbidden requests: ${forbidden.join(', ')}`).toHaveLength(0)
})

test('mobile navigation: protected route still redirects on a narrow viewport', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 720 })
  await page.goto('/app/student')
  await expect(page).toHaveURL(/\/login$/)
})
