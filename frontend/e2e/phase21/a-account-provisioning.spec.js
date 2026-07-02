// Phase 21 — account provisioning + forced first-login password change (live).
// SystemAdmin onboards a tenant + its first SchoolAdmin without ever typing a
// password; the SchoolAdmin's temp-password login is forced into the
// change-password page before the dashboard is reachable; the SchoolAdmin then
// provisions a Teacher the same way, with the same forced-change gate.
import { test, expect } from '@playwright/test'
import { CODES, HAS_CREDS, backendUp, resetE2E, nav, attachGuards } from '../support/ph21.js'

const NEW_PASSWORD = 'Fresh#Pass9000'

async function loginWith(page, loginCode, password) {
  await page.goto('/login')
  await page.getByLabel(/login code/i).fill(loginCode)
  await page.getByLabel(/^password/i).fill(password)
  await page.getByRole('button', { name: /sign in/i }).click()
}

async function signOut(page) {
  await page.getByRole('button', { name: 'Account menu' }).click()
  await page.getByRole('menuitem', { name: /sign out/i }).click()
  await expect(page).toHaveURL(/\/login|\/$/, { timeout: 15000 })
}

async function readCredential(modal) {
  const monoValues = modal.locator('.credentials-panel__value--mono')
  return {
    loginCode: (await monoValues.nth(0).textContent()).trim(),
    temporaryPassword: (await monoValues.nth(1).textContent()).trim(),
  }
}

async function forcedChangePassword(page, tempPassword) {
  await expect(page).toHaveURL(/\/change-password$/, { timeout: 15000 })
  await expect(page.getByRole('heading', { name: 'Change your password to continue' })).toBeVisible()
  await page.getByLabel(/current password/i).fill(tempPassword)
  await page.getByLabel(/^new password/i).fill(NEW_PASSWORD)
  await page.getByLabel(/confirm new password/i).fill(NEW_PASSWORD)
  await page.getByRole('button', { name: /change password and continue/i }).click()
}

test.describe('Phase 21 — account provisioning + forced password change', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    await resetE2E()
  })

  test('PH21-A1 SystemAdmin onboards a tenant + SchoolAdmin; SchoolAdmin then provisions a Teacher — both forced to change their temporary password before reaching a dashboard', async ({ page }) => {
    const guards = attachGuards(page)
    const stamp = String(Date.now()).slice(-9)
    const tenantId = `PH21-E2E-${stamp}`

    // --- SystemAdmin: create tenant + first SchoolAdmin (no password typed) ---
    await loginWith(page, CODES.systemAdmin, process.env.E2E_PASSWORD)
    await expect(page).not.toHaveURL(/\/login$/, { timeout: 15000 })
    await nav(page, '/app/system/onboarding')
    await expect(page.getByRole('heading', { name: 'Tenant onboarding' })).toBeVisible()

    await page.getByLabel('Tenant id', { exact: true }).fill(tenantId)
    await page.getByLabel('Tenant name', { exact: true }).fill(`Phase21 School ${stamp}`)
    await page.getByRole('button', { name: 'Create tenant' }).click()
    await expect(page.getByText(`Tenant ${tenantId} created.`)).toBeVisible()

    await page.getByLabel('Plan', { exact: true }).selectOption({ index: 1 })
    await page.getByRole('button', { name: 'Assign plan' }).click()
    await expect(page.getByText('The plan was assigned.')).toBeVisible()

    await expect(page.getByLabel(/password/i)).toHaveCount(0) // no password field anywhere on this form
    await page.getByLabel('Full name', { exact: true }).fill('Phase Founder Admin')
    await page.getByRole('button', { name: 'Create school admin' }).click()
    const adminModal = page.getByRole('dialog', { name: 'Account credentials' })
    await expect(adminModal).toBeVisible()
    const adminCred = await readCredential(adminModal)
    expect(adminCred.loginCode).toBeTruthy()
    expect(adminCred.temporaryPassword).toBeTruthy()
    await adminModal.getByRole('button', { name: 'Close', exact: true }).click()

    await page.getByRole('button', { name: 'Activate tenant' }).click()
    await expect(page.getByText('The tenant is active.')).toBeVisible()

    // --- New SchoolAdmin: temp-password login is forced into change-password ---
    await signOut(page)
    await loginWith(page, adminCred.loginCode, adminCred.temporaryPassword)
    await forcedChangePassword(page, adminCred.temporaryPassword)
    await expect(page).toHaveURL(/\/app\/school$/, { timeout: 15000 })
    await expect(page.getByRole('heading', { name: 'School administration' })).toBeVisible()

    // A route the SchoolAdmin is otherwise authorized for is reachable now that the
    // password has been changed (proves the gate cleared, not just the UI redirect).
    await nav(page, '/app/school/users')
    await expect(page.getByRole('heading', { name: /users/i })).toBeVisible()

    // --- SchoolAdmin provisions a Teacher (English name, no password typed). A Teacher
    // (not a Student) is used here because this tenant was just onboarded and has no
    // Grade yet — Student creation requires one; Teacher exercises the identical
    // provisioning + forced-password-change code path without that extra setup. ---
    await expect(page.getByLabel(/password/i)).toHaveCount(0)
    await expect(page.getByLabel('Login code')).toHaveCount(0)
    await page.getByLabel('Name', { exact: true }).fill('Phase Twenty One Teacher')
    await page.getByLabel('Role', { exact: true }).selectOption({ label: 'Teacher' })
    await page.getByRole('button', { name: 'Create account' }).click()
    const teacherModal = page.getByRole('dialog', { name: 'Account credentials' })
    await expect(teacherModal).toBeVisible()
    const teacherCred = await readCredential(teacherModal)
    expect(teacherCred.loginCode).toBeTruthy()
    expect(teacherCred.temporaryPassword).toBeTruthy()
    await teacherModal.getByRole('button', { name: 'Close', exact: true }).click()

    // --- New Teacher: temp-password login is forced into change-password ---
    await signOut(page)
    await loginWith(page, teacherCred.loginCode, teacherCred.temporaryPassword)
    await forcedChangePassword(page, teacherCred.temporaryPassword)
    await expect(page).toHaveURL(/\/app\/teacher$/, { timeout: 15000 })
    await expect(page.getByRole('heading', { name: /teacher dashboard/i })).toBeVisible()

    guards.assertNoForbidden()
  })

  test('PH21-A2 a non-English full name is rejected on both the onboarding and school-user creation forms', async ({ page }) => {
    await loginWith(page, CODES.systemAdmin, process.env.E2E_PASSWORD)
    await expect(page).not.toHaveURL(/\/login$/, { timeout: 15000 })
    await nav(page, '/app/system/onboarding')
    await page.getByLabel('Full name', { exact: true }).fill('محمد أحمد')
    await expect(page.getByText('Full name must be written in English letters only.')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Create school admin' })).toBeDisabled()

    await signOut(page)
    await loginWith(page, CODES.schoolAdmin, process.env.E2E_PASSWORD)
    await expect(page).not.toHaveURL(/\/login$/, { timeout: 15000 })
    await nav(page, '/app/school/users')
    await page.getByLabel('Name', { exact: true }).fill('محمد أحمد')
    await expect(page.getByText('Full name must be written in English letters only.')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Create account' })).toBeDisabled()
  })
})
