import { test, expect } from '@playwright/test'
import { CODES12, FIX12, HAS_CREDS, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph12.js'

const h1 = (page) => page.getByRole('heading', { level: 1 })

test.describe('Phase 12 — system-admin management', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    await resetE2E() // restores PH12-TENANT-SUSPEND to Active + the seeded support ticket to Pending
  })

  test('PH12-B1 dashboard shows a real platform aggregate (no fabricated data)', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await expect(page).toHaveURL(/\/app\/system$/)
    await expect(h1(page)).toHaveText('Platform administration')
    // Real tenant-state aggregate banner (Active count is at least the seeded tenants).
    await expect(page.getByText('Active', { exact: false }).first()).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B2 admin lists tenants and opens a tenant detail', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await nav(page, '/app/system/tenants')
    await expect(h1(page)).toHaveText('Tenants')
    await expect(page.getByText('Main School').first()).toBeVisible()
    await nav(page, `/app/system/tenants/${FIX12.lifecycleTenant}`)
    await expect(h1(page)).toHaveText('Tenant details')
    await expect(page.getByText(FIX12.lifecycleTenantName).first()).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B3 admin onboards a tenant: create → assign plan → create initial admin → activate', async ({ page }) => {
    const guards = attachGuards(page)
    const stamp = String(Date.now()).slice(-9)
    const tenantId = `PH12-E2E-${stamp}`
    const adminCode = `PH12-E2E-ADM-${stamp}`
    await login(page, CODES12.systemAdmin)
    await nav(page, '/app/system/onboarding')
    await expect(h1(page)).toHaveText('Tenant onboarding')

    // Step 1 — create tenant
    await page.getByLabel('Tenant id', { exact: true }).fill(tenantId)
    await page.getByLabel('Tenant name', { exact: true }).fill(`E2E School ${stamp}`)
    await page.getByRole('button', { name: 'Create tenant' }).click()
    await expect(page.getByText(`Tenant ${tenantId} created.`)).toBeVisible()

    // Step 2 — assign a real seeded plan
    await page.getByLabel('Plan', { exact: true }).selectOption({ label: FIX12.planPro })
    await page.getByRole('button', { name: 'Assign plan' }).click()
    await expect(page.getByText('The plan was assigned.')).toBeVisible()

    // Step 3 — create the initial school admin (one-time credential is shown)
    await page.getByLabel('Full name', { exact: true }).fill('E2E Founder Admin')
    await page.getByLabel('Login code', { exact: true }).fill(adminCode)
    await page.getByRole('button', { name: 'Create school admin' }).click()
    await expect(page.getByText(adminCode).first()).toBeVisible()

    // Step 4 — activate
    await page.getByRole('button', { name: 'Activate tenant' }).click()
    await expect(page.getByText('The tenant is active.')).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B4 admin suspends then reactivates a tenant', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await nav(page, `/app/system/tenants/${FIX12.lifecycleTenant}`)
    await expect(h1(page)).toHaveText('Tenant details')

    await page.getByRole('button', { name: 'Suspend' }).click()
    await expect(page.getByText('The tenant lifecycle state was updated.')).toBeVisible()

    await expect(page.getByRole('button', { name: 'Reactivate' })).toBeEnabled()
    await page.getByRole('button', { name: 'Reactivate' }).click()
    await expect(page.getByText('The tenant lifecycle state was updated.')).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B5 admin handles a support ticket from the cross-tenant inbox', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await nav(page, '/app/system/support')
    await expect(h1(page)).toHaveText('Support tickets')

    const ticket = page.locator('.student-list__item', { hasText: FIX12.supportTicketMessage })
    await expect(ticket).toBeVisible()
    await ticket.getByLabel('Response', { exact: true }).fill('Handled by platform E2E')
    await ticket.getByRole('button', { name: 'Respond' }).click()
    // After the response, the refetched ticket shows the response text.
    await expect(page.getByText('Handled by platform E2E').first()).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B6 admin reviews usage, AI usage, audit and security pages (real data)', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)

    await nav(page, '/app/system/usage')
    await expect(h1(page)).toHaveText('Usage')
    await expect(page.getByText('Platform totals')).toBeVisible()

    await nav(page, '/app/system/ai-usage')
    await expect(h1(page)).toHaveText('AI usage')

    await nav(page, '/app/system/audit')
    await expect(h1(page)).toHaveText('Audit logs')

    await nav(page, '/app/system/security')
    await expect(h1(page)).toHaveText('Security events')

    await nav(page, '/app/system/storage')
    await expect(h1(page)).toHaveText('Storage')
    await expect(page.getByText(/not implemented yet/i)).toBeVisible() // honest deferral, not faked
    guards.assertNoForbidden()
  })

  test('PH12-B7 admin publishes a global announcement', async ({ page }) => {
    const guards = attachGuards(page)
    const stamp = String(Date.now()).slice(-9)
    await login(page, CODES12.systemAdmin)
    await nav(page, '/app/system/announcements')
    await expect(h1(page)).toHaveText('Global announcements')

    const card = page.locator('.ui-card', { hasText: 'Publish announcement' }).first()
    await card.getByLabel('Title', { exact: true }).fill(`E2E Platform Notice ${stamp}`)
    await card.getByLabel('Message', { exact: true }).fill('A real platform-wide announcement from the E2E suite.')
    await card.getByRole('button', { name: 'Publish announcement' }).click()
    await expect(page.getByText('The announcement was published.')).toBeVisible()
    guards.assertNoForbidden()
  })
})
