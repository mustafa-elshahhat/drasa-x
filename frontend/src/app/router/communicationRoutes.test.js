import { describe, expect, it } from 'vitest'
import { ROUTES } from './routes'
import { NAV_ITEMS, navItemsForRole } from '../../navigation/navConfig'
import { ROLES } from '../../features/auth/roles'

const commsPaths = ['/app/notifications', '/app/notifications/preferences', '/app/messages', '/app/messages/:conversationId']
const commsRoutes = ROUTES.filter((r) => commsPaths.includes(r.path))

describe('Phase 13 shared communication routes', () => {
  it('registers every shared communication route', () => {
    for (const p of commsPaths) {
      expect(ROUTES.some((r) => r.path === p)).toBe(true)
    }
  })

  it('requires authentication for every communication route', () => {
    for (const r of commsRoutes) expect(r.requiresAuth).toBe(true)
  })

  it('exposes Messages + Notifications nav to every tenant role (not platform admin)', () => {
    for (const role of [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN]) {
      const keys = navItemsForRole(role).map((i) => i.key)
      expect(keys).toContain('messages')
      expect(keys).toContain('notifications-center')
    }
    const sysKeys = navItemsForRole(ROLES.SYSTEM_ADMIN).map((i) => i.key)
    expect(sysKeys).not.toContain('messages')
    expect(sysKeys).not.toContain('notifications-center')
  })

  it('points messaging/notifications nav at the shared backend-backed routes', () => {
    const byKey = Object.fromEntries(NAV_ITEMS.map((i) => [i.key, i.to]))
    expect(byKey.messages).toBe('/app/messages')
    expect(byKey['notifications-center']).toBe('/app/notifications')
  })
})
