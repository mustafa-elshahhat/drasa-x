import TeacherDashboardPage from './dashboard/TeacherDashboardPage'
import TeacherClassesPage from './classes/TeacherClassesPage'
import TeacherSubjectsPage from './subjects/TeacherSubjectsPage'
import TeacherUnitsPage from './units/TeacherUnitsPage'
import TeacherLessonPage from './lessons/TeacherLessonPage'
import TeacherStudentsPage from './students/TeacherStudentsPage'
import TeacherQuizzesPage from './quizzes/TeacherQuizzesPage'
import TeacherQuizGeneratePage from './quizzes/TeacherQuizGeneratePage'
import TeacherSubmissionPage from './submissions/TeacherSubmissionPage'
import TeacherNotificationsPage from './notifications/TeacherNotificationsPage'
import TeacherOfficeHoursPage from './office-hours/TeacherOfficeHoursPage'
import TeacherSettingsPage from './settings/TeacherSettingsPage'

// TEMPORARY compatibility facade (Phase 6 split). The teacher dispatcher was
// split into the real, lazily-loaded page modules above; routes.teacher.tsx
// loads them directly. Kept only for the legacy view-based test harness.
const VIEWS = {
  classes: TeacherClassesPage,
  subjects: TeacherSubjectsPage,
  units: TeacherUnitsPage,
  lesson: TeacherLessonPage,
  students: TeacherStudentsPage,
  quizzes: TeacherQuizzesPage,
  'quiz-generate': TeacherQuizGeneratePage,
  submission: TeacherSubmissionPage,
  notifications: TeacherNotificationsPage,
  'office-hours': TeacherOfficeHoursPage,
  settings: TeacherSettingsPage,
}

export default function TeacherPortalPage({ view }) {
  const Page = VIEWS[view] || TeacherDashboardPage
  return <Page />
}
