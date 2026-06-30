import { describe, it, expect } from 'vitest'
import { ROUTES, findRouteByPath } from './routes'

// Phase 11 regression guard for the portal split: every route must resolve to a
// real lazily-loaded component (the giant `props.view` dispatchers were removed),
// paths are unique, and each area keeps its full route surface.
describe('route inventory (post-split)', () => {
  it('exposes the full route surface with no duplicate paths', () => {
    expect(ROUTES.length).toBeGreaterThanOrEqual(120)
    const paths = ROUTES.map((r) => r.path)
    expect(new Set(paths).size).toBe(paths.length) // no duplicates
  })

  it('every route declares a path and a (lazy) Component', () => {
    for (const r of ROUTES) {
      expect(typeof r.path).toBe('string')
      expect(r.Component).toBeTruthy()
    }
  })

  it('keeps every role portal area fully routed', () => {
    const count = (prefix) => ROUTES.filter((r) => r.path.startsWith(prefix)).length
    expect(count('/app/student')).toBeGreaterThanOrEqual(33)
    expect(count('/app/teacher')).toBeGreaterThanOrEqual(17)
    expect(count('/app/parent')).toBeGreaterThanOrEqual(12)
    expect(count('/app/school')).toBeGreaterThanOrEqual(25)
    expect(count('/app/system')).toBeGreaterThanOrEqual(18)
  })

  it('keeps the shared communication surfaces routed', () => {
    for (const p of ['/app/notifications', '/app/notifications/preferences', '/app/messages', '/app/messages/:conversationId']) {
      expect(findRouteByPath(p)).toBeTruthy()
    }
  })

  it('findRouteByPath returns metadata for a known path and null for an unknown one', () => {
    expect(findRouteByPath('/app/student')?.titleKey).toBe('student.dashboard.title')
    expect(findRouteByPath('/does/not/exist')).toBeNull()
  })
})
