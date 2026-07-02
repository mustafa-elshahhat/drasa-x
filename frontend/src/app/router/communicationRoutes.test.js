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

  it('exposes Messages nav to every tenant role but never platform admin (backend 403s SystemAdmin there)', () => {
    for (const role of [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN]) {
      const keys = navItemsForRole(role).map((i) => i.key)
      expect(keys).toContain('messages')
    }
    const sysKeys = navItemsForRole(ROLES.SYSTEM_ADMIN).map((i) => i.key)
    expect(sysKeys).not.toContain('messages')
  })

  // §7 row 2 / P1-9 / D5: NotificationsController is plain [Authorize] (no tenant requirement),
  // so SystemAdmin genuinely can use it — the nav item now matches what the header
  // NotificationBell already did for SystemAdmin, instead of hiding an otherwise-working surface.
  it('exposes Notifications nav to every authenticated role, including platform admin', () => {
    for (const role of [ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN, ROLES.SYSTEM_ADMIN]) {
      const keys = navItemsForRole(role).map((i) => i.key)
      expect(keys).toContain('notifications-center')
    }
  })

  // §7 row 1 / P1-9 / D5: the route itself must reject SystemAdmin (route guard), not just hide
  // the nav link — a FE-allowed/BE-blocked mismatch is still reachable by typing the URL.
  it('restricts /app/messages route guards to tenant roles only (SystemAdmin excluded)', () => {
    const messageRoutes = ROUTES.filter((r) => r.path === '/app/messages' || r.path === '/app/messages/:conversationId')
    expect(messageRoutes).toHaveLength(2)
    for (const r of messageRoutes) {
      expect(r.roles).toEqual(expect.arrayContaining([ROLES.STUDENT, ROLES.TEACHER, ROLES.PARENT, ROLES.SCHOOL_ADMIN]))
      expect(r.roles).not.toContain(ROLES.SYSTEM_ADMIN)
    }
  })

  it('leaves /app/notifications* routes open to any authenticated role (no role restriction)', () => {
    const notifRoutes = ROUTES.filter((r) => r.path === '/app/notifications' || r.path === '/app/notifications/preferences')
    expect(notifRoutes).toHaveLength(2)
    for (const r of notifRoutes) expect(r.roles).toBeUndefined()
  })

  it('points messaging/notifications nav at the shared backend-backed routes', () => {
    const byKey = Object.fromEntries(NAV_ITEMS.map((i) => [i.key, i.to]))
    expect(byKey.messages).toBe('/app/messages')
    expect(byKey['notifications-center']).toBe('/app/notifications')
  })
})
