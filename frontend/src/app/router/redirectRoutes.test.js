import { describe, expect, it } from 'vitest'
import { ROUTES } from './routes'

// Regression test for D3 (duplicate student routes) / D4 (/app/teacher/assignments)
// — audit-driven fix pass, P3-10/P3-11. Each entry below was previously an
// unlinked, byte-identical alias rendering the SAME component as its
// canonical route under a second URL; each now redirects to the canonical
// path instead.

const EXPECTED_REDIRECTS = {
  '/app/student/comments/:materialId': '/app/student/materials/:materialId',
  '/app/student/leaderboard/:competitionId': '/app/student/competitions/:competitionId',
  '/app/student/notifications': '/app/notifications',
  '/app/student/streaks': '/app/student/badges',
  '/app/student/profile': '/app/profile',
  '/app/teacher/assignments': '/app/teacher/homework',
}

describe('duplicate route alias redirects (D3/D4)', () => {
  for (const [from, to] of Object.entries(EXPECTED_REDIRECTS)) {
    it(`redirects ${from} -> ${to}`, () => {
      const route = ROUTES.find((r) => r.path === from)
      expect(route).toBeDefined()
      expect(route.props?.to).toBe(to)
    })
  }

  it('every redirect route still requires authentication (no auth bypass introduced)', () => {
    for (const from of Object.keys(EXPECTED_REDIRECTS)) {
      const route = ROUTES.find((r) => r.path === from)
      expect(route.requiresAuth).toBe(true)
    }
  })

  it('the canonical targets are themselves real, registered routes (not dangling)', () => {
    const canonicalPatterns = new Set(ROUTES.map((r) => r.path))
    // Param-carrying targets are templates, not literal paths — check the
    // equivalent :param route pattern is registered instead of a literal match.
    expect(canonicalPatterns.has('/app/student/materials/:materialId')).toBe(true)
    expect(canonicalPatterns.has('/app/student/competitions/:competitionId')).toBe(true)
    expect(canonicalPatterns.has('/app/notifications')).toBe(true)
    expect(canonicalPatterns.has('/app/student/badges')).toBe(true)
    expect(canonicalPatterns.has('/app/profile')).toBe(true)
    expect(canonicalPatterns.has('/app/teacher/homework')).toBe(true)
  })
})
