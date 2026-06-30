import { lazy } from 'react'
import type { AppRoute, RouteComponent } from './route.types'
import { ROLES } from '../../features/auth/roles'

// Parent portal (Phase 10 surface) — real lazy-loaded page modules (Phase 6 split).
const ParentDashboardPage = lazy(() => import('../../pages/parent/dashboard/ParentDashboardPage'))
const ParentChildrenPage = lazy(() => import('../../pages/parent/children/ParentChildrenPage'))
const ParentChildOverviewPage = lazy(() => import('../../pages/parent/children/ParentChildOverviewPage'))
const ParentChildProgressPage = lazy(() => import('../../pages/parent/children/ParentChildProgressPage'))
const ParentChildAttendancePage = lazy(() => import('../../pages/parent/children/ParentChildAttendancePage'))
const ParentChildPointsPage = lazy(() => import('../../pages/parent/children/ParentChildPointsPage'))
const ParentDocumentsPage = lazy(() => import('../../pages/parent/documents/ParentDocumentsPage'))
const ParentConversationsPage = lazy(() => import('../../pages/parent/conversations/ParentConversationsPage'))
const ParentNotificationsPage = lazy(() => import('../../pages/parent/notifications/ParentNotificationsPage'))
const ParentAnnouncementsPage = lazy(() => import('../../pages/parent/announcements/ParentAnnouncementsPage'))
const ParentSettingsPage = lazy(() => import('../../pages/parent/settings/ParentSettingsPage'))
const ProfilePage = lazy(() => import('../../pages/app/ProfilePage.jsx'))

const r = (path: string, titleKey: string, Component: RouteComponent): AppRoute => ({
  path,
  titleKey,
  requiresAuth: true,
  roles: [ROLES.PARENT],
  Component,
})

export const parentRoutes: AppRoute[] = [
  r('/app/parent', 'parent.dashboard.title', ParentDashboardPage),
  r('/app/parent/children', 'parent.children.title', ParentChildrenPage),
  r('/app/parent/children/:childId', 'parent.child.title', ParentChildOverviewPage),
  r('/app/parent/children/:childId/progress', 'parent.progress.title', ParentChildProgressPage),
  r('/app/parent/children/:childId/attendance', 'parent.attendance.title', ParentChildAttendancePage),
  r('/app/parent/children/:childId/points', 'parent.points.title', ParentChildPointsPage),
  r('/app/parent/documents', 'parent.documents.title', ParentDocumentsPage),
  r('/app/parent/conversations', 'parent.conversations.title', ParentConversationsPage),
  r('/app/parent/notifications', 'parent.notifications.title', ParentNotificationsPage),
  r('/app/parent/announcements', 'parent.announcements.title', ParentAnnouncementsPage),
  r('/app/parent/settings', 'parent.settings.title', ParentSettingsPage),
  { path: '/app/parent/profile', titleKey: 'nav.profile', requiresAuth: true, roles: [ROLES.PARENT], Component: ProfilePage },
]
