import { lazy } from 'react'
import type { AppRoute, RouteComponent } from './route.types'
import { ROLES } from '../../features/auth/roles'

// School Admin portal (Phase 11 surface) — real lazy-loaded page modules (Phase 7
// split). Every route stays SchoolAdmin-only.
const SchoolDashboardPage = lazy(() => import('../../pages/school/dashboard/SchoolDashboardPage'))
const SchoolProfilePage = lazy(() => import('../../pages/school/profile/SchoolProfilePage'))
const SchoolSubscriptionPage = lazy(() => import('../../pages/school/subscription/SchoolSubscriptionPage'))
const SchoolStoragePage = lazy(() => import('../../pages/school/storage/SchoolStoragePage'))
const SchoolAcademicYearsPage = lazy(() => import('../../pages/school/academic-years/SchoolAcademicYearsPage'))
const SchoolTermsPage = lazy(() => import('../../pages/school/terms/SchoolTermsPage'))
const SchoolGradesPage = lazy(() => import('../../pages/school/grades/SchoolGradesPage'))
const SchoolClassesPage = lazy(() => import('../../pages/school/classes/SchoolClassesPage'))
const SchoolClassDetailPage = lazy(() => import('../../pages/school/classes/SchoolClassDetailPage'))
const SchoolSubjectsPage = lazy(() => import('../../pages/school/subjects/SchoolSubjectsPage'))
const SchoolUsersPage = lazy(() => import('../../pages/school/users/SchoolUsersPage'))
const SchoolUserDetailPage = lazy(() => import('../../pages/school/users/SchoolUserDetailPage'))
const SchoolRelationshipsPage = lazy(() => import('../../pages/school/relationships/SchoolRelationshipsPage'))
const SchoolTeacherAssignmentsPage = lazy(() => import('../../pages/school/teacher-assignments/SchoolTeacherAssignmentsPage'))
const SchoolCurriculumPage = lazy(() => import('../../pages/school/curriculum/SchoolCurriculumPage'))
const SchoolAnnouncementsPage = lazy(() => import('../../pages/school/announcements/SchoolAnnouncementsPage'))
const SchoolDocumentRequestsPage = lazy(() => import('../../pages/school/document-requests/SchoolDocumentRequestsPage'))
const SchoolCommunitiesPage = lazy(() => import('../../pages/school/communities/SchoolCommunitiesPage'))
const SchoolCompetitionsPage = lazy(() => import('../../pages/school/competitions/SchoolCompetitionsPage'))
const SchoolSuggestionsPage = lazy(() => import('../../pages/school/suggestions/SchoolSuggestionsPage'))
const SchoolGamificationRulesPage = lazy(() => import('../../pages/school/gamification-rules/SchoolGamificationRulesPage'))
const SchoolReportsPage = lazy(() => import('../../pages/school/reports/SchoolReportsPage'))
const SchoolAiUsagePage = lazy(() => import('../../pages/school/ai-usage/SchoolAiUsagePage'))
const SchoolSupportPage = lazy(() => import('../../pages/school/support/SchoolSupportPage'))
const SchoolAuditPage = lazy(() => import('../../pages/school/audit-log/SchoolAuditPage'))
const SchoolSettingsPage = lazy(() => import('../../pages/school/settings/SchoolSettingsPage'))

const r = (
  path: string,
  titleKey: string,
  Component: RouteComponent,
  props?: Record<string, unknown>,
): AppRoute => ({ path, titleKey, requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component, props })

export const schoolRoutes: AppRoute[] = [
  r('/app/school', 'school.pages.dashboard.title', SchoolDashboardPage),
  r('/app/school/profile', 'school.pages.profile.title', SchoolProfilePage),
  r('/app/school/academic-years', 'school.pages.academicYears.title', SchoolAcademicYearsPage),
  r('/app/school/terms', 'school.pages.terms.title', SchoolTermsPage),
  r('/app/school/grades', 'school.pages.grades.title', SchoolGradesPage),
  r('/app/school/classes', 'school.pages.classes.title', SchoolClassesPage),
  r('/app/school/classes/:classId', 'school.pages.classDetails.title', SchoolClassDetailPage),
  r('/app/school/subjects', 'school.pages.subjects.title', SchoolSubjectsPage),
  r('/app/school/users', 'school.pages.users.title', SchoolUsersPage, { role: null, canCreate: true }),
  r('/app/school/users/:userId', 'school.pages.userDetails.title', SchoolUserDetailPage),
  r('/app/school/students', 'school.pages.students.title', SchoolUsersPage, { role: 'Student' }),
  r('/app/school/students/:userId', 'school.pages.userDetails.title', SchoolUserDetailPage),
  r('/app/school/teachers', 'school.pages.teachers.title', SchoolUsersPage, { role: 'Teacher' }),
  r('/app/school/teachers/:userId', 'school.pages.userDetails.title', SchoolUserDetailPage),
  r('/app/school/parents', 'school.pages.parents.title', SchoolUsersPage, { role: 'Parent' }),
  r('/app/school/parents/:userId', 'school.pages.userDetails.title', SchoolUserDetailPage),
  r('/app/school/relationships', 'school.pages.relationships.title', SchoolRelationshipsPage),
  r('/app/school/teacher-assignments', 'school.pages.teacherAssignments.title', SchoolTeacherAssignmentsPage),
  r('/app/school/curriculum', 'school.pages.curriculum.title', SchoolCurriculumPage),
  r('/app/school/announcements', 'school.pages.announcements.title', SchoolAnnouncementsPage),
  r('/app/school/document-requests', 'school.pages.documentRequests.title', SchoolDocumentRequestsPage),
  r('/app/school/communities', 'school.pages.communities.title', SchoolCommunitiesPage),
  r('/app/school/competitions', 'school.pages.competitions.title', SchoolCompetitionsPage),
  r('/app/school/suggestions', 'school.pages.suggestions.title', SchoolSuggestionsPage),
  r('/app/school/gamification-rules', 'school.pages.gamificationRules.title', SchoolGamificationRulesPage),
  r('/app/school/reports', 'school.pages.reports.title', SchoolReportsPage),
  r('/app/school/ai-usage', 'school.pages.aiUsage.title', SchoolAiUsagePage),
  r('/app/school/storage', 'school.pages.storage.title', SchoolStoragePage),
  r('/app/school/subscription', 'school.pages.subscription.title', SchoolSubscriptionPage),
  r('/app/school/support', 'school.pages.support.title', SchoolSupportPage),
  r('/app/school/audit-log', 'school.pages.audit.title', SchoolAuditPage),
  r('/app/school/settings', 'school.pages.settings.title', SchoolSettingsPage),
]
