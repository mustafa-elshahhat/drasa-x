// =============================================================================
// Phase 17 — cross-cutting REGRESSION smoke (live).
//
// One repeatable suite that re-proves the platform's load-bearing journeys after
// Phase 16, mixing real browser UI (session protection + login/logout) with
// browser-origin HTTP (Playwright request) → DerasaX-backend, exactly as the SPA
// client would call it. No mocking; honest skips when the backend/creds are absent.
//
// Covers: anonymous→login redirect for every role dashboard; login/logout/session;
// role-scoped dashboard access (school vs system) + cross-role 403; school-admin
// teacher/student/class visibility; teacher lesson-material round-trip; student
// own-vs-cross-tenant; parent linked-child access; Phase 16 file upload/download +
// signed token + soft-delete; CV enrollment assets honestly default-OFF;
// cross-tenant negative. File components are not yet embedded in the dense portal
// screens (Phase 16 §O), so the file smoke runs at the API layer — documented, not
// faked, in PHASE17_REMAINING_GAPS.md.
// =============================================================================
import { test, expect, request as pwRequest } from '@playwright/test'

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const PASSWORD = process.env.E2E_PASSWORD || ''
const HAS_CREDS = Boolean(PASSWORD)
const LESSON = 'PH8-LESSON-T1'

const PDF = Buffer.concat([Buffer.from('%PDF-1.4\n'), Buffer.alloc(48)])
const PNG = Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), Buffer.alloc(48)])

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

async function login(code) {
  const ctx = await pwRequest.newContext()
  try {
    const res = await ctx.post(`${BACKEND}/api/v1/account/login`, {
      headers: { 'Content-Type': 'application/json' },
      data: { UserID: code, Password: PASSWORD },
      timeout: 15000,
    })
    if (!res.ok()) throw new Error(`login ${code}: ${res.status()}`)
    return await res.json()
  } finally {
    await ctx.dispose()
  }
}

const authCtx = (token) => pwRequest.newContext({ extraHTTPHeaders: { Authorization: `Bearer ${token}` } })

// ---------------------------------------------------------------------------
// Backend-INDEPENDENT: session protection on every role dashboard (no creds).
// ---------------------------------------------------------------------------
test.describe('Phase 17 — session protection (no backend required)', () => {
  for (const route of ['/app/student', '/app/teacher', '/app/parent', '/app/school', '/app/system']) {
    test(`anonymous is redirected from ${route} to login`, async ({ page }) => {
      await page.goto(route)
      await expect(page).toHaveURL(/\/login$/)
      await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible()
    })
  }
})

// ---------------------------------------------------------------------------
// Backend-DEPENDENT live matrix (needs the local stack + E2E_PASSWORD).
// ---------------------------------------------------------------------------
test.describe('Phase 17 — regression smoke (live)', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live Phase 17 smoke')

  let admin, sys, teacher, studentT1, studentT2, parent

  test.beforeAll(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    admin = await login('ADMIN-T1')
    sys = await login('SYS-1')
    teacher = await login('TEACH-T1')
    studentT1 = await login('STU-T1')
    studentT2 = await login('STU-T2')
    parent = await login('PH10-PARENT-T1')
  })

  test('login → role shell → logout → protected route is re-protected (UI session)', async ({ page }) => {
    await page.goto('/login')
    await page.getByLabel(/login code/i).fill('ADMIN-T1')
    await page.getByLabel(/^password/i).fill(PASSWORD)
    await page.getByRole('button', { name: /sign in/i }).click()
    await expect(page).toHaveURL(/\/app/)
    await expect(page.getByRole('navigation', { name: /main navigation/i })).toBeVisible()

    await page.getByRole('button', { name: /sign out/i }).click()
    await expect(page).toHaveURL(/\/login$/)
    await page.goto('/app/profile')
    await expect(page).toHaveURL(/\/login$/)
  })

  test('role-scoped dashboards: each admin reaches its own, and cross-role is 403', async () => {
    const a = await authCtx(admin.token)
    const s = await authCtx(sys.token)
    const stu = await authCtx(studentT1.token)
    try {
      expect((await a.get(`${BACKEND}/api/v1/school-admin/dashboard`)).status()).toBe(200)
      expect((await s.get(`${BACKEND}/api/v1/system-admin/dashboard`)).status()).toBe(200)
      // Cross-role: a student cannot reach the school-admin dashboard; a school admin
      // cannot reach the platform (system-admin) dashboard.
      expect((await stu.get(`${BACKEND}/api/v1/school-admin/dashboard`)).status()).toBe(403)
      expect((await a.get(`${BACKEND}/api/v1/system-admin/dashboard`)).status()).toBe(403)
    } finally {
      await a.dispose(); await s.dispose(); await stu.dispose()
    }
  })

  test('school admin sees tenant teachers / students / classes', async () => {
    const a = await authCtx(admin.token)
    try {
      expect((await a.get(`${BACKEND}/api/v1/tenant-users?role=Teacher&pageSize=20`)).status()).toBe(200)
      expect((await a.get(`${BACKEND}/api/v1/tenant-users?role=Student&pageSize=20`)).status()).toBe(200)
      expect((await a.get(`${BACKEND}/api/v1/classes`)).status()).toBe(200)
    } finally {
      await a.dispose()
    }
  })

  test('teacher uploads lesson material; enrolled student downloads; teacher soft-deletes', async () => {
    const t = await authCtx(teacher.token)
    const up = await t.post(`${BACKEND}/api/LessonMaterial/UploadMaterial`, {
      multipart: {
        File: { name: 'p17-notes.pdf', mimeType: 'application/pdf', buffer: PDF },
        LessonId: LESSON, Title: 'Phase17 smoke notes', Type: 'Document',
      },
    })
    expect(up.status()).toBe(201)
    const fileId = (await up.json()).data.fileRecordId
    expect(fileId).toBeTruthy()

    const s1 = await authCtx(studentT1.token)
    expect((await s1.get(`${BACKEND}/api/v1/files/${fileId}/download`)).status()).toBe(200)
    await s1.dispose()

    // Cross-tenant student is blocked (existence hidden → 404).
    const s2 = await authCtx(studentT2.token)
    expect((await s2.get(`${BACKEND}/api/v1/files/${fileId}/download`)).status()).toBe(404)
    await s2.dispose()

    // Owner soft-delete blocks future downloads (keeps the smoke self-tidy).
    expect((await t.delete(`${BACKEND}/api/v1/files/${fileId}`)).status()).toBe(200)
    expect((await t.get(`${BACKEND}/api/v1/files/${fileId}/download`)).status()).toBe(404)
    await t.dispose()
  })

  test('student sees own-tenant data and is blocked cross-tenant', async () => {
    const stu = await authCtx(studentT1.token)
    try {
      expect((await stu.get(`${BACKEND}/api/Grades/GetGradeById?id=G7-ID`)).status()).toBe(200)
      expect((await stu.get(`${BACKEND}/api/Grades/GetGradeById?id=T2-G7`)).status()).toBe(404)
    } finally {
      await stu.dispose()
    }
  })

  test('parent reaches their linked-child document surface (read-only smoke)', async () => {
    // Read-only on purpose so the smoke is idempotent across repeated official runs
    // (the linked-parent CREATE/attach/unrelated-404 path is proven in e2e/phase16).
    const p = await authCtx(parent.token)
    try {
      expect((await p.get(`${BACKEND}/api/v1/parent-requests`)).status()).toBe(200)
    } finally {
      await p.dispose()
    }
  })

  test('Phase 16 generic file: upload → download → signed token → soft-delete', async () => {
    const a = await authCtx(admin.token)
    try {
      const up = await a.post(`${BACKEND}/api/v1/files/upload`, {
        multipart: { File: { name: 'p17.png', mimeType: 'image/png', buffer: PNG }, Purpose: 'CommunityAttachment' },
      })
      expect(up.status()).toBe(201)
      const fileId = (await up.json()).data.id

      expect((await a.get(`${BACKEND}/api/v1/files/${fileId}/download`)).status()).toBe(200)

      const signed = await a.post(`${BACKEND}/api/v1/files/${fileId}/signed-download`)
      expect(signed.status()).toBe(200)
      const token = (await signed.json()).data.token
      const anon = await pwRequest.newContext()
      expect((await anon.get(`${BACKEND}/api/v1/files/download?token=${encodeURIComponent(token)}`)).status()).toBe(200)
      await anon.dispose()

      expect((await a.delete(`${BACKEND}/api/v1/files/${fileId}`)).status()).toBe(200)
      expect((await a.get(`${BACKEND}/api/v1/files/${fileId}/download`)).status()).toBe(404)
    } finally {
      await a.dispose()
    }
  })

  test('CV enrollment assets are honestly disabled by default (status + 403)', async () => {
    const t = await authCtx(teacher.token)
    try {
      const status = await t.get(`${BACKEND}/api/v1/vision/enrollment-assets/status`)
      expect(status.status()).toBe(200)
      expect(JSON.stringify(await status.json())).toContain('"enabled":false')

      const attempt = await t.post(`${BACKEND}/api/v1/vision/enrollments/any-id/asset`, {
        multipart: { File: { name: 'face.png', mimeType: 'image/png', buffer: PNG }, ConsentObtained: 'true' },
      })
      expect(attempt.status()).toBe(403)
    } finally {
      await t.dispose()
    }
  })
})
