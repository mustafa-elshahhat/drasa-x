// Testing gap T-17 (route/detail/RBAC audit, Phase 5 instruction): "E2E:
// student material opens/downloads real content." B14 (b-curriculum.spec.js)
// only proves a material's name renders on the LESSON page's material list —
// nothing exercises the dedicated `/app/student/materials/:id` detail page
// (P1-6 this pass: real title/type + a genuine backend-mediated download or
// external-link action, replacing the previous placeholder/blank detail).
import { test, expect } from '@playwright/test'
import { FIX, HAS_CREDS, backendUp, resetE2E, login, nav, attachGuards } from '../support/ph8.js'

test.describe('Phase 8 / T-17 — student material detail opens real content', () => {
  test.beforeEach(async () => {
    test.skip(!HAS_CREDS, 'E2E credentials not provided')
    test.skip(!(await backendUp()), 'DerasaX-backend not running')
    await resetE2E()
  })

  test('T-17 material detail shows real metadata and a working download/open action', async ({ page }) => {
    const guards = attachGuards(page)
    await login(page)
    await nav(page, `/app/student/materials/${FIX.materialT1}`)

    await expect(page.getByRole('heading', { name: 'Algebra Worksheet' })).toBeVisible({ timeout: 15000 })
    await expect(page.getByText('Material not found')).not.toBeVisible()

    const downloadBtn = page.getByRole('button', { name: 'Download' })
    const openLink = page.getByRole('link', { name: 'Open link' })

    if (await downloadBtn.isVisible().catch(() => false)) {
      const [download] = await Promise.all([
        page.waitForEvent('download', { timeout: 15000 }),
        downloadBtn.click(),
      ])
      expect(await download.suggestedFilename()).toBeTruthy()
    } else {
      await expect(openLink).toBeVisible()
      const href = await openLink.getAttribute('href')
      expect(href).toBeTruthy()
    }
    guards.assertNoForbidden()
  })
})
