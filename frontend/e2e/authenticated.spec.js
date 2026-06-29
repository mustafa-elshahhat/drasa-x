import { test, expect, request as pwRequest } from '@playwright/test'

// Backend-DEPENDENT E2E (Phase 7 §22). These require the local DerasaX-backend
// and a seeded login. They detect the backend and seed credentials from env and
// skip cleanly (with a reason) when either is missing, so the suite never
// reports a false pass. To run them, start the local stack and set:
//   E2E_LOGIN_CODE / E2E_PASSWORD  (a seeded user)
const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const LOGIN_CODE = process.env.E2E_LOGIN_CODE
const PASSWORD = process.env.E2E_PASSWORD

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

test.describe('authenticated flows (requires local backend + seed user)', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!LOGIN_CODE || !PASSWORD, 'Set E2E_LOGIN_CODE / E2E_PASSWORD to run')

  test.beforeEach(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
  })

  test('valid user can log in and sees their role shell', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel(/login code/i).fill(LOGIN_CODE)
    await page.getByLabel(/^password/i).fill(PASSWORD)
    await page.getByRole('button', { name: /sign in/i }).click()
    await expect(page).toHaveURL(/\/app/)
    await expect(page.getByRole('navigation', { name: /main navigation/i })).toBeVisible()
  })

  test('logout returns to login and protects the app again', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel(/login code/i).fill(LOGIN_CODE)
    await page.getByLabel(/^password/i).fill(PASSWORD)
    await page.getByRole('button', { name: /sign in/i }).click()
    await expect(page).toHaveURL(/\/app/)
    await page.getByRole('button', { name: /sign out/i }).click()
    await expect(page).toHaveURL(/\/login$/)
    await page.goto('/app/profile')
    await expect(page).toHaveURL(/\/login$/)
  })
})
