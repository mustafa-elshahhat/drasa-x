// =============================================================================
// Phase 16 — durable file storage: LIVE acceptance.
// Real browser-origin HTTP (Playwright request) → DerasaX-backend → storage
// provider (local) → PostgreSQL. No mocking. Proves the required flows:
//   1. teacher uploads a lesson material  → enrolled student downloads it
//   2. parent document: linked parent can download, unrelated parent cannot
//   3. community attachment upload + download (generic file API)
//   4. cross-tenant file access is rejected (404)
//   5. a sensitive download creates auditable backend evidence
//   6. CV enrollment asset is honestly DISABLED by default (status + 403)
//   7. invalid type / oversized / signed-token validation
// The webServer (vite preview) is started by Playwright; these specs talk to the
// backend directly via its account contract, exactly as the browser client would.
// =============================================================================
import { test, expect, request as pwRequest } from '@playwright/test'

const BACKEND = process.env.VITE_DOTNET_URL || 'http://localhost:5155'
const PASSWORD = process.env.E2E_PASSWORD || ''
const HAS_CREDS = Boolean(PASSWORD)
const LESSON = 'PH8-LESSON-T1' // seeded tenant-1 lesson

const PDF = Buffer.concat([Buffer.from('%PDF-1.4\n'), Buffer.alloc(64)])
const PNG = Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), Buffer.alloc(64)])

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
    return await res.json() // { token, role, id }
  } finally {
    await ctx.dispose()
  }
}

function authCtx(token) {
  return pwRequest.newContext({ extraHTTPHeaders: { Authorization: `Bearer ${token}` } })
}

test.describe('Phase 16 — durable file storage (live)', () => {
  test.describe.configure({ mode: 'serial' })
  test.skip(!HAS_CREDS, 'Set E2E_PASSWORD to run the live matrix')

  let teacher, studentT1, studentT2, admin, parent, unrelatedParent

  test.beforeAll(async () => {
    test.skip(!(await backendUp()), `DerasaX-backend not reachable at ${BACKEND}`)
    teacher = await login('TEACH-T1')
    studentT1 = await login('STU-T1')
    studentT2 = await login('STU-T2')
    admin = await login('ADMIN-T1')
    parent = await login('PH10-PARENT-T1')
    unrelatedParent = await login('PH11-PARENT-T1')
  })

  test('1. teacher uploads lesson material; enrolled student downloads; cross-tenant blocked', async () => {
    const t = await authCtx(teacher.token)
    const up = await t.post(`${BACKEND}/api/LessonMaterial/UploadMaterial`, {
      multipart: {
        File: { name: 'algebra.pdf', mimeType: 'application/pdf', buffer: PDF },
        LessonId: LESSON,
        Title: 'Phase16 algebra notes',
        Type: 'Document',
      },
    })
    expect(up.status()).toBe(201)
    const fileId = (await up.json()).data.fileRecordId
    expect(fileId).toBeTruthy()
    await t.dispose()

    // Enrolled tenant-1 student downloads the tenant-internal material.
    const s1 = await authCtx(studentT1.token)
    const dl = await s1.get(`${BACKEND}/api/v1/files/${fileId}/download`)
    expect(dl.status()).toBe(200)
    expect((await dl.body()).length).toBeGreaterThan(0)
    await s1.dispose()

    // A student in another tenant cannot.
    const s2 = await authCtx(studentT2.token)
    const blocked = await s2.get(`${BACKEND}/api/v1/files/${fileId}/download`)
    expect(blocked.status()).toBe(404)
    await s2.dispose()
  })

  test('2. parent document: linked parent downloads, unrelated parent rejected; audited', async () => {
    const p = await authCtx(parent.token)
    const create = await p.post(`${BACKEND}/api/v1/parent-requests`, {
      headers: { 'Content-Type': 'application/json' },
      data: { studentId: studentT1.id, type: 0, title: 'P16 report', body: 'Please share the report.' },
    })
    expect(create.status()).toBe(201)
    const requestId = (await create.json()).data.id

    const attach = await p.post(`${BACKEND}/api/v1/parent-requests/${requestId}/attachment`, {
      multipart: { File: { name: 'id.pdf', mimeType: 'application/pdf', buffer: PDF } },
    })
    expect(attach.status()).toBe(200)

    // Owner parent downloads the sensitive document.
    const dl = await p.get(`${BACKEND}/api/v1/parent-requests/${requestId}/attachment/download`)
    expect(dl.status()).toBe(200)
    await p.dispose()

    // Unrelated same-tenant parent cannot.
    const u = await authCtx(unrelatedParent.token)
    const blocked = await u.get(`${BACKEND}/api/v1/parent-requests/${requestId}/attachment/download`)
    expect(blocked.status()).toBe(404)
    await u.dispose()

    // Sensitive download produced auditable backend evidence (SchoolAdmin audit feed).
    const a = await authCtx(admin.token)
    const audit = await a.get(`${BACKEND}/api/v1/audit?pageSize=50`)
    expect(audit.status()).toBe(200)
    const body = await audit.json()
    const rows = body.data ?? body.items ?? []
    expect(JSON.stringify(rows)).toContain('FileRecord')
    await a.dispose()
  })

  test('3. community attachment upload + download via the generic file API', async () => {
    const a = await authCtx(admin.token)
    const up = await a.post(`${BACKEND}/api/v1/files/upload`, {
      multipart: {
        File: { name: 'flyer.png', mimeType: 'image/png', buffer: PNG },
        Purpose: 'CommunityAttachment',
      },
    })
    expect(up.status()).toBe(201)
    const fileId = (await up.json()).data.id

    const dl = await a.get(`${BACKEND}/api/v1/files/${fileId}/download`)
    expect(dl.status()).toBe(200)

    // Signed, time-limited token download works anonymously (no auth header).
    const signed = await a.post(`${BACKEND}/api/v1/files/${fileId}/signed-download`)
    expect(signed.status()).toBe(200)
    const token = (await signed.json()).data.token
    const anon = await pwRequest.newContext()
    const viaToken = await anon.get(`${BACKEND}/api/v1/files/download?token=${encodeURIComponent(token)}`)
    expect(viaToken.status()).toBe(200)
    const tampered = await anon.get(`${BACKEND}/api/v1/files/download?token=${encodeURIComponent(token.slice(0, -2))}`)
    expect([400, 404]).toContain(tampered.status())
    await anon.dispose()

    // Soft-delete blocks future downloads.
    const del = await a.delete(`${BACKEND}/api/v1/files/${fileId}`)
    expect(del.status()).toBe(200)
    const after = await a.get(`${BACKEND}/api/v1/files/${fileId}/download`)
    expect(after.status()).toBe(404)
    await a.dispose()
  })

  test('4. validation: invalid type and oversized are rejected', async () => {
    const a = await authCtx(admin.token)
    const badType = await a.post(`${BACKEND}/api/v1/files/upload`, {
      multipart: { File: { name: 'x.exe', mimeType: 'application/octet-stream', buffer: Buffer.alloc(8) }, Purpose: 'Other' },
    })
    expect(badType.status()).toBe(400)

    const oversized = await a.post(`${BACKEND}/api/v1/files/upload`, {
      multipart: { File: { name: 'big.png', mimeType: 'image/png', buffer: Buffer.alloc(6 * 1024 * 1024) }, Purpose: 'ProfileImage' },
    })
    expect(oversized.status()).toBe(400)
    await a.dispose()
  })

  test('5. CV enrollment assets are honestly disabled by default', async () => {
    const t = await authCtx(teacher.token)
    const status = await t.get(`${BACKEND}/api/v1/vision/enrollment-assets/status`)
    expect(status.status()).toBe(200)
    expect(JSON.stringify(await status.json())).toContain('"enabled":false')

    const attempt = await t.post(`${BACKEND}/api/v1/vision/enrollments/any-id/asset`, {
      multipart: { File: { name: 'face.png', mimeType: 'image/png', buffer: PNG }, ConsentObtained: 'true' },
    })
    expect(attempt.status()).toBe(403)
    await t.dispose()
  })
})
