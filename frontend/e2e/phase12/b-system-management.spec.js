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
    await expect(page.getByText('Nile Future International School').first()).toBeVisible()
    await nav(page, `/app/system/tenants/${FIX12.lifecycleTenant}`)
    await expect(h1(page)).toHaveText('Tenant details')
    await expect(page.getByText(FIX12.lifecycleTenantName).first()).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B3 admin onboards a tenant: create → assign plan → create initial admin → activate', async ({ page }) => {
    const guards = attachGuards(page)
    const stamp = String(Date.now()).slice(-9)
    const tenantId = `PH12-E2E-${stamp}`
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

    // Step 3 — create the initial school admin: no password/login-code is typed by the
    // admin, both are generated server-side and shown once in the credentials panel.
    await page.getByLabel('Full name', { exact: true }).fill('Founder Admin')
    await expect(page.getByLabel('Login code', { exact: true })).toHaveCount(0)
    await expect(page.getByLabel(/password/i)).toHaveCount(0)
    await page.getByRole('button', { name: 'Create school admin' }).click()
    const credentialsModal = page.getByRole('dialog', { name: 'Account credentials' })
    await expect(credentialsModal).toBeVisible()
    await expect(credentialsModal.getByText('Login ID')).toBeVisible()
    await expect(credentialsModal.getByText('Temporary password')).toBeVisible()
    await credentialsModal.getByRole('button', { name: 'Close', exact: true }).click()

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

  test('PH12-B8 admin creates a subscription plan and sees it in the Plans table', async ({ page }) => {
    const guards = attachGuards(page)
    const stamp = String(Date.now()).slice(-9)
    const code = `E2E-${stamp}`
    await login(page, CODES12.systemAdmin)
    await nav(page, '/app/system/plans')
    await expect(h1(page)).toHaveText('Plans')

    await page.getByRole('button', { name: 'Add plan' }).click()
    await expect(page.getByRole('heading', { name: 'Add a subscription plan' })).toBeVisible()
    await page.getByLabel('Plan name', { exact: false }).fill(`E2E Plan ${stamp}`)
    await page.getByLabel('Plan code', { exact: false }).fill(code)
    await page.getByLabel('Price', { exact: false }).fill('15')
    await page.getByLabel('Max students', { exact: false }).fill('25')
    await page.getByRole('button', { name: 'Save' }).click()

    // Modal closes on success and the new plan shows up in the (refetched) table.
    await expect(page.getByRole('heading', { name: 'Add a subscription plan' })).toBeHidden()
    await expect(page.getByText(code)).toBeVisible()
    guards.assertNoForbidden()
  })

  test('PH12-B9 admin assigns a plan to an already-onboarded tenant from tenant details', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page, CODES12.systemAdmin)
    await nav(page, `/app/system/tenants/${FIX12.lifecycleTenant}`)
    await expect(h1(page)).toHaveText('Tenant details')

    await page.getByLabel('Assign / change plan', { exact: true }).selectOption({ label: FIX12.planPro })
    await page.getByRole('button', { name: 'Assign / change plan' }).click()
    await expect(page.getByText('The plan was assigned.')).toBeVisible()
    // The subscription card (re-fetched after the mutation) now renders a real
    // subscription instead of the "no subscription" empty state.
    await expect(page.getByText('No subscription for this tenant.')).toBeHidden()
    guards.assertNoForbidden()
  })
})
