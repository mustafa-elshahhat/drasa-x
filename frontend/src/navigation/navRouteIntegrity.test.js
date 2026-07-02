import { describe, it, expect } from 'vitest'
import { NAV_ITEMS } from './navConfig'
import { findRouteByPath } from '../app/router/routes'

// Testing gap T-01 (audit §10 item 1): every nav destination must resolve to a
// real registered route (or a known external/special target) — a stale `to`
// left behind after a route rename/removal would otherwise silently 404.

describe('nav <-> route registry cross-check (T-01)', () => {
  it('every NAV_ITEMS[].to resolves to a registered route', () => {
    const missing = NAV_ITEMS.filter((item) => !findRouteByPath(item.to)).map((item) => `${item.key} -> ${item.to}`)
    expect(missing).toEqual([])
  })

  // Header.jsx hardcodes: brand -> /app, account menu -> /app/profile,
  // /app/security, and a per-role /app/{slug}/settings.
  it('every Header.jsx hardcoded link target resolves to a registered route', () => {
    const roleSettingsPaths = ['/app/student/settings', '/app/teacher/settings', '/app/parent/settings', '/app/school/settings', '/app/system/settings']
    const headerTargets = ['/app', '/app/profile', '/app/security', ...roleSettingsPaths]
    const missing = headerTargets.filter((path) => !findRouteByPath(path))
    expect(missing).toEqual([])
  })

  // Sidebar.jsx's "Need help?" card: SystemAdmin -> support inbox, SchoolAdmin
  // -> support inbox, every tenant role -> the shared messaging surface.
  it('every Sidebar.jsx help-card destination resolves to a registered route', () => {
    const helpTargets = ['/app/system/support', '/app/school/support', '/app/messages']
    const missing = helpTargets.filter((path) => !findRouteByPath(path))
    expect(missing).toEqual([])
  })
})
