import { lazy } from 'react'
import type { AppRoute, RouteComponent } from './route.types'
import { ROLES } from '../../features/auth/roles'

// Teacher portal (Phase 9 surface) — real lazy-loaded page modules (Phase 6 split).
// Available to Teacher AND SchoolAdmin (an admin may act on teacher surfaces).
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
const TeacherSettingsPage = lazy(() => import('../../pages/teacher/settings/TeacherSettingsPage'))

const r = (path: string, titleKey: string, Component: RouteComponent): AppRoute => ({
  path,
  titleKey,
  requiresAuth: true,
  roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN],
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
  // /assignments now resolves to the real homework lifecycle (no longer a quizzes stand-in).
  r('/app/teacher/assignments', 'nav.assignments', TeacherHomeworkPage),
  r('/app/teacher/notifications', 'teacher.notifications.title', TeacherNotificationsPage),
  r('/app/teacher/office-hours', 'teacher.officeHours.title', TeacherOfficeHoursPage),
  r('/app/teacher/settings', 'teacher.settings.title', TeacherSettingsPage),
]
