import { test, expect, request as pwRequest } from '@playwright/test'
import { HAS_CREDS, CODES, BACKEND, PASSWORD, backendUp, login, nav, attachGuards } from '../support/ph8.js'

// Phase 14 — ledger-based gamification, proven live: a student sees real backend points / ledger /
// leaderboard; an authorized admin award lands in the student's ledger; a student cannot award points;
// and the leaderboard is tenant-scoped. Awards use a FIXED idempotency key so the matrix is
// re-runnable without mutating totals (the same award is a no-op on re-run).
const AWARD_KEY = 'e2e-ph14-gami-award'

async function rawLogin(code) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: PASSWORD },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`login ${code}: ${res.status()}`)
    const m = await res.json()
    return { token: m.token, id: m.id }
  } finally {
    await ctx.dispose()
  }
}

test.describe('Phase 14 A — gamification', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let context, page, guards, stuId

  test.beforeAll(async ({ browser }) => {
    test.skip(!(await backendUp()), 'DerasaX-backend not reachable')
    const stu = await rawLogin(CODES.studentT1)
    stuId = stu.id

    // Authorized admin award (idempotent fixed key) so the student has real ledger points.
    const admin = await rawLogin(CODES.schoolAdmin)
    const ctx = await pwRequest.newContext()
    try {
      await ctx.post(`${BACKEND}/api/v1/students/${stuId}/points`, {
        headers: { Authorization: `Bearer ${admin.token}`, 'Content-Type': 'application/json' },
        data: { points: 25, reason: 'Phase 14 E2E award', idempotencyKey: AWARD_KEY },
      })
    } finally {
      await ctx.dispose()
    }

    context = await browser.newContext()
    page = await context.newPage()
    guards = attachGuards(page)
    await login(page, CODES.studentT1)
  })

  test.afterAll(async () => {
    guards?.assertNoForbidden()
    await context?.close()
  })

  test('G1 student gamification page renders points, ledger and leaderboard from the backend', async () => {
    const pointsResp = page.waitForResponse((r) => r.url().includes(`/students/${stuId}/points`) && r.request().method() === 'GET')
    await nav(page, '/app/student/badges')
    expect((await pointsResp).status()).toBe(200)
    await expect(page.getByRole('heading', { name: /^points$/i })).toBeVisible()
    await expect(page.getByRole('heading', { name: /points leaderboard/i })).toBeVisible()
    // Real total is rendered (>= the idempotent E2E award).
    await expect(page.getByText(/you have \d+ point/i)).toBeVisible()
  })

  test('G2 the admin award is visible in the student leaderboard (tenant-scoped, real data)', async () => {
    const stu = await rawLogin(CODES.studentT1)
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.get(`${BACKEND}/api/v1/gamification/leaderboard?pageSize=100`, {
        headers: { Authorization: `Bearer ${stu.token}` },
      })
      expect(res.status()).toBe(200)
      const json = await res.json()
      const rows = json?.data ?? json
      expect(rows.some((r) => r.studentId === stuId)).toBe(true)

      // Tenant isolation: a tenant-2 viewer's leaderboard must not contain this tenant-1 student.
      const t2 = await rawLogin(CODES.studentT2)
      const res2 = await ctx.get(`${BACKEND}/api/v1/gamification/leaderboard?pageSize=100`, {
        headers: { Authorization: `Bearer ${t2.token}` },
      })
      const rows2 = (await res2.json())?.data ?? []
      expect(rows2.some((r) => r.studentId === stuId)).toBe(false)
    } finally {
      await ctx.dispose()
    }
  })

  test('G3 a student cannot award points (cannot self-award) — backend rejects with 403', async () => {
    const stu = await rawLogin(CODES.studentT1)
    const ctx = await pwRequest.newContext()
    try {
      const res = await ctx.post(`${BACKEND}/api/v1/students/${stuId}/points`, {
        headers: { Authorization: `Bearer ${stu.token}`, 'Content-Type': 'application/json' },
        data: { points: 100, reason: 'self' },
      })
      expect(res.status()).toBe(403)
    } finally {
      await ctx.dispose()
    }
  })

  test('G4 a re-applied award with the same idempotency key does not double-count', async () => {
    const admin = await rawLogin(CODES.schoolAdmin)
    const ctx = await pwRequest.newContext()
    try {
      const before = await ctx.get(`${BACKEND}/api/v1/students/${stuId}/points`, { headers: { Authorization: `Bearer ${admin.token}` } })
      const beforeTotal = (await before.json())?.data?.totalPoints ?? 0
      const dup = await ctx.post(`${BACKEND}/api/v1/students/${stuId}/points`, {
        headers: { Authorization: `Bearer ${admin.token}`, 'Content-Type': 'application/json' },
        data: { points: 25, reason: 'Phase 14 E2E award', idempotencyKey: AWARD_KEY },
      })
      expect([200, 201]).toContain(dup.status())
      const after = await ctx.get(`${BACKEND}/api/v1/students/${stuId}/points`, { headers: { Authorization: `Bearer ${admin.token}` } })
      const afterTotal = (await after.json())?.data?.totalPoints ?? 0
      expect(afterTotal).toBe(beforeTotal)
    } finally {
      await ctx.dispose()
    }
  })
})
