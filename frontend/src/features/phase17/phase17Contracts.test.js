// =============================================================================
// Phase 17 — frontend client CONTRACT regression pack.
//
// Closes the audited gap that the `school` and `system` API clients had no
// client-shape test, and pins the platform-wide browser invariant: every feature
// client talks to DerasaX-backend via backend-RELATIVE paths only — never an
// absolute URL, never the AI service (:8000 / school-ai-rag), never a raw object-
// storage host. A regression here is an SSRF / data-exfiltration class risk, so it
// is asserted in one durable place. Also locks the Phase 16 generic-upload purpose
// gate on the client side (the UI cannot even offer a restricted purpose).
// =============================================================================
import { describe, it, expect, vi, beforeEach } from 'vitest'

// Capture every path the clients ask the canonical api to hit.
const paths = []
const rejecting = { current: false }
vi.mock('../../lib/api/client', () => {
  const recordGet = (p) => {
    paths.push(p)
    if (rejecting.current) return Promise.reject(new Error('boom'))
    return Promise.resolve({ data: {}, items: [] })
  }
  const recordWrite = (p) => {
    paths.push(p)
    if (rejecting.current) return Promise.reject(new Error('boom'))
    return Promise.resolve({ data: {} })
  }
  return {
    api: {
      get: vi.fn((p) => recordGet(p)),
      post: vi.fn((p) => recordWrite(p)),
      put: vi.fn((p) => recordWrite(p)),
      patch: vi.fn((p) => recordWrite(p)),
      del: vi.fn((p) => recordWrite(p)),
      upload: vi.fn((p) => recordWrite(p)),
    },
    apiFetch: vi.fn(async () => ({
      ok: true,
      blob: async () => new Blob(['x']),
      headers: { get: () => 'attachment; filename="x.pdf"' },
    })),
  }
})

import { apiFetch } from '../../lib/api/client'
import { schoolApi } from '../school/schoolApi'
import { systemApi } from '../system/systemApi'
import { filesApi, FILE_PURPOSES } from '../files/filesApi'

beforeEach(() => {
  paths.length = 0
  rejecting.current = false
  vi.clearAllMocks()
})

// A path is "backend-relative + safe" iff it starts with "/api" and contains no
// scheme, no AI-service port, and no object-storage host.
const FORBIDDEN = [/^https?:\/\//i, /:8000\b/, /school-ai-rag/i, /amazonaws\.com/i, /\bs3[.-]/i, /onrender\.com/i, /\.r2\.cloudflarestorage/i, /blob\.core\.windows\.net/i]
function assertBackendRelative(captured) {
  expect(captured.length).toBeGreaterThan(0)
  for (const p of captured) {
    expect(typeof p).toBe('string')
    expect(p.startsWith('/api')).toBe(true)
    for (const bad of FORBIDDEN) expect(p).not.toMatch(bad)
  }
}

describe('Phase 17 — frontend API client contracts (backend-only)', () => {
  it('schoolApi issues only backend-relative paths (reads + writes)', async () => {
    await Promise.allSettled([
      schoolApi.dashboard(),
      schoolApi.profile(),
      schoolApi.subscription(),
      schoolApi.usage(),
      schoolApi.academicYears(),
      schoolApi.createAcademicYear({ name: 'AY' }),
      schoolApi.terms(),
      schoolApi.grades(),
      schoolApi.createGrade({ name: 'G' }),
      schoolApi.classes(),
      schoolApi.subjects(),
      schoolApi.users('Teacher'),
      schoolApi.createUser({ fullName: 'x', loginCode: 'y', role: 'Teacher' }),
      schoolApi.setUserEnabled('u1', false),
      schoolApi.relationships(),
      schoolApi.createRelationship({ parentId: 'p', studentId: 's' }),
      schoolApi.deactivateRelationship('r1'),
      schoolApi.subjectAssignments(),
      schoolApi.classAssignments(),
      schoolApi.announcements(),
      schoolApi.publishAnnouncement('a1', true),
      schoolApi.documentRequests(),
      schoolApi.respondDocumentRequest('d1', { body: 'b' }),
      schoolApi.transitionDocumentRequest('d1', { status: 1 }),
      schoolApi.communities(),
      schoolApi.competitions(),
      schoolApi.aiUsage(),
      schoolApi.support(),
      schoolApi.audit(),
      schoolApi.settings(),
      schoolApi.upsertSetting({ key: 'k', value: 'v' }),
    ])
    assertBackendRelative(paths)
  })

  it('systemApi issues only backend-relative paths (reads + writes)', async () => {
    await Promise.allSettled([
      systemApi.dashboard(),
      systemApi.tenants('Active'),
      systemApi.tenant('t1'),
      systemApi.tenantSubscription('t1'),
      systemApi.tenantUsage('t1'),
      systemApi.createTenant({ name: 'n' }),
      systemApi.setTenantStatus('t1', 'suspend'),
      systemApi.plans(),
      systemApi.assignPlan({ tenantId: 't1', planDefinitionId: 'pd' }),
      systemApi.subscriptions(),
      systemApi.createSchoolAdmin('t1', { fullName: 'x', loginCode: 'y' }),
      systemApi.usage(),
      systemApi.aiUsage(),
      systemApi.storage(),
      systemApi.supportTickets({ status: 1 }),
      systemApi.respondSupportTicket('s1', { responseMessage: 'm', status: 1 }),
      systemApi.announcements(),
      systemApi.createAnnouncement({ title: 't' }),
      systemApi.featureFlags(),
      systemApi.upsertFeatureFlag({ key: 'k', isEnabled: true }),
      systemApi.settings(),
      systemApi.upsertSetting({ key: 'k', value: 'v', valueType: 'string', isSecret: false }),
      systemApi.audit(),
      systemApi.securityEvents(),
      systemApi.operationalStatus(),
      systemApi.exportTenantData('t1'),
      systemApi.requestTenantDeletion('t1'),
    ])
    assertBackendRelative(paths)
  })

  it('filesApi issues only backend-relative paths (upload/metadata/signed/remove/download)', async () => {
    const file = new File([new Uint8Array(2)], 'a.pdf', { type: 'application/pdf' })
    await Promise.allSettled([
      filesApi.upload({ file, purpose: 'CommunityAttachment' }),
      filesApi.metadata('f1'),
      filesApi.signedDownload('f1'),
      filesApi.remove('f1'),
    ])
    assertBackendRelative(paths)

    // The backend-mediated download goes through apiFetch with a backend-relative
    // path — never a raw storage URL.
    await filesApi.download('f1', 'a.pdf')
    expect(apiFetch).toHaveBeenCalledWith('/api/v1/files/f1/download', { method: 'GET' })
  })

  it('the generic-upload purpose set never offers a restricted purpose (mirrors the backend 400 gate)', () => {
    const restricted = ['LessonMaterial', 'ParentDocumentRequest', 'ParentDocumentResponse', 'CvEnrollmentAsset']
    const offered = Object.values(FILE_PURPOSES)
    for (const r of restricted) expect(offered).not.toContain(r)
    // And every offered purpose is a non-empty string (no accidental undefineds).
    for (const p of offered) expect(typeof p === 'string' && p.length > 0).toBe(true)
  })

  it('client error mapping is stable — a backend failure propagates (never silently swallowed)', async () => {
    rejecting.current = true
    await expect(schoolApi.dashboard()).rejects.toThrow()
    await expect(systemApi.dashboard()).rejects.toThrow()
    await expect(filesApi.metadata('f1')).rejects.toThrow()
  })
})
