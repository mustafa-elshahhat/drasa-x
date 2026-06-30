// Phase 13 A — notification centre: real inbox, real unread count, mark-read.
import { test, expect } from '@playwright/test'
import { BACKEND, PASSWORD, HAS_CREDS, CODES13, backendUp, resetE2E, login, nav, attachGuards, apiLoginFull, unreadCount } from '../support/ph13.js'

test.describe('Phase 13 A — notification centre', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run live E2E')

  let context, page, guards

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    await resetE2E()
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES13.studentA)
  })
  test.afterAll(async () => { await context?.close() })

  test('A1 the inbox lists the student\'s own seeded notifications', async () => {
    await nav(page, '/app/notifications')
    await expect(page.getByRole('heading', { name: /^notifications$/i })).toBeVisible()
    await expect(page.getByText(/new message from your teacher|school announcement posted/i).first()).toBeVisible({ timeout: 15000 })
    guards.assertNoForbidden()
  })

  test('A2 the unread count shown matches the server', async () => {
    const { token } = await apiLoginFull(CODES13.studentA)
    const server = await unreadCount(token)
    await nav(page, '/app/notifications')
    await expect(page.getByText(new RegExp(`Unread: ${server}`))).toBeVisible({ timeout: 15000 })
  })

  test('A3 marking one notification read decrements the count', async () => {
    const { token } = await apiLoginFull(CODES13.studentA)
    const before = await unreadCount(token)
    await nav(page, '/app/notifications')
    await page.getByRole('button', { name: /^mark read$/i }).first().click()
    await expect.poll(async () => unreadCount(token), { timeout: 10000 }).toBe(before - 1)
    guards.assertNoForbidden()
  })
})
