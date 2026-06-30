import StudentDashboardPage from './dashboard/StudentDashboardPage'
import StudentSubjectsPage from './subjects/StudentSubjectsPage'
import StudentSubjectDetailsPage from './subjects/StudentSubjectDetailsPage'
import StudentUnitsPage from './units/StudentUnitsPage'
import StudentLessonsPage from './lessons/StudentLessonsPage'
import StudentMaterialsPage from './materials/StudentMaterialsPage'
import StudentHomeworkPage from './homework/StudentHomeworkPage'
import StudentQuizzesPage from './quizzes/StudentQuizzesPage'
import StudentQuizAttemptPage from './quizzes/StudentQuizAttemptPage'
import StudentQuizResultPage from './quizzes/StudentQuizResultPage'
import StudentTutorPage from './tutor/StudentTutorPage'
import StudentProgressPage from './progress/StudentProgressPage'
import StudentAttendancePage from './attendance/StudentAttendancePage'
import StudentCommunitiesPage from './communities/StudentCommunitiesPage'
import StudentCompetitionsPage from './competitions/StudentCompetitionsPage'
import StudentOfficeHoursPage from './office-hours/StudentOfficeHoursPage'
import StudentNotificationsPage from './notifications/StudentNotificationsPage'
import StudentAnnouncementsPage from './announcements/StudentAnnouncementsPage'
import StudentSuggestionsPage from './suggestions/StudentSuggestionsPage'
import StudentBadgesPage from './badges/StudentBadgesPage'
import StudentSettingsPage from './settings/StudentSettingsPage'

// =============================================================================
// TEMPORARY compatibility facade (Phase 5 split).
//
// The 3000+ line student dispatcher was split into the real, lazily-loaded page
// modules above. No production route depends on this facade — routes.student.tsx
// lazy-loads each page module directly. This thin view-mapper is kept ONLY so the
// legacy `StudentPortalPage.test.jsx` (which drives screens by a `view` prop)
// keeps exercising the same behavior against the new modules.
// =============================================================================
const VIEWS = {
  subjects: StudentSubjectsPage,
  'subject-details': StudentSubjectDetailsPage,
  units: StudentUnitsPage,
  lessons: StudentLessonsPage,
  materials: StudentMaterialsPage,
  homework: StudentHomeworkPage,
  quizzes: StudentQuizzesPage,
  'quiz-attempt': StudentQuizAttemptPage,
  'quiz-result': StudentQuizResultPage,
  tutor: StudentTutorPage,
  attendance: StudentAttendancePage,
  communities: StudentCommunitiesPage,
  competitions: StudentCompetitionsPage,
  'office-hours': StudentOfficeHoursPage,
  notifications: StudentNotificationsPage,
  announcements: StudentAnnouncementsPage,
  suggestions: StudentSuggestionsPage,
  badges: StudentBadgesPage,
  settings: StudentSettingsPage,
}

export default function StudentPortalPage({ view }) {
  if (view === 'recommendations') return <StudentProgressPage mode="recommendations" />
  if (view === 'engagement') return <StudentProgressPage mode="engagement" />
  if (view === 'progress') return <StudentProgressPage mode="progress" />
  const Page = VIEWS[view] || StudentDashboardPage
  return <Page />
}
