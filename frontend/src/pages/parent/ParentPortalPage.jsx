import ParentDashboardPage from './dashboard/ParentDashboardPage'
import ParentChildrenPage from './children/ParentChildrenPage'
import ParentChildOverviewPage from './children/ParentChildOverviewPage'
import ParentChildProgressPage from './children/ParentChildProgressPage'
import ParentChildAttendancePage from './children/ParentChildAttendancePage'
import ParentChildPointsPage from './children/ParentChildPointsPage'
import ParentDocumentsPage from './documents/ParentDocumentsPage'
import ParentConversationsPage from './conversations/ParentConversationsPage'
import ParentNotificationsPage from './notifications/ParentNotificationsPage'
import ParentAnnouncementsPage from './announcements/ParentAnnouncementsPage'
import ParentSettingsPage from './settings/ParentSettingsPage'

// TEMPORARY compatibility facade (Phase 6 split). The parent dispatcher was split
// into the real, lazily-loaded page modules above; routes.parent.tsx loads them
// directly. Kept only for the legacy view-based test harness.
const VIEWS = {
  children: ParentChildrenPage,
  child: ParentChildOverviewPage,
  'child-progress': ParentChildProgressPage,
  'child-attendance': ParentChildAttendancePage,
  'child-points': ParentChildPointsPage,
  documents: ParentDocumentsPage,
  conversations: ParentConversationsPage,
  notifications: ParentNotificationsPage,
  announcements: ParentAnnouncementsPage,
  settings: ParentSettingsPage,
}

export default function ParentPortalPage({ view }) {
  const Page = VIEWS[view] || ParentDashboardPage
  return <Page />
}
