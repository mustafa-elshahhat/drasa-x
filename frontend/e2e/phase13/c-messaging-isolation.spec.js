// Phase 13 C — messaging round-trip + participant/cross-tenant isolation.
import { test, expect } from '@playwright/test'
import { BACKEND, PASSWORD, HAS_CREDS, CODES, CODES13, backendUp, resetE2E, login, nav, attachGuards, apiLoginFull, startConversation } from '../support/ph13.js'

test.describe('Phase 13 C — messaging + isolation', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run live E2E')

  let context, page, guards, convId

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    await resetE2E()
    // Teacher starts a real conversation with the linked student (relationship-gated).
    const { token: teacherToken } = await apiLoginFull(CODES13.teacher)
    const studentA = await apiLoginFull(CODES13.studentA)
    convId = await startConversation(teacherToken, {
      participantUserId: studentA.id,
      subject: 'PH13 E2E thread',
      firstMessage: 'Hello from your teacher.',
    })
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
  })
  test.afterAll(async () => { await context?.close() })

  test('C1 the recipient sees the thread and can reply through the UI', async () => {
    await login(page, CODES13.studentA)
    await nav(page, '/app/messages')
    await expect(page.getByText('PH13 E2E thread')).toBeVisible({ timeout: 15000 })
    await page.getByRole('link', { name: 'PH13 E2E thread' }).click()
    await expect(page.getByText('Hello from your teacher.')).toBeVisible({ timeout: 15000 })

    await page.getByRole('textbox').fill('Thanks, understood!')
    await page.getByRole('button', { name: /^send$/i }).click()
    await expect(page.getByText('Thanks, understood!')).toBeVisible({ timeout: 15000 })
    guards.assertNoForbidden()
  })

  test('C2 the teacher sees the recipient\'s reply', async () => {
    const tCtx = await page.context().browser().newContext()
    const tp = await tCtx.newPage()
    await login(tp, CODES13.teacher)
    await nav(tp, `/app/messages/${convId}`)
    await expect(tp.getByText('Thanks, understood!')).toBeVisible({ timeout: 15000 })
    await tCtx.close()
  })

  test('C3 a non-participant in the same tenant cannot read the thread', async () => {
    const bCtx = await page.context().browser().newContext()
    const bp = await bCtx.newPage()
    await login(bp, CODES13.studentB)
    await nav(bp, `/app/messages/${convId}`)
    await expect(bp.getByText(/don't have access|access to this conversation/i)).toBeVisible({ timeout: 15000 })
    await bCtx.close()
  })

  test('C4 a cross-tenant user cannot read the thread', async () => {
    const xCtx = await page.context().browser().newContext()
    const xp = await xCtx.newPage()
    await login(xp, CODES.studentT1) // tenant-1 student — different tenant from PH13
    await nav(xp, `/app/messages/${convId}`)
    await expect(xp.getByText(/don't have access|access to this conversation/i)).toBeVisible({ timeout: 15000 })
    await xCtx.close()
  })
})
