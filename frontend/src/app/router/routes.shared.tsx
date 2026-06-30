import { lazy } from 'react'
import type { AppRoute } from './route.types'
import { PERMISSIONS } from '../../features/auth/roles'

// Authenticated shell + account surfaces, and the shared communication surfaces
// available to any authenticated tenant role (Phase 13).
const AppHome = lazy(() => import('../../pages/app/AppHome.jsx'))
const ProfilePage = lazy(() => import('../../pages/app/ProfilePage.jsx'))
const SecurityPage = lazy(() => import('../../pages/app/SecurityPage.jsx'))
// Communication surfaces (Phase 8 split) — real lazy page modules.
const CommunicationNotificationsPage = lazy(() => import('../../pages/app/communication/CommunicationNotificationsPage'))
const CommunicationPreferencesPage = lazy(() => import('../../pages/app/communication/CommunicationPreferencesPage'))
const CommunicationMessagesPage = lazy(() => import('../../pages/app/communication/CommunicationMessagesPage'))
const CommunicationThreadPage = lazy(() => import('../../pages/app/communication/CommunicationThreadPage'))

export const sharedRoutes: AppRoute[] = [
  // ---- Authenticated shell ----
  { path: '/app', titleKey: 'nav.dashboard', requiresAuth: true, Component: AppHome },
  {
    path: '/app/profile',
    titleKey: 'nav.profile',
    requiresAuth: true,
    permission: PERMISSIONS.MANAGE_OWN_ACCOUNT,
    Component: ProfilePage,
  },
  {
    path: '/app/security',
    titleKey: 'nav.security',
    requiresAuth: true,
    permission: PERMISSIONS.MANAGE_OWN_ACCOUNT,
    Component: SecurityPage,
  },

  // ---- Shared communication surfaces (Phase 13) — any authenticated tenant role ----
  { path: '/app/notifications', titleKey: 'notifications.title', requiresAuth: true, Component: CommunicationNotificationsPage },
  { path: '/app/notifications/preferences', titleKey: 'notifications.preferencesTitle', requiresAuth: true, Component: CommunicationPreferencesPage },
  { path: '/app/messages', titleKey: 'messages.title', requiresAuth: true, Component: CommunicationMessagesPage },
  { path: '/app/messages/:conversationId', titleKey: 'messages.thread', requiresAuth: true, Component: CommunicationThreadPage },
]
