import { lazy } from 'react'
import type { AppRoute, RouteComponent } from './route.types'
import { ROLES } from '../../features/auth/roles'

// Teacher portal (Phase 9 surface) — real lazy-loaded page modules (Phase 6 split).
// Teacher ONLY. SchoolAdmin must not see, land on, or directly access any
// /app/teacher/* route — SchoolAdmin's administrative capabilities live under
// /app/school/* instead (see routes.school.tsx). A previous pass allowed
// SchoolAdmin on these routes ("an admin may act on teacher surfaces"); that is
// no longer the desired product behavior (SchoolAdmin Teacher-portal removal).
const TeacherDashboardPage = lazy(() => import('../../pages/teacher/dashboard/TeacherDashboardPage'))
const TeacherClassesPage = lazy(() => import('../../pages/teacher/classes/TeacherClassesPage'))
const TeacherSubjectsPage = lazy(() => import('../../pages/teacher/subjects/TeacherSubjectsPage'))
const TeacherUnitsPage = lazy(() => import('../../pages/teacher/units/TeacherUnitsPage'))
const TeacherLessonPage = lazy(() => import('../../pages/teacher/lessons/TeacherLessonPage'))
const TeacherStudentsPage = lazy(() => import('../../pages/teacher/students/TeacherStudentsPage'))
const TeacherQuizzesPage = lazy(() => import('../../pages/teacher/quizzes/TeacherQuizzesPage'))
const TeacherQuizGeneratePage = lazy(() => import('../../pages/teacher/quizzes/TeacherQuizGeneratePage'))
const TeacherSubmissionPage = lazy(() => import('../../pages/teacher/submissions/TeacherSubmissionPage'))
const TeacherHomeworkPage = lazy(() => import('../../pages/teacher/homework/TeacherHomeworkPage'))
const TeacherCompetitionsPage = lazy(() => import('../../pages/teacher/competitions/TeacherCompetitionsPage'))
const TeacherNotificationsPage = lazy(() => import('../../pages/teacher/notifications/TeacherNotificationsPage'))
const TeacherOfficeHoursPage = lazy(() => import('../../pages/teacher/office-hours/TeacherOfficeHoursPage'))
const TeacherCommunitiesPage = lazy(() => import('../../pages/teacher/communities/TeacherCommunitiesPage'))
const TeacherSettingsPage = lazy(() => import('../../pages/teacher/settings/TeacherSettingsPage'))
const RedirectPage = lazy(() => import('../../pages/app/RedirectPage.jsx'))

const r = (path: string, titleKey: string, Component: RouteComponent): AppRoute => ({
  path,
  titleKey,
  requiresAuth: true,
  roles: [ROLES.TEACHER],
  Component,
})

export const teacherRoutes: AppRoute[] = [
  r('/app/teacher', 'teacher.dashboard.title', TeacherDashboardPage),
  r('/app/teacher/classes', 'teacher.classes.title', TeacherClassesPage),
  r('/app/teacher/classes/:classId', 'teacher.classes.details', TeacherClassesPage),
  r('/app/teacher/subjects', 'teacher.subjects.title', TeacherSubjectsPage),
  r('/app/teacher/subjects/:subjectId', 'teacher.curriculum.units', TeacherSubjectsPage),
  r('/app/teacher/units/:unitId', 'teacher.curriculum.lessons', TeacherUnitsPage),
  r('/app/teacher/lessons/:lessonId', 'teacher.curriculum.lessonDetails', TeacherLessonPage),
  r('/app/teacher/students', 'teacher.students.title', TeacherStudentsPage),
  r('/app/teacher/students/:studentId', 'teacher.students.details', TeacherStudentsPage),
  r('/app/teacher/quizzes', 'teacher.quizzes.title', TeacherQuizzesPage),
  r('/app/teacher/quizzes/:quizId', 'teacher.quizzes.details', TeacherQuizzesPage),
  r('/app/teacher/quiz-generate', 'teacher.generate.title', TeacherQuizGeneratePage),
  r('/app/teacher/submissions/:attemptId', 'teacher.grading.title', TeacherSubmissionPage),
  // Homework lifecycle (teacher + school admin). Backend: api/v1/homework/*.
  r('/app/teacher/homework', 'teacher.homework.title', TeacherHomeworkPage),
  r('/app/teacher/homework/new', 'teacher.homework.new', TeacherHomeworkPage),
  r('/app/teacher/homework/:homeworkId', 'teacher.homework.details', TeacherHomeworkPage),
  r('/app/teacher/homework/:homeworkId/submissions', 'teacher.homework.submissions', TeacherHomeworkPage),
  // Competition lifecycle (teacher + school admin). Backend: api/v1/competitions/*.
  r('/app/teacher/competitions', 'teacher.competitions.title', TeacherCompetitionsPage),
  r('/app/teacher/competitions/new', 'teacher.competitions.new', TeacherCompetitionsPage),
  r('/app/teacher/competitions/:competitionId', 'teacher.competitions.details', TeacherCompetitionsPage),
  r('/app/teacher/competitions/:competitionId/submissions', 'teacher.competitions.submissions', TeacherCompetitionsPage),
  r('/app/teacher/competitions/:competitionId/leaderboard', 'teacher.competitions.leaderboard', TeacherCompetitionsPage),
  // D4 (audit-driven fix pass): /assignments is a legacy alias of the real
  // homework lifecycle — redirect to the canonical URL instead of rendering
  // the same page under two different paths (was previously wired to the
  // same component directly, which left the URL bar inconsistent).
  {
    path: '/app/teacher/assignments',
    titleKey: 'nav.assignments',
    requiresAuth: true,
    roles: [ROLES.TEACHER],
    Component: RedirectPage,
    props: { to: '/app/teacher/homework' },
  },
  r('/app/teacher/notifications', 'teacher.notifications.title', TeacherNotificationsPage),
  r('/app/teacher/office-hours', 'teacher.officeHours.title', TeacherOfficeHoursPage),
  r('/app/teacher/communities', 'teacher.communities.title', TeacherCommunitiesPage),
  r('/app/teacher/settings', 'teacher.settings.title', TeacherSettingsPage),
]
