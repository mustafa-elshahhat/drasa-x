// =============================================================================
// Central route registry (Phase 7 §6). Every route declares metadata —
// path, title, auth requirement, allowed roles/permission, nav visibility, and a
// lazily-loaded component — so routing, breadcrumbs, navigation, and guards all
// read from ONE source of truth. Lazy imports keep each area out of the initial
// bundle (Phase 7 §21).
// =============================================================================
import { lazy } from 'react'
import { ROLES, PERMISSIONS } from '../../features/auth/roles'

// --- Lazy page components (code-split per route) ---------------------------
// Public marketing pages (Phase 20 frontend unification) render inside the
// light-themed PublicLayout; tagged with `layout: 'public'` below.
const HomePage = lazy(() => import('../../pages/public/HomePage.jsx'))
const RequestDemoPage = lazy(() => import('../../pages/public/RequestDemoPage.jsx'))
const EventsPage = lazy(() => import('../../pages/public/EventsPage.jsx'))
const ActivitiesPage = lazy(() => import('../../pages/public/ActivitiesPage.jsx'))
const NewsPage = lazy(() => import('../../pages/public/NewsPage.jsx'))
const LoginPage = lazy(() => import('../../pages/auth/LoginPage.jsx'))
const AppHome = lazy(() => import('../../pages/app/AppHome.jsx'))
const ProfilePage = lazy(() => import('../../pages/app/ProfilePage.jsx'))
const SecurityPage = lazy(() => import('../../pages/app/SecurityPage.jsx'))
const StudentPortalPage = lazy(() => import('../../pages/student/StudentPortalPage.jsx'))
const TeacherPortalPage = lazy(() => import('../../pages/teacher/TeacherPortalPage.jsx'))
const ParentPortalPage = lazy(() => import('../../pages/parent/ParentPortalPage.jsx'))
const SchoolAdminPortalPage = lazy(() => import('../../pages/school/SchoolAdminPortalPage.jsx'))
const SystemAdminPortalPage = lazy(() => import('../../pages/system/SystemAdminPortalPage.jsx'))
const CommunicationPage = lazy(() => import('../../pages/app/CommunicationPage.jsx'))
const VisionStaffPage = lazy(() => import('../../pages/vision/VisionStaffPage.jsx'))
const StudentVisionPage = lazy(() => import('../../pages/vision/StudentVisionPage.jsx'))
const ParentVisionPage = lazy(() => import('../../pages/vision/ParentVisionPage.jsx'))

/**
 * @typedef {Object} AppRoute
 * @property {string} path
 * @property {string} [titleKey]      i18n key for the page/breadcrumb title
 * @property {boolean} [requiresAuth]
 * @property {boolean} [anonymousOnly]
 * @property {string} [layout]        'public' = render inside the marketing PublicLayout
 * @property {string[]} [roles]       allowed roles (undefined = any authenticated)
 * @property {string} [permission]    required frontend permission
 * @property {React.LazyExoticComponent} [Component]
 * @property {Object} [props]         static props passed to the component
 */

/** @type {AppRoute[]} */
export const ROUTES = [
  // ---- Public marketing (Phase 20) — rendered inside PublicLayout ----
  { path: '/', titleKey: 'app.name', layout: 'public', Component: HomePage },
  { path: '/request-demo', titleKey: 'public.requestDemo.title', layout: 'public', Component: RequestDemoPage },
  { path: '/events', titleKey: 'public.events.title', layout: 'public', Component: EventsPage },
  { path: '/activities', titleKey: 'public.activities.title', layout: 'public', Component: ActivitiesPage },
  { path: '/news', titleKey: 'public.news.title', layout: 'public', Component: NewsPage },

  // ---- Auth ----
  { path: '/login', titleKey: 'auth.title', anonymousOnly: true, Component: LoginPage },

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
  { path: '/app/notifications', titleKey: 'notifications.title', requiresAuth: true, Component: CommunicationPage, props: { view: 'notifications' } },
  { path: '/app/notifications/preferences', titleKey: 'notifications.preferencesTitle', requiresAuth: true, Component: CommunicationPage, props: { view: 'preferences' } },
  { path: '/app/messages', titleKey: 'messages.title', requiresAuth: true, Component: CommunicationPage, props: { view: 'messages' } },
  { path: '/app/messages/:conversationId', titleKey: 'messages.thread', requiresAuth: true, Component: CommunicationPage, props: { view: 'thread' } },

  // ---- Computer-vision attendance + engagement (Phase 15) ----
  { path: '/app/vision', titleKey: 'vision.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: VisionStaffPage },
  { path: '/app/vision/sessions/:sessionId', titleKey: 'vision.sessionTitleFallback', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: VisionStaffPage },
  { path: '/app/student/vision', titleKey: 'vision.studentTitle', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentVisionPage },
  { path: '/app/parent/children/:childId/vision', titleKey: 'vision.parentTitle', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentVisionPage },

  // ---- Student portal (Phase 8) ----
  {
    path: '/app/student',
    titleKey: 'student.dashboard.title',
    requiresAuth: true,
    roles: [ROLES.STUDENT],
    Component: StudentPortalPage,
    props: { view: 'dashboard' },
  },
  { path: '/app/student/subjects', titleKey: 'student.subjects.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'subjects' } },
  { path: '/app/student/subjects/:subjectId', titleKey: 'student.subjects.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'subject-details' } },
  { path: '/app/student/units', titleKey: 'student.units.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'units' } },
  { path: '/app/student/units/:unitId', titleKey: 'student.units.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'units' } },
  { path: '/app/student/lessons', titleKey: 'student.lessons.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'lessons' } },
  { path: '/app/student/lessons/:lessonId', titleKey: 'student.lessons.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'lessons' } },
  { path: '/app/student/materials/:materialId', titleKey: 'student.materials.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'materials' } },
  { path: '/app/student/comments/:materialId', titleKey: 'student.comments.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'materials' } },
  { path: '/app/student/homework', titleKey: 'student.homework.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'homework' } },
  { path: '/app/student/homework/:homeworkId', titleKey: 'student.homework.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'homework' } },
  { path: '/app/student/quizzes', titleKey: 'student.quizzes.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'quizzes' } },
  { path: '/app/student/quizzes/:quizId', titleKey: 'student.quizzes.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'quizzes' } },
  { path: '/app/student/quiz-attempts/:attemptId', titleKey: 'student.quizzes.attempt', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'quiz-attempt' } },
  { path: '/app/student/quiz-attempts/:attemptId/result', titleKey: 'student.quizzes.result', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'quiz-result' } },
  { path: '/app/student/ai-tutor', titleKey: 'student.tutor.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'tutor' } },
  { path: '/app/student/recommendations', titleKey: 'student.recommendations.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'recommendations' } },
  { path: '/app/student/progress', titleKey: 'student.progress.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'progress' } },
  { path: '/app/student/attendance', titleKey: 'student.attendance.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'attendance' } },
  { path: '/app/student/engagement', titleKey: 'student.engagement.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'engagement' } },
  { path: '/app/student/communities', titleKey: 'student.communities.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'communities' } },
  { path: '/app/student/communities/:communityId', titleKey: 'student.communities.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'communities' } },
  { path: '/app/student/competitions', titleKey: 'student.competitions.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'competitions' } },
  { path: '/app/student/competitions/:competitionId', titleKey: 'student.competitions.details', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'competitions' } },
  { path: '/app/student/leaderboard/:competitionId', titleKey: 'student.leaderboard.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'competitions' } },
  { path: '/app/student/office-hours', titleKey: 'student.officeHours.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'office-hours' } },
  { path: '/app/student/notifications', titleKey: 'student.notifications.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'notifications' } },
  { path: '/app/student/announcements', titleKey: 'student.announcements.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'announcements' } },
  { path: '/app/student/suggestions', titleKey: 'student.suggestions.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'suggestions' } },
  { path: '/app/student/badges', titleKey: 'student.badges.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'badges' } },
  { path: '/app/student/streaks', titleKey: 'student.streaks.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'badges' } },
  { path: '/app/student/settings', titleKey: 'student.settings.title', requiresAuth: true, roles: [ROLES.STUDENT], Component: StudentPortalPage, props: { view: 'settings' } },
  { path: '/app/student/profile', titleKey: 'nav.profile', requiresAuth: true, roles: [ROLES.STUDENT], Component: ProfilePage },

  // ---- Teacher portal (Phase 9) ----
  { path: '/app/teacher', titleKey: 'teacher.dashboard.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'dashboard' } },
  { path: '/app/teacher/classes', titleKey: 'teacher.classes.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'classes' } },
  { path: '/app/teacher/classes/:classId', titleKey: 'teacher.classes.details', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'classes' } },
  { path: '/app/teacher/subjects', titleKey: 'teacher.subjects.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'subjects' } },
  { path: '/app/teacher/subjects/:subjectId', titleKey: 'teacher.curriculum.units', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'subjects' } },
  { path: '/app/teacher/units/:unitId', titleKey: 'teacher.curriculum.lessons', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'units' } },
  { path: '/app/teacher/lessons/:lessonId', titleKey: 'teacher.curriculum.lessonDetails', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'lesson' } },
  { path: '/app/teacher/students', titleKey: 'teacher.students.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'students' } },
  { path: '/app/teacher/students/:studentId', titleKey: 'teacher.students.details', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'students' } },
  { path: '/app/teacher/quizzes', titleKey: 'teacher.quizzes.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'quizzes' } },
  { path: '/app/teacher/quizzes/:quizId', titleKey: 'teacher.quizzes.details', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'quizzes' } },
  { path: '/app/teacher/quiz-generate', titleKey: 'teacher.generate.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'quiz-generate' } },
  { path: '/app/teacher/submissions/:attemptId', titleKey: 'teacher.grading.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'submission' } },
  { path: '/app/teacher/assignments', titleKey: 'nav.assignments', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'quizzes' } },
  { path: '/app/teacher/notifications', titleKey: 'teacher.notifications.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'notifications' } },
  { path: '/app/teacher/office-hours', titleKey: 'teacher.officeHours.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'office-hours' } },
  { path: '/app/teacher/settings', titleKey: 'teacher.settings.title', requiresAuth: true, roles: [ROLES.TEACHER, ROLES.SCHOOL_ADMIN], Component: TeacherPortalPage, props: { view: 'settings' } },

  // ---- Parent portal (Phase 10) ----
  { path: '/app/parent', titleKey: 'parent.dashboard.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'dashboard' } },
  { path: '/app/parent/children', titleKey: 'parent.children.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'children' } },
  { path: '/app/parent/children/:childId', titleKey: 'parent.child.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'child' } },
  { path: '/app/parent/children/:childId/progress', titleKey: 'parent.progress.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'child-progress' } },
  { path: '/app/parent/children/:childId/attendance', titleKey: 'parent.attendance.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'child-attendance' } },
  { path: '/app/parent/children/:childId/points', titleKey: 'parent.points.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'child-points' } },
  { path: '/app/parent/documents', titleKey: 'parent.documents.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'documents' } },
  { path: '/app/parent/conversations', titleKey: 'parent.conversations.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'conversations' } },
  { path: '/app/parent/notifications', titleKey: 'parent.notifications.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'notifications' } },
  { path: '/app/parent/announcements', titleKey: 'parent.announcements.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'announcements' } },
  { path: '/app/parent/settings', titleKey: 'parent.settings.title', requiresAuth: true, roles: [ROLES.PARENT], Component: ParentPortalPage, props: { view: 'settings' } },
  { path: '/app/parent/profile', titleKey: 'nav.profile', requiresAuth: true, roles: [ROLES.PARENT], Component: ProfilePage },

  // ---- School Admin portal (Phase 11) ----
  { path: '/app/school', titleKey: 'school.pages.dashboard.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'dashboard' } },
  { path: '/app/school/profile', titleKey: 'school.pages.profile.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'profile' } },
  { path: '/app/school/academic-years', titleKey: 'school.pages.academicYears.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'academic-years' } },
  { path: '/app/school/terms', titleKey: 'school.pages.terms.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'terms' } },
  { path: '/app/school/grades', titleKey: 'school.pages.grades.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'grades' } },
  { path: '/app/school/classes', titleKey: 'school.pages.classes.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'classes' } },
  { path: '/app/school/subjects', titleKey: 'school.pages.subjects.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'subjects' } },
  { path: '/app/school/users', titleKey: 'school.pages.users.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'users' } },
  { path: '/app/school/students', titleKey: 'school.pages.students.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'students' } },
  { path: '/app/school/teachers', titleKey: 'school.pages.teachers.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'teachers' } },
  { path: '/app/school/parents', titleKey: 'school.pages.parents.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'parents' } },
  { path: '/app/school/relationships', titleKey: 'school.pages.relationships.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'relationships' } },
  { path: '/app/school/teacher-assignments', titleKey: 'school.pages.teacherAssignments.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'teacher-assignments' } },
  { path: '/app/school/curriculum', titleKey: 'school.pages.curriculum.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'curriculum' } },
  { path: '/app/school/announcements', titleKey: 'school.pages.announcements.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'announcements' } },
  { path: '/app/school/document-requests', titleKey: 'school.pages.documentRequests.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'document-requests' } },
  { path: '/app/school/communities', titleKey: 'school.pages.communities.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'communities' } },
  { path: '/app/school/competitions', titleKey: 'school.pages.competitions.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'competitions' } },
  { path: '/app/school/reports', titleKey: 'school.pages.reports.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'reports' } },
  { path: '/app/school/ai-usage', titleKey: 'school.pages.aiUsage.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'ai-usage' } },
  { path: '/app/school/storage', titleKey: 'school.pages.storage.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'storage' } },
  { path: '/app/school/subscription', titleKey: 'school.pages.subscription.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'subscription' } },
  { path: '/app/school/support', titleKey: 'school.pages.support.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'support' } },
  { path: '/app/school/audit-log', titleKey: 'school.pages.audit.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'audit-log' } },
  { path: '/app/school/settings', titleKey: 'school.pages.settings.title', requiresAuth: true, roles: [ROLES.SCHOOL_ADMIN], Component: SchoolAdminPortalPage, props: { view: 'settings' } },

  // ---- System Admin (platform) portal (Phase 12) ----
  { path: '/app/system', titleKey: 'system.pages.dashboard.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'dashboard' } },
  { path: '/app/system/tenants', titleKey: 'system.pages.tenants.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'tenants' } },
  { path: '/app/system/tenants/:tenantId', titleKey: 'system.pages.tenantDetails.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'tenant-details' } },
  { path: '/app/system/onboarding', titleKey: 'system.pages.onboarding.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'onboarding' } },
  { path: '/app/system/plans', titleKey: 'system.pages.plans.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'plans' } },
  { path: '/app/system/subscriptions', titleKey: 'system.pages.subscriptions.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'subscriptions' } },
  { path: '/app/system/usage', titleKey: 'system.pages.usage.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'usage' } },
  { path: '/app/system/ai-usage', titleKey: 'system.pages.aiUsage.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'ai-usage' } },
  { path: '/app/system/storage', titleKey: 'system.pages.storage.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'storage' } },
  { path: '/app/system/support', titleKey: 'system.pages.support.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'support' } },
  { path: '/app/system/feature-flags', titleKey: 'system.pages.featureFlags.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'feature-flags' } },
  { path: '/app/system/announcements', titleKey: 'system.pages.announcements.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'announcements' } },
  { path: '/app/system/health', titleKey: 'system.pages.health.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'health' } },
  { path: '/app/system/errors', titleKey: 'system.pages.errorMonitoring.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'errors' } },
  { path: '/app/system/audit', titleKey: 'system.pages.audit.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'audit' } },
  { path: '/app/system/security', titleKey: 'system.pages.security.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'security' } },
  { path: '/app/system/backups', titleKey: 'system.pages.backups.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'backups' } },
  { path: '/app/system/settings', titleKey: 'system.pages.settings.title', requiresAuth: true, roles: [ROLES.SYSTEM_ADMIN], Component: SystemAdminPortalPage, props: { view: 'settings' } },
]

/** Look up route metadata by exact path (used by breadcrumbs/title). */
export function findRouteByPath(path) {
  return ROUTES.find((r) => r.path === path) || null
}
