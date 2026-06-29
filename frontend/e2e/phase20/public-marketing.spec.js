import { test, expect } from '@playwright/test'

// Phase 20 — unified frontend. The single app now serves the public marketing
// pages, the auth page, and the protected portal. These checks are
// backend-INDEPENDENT (run against the built preview server): the public pages
// need no auth, navigation into the real /login works, the /app guard is intact,
// and RTL works on a public page.

test('public homepage renders the DerasaX marketing hero without auth', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('heading', { name: /DerasaX/i })).toBeVisible()
  await expect(page.getByAltText('Our Events')).toBeVisible()
  await expect(page.getByAltText('Our Activities')).toBeVisible()
  await expect(page.getByAltText('Our News')).toBeVisible()
  // No fabricated statistics on the public page.
  await expect(page.locator('body')).not.toContainText('55%')
})

test('marketing "Sign in" navigates to the real /login', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('link', { name: /sign in/i }).click()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()
})

test('the request-demo page renders the marketing form without auth', async ({ page }) => {
  await page.goto('/request-demo')
  await expect(page.getByRole('heading', { name: /request the demo/i })).toBeVisible()
  await expect(page.getByRole('button', { name: /request the demo/i })).toBeVisible()
})

test('unauthenticated /app still redirects to /login (role guard intact)', async ({ page }) => {
  await page.goto('/app/teacher')
  await expect(page).toHaveURL(/\/login$/)
})

test('switching a public page to Arabic flips the document direction to RTL', async ({ page }) => {
  await page.goto('/')
  await expect(page.locator('html')).toHaveAttribute('dir', 'ltr')
  await page.getByLabel(/language/i).selectOption('ar')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
})
