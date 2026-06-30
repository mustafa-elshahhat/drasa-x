import { lazy } from 'react'
import type { AppRoute, RouteComponent } from './route.types'
import { ROLES } from '../../features/auth/roles'

// Student portal (Phase 8 surface) — now real lazy-loaded page modules (Phase 5
// split). No route depends on the old StudentPortalPage view-dispatcher.
const StudentDashboardPage = lazy(() => import('../../pages/student/dashboard/StudentDashboardPage'))
const StudentSubjectsPage = lazy(() => import('../../pages/student/subjects/StudentSubjectsPage'))
const StudentSubjectDetailsPage = lazy(() => import('../../pages/student/subjects/StudentSubjectDetailsPage'))
const StudentUnitsPage = lazy(() => import('../../pages/student/units/StudentUnitsPage'))
const StudentLessonsPage = lazy(() => import('../../pages/student/lessons/StudentLessonsPage'))
const StudentMaterialsPage = lazy(() => import('../../pages/student/materials/StudentMaterialsPage'))
const StudentHomeworkPage = lazy(() => import('../../pages/student/homework/StudentHomeworkPage'))
const StudentQuizzesPage = lazy(() => import('../../pages/student/quizzes/StudentQuizzesPage'))
const StudentQuizAttemptPage = lazy(() => import('../../pages/student/quizzes/StudentQuizAttemptPage'))
const StudentQuizResultPage = lazy(() => import('../../pages/student/quizzes/StudentQuizResultPage'))
const StudentTutorPage = lazy(() => import('../../pages/student/tutor/StudentTutorPage'))
const StudentProgressPage = lazy(() => import('../../pages/student/progress/StudentProgressPage'))
const StudentAttendancePage = lazy(() => import('../../pages/student/attendance/StudentAttendancePage'))
const StudentCommunitiesPage = lazy(() => import('../../pages/student/communities/StudentCommunitiesPage'))
const StudentCompetitionsPage = lazy(() => import('../../pages/student/competitions/StudentCompetitionsPage'))
const StudentOfficeHoursPage = lazy(() => import('../../pages/student/office-hours/StudentOfficeHoursPage'))
const StudentNotificationsPage = lazy(() => import('../../pages/student/notifications/StudentNotificationsPage'))
const StudentAnnouncementsPage = lazy(() => import('../../pages/student/announcements/StudentAnnouncementsPage'))
const StudentSuggestionsPage = lazy(() => import('../../pages/student/suggestions/StudentSuggestionsPage'))
const StudentBadgesPage = lazy(() => import('../../pages/student/badges/StudentBadgesPage'))
const StudentSettingsPage = lazy(() => import('../../pages/student/settings/StudentSettingsPage'))
const ProfilePage = lazy(() => import('../../pages/app/ProfilePage.jsx'))

const r = (
  path: string,
  titleKey: string,
  Component: RouteComponent,
  props?: Record<string, unknown>,
): AppRoute => ({ path, titleKey, requiresAuth: true, roles: [ROLES.STUDENT], Component, props })

export const studentRoutes: AppRoute[] = [
  r('/app/student', 'student.dashboard.title', StudentDashboardPage),
  r('/app/student/subjects', 'student.subjects.title', StudentSubjectsPage),
  r('/app/student/subjects/:subjectId', 'student.subjects.details', StudentSubjectDetailsPage),
  r('/app/student/units', 'student.units.title', StudentUnitsPage),
  r('/app/student/units/:unitId', 'student.units.details', StudentUnitsPage),
  r('/app/student/lessons', 'student.lessons.title', StudentLessonsPage),
  r('/app/student/lessons/:lessonId', 'student.lessons.details', StudentLessonsPage),
  r('/app/student/materials/:materialId', 'student.materials.details', StudentMaterialsPage),
  r('/app/student/comments/:materialId', 'student.comments.title', StudentMaterialsPage),
  r('/app/student/homework', 'student.homework.title', StudentHomeworkPage),
  r('/app/student/homework/:homeworkId', 'student.homework.details', StudentHomeworkPage),
  r('/app/student/quizzes', 'student.quizzes.title', StudentQuizzesPage),
  r('/app/student/quizzes/:quizId', 'student.quizzes.details', StudentQuizzesPage),
  r('/app/student/quiz-attempts/:attemptId', 'student.quizzes.attempt', StudentQuizAttemptPage),
  r('/app/student/quiz-attempts/:attemptId/result', 'student.quizzes.result', StudentQuizResultPage),
  r('/app/student/ai-tutor', 'student.tutor.title', StudentTutorPage),
  r('/app/student/recommendations', 'student.recommendations.title', StudentProgressPage, { mode: 'recommendations' }),
  r('/app/student/progress', 'student.progress.title', StudentProgressPage, { mode: 'progress' }),
  r('/app/student/attendance', 'student.attendance.title', StudentAttendancePage),
  r('/app/student/engagement', 'student.engagement.title', StudentProgressPage, { mode: 'engagement' }),
  r('/app/student/communities', 'student.communities.title', StudentCommunitiesPage),
  r('/app/student/communities/:communityId', 'student.communities.details', StudentCommunitiesPage),
  r('/app/student/competitions', 'student.competitions.title', StudentCompetitionsPage),
  r('/app/student/competitions/:competitionId', 'student.competitions.details', StudentCompetitionsPage),
  r('/app/student/leaderboard/:competitionId', 'student.leaderboard.title', StudentCompetitionsPage),
  r('/app/student/office-hours', 'student.officeHours.title', StudentOfficeHoursPage),
  r('/app/student/notifications', 'student.notifications.title', StudentNotificationsPage),
  r('/app/student/announcements', 'student.announcements.title', StudentAnnouncementsPage),
  r('/app/student/suggestions', 'student.suggestions.title', StudentSuggestionsPage),
  r('/app/student/badges', 'student.badges.title', StudentBadgesPage),
  r('/app/student/streaks', 'student.streaks.title', StudentBadgesPage),
  r('/app/student/settings', 'student.settings.title', StudentSettingsPage),
  { path: '/app/student/profile', titleKey: 'nav.profile', requiresAuth: true, roles: [ROLES.STUDENT], Component: ProfilePage },
]
