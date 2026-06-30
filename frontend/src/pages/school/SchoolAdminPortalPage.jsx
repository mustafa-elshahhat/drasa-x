import SchoolDashboardPage from './dashboard/SchoolDashboardPage'
import SchoolProfilePage from './profile/SchoolProfilePage'
import SchoolSubscriptionPage from './subscription/SchoolSubscriptionPage'
import SchoolStoragePage from './storage/SchoolStoragePage'
import SchoolAcademicYearsPage from './academic-years/SchoolAcademicYearsPage'
import SchoolTermsPage from './terms/SchoolTermsPage'
import SchoolGradesPage from './grades/SchoolGradesPage'
import SchoolClassesPage from './classes/SchoolClassesPage'
import SchoolSubjectsPage from './subjects/SchoolSubjectsPage'
import SchoolUsersPage from './users/SchoolUsersPage'
import SchoolRelationshipsPage from './relationships/SchoolRelationshipsPage'
import SchoolTeacherAssignmentsPage from './teacher-assignments/SchoolTeacherAssignmentsPage'
import SchoolCurriculumPage from './curriculum/SchoolCurriculumPage'
import SchoolAnnouncementsPage from './announcements/SchoolAnnouncementsPage'
import SchoolDocumentRequestsPage from './document-requests/SchoolDocumentRequestsPage'
import SchoolCommunitiesPage from './communities/SchoolCommunitiesPage'
import SchoolCompetitionsPage from './competitions/SchoolCompetitionsPage'
import SchoolReportsPage from './reports/SchoolReportsPage'
import SchoolAiUsagePage from './ai-usage/SchoolAiUsagePage'
import SchoolSupportPage from './support/SchoolSupportPage'
import SchoolAuditPage from './audit-log/SchoolAuditPage'
import SchoolSettingsPage from './settings/SchoolSettingsPage'

// TEMPORARY compatibility facade (Phase 7 split). The school-admin dispatcher was
// split into the real, lazily-loaded page modules above; routes.school.tsx loads
// them directly. Kept only for the legacy view-based test harness.
const VIEWS = {
  profile: SchoolProfilePage,
  'academic-years': SchoolAcademicYearsPage,
  terms: SchoolTermsPage,
  grades: SchoolGradesPage,
  classes: SchoolClassesPage,
  subjects: SchoolSubjectsPage,
  relationships: SchoolRelationshipsPage,
  'teacher-assignments': SchoolTeacherAssignmentsPage,
  curriculum: SchoolCurriculumPage,
  announcements: SchoolAnnouncementsPage,
  'document-requests': SchoolDocumentRequestsPage,
  communities: SchoolCommunitiesPage,
  competitions: SchoolCompetitionsPage,
  reports: SchoolReportsPage,
  'ai-usage': SchoolAiUsagePage,
  storage: SchoolStoragePage,
  subscription: SchoolSubscriptionPage,
  support: SchoolSupportPage,
  'audit-log': SchoolAuditPage,
  settings: SchoolSettingsPage,
}

export default function SchoolAdminPortalPage({ view }) {
  if (view === 'users') return <SchoolUsersPage role={null} canCreate />
  if (view === 'students') return <SchoolUsersPage role="Student" />
  if (view === 'teachers') return <SchoolUsersPage role="Teacher" />
  if (view === 'parents') return <SchoolUsersPage role="Parent" />
  const Page = VIEWS[view] || SchoolDashboardPage
  return <Page />
}
