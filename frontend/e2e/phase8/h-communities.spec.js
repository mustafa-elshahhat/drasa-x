import { test, expect, request as pwRequest } from '@playwright/test'
import { HAS_CREDS, CODES, FIX, BACKEND, backendUp, login, nav, resetE2E, attachGuards, apiLogin } from '../support/ph8.js'

// Group H — Communities & comments (H57–H62).
test.describe('Phase 8 H — communities', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards
  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    await resetE2E() // clear STU-T1 membership/posts
    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })
  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('H57 eligible (tenant-scoped) communities are listed', async () => {
    await nav(page, '/app/student/communities')
    await expect(page.getByRole('heading', { name: /^communities$/i })).toBeVisible()
    await expect(page.getByText('Phase 8 Math Club').first()).toBeVisible()
    // Tenant scoping is the real contract here: the seeded same-tenant community is listed and
    // the cross-tenant community never leaks in. (An exact total count is not deterministic — a
    // later phase, e.g. Phase 14, seeds additional same-tenant communities in the shared run.)
    await expect(page.getByText('Phase 8 Tenant2 Club')).toHaveCount(0)
  })

  test('H58 community details render info and the membership action', async () => {
    await nav(page, `/app/student/communities/${FIX.commT1}`)
    await expect(page.getByRole('heading', { name: /phase 8 math club/i })).toBeVisible()
    await expect(page.getByText(/community posts/i)).toBeVisible()
    await expect(page.getByRole('button', { name: /join community/i })).toBeVisible()
  })

  test('H59 joining a community persists across reload', async () => {
    await page.goto(`/app/student/communities/${FIX.commT1}`)
    const joinP = page.waitForResponse((r) => r.url().includes(`/communities/${FIX.commT1}/join`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /join community/i }).click()
    expect((await joinP).status()).toBe(200)
    // The membership is durably persisted (verified against the backend directly).
    const token = await apiLogin(CODES.studentT1)
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/api/v1/communities/${FIX.commT1}`, { headers: { Authorization: `Bearer ${token}` } })
      const json = await res.json()
      expect((json?.data ?? json)?.memberCount).toBe(2)
    } finally {
      await ctx.dispose()
    }
  })

  test('H60 a member can create a post and it is shown', async () => {
    await page.goto(`/app/student/communities/${FIX.commT1}`)
    const body = 'Phase 8 community post from the live acceptance run.'
    await page.getByLabel(/new post/i).fill(body)
    const postP = page.waitForResponse((r) => r.url().includes(`/communities/${FIX.commT1}/posts`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /submit/i }).click()
    expect((await postP).status()).toBe(201)
    await expect(page.getByText(body).first()).toBeVisible()
  })

  test('H61 a member can add a material comment and it is shown', async () => {
    await page.goto(`/app/student/materials/${FIX.materialT1}`)
    const body = 'Phase 8 material comment from the live acceptance run.'
    await page.getByLabel(/add a comment/i).fill(body)
    const cP = page.waitForResponse((r) => r.url().includes(`/lesson-materials/${FIX.materialT1}/comments`) && r.request().method() === 'POST')
    await page.getByRole('button', { name: /submit/i }).click()
    expect((await cP).status()).toBe(201)
    await expect(page.getByText(body).first()).toBeVisible()
  })

  test('H62 a cross-tenant community is denied and user content renders as safe text', async () => {
    // Cross-tenant community id leaks nothing — the backend returns 404.
    const token = await apiLogin(CODES.studentT1)
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/api/v1/communities/${FIX.commT2}`, { headers: { Authorization: `Bearer ${token}` } })
      expect(res.status()).toBe(404)
    } finally {
      await ctx.dispose()
    }
    // The UI surfaces a usable error state (no foreign-tenant content rendered).
    await page.goto(`/app/student/communities/${FIX.commT2}`)
    await expect(page.locator('[role="alert"]').first()).toBeVisible()
    await expect(page.getByText('Phase 8 Math Club')).toHaveCount(0)

    // Existing posts render as escaped text (no raw HTML / injected script).
    await page.goto(`/app/student/communities/${FIX.commT1}`)
    await expect(page.getByText(/welcome to the phase 8 math club/i).first()).toBeVisible()
    await expect(page.locator('main script:not([src])')).toHaveCount(0)
  })
})
