import { describe, it, expect } from 'vitest'
import { ROUTES } from './routes'

// Phase 20 frontend unification — the marketing routes are registered in the
// single route registry, are public (no auth), and render under the public
// layout, while /login stays a distinct anonymous-only auth route.
describe('public marketing routes', () => {
  const marketing = ROUTES.filter((r) => r.layout === 'public')

  it('registers the homepage and marketing pages under the public layout', () => {
    const paths = marketing.map((r) => r.path)
    expect(paths).toEqual(
      expect.arrayContaining(['/', '/request-demo', '/events', '/activities', '/news'])
    )
    expect(marketing.length).toBeGreaterThanOrEqual(5)
  })

  it('marketing routes are public (no auth) and not anonymous-only', () => {
    for (const r of marketing) {
      expect(r.requiresAuth).toBeFalsy()
      expect(r.anonymousOnly).toBeFalsy()
      expect(r.Component).toBeTruthy()
      expect(typeof r.titleKey).toBe('string')
    }
  })

  it('keeps the homepage at "/" inside the public layout', () => {
    const home = ROUTES.find((r) => r.path === '/')
    expect(home).toBeTruthy()
    expect(home.layout).toBe('public')
  })

  it('keeps /login as a distinct anonymous-only auth route (no marketing chrome)', () => {
    const login = ROUTES.find((r) => r.path === '/login')
    expect(login).toBeTruthy()
    expect(login.anonymousOnly).toBe(true)
    expect(login.layout).not.toBe('public')
  })

  // Task 3 (audit-driven fix pass): the forgot/reset password flow registers two
  // more anonymous-only auth routes alongside /login (no marketing chrome either).
  it('registers /forgot-password and /reset-password as anonymous-only auth routes', () => {
    for (const path of ['/forgot-password', '/reset-password']) {
      const route = ROUTES.find((r) => r.path === path)
      expect(route, `${path} is registered`).toBeTruthy()
      expect(route.anonymousOnly).toBe(true)
      expect(route.layout).not.toBe('public')
      expect(route.requiresAuth).toBeFalsy()
      expect(route.Component).toBeTruthy()
    }
  })
})
