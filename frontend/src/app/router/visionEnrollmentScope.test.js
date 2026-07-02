import { describe, it, expect } from 'vitest'
import { ROUTES } from './routes'
import { NAV_ITEMS } from '../../navigation/navConfig'

// Contract-gap appendix decision (audit §8.3 "Vision"): `visionApi.enrollFace`/
// `listEnrollments` are real, backend-integrated, and unit-tested at the API
// client layer (see features/vision/visionApi.test.js) — the backend is ready.
// But NO page/route/nav in this pass builds a face-enrollment CAPTURE UI,
// deliberately: enrolling a student's biometric face data needs an explicit
// consent/retention/UX product decision (who can enroll, what a student/parent
// is told, how long the data is kept, how to revoke it) the same way the other
// audit §12 decisions were made up front — inventing that flow silently inside
// a route/RBAC bug-fix pass would be worse than leaving it unbuilt. This test
// pins the current, deliberate route/nav surface so any future addition of an
// enrollment capture UI is a visible, intentional change to this file, not an
// accidental one.

describe('vision face-enrollment capture UI (deliberately not built)', () => {
  it('registers only the known, reviewed vision routes — no enrollment capture route', () => {
    const visionRoutes = ROUTES.filter((r) => r.path.includes('vision')).map((r) => r.path).sort()
    expect(visionRoutes).toEqual([
      '/app/parent/children/:childId/vision',
      '/app/student/vision',
      '/app/vision',
      '/app/vision/sessions/:sessionId',
    ])
  })

  it('exposes no nav item pointing at an enrollment path', () => {
    expect(NAV_ITEMS.some((item) => /enroll/i.test(item.to))).toBe(false)
  })
})
