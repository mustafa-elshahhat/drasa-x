# DerasaX Project Pages and Flows — Source of Truth

Generated from the uploaded `drasa-x-main.zip` by static inspection. This file is intended to be sent to Claude Design together with the Figma `.fig` file. It defines what pages and flows must exist; Figma will define the visual styling.

## 1. Purpose

This Markdown file is the product/page/flow source of truth before rebuilding the presentation prototype. It is not a Flutter spec and not a visual design document. The later Claude Design task should use this report for completeness and the Figma file for UI. No route, role, action, or presentation-critical flow should be omitted.

## 2. Source Materials Inspected

| Source | Type | What it contributed | Confidence |
| --- | --- | --- | --- |
| frontend/src/app/router/routes.js | Frontend route registry | 122 declared routes with title keys, layouts, role guards, and page components | High |
| frontend/src/navigation/navConfig.js | Frontend navigation | 63 sidebar/navigation entries by role | High |
| frontend/src/pages/**/*.jsx | Frontend pages | Public, auth, app, student, teacher, parent, school admin, system admin, communication, vision pages | High |
| frontend/src/features/**/*Api.js | Frontend API clients | Screen-to-API dependency mapping and mutation actions | High |
| frontend/src/features/auth/roles.js / guards.jsx | Auth/roles | 5 backend roles, role home redirects, permissions, route guards | High |
| frontend/src/i18n/locales/en.js and ar.js | Copy/i18n | English/Arabic labels and RTL/LTR support | High |
| frontend/e2e and frontend/src/**/*.test.* | Tests | Route, role, public/auth, a11y and portal smoke coverage signals | Medium |
| backend/src/DerasaX.Api/Controllers/*.cs | Backend APIs | API endpoint capabilities, role policies, backend-supported actions | High |
| ai/app/**/*.py | AI service | RAG, quiz, prediction, analysis, vision internal service capabilities | Medium |
| frontend/README.md, ai/README.md, docker-compose.yml | Docs/config | Unified frontend architecture, backend-only browser API model, AI service role | High |
| Prototype files | Prototype coverage | No `.dc.html`, `cloud-design-prototype-spec`, or prototype package found inside supplied ZIP | High |
| Figma file | Design source | Not supplied in this turn; later Claude Design should inspect the `.fig` directly | High |

## 3. Current Product Architecture Summary

- One active unified React/Vite frontend under `frontend`, serving public marketing, `/login`, and all `/app/**` portals.
- Browser calls only `DerasaX-backend`; frontend README explicitly says the browser never calls the AI service directly.
- Backend is ASP.NET Core with PostgreSQL; controllers under `backend/src/DerasaX.Api/Controllers` expose tenant, school, student, teacher, parent, system-admin, AI, messaging, notification, file, gamification, office-hours, and CV endpoints.
- AI service under `ai` is FastAPI and supports RAG tutoring, quiz generation, prediction/analysis, ingest, and vision inference behind backend mediation.
- Roles found in frontend and backend policy naming: Student, Teacher, Parent, SchoolAdmin, SystemAdmin. Guest/Public is unauthenticated.
- Public routes use `PublicLayout`; authenticated routes use `AppShell`; status pages are standalone.
- No current prototype files were present in this uploaded ZIP, so prototype coverage is listed as not verifiable from this package.

## 4. Roles and Access Model

| Role | Allowed product areas | Home route | Restriction model |
| --- | --- | --- | --- |
| Guest/Public | View marketing home, events, activities, news, request demo, sign in | / | Cannot access `/app/**`; no tenant data. |
| Student | Own learning, subjects, lessons, materials, AI tutor, homework, quizzes, progress, attendance, engagement, communities, competitions, office hours, notifications, suggestions, badges/streaks | /app/student | Own data only. |
| Teacher | Assigned classes/subjects/students, quiz generation/review/publish/assign, submissions/grading, student progress, office hours, CV sessions, notifications | /app/teacher | Teacher-scoped classes/students; some routes shared with School Admin. |
| Parent | Linked children, child progress/attendance/points/vision, documents, conversations, announcements, notifications | /app/parent | Linked-child only. |
| School Admin | Tenant dashboard, users, grades/classes/subjects, parent links, teacher assignments, curriculum, announcements, document requests, reports, AI/storage/subscription/support/audit/settings, teacher surface access | /app/school | Tenant-only administrative scope. |
| System Admin | Platform dashboard, tenants, onboarding, plans/subscriptions, usage, AI/storage, support, feature flags, platform announcements, health/errors/audit/security/backups/settings | /app/system | Platform-wide scope; not tenant member UI except platform management. |

## 5. Complete Route Inventory

Route entries found: **126** total = 122 route-registry entries + 4 status/wildcard routes from `App.jsx`.

| Route | Title key | Access | Layout | Component | View prop | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| / | app.name | Guest/Public | PublicLayout | HomePage | - | frontend/src/pages/public/HomePage.jsx | Implemented route |
| /request-demo | public.requestDemo.title | Guest/Public | PublicLayout | RequestDemoPage | - | frontend/src/pages/public/RequestDemoPage.jsx | Implemented route |
| /events | public.events.title | Guest/Public | PublicLayout | EventsPage | - | frontend/src/pages/public/EventsPage.jsx | Implemented route |
| /activities | public.activities.title | Guest/Public | PublicLayout | ActivitiesPage | - | frontend/src/pages/public/ActivitiesPage.jsx | Implemented route |
| /news | public.news.title | Guest/Public | PublicLayout | NewsPage | - | frontend/src/pages/public/NewsPage.jsx | Implemented route |
| /login | auth.title | Anonymous only | Standalone/Auth | LoginPage | - | frontend/src/pages/auth/LoginPage.jsx | Implemented route |
| /app | nav.dashboard | Any authenticated role | AppShell | AppHome | - | frontend/src/pages/app/AppHome.jsx | Implemented route |
| /app/profile | nav.profile | Any authenticated role | AppShell | ProfilePage | - | frontend/src/pages/app/ProfilePage.jsx | Implemented route |
| /app/security | nav.security | Any authenticated role | AppShell | SecurityPage | - | frontend/src/pages/app/SecurityPage.jsx | Implemented route |
| /app/notifications | notifications.title | Any authenticated role | AppShell | CommunicationPage | notifications | frontend/src/pages/app/CommunicationPage.jsx | Implemented route |
| /app/notifications/preferences | notifications.preferencesTitle | Any authenticated role | AppShell | CommunicationPage | preferences | frontend/src/pages/app/CommunicationPage.jsx | Implemented route |
| /app/messages | messages.title | Any authenticated role | AppShell | CommunicationPage | messages | frontend/src/pages/app/CommunicationPage.jsx | Implemented route |
| /app/messages/:conversationId | messages.thread | Any authenticated role | AppShell | CommunicationPage | thread | frontend/src/pages/app/CommunicationPage.jsx | Implemented route |
| /app/vision | vision.title | Teacher, School Admin | AppShell | VisionStaffPage | - | frontend/src/pages/vision/VisionStaffPage.jsx | Implemented route |
| /app/vision/sessions/:sessionId | vision.sessionTitleFallback | Teacher, School Admin | AppShell | VisionStaffPage | - | frontend/src/pages/vision/VisionStaffPage.jsx | Implemented route |
| /app/student/vision | vision.studentTitle | Student | AppShell | StudentVisionPage | - | frontend/src/pages/vision/StudentVisionPage.jsx | Implemented route |
| /app/parent/children/:childId/vision | vision.parentTitle | Parent | AppShell | ParentVisionPage | - | frontend/src/pages/vision/ParentVisionPage.jsx | Implemented route |
| /app/student | student.dashboard.title | Student | AppShell | StudentPortalPage | dashboard | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/subjects | student.subjects.title | Student | AppShell | StudentPortalPage | subjects | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/subjects/:subjectId | student.subjects.details | Student | AppShell | StudentPortalPage | subject-details | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/units | student.units.title | Student | AppShell | StudentPortalPage | units | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/units/:unitId | student.units.details | Student | AppShell | StudentPortalPage | units | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/lessons | student.lessons.title | Student | AppShell | StudentPortalPage | lessons | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/lessons/:lessonId | student.lessons.details | Student | AppShell | StudentPortalPage | lessons | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/materials/:materialId | student.materials.details | Student | AppShell | StudentPortalPage | materials | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/comments/:materialId | student.comments.title | Student | AppShell | StudentPortalPage | materials | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/homework | student.homework.title | Student | AppShell | StudentPortalPage | homework | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/homework/:homeworkId | student.homework.details | Student | AppShell | StudentPortalPage | homework | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/quizzes | student.quizzes.title | Student | AppShell | StudentPortalPage | quizzes | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/quizzes/:quizId | student.quizzes.details | Student | AppShell | StudentPortalPage | quizzes | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/quiz-attempts/:attemptId | student.quizzes.attempt | Student | AppShell | StudentPortalPage | quiz-attempt | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/quiz-attempts/:attemptId/result | student.quizzes.result | Student | AppShell | StudentPortalPage | quiz-result | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/ai-tutor | student.tutor.title | Student | AppShell | StudentPortalPage | tutor | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/recommendations | student.recommendations.title | Student | AppShell | StudentPortalPage | recommendations | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/progress | student.progress.title | Student | AppShell | StudentPortalPage | progress | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/attendance | student.attendance.title | Student | AppShell | StudentPortalPage | attendance | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/engagement | student.engagement.title | Student | AppShell | StudentPortalPage | engagement | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/communities | student.communities.title | Student | AppShell | StudentPortalPage | communities | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/communities/:communityId | student.communities.details | Student | AppShell | StudentPortalPage | communities | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/competitions | student.competitions.title | Student | AppShell | StudentPortalPage | competitions | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/competitions/:competitionId | student.competitions.details | Student | AppShell | StudentPortalPage | competitions | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/leaderboard/:competitionId | student.leaderboard.title | Student | AppShell | StudentPortalPage | competitions | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/office-hours | student.officeHours.title | Student | AppShell | StudentPortalPage | office-hours | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/notifications | student.notifications.title | Student | AppShell | StudentPortalPage | notifications | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/announcements | student.announcements.title | Student | AppShell | StudentPortalPage | announcements | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/suggestions | student.suggestions.title | Student | AppShell | StudentPortalPage | suggestions | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/badges | student.badges.title | Student | AppShell | StudentPortalPage | badges | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/streaks | student.streaks.title | Student | AppShell | StudentPortalPage | badges | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/settings | student.settings.title | Student | AppShell | StudentPortalPage | settings | frontend/src/pages/student/StudentPortalPage.jsx | Implemented route |
| /app/student/profile | nav.profile | Student | AppShell | ProfilePage | - | frontend/src/pages/app/ProfilePage.jsx | Implemented route |
| /app/teacher | teacher.dashboard.title | Teacher, School Admin | AppShell | TeacherPortalPage | dashboard | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/classes | teacher.classes.title | Teacher, School Admin | AppShell | TeacherPortalPage | classes | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/classes/:classId | teacher.classes.details | Teacher, School Admin | AppShell | TeacherPortalPage | classes | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/subjects | teacher.subjects.title | Teacher, School Admin | AppShell | TeacherPortalPage | subjects | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/subjects/:subjectId | teacher.curriculum.units | Teacher, School Admin | AppShell | TeacherPortalPage | subjects | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/units/:unitId | teacher.curriculum.lessons | Teacher, School Admin | AppShell | TeacherPortalPage | units | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/lessons/:lessonId | teacher.curriculum.lessonDetails | Teacher, School Admin | AppShell | TeacherPortalPage | lesson | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/students | teacher.students.title | Teacher, School Admin | AppShell | TeacherPortalPage | students | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/students/:studentId | teacher.students.details | Teacher, School Admin | AppShell | TeacherPortalPage | students | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/quizzes | teacher.quizzes.title | Teacher, School Admin | AppShell | TeacherPortalPage | quizzes | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/quizzes/:quizId | teacher.quizzes.details | Teacher, School Admin | AppShell | TeacherPortalPage | quizzes | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/quiz-generate | teacher.generate.title | Teacher, School Admin | AppShell | TeacherPortalPage | quiz-generate | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/submissions/:attemptId | teacher.grading.title | Teacher, School Admin | AppShell | TeacherPortalPage | submission | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/assignments | nav.assignments | Teacher, School Admin | AppShell | TeacherPortalPage | quizzes | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/notifications | teacher.notifications.title | Teacher, School Admin | AppShell | TeacherPortalPage | notifications | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/office-hours | teacher.officeHours.title | Teacher, School Admin | AppShell | TeacherPortalPage | office-hours | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/teacher/settings | teacher.settings.title | Teacher, School Admin | AppShell | TeacherPortalPage | settings | frontend/src/pages/teacher/TeacherPortalPage.jsx | Implemented route |
| /app/parent | parent.dashboard.title | Parent | AppShell | ParentPortalPage | dashboard | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/children | parent.children.title | Parent | AppShell | ParentPortalPage | children | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/children/:childId | parent.child.title | Parent | AppShell | ParentPortalPage | child | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/children/:childId/progress | parent.progress.title | Parent | AppShell | ParentPortalPage | child-progress | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/children/:childId/attendance | parent.attendance.title | Parent | AppShell | ParentPortalPage | child-attendance | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/children/:childId/points | parent.points.title | Parent | AppShell | ParentPortalPage | child-points | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/documents | parent.documents.title | Parent | AppShell | ParentPortalPage | documents | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/conversations | parent.conversations.title | Parent | AppShell | ParentPortalPage | conversations | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/notifications | parent.notifications.title | Parent | AppShell | ParentPortalPage | notifications | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/announcements | parent.announcements.title | Parent | AppShell | ParentPortalPage | announcements | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/settings | parent.settings.title | Parent | AppShell | ParentPortalPage | settings | frontend/src/pages/parent/ParentPortalPage.jsx | Implemented route |
| /app/parent/profile | nav.profile | Parent | AppShell | ProfilePage | - | frontend/src/pages/app/ProfilePage.jsx | Implemented route |
| /app/school | school.pages.dashboard.title | School Admin | AppShell | SchoolAdminPortalPage | dashboard | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/profile | school.pages.profile.title | School Admin | AppShell | SchoolAdminPortalPage | profile | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/academic-years | school.pages.academicYears.title | School Admin | AppShell | SchoolAdminPortalPage | academic-years | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/terms | school.pages.terms.title | School Admin | AppShell | SchoolAdminPortalPage | terms | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/grades | school.pages.grades.title | School Admin | AppShell | SchoolAdminPortalPage | grades | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/classes | school.pages.classes.title | School Admin | AppShell | SchoolAdminPortalPage | classes | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/subjects | school.pages.subjects.title | School Admin | AppShell | SchoolAdminPortalPage | subjects | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/users | school.pages.users.title | School Admin | AppShell | SchoolAdminPortalPage | users | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/students | school.pages.students.title | School Admin | AppShell | SchoolAdminPortalPage | students | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/teachers | school.pages.teachers.title | School Admin | AppShell | SchoolAdminPortalPage | teachers | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/parents | school.pages.parents.title | School Admin | AppShell | SchoolAdminPortalPage | parents | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/relationships | school.pages.relationships.title | School Admin | AppShell | SchoolAdminPortalPage | relationships | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/teacher-assignments | school.pages.teacherAssignments.title | School Admin | AppShell | SchoolAdminPortalPage | teacher-assignments | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/curriculum | school.pages.curriculum.title | School Admin | AppShell | SchoolAdminPortalPage | curriculum | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/announcements | school.pages.announcements.title | School Admin | AppShell | SchoolAdminPortalPage | announcements | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/document-requests | school.pages.documentRequests.title | School Admin | AppShell | SchoolAdminPortalPage | document-requests | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/communities | school.pages.communities.title | School Admin | AppShell | SchoolAdminPortalPage | communities | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/competitions | school.pages.competitions.title | School Admin | AppShell | SchoolAdminPortalPage | competitions | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/reports | school.pages.reports.title | School Admin | AppShell | SchoolAdminPortalPage | reports | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/ai-usage | school.pages.aiUsage.title | School Admin | AppShell | SchoolAdminPortalPage | ai-usage | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/storage | school.pages.storage.title | School Admin | AppShell | SchoolAdminPortalPage | storage | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/subscription | school.pages.subscription.title | School Admin | AppShell | SchoolAdminPortalPage | subscription | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/support | school.pages.support.title | School Admin | AppShell | SchoolAdminPortalPage | support | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/audit-log | school.pages.audit.title | School Admin | AppShell | SchoolAdminPortalPage | audit-log | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/school/settings | school.pages.settings.title | School Admin | AppShell | SchoolAdminPortalPage | settings | frontend/src/pages/school/SchoolAdminPortalPage.jsx | Implemented route |
| /app/system | system.pages.dashboard.title | System Admin | AppShell | SystemAdminPortalPage | dashboard | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/tenants | system.pages.tenants.title | System Admin | AppShell | SystemAdminPortalPage | tenants | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/tenants/:tenantId | system.pages.tenantDetails.title | System Admin | AppShell | SystemAdminPortalPage | tenant-details | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/onboarding | system.pages.onboarding.title | System Admin | AppShell | SystemAdminPortalPage | onboarding | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/plans | system.pages.plans.title | System Admin | AppShell | SystemAdminPortalPage | plans | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/subscriptions | system.pages.subscriptions.title | System Admin | AppShell | SystemAdminPortalPage | subscriptions | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/usage | system.pages.usage.title | System Admin | AppShell | SystemAdminPortalPage | usage | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/ai-usage | system.pages.aiUsage.title | System Admin | AppShell | SystemAdminPortalPage | ai-usage | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/storage | system.pages.storage.title | System Admin | AppShell | SystemAdminPortalPage | storage | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/support | system.pages.support.title | System Admin | AppShell | SystemAdminPortalPage | support | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/feature-flags | system.pages.featureFlags.title | System Admin | AppShell | SystemAdminPortalPage | feature-flags | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/announcements | system.pages.announcements.title | System Admin | AppShell | SystemAdminPortalPage | announcements | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/health | system.pages.health.title | System Admin | AppShell | SystemAdminPortalPage | health | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/errors | system.pages.errorMonitoring.title | System Admin | AppShell | SystemAdminPortalPage | errors | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/audit | system.pages.audit.title | System Admin | AppShell | SystemAdminPortalPage | audit | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/security | system.pages.security.title | System Admin | AppShell | SystemAdminPortalPage | security | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/backups | system.pages.backups.title | System Admin | AppShell | SystemAdminPortalPage | backups | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /app/system/settings | system.pages.settings.title | System Admin | AppShell | SystemAdminPortalPage | settings | frontend/src/pages/system/SystemAdminPortalPage.jsx | Implemented route |
| /forbidden | pages.forbiddenTitle | Any role after failed guard | Standalone status page | ForbiddenPage | forbidden | frontend/src/pages/status/StatusPages.jsx | Implemented explicit route |
| /session-expired | pages.sessionExpiredTitle | Any user with expired auth | Standalone status page | SessionExpiredPage | session-expired | frontend/src/pages/status/StatusPages.jsx | Implemented explicit route |
| /suspended | pages.suspendedTitle | Tenant users when tenant is suspended | Standalone status page | SuspendedTenantPage | suspended | frontend/src/pages/status/StatusPages.jsx | Implemented explicit route |
| * | pages.notFoundTitle | All users | Standalone status page | NotFoundPage | not-found | frontend/src/pages/status/StatusPages.jsx | Implemented wildcard route |

## 6. Navigation Map

Navigation entries found in `navConfig.js`: **63**.

| Nav key | Label key | Target route | Visible to |
| --- | --- | --- | --- |
| student | nav.myLearning | /app/student | Student |
| student-subjects | student.subjects.title | /app/student/subjects | Student |
| student-homework | student.homework.title | /app/student/homework | Student |
| student-quizzes | student.quizzes.title | /app/student/quizzes | Student |
| student-tutor | student.tutor.title | /app/student/ai-tutor | Student |
| student-progress | student.progress.title | /app/student/progress | Student |
| student-communities | student.communities.title | /app/student/communities | Student |
| student-competitions | student.competitions.title | /app/student/competitions | Student |
| student-office-hours | student.officeHours.title | /app/student/office-hours | Student |
| student-notifications | student.notifications.title | /app/student/notifications | Student |
| student-suggestions | student.suggestions.title | /app/student/suggestions | Student |
| teacher | teacher.dashboard.title | /app/teacher | Teacher, School Admin |
| teacher-classes | teacher.classes.title | /app/teacher/classes | Teacher, School Admin |
| teacher-subjects | teacher.subjects.title | /app/teacher/subjects | Teacher, School Admin |
| teacher-students | teacher.students.title | /app/teacher/students | Teacher, School Admin |
| teacher-quizzes | teacher.quizzes.title | /app/teacher/quizzes | Teacher, School Admin |
| teacher-generate | teacher.generate.title | /app/teacher/quiz-generate | Teacher, School Admin |
| teacher-office-hours | teacher.officeHours.title | /app/teacher/office-hours | Teacher, School Admin |
| teacher-notifications | teacher.notifications.title | /app/teacher/notifications | Teacher, School Admin |
| parent | parent.dashboard.title | /app/parent | Parent |
| parent-children | parent.children.title | /app/parent/children | Parent |
| parent-documents | parent.documents.title | /app/parent/documents | Parent |
| parent-conversations | parent.conversations.title | /app/parent/conversations | Parent |
| parent-announcements | parent.announcements.title | /app/parent/announcements | Parent |
| parent-notifications | parent.notifications.title | /app/parent/notifications | Parent |
| school | nav.schoolAdmin | /app/school | School Admin |
| school-students | school.pages.students.title | /app/school/students | School Admin |
| school-teachers | school.pages.teachers.title | /app/school/teachers | School Admin |
| school-parents | school.pages.parents.title | /app/school/parents | School Admin |
| school-relationships | school.pages.relationships.title | /app/school/relationships | School Admin |
| school-assignments | school.pages.teacherAssignments.title | /app/school/teacher-assignments | School Admin |
| school-classes | school.pages.classes.title | /app/school/classes | School Admin |
| school-academic-years | school.pages.academicYears.title | /app/school/academic-years | School Admin |
| school-curriculum | school.pages.curriculum.title | /app/school/curriculum | School Admin |
| school-announcements | school.pages.announcements.title | /app/school/announcements | School Admin |
| school-document-requests | school.pages.documentRequests.title | /app/school/document-requests | School Admin |
| school-reports | school.pages.reports.title | /app/school/reports | School Admin |
| school-ai-usage | school.pages.aiUsage.title | /app/school/ai-usage | School Admin |
| school-subscription | school.pages.subscription.title | /app/school/subscription | School Admin |
| school-support | school.pages.support.title | /app/school/support | School Admin |
| school-audit | school.pages.audit.title | /app/school/audit-log | School Admin |
| school-settings | school.pages.settings.title | /app/school/settings | School Admin |
| system | nav.systemAdmin | /app/system | System Admin |
| system-tenants | system.pages.tenants.title | /app/system/tenants | System Admin |
| system-onboarding | system.pages.onboarding.title | /app/system/onboarding | System Admin |
| system-plans | system.pages.plans.title | /app/system/plans | System Admin |
| system-subscriptions | system.pages.subscriptions.title | /app/system/subscriptions | System Admin |
| system-usage | system.pages.usage.title | /app/system/usage | System Admin |
| system-ai-usage | system.pages.aiUsage.title | /app/system/ai-usage | System Admin |
| system-storage | system.pages.storage.title | /app/system/storage | System Admin |
| system-support | system.pages.support.title | /app/system/support | System Admin |
| system-feature-flags | system.pages.featureFlags.title | /app/system/feature-flags | System Admin |
| system-announcements | system.pages.announcements.title | /app/system/announcements | System Admin |
| system-health | system.pages.health.title | /app/system/health | System Admin |
| system-errors | system.pages.errorMonitoring.title | /app/system/errors | System Admin |
| system-audit | system.pages.audit.title | /app/system/audit | System Admin |
| system-security | system.pages.security.title | /app/system/security | System Admin |
| system-backups | system.pages.backups.title | /app/system/backups | System Admin |
| system-settings | system.pages.settings.title | /app/system/settings | System Admin |
| messages | messages.title | /app/messages | Student, Teacher, Parent, School Admin |
| notifications-center | notifications.title | /app/notifications | Student, Teacher, Parent, School Admin |
| profile | nav.profile | /app/profile | All authenticated roles |
| security | nav.security | /app/security | All authenticated roles |

### Authenticated landing redirects

| Role | Home route |
| --- | --- |
| Student | /app/student |
| Teacher | /app/teacher |
| Parent | /app/parent |
| School Admin | /app/school |
| System Admin | /app/system |
| Unknown authenticated role | /app |

## 7. Complete Screen Inventory

### Global / Shared

#### Home public landing

- Route(s): `/`
- Purpose/content: Hero/content cards/FAQ/public product intro; PublicLayout; Guest/Public.
- Evidence: `frontend/src/pages/public/HomePage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Request demo

- Route(s): `/request-demo`
- Purpose/content: Demo request form with success state; Guest/Public.
- Evidence: `frontend/src/pages/public/RequestDemoPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Events

- Route(s): `/events`
- Purpose/content: Static public info page.
- Evidence: `frontend/src/pages/public/EventsPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Activities

- Route(s): `/activities`
- Purpose/content: Static public info page.
- Evidence: `frontend/src/pages/public/ActivitiesPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### News

- Route(s): `/news`
- Purpose/content: Static public info page.
- Evidence: `frontend/src/pages/public/NewsPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Login

- Route(s): `/login`
- Purpose/content: Anonymous-only auth form; redirects by role after login.
- Evidence: `frontend/src/pages/auth/LoginPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### App home / role redirect shell

- Route(s): `/app`
- Purpose/content: Authenticated index / dashboard shell.
- Evidence: `frontend/src/pages/app/AppHome.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Profile

- Route(s): `/app/profile`
- Purpose/content: All authenticated account profile.
- Evidence: `frontend/src/pages/app/ProfilePage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Security

- Route(s): `/app/security`
- Purpose/content: All authenticated password change form.
- Evidence: `frontend/src/pages/app/SecurityPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Shared notifications

- Route(s): `/app/notifications`
- Purpose/content: Tenant roles shared notification center.
- Evidence: `frontend/src/pages/app/CommunicationPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Notification preferences

- Route(s): `/app/notifications/preferences`
- Purpose/content: Per-category in-app/email preferences.
- Evidence: `frontend/src/pages/app/CommunicationPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Messages list

- Route(s): `/app/messages`
- Purpose/content: Conversation inbox and new conversation modal.
- Evidence: `frontend/src/pages/app/CommunicationPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Message thread

- Route(s): `/app/messages/:conversationId`
- Purpose/content: Conversation details and reply form.
- Evidence: `frontend/src/pages/app/CommunicationPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Forbidden

- Route(s): `/forbidden`
- Purpose/content: Wrong role/permission fallback.
- Evidence: `frontend/src/pages/status/StatusPages.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Session expired

- Route(s): `/session-expired`
- Purpose/content: Expired auth fallback.
- Evidence: `frontend/src/pages/status/StatusPages.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Suspended tenant

- Route(s): `/suspended`
- Purpose/content: Suspended tenant fallback.
- Evidence: `frontend/src/pages/status/StatusPages.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Not found

- Route(s): `*`
- Purpose/content: 404 fallback.
- Evidence: `frontend/src/pages/status/StatusPages.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

### Student

#### Student dashboard

- Route(s): `/app/student`
- Purpose/content: Metric cards for assigned subjects, homework, quizzes, recommendations, office hours, badges, communities, competitions.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subjects list

- Route(s): `/app/student/subjects`
- Purpose/content: Assigned subjects.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subject details

- Route(s): `/app/student/subjects/:subjectId`
- Purpose/content: Subject metadata and linked units.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Units list

- Route(s): `/app/student/units`
- Purpose/content: Units list view.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Unit details/lessons

- Route(s): `/app/student/units/:unitId`
- Purpose/content: Unit-linked lessons.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Lessons list

- Route(s): `/app/student/lessons`
- Purpose/content: Lessons available to the student.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Lesson detail

- Route(s): `/app/student/lessons/:lessonId`
- Purpose/content: Lesson content/materials and Complete lesson action.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Material detail

- Route(s): `/app/student/materials/:materialId`
- Purpose/content: Lesson material viewer.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Material comments

- Route(s): `/app/student/comments/:materialId`
- Purpose/content: Resource comments list + submit comment.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Homework list

- Route(s): `/app/student/homework`
- Purpose/content: Assigned homework.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Homework detail

- Route(s): `/app/student/homework/:homeworkId`
- Purpose/content: Homework details + my submission + submit work form.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Quiz list

- Route(s): `/app/student/quizzes`
- Purpose/content: Assigned quizzes.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Quiz detail

- Route(s): `/app/student/quizzes/:quizId`
- Purpose/content: Quiz instructions + start quiz + previous attempts.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Quiz attempt

- Route(s): `/app/student/quiz-attempts/:attemptId`
- Purpose/content: Answer questions, save answers, submit attempt.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Quiz result

- Route(s): `/app/student/quiz-attempts/:attemptId/result`
- Purpose/content: Result/feedback after submission.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### AI tutor

- Route(s): `/app/student/ai-tutor`
- Purpose/content: Curriculum-grounded chat with citations/sources.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Recommendations

- Route(s): `/app/student/recommendations`
- Purpose/content: AI recommendations from progress data.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Progress

- Route(s): `/app/student/progress`
- Purpose/content: Summary, subject progress, lesson progress, metrics, attempts, insights, pain points, recommendations, predictions.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Attendance

- Route(s): `/app/student/attendance`
- Purpose/content: Own attendance history.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Engagement

- Route(s): `/app/student/engagement`
- Purpose/content: Engagement summary via CV data.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Communities

- Route(s): `/app/student/communities`
- Purpose/content: Community list.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Community detail

- Route(s): `/app/student/communities/:communityId`
- Purpose/content: Join/leave and post flow.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Competitions

- Route(s): `/app/student/competitions`
- Purpose/content: Competition list.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Competition detail

- Route(s): `/app/student/competitions/:competitionId`
- Purpose/content: Enter + submit/update work + leaderboard.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Leaderboard

- Route(s): `/app/student/leaderboard/:competitionId`
- Purpose/content: Competition leaderboard.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Office hours

- Route(s): `/app/student/office-hours`
- Purpose/content: Available slots + own bookings.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Student notifications

- Route(s): `/app/student/notifications`
- Purpose/content: Role-specific notification list.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Announcements

- Route(s): `/app/student/announcements`
- Purpose/content: Tenant announcements.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Suggestions

- Route(s): `/app/student/suggestions`
- Purpose/content: Submit suggestion to school.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Badges

- Route(s): `/app/student/badges`
- Purpose/content: Badge catalog, earned badges, streak.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Streaks

- Route(s): `/app/student/streaks`
- Purpose/content: Alias view into badges/streak data.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Settings

- Route(s): `/app/student/settings`
- Purpose/content: Student account/settings; change password link.
- Evidence: `frontend/src/pages/student/StudentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Student profile

- Route(s): `/app/student/profile`
- Purpose/content: Shared profile page.
- Evidence: `frontend/src/pages/app/ProfilePage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Student CV engagement

- Route(s): `/app/student/vision`
- Purpose/content: Own engagement summary.
- Evidence: `frontend/src/pages/vision/StudentVisionPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

### Teacher / School Admin teacher surface

#### Teacher dashboard

- Route(s): `/app/teacher`
- Purpose/content: Class/subject/student/quiz/grading metrics.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Classes

- Route(s): `/app/teacher/classes`
- Purpose/content: Assigned classes list.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Class detail

- Route(s): `/app/teacher/classes/:classId`
- Purpose/content: Class students.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subjects

- Route(s): `/app/teacher/subjects`
- Purpose/content: Assigned subjects.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subject units

- Route(s): `/app/teacher/subjects/:subjectId`
- Purpose/content: Curriculum units for subject.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Unit lessons

- Route(s): `/app/teacher/units/:unitId`
- Purpose/content: Lessons under unit.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Lesson detail

- Route(s): `/app/teacher/lessons/:lessonId`
- Purpose/content: Lesson metadata/materials.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Students

- Route(s): `/app/teacher/students`
- Purpose/content: Assigned students list.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Student detail

- Route(s): `/app/teacher/students/:studentId`
- Purpose/content: Progress/pain points/points ledger/award points.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Quizzes

- Route(s): `/app/teacher/quizzes`
- Purpose/content: Draft/published quizzes.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Quiz detail

- Route(s): `/app/teacher/quizzes/:quizId`
- Purpose/content: Questions, assignments, submissions, analytics, publish/archive.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### AI quiz generation

- Route(s): `/app/teacher/quiz-generate`
- Purpose/content: Generate draft quiz with teacher review requirement.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Submission detail

- Route(s): `/app/teacher/submissions/:attemptId`
- Purpose/content: Manual grading and feedback.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Assignments alias

- Route(s): `/app/teacher/assignments`
- Purpose/content: Alias to quizzes view.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Teacher notifications

- Route(s): `/app/teacher/notifications`
- Purpose/content: Teacher notification list.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Teacher office hours

- Route(s): `/app/teacher/office-hours`
- Purpose/content: Office hours/bookings/attendance.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Teacher settings

- Route(s): `/app/teacher/settings`
- Purpose/content: Settings and security link.
- Evidence: `frontend/src/pages/teacher/TeacherPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### CV sessions

- Route(s): `/app/vision`
- Purpose/content: Teacher/school-admin CV attendance session list and start form.
- Evidence: `frontend/src/pages/vision/VisionStaffPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### CV session detail

- Route(s): `/app/vision/sessions/:sessionId`
- Purpose/content: Analyze frame, review candidates, confirm/reject/override, end session.
- Evidence: `frontend/src/pages/vision/VisionStaffPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

### Parent

#### Parent dashboard

- Route(s): `/app/parent`
- Purpose/content: Linked-child summary, child/progress/attendance/documents metrics.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Children

- Route(s): `/app/parent/children`
- Purpose/content: Linked child list.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Child overview

- Route(s): `/app/parent/children/:childId`
- Purpose/content: Child details overview.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Child progress

- Route(s): `/app/parent/children/:childId/progress`
- Purpose/content: Progress/pain points/recommendations for linked child.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Child attendance

- Route(s): `/app/parent/children/:childId/attendance`
- Purpose/content: Linked child attendance history.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Child points

- Route(s): `/app/parent/children/:childId/points`
- Purpose/content: Gamification points and ledger.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Document requests

- Route(s): `/app/parent/documents`
- Purpose/content: Create request, track status, download response.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Parent conversations

- Route(s): `/app/parent/conversations`
- Purpose/content: Messaging entry.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Parent notifications

- Route(s): `/app/parent/notifications`
- Purpose/content: Notification list.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Parent announcements

- Route(s): `/app/parent/announcements`
- Purpose/content: School announcements.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Parent settings

- Route(s): `/app/parent/settings`
- Purpose/content: Settings/security link.
- Evidence: `frontend/src/pages/parent/ParentPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Parent profile

- Route(s): `/app/parent/profile`
- Purpose/content: Shared profile page.
- Evidence: `frontend/src/pages/app/ProfilePage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Child engagement

- Route(s): `/app/parent/children/:childId/vision`
- Purpose/content: Linked-child CV engagement summary.
- Evidence: `frontend/src/pages/vision/ParentVisionPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

### School Admin

#### School dashboard

- Route(s): `/app/school`
- Purpose/content: Tenant admin metrics.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### School profile

- Route(s): `/app/school/profile`
- Purpose/content: Tenant profile.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Academic years

- Route(s): `/app/school/academic-years`
- Purpose/content: Create/list academic years.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Terms

- Route(s): `/app/school/terms`
- Purpose/content: Create/list terms.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Grades

- Route(s): `/app/school/grades`
- Purpose/content: Create/list grades.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Classes

- Route(s): `/app/school/classes`
- Purpose/content: Create/list school classes.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subjects

- Route(s): `/app/school/subjects`
- Purpose/content: Create/list subjects.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Users

- Route(s): `/app/school/users`
- Purpose/content: Create/list tenant users.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Students

- Route(s): `/app/school/students`
- Purpose/content: Student user management.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Teachers

- Route(s): `/app/school/teachers`
- Purpose/content: Teacher user management.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Parents

- Route(s): `/app/school/parents`
- Purpose/content: Parent user management.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Relationships

- Route(s): `/app/school/relationships`
- Purpose/content: Parent-student links.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Teacher assignments

- Route(s): `/app/school/teacher-assignments`
- Purpose/content: Class/subject teacher assignments.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Curriculum

- Route(s): `/app/school/curriculum`
- Purpose/content: Curriculum list/management surface.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Announcements

- Route(s): `/app/school/announcements`
- Purpose/content: Create and publish/unpublish announcements.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Document requests

- Route(s): `/app/school/document-requests`
- Purpose/content: Respond and transition parent document requests.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Communities

- Route(s): `/app/school/communities`
- Purpose/content: Community oversight.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Competitions

- Route(s): `/app/school/competitions`
- Purpose/content: Competition oversight.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Reports

- Route(s): `/app/school/reports`
- Purpose/content: Tenant users/assessment/audit/AI reports.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### AI usage

- Route(s): `/app/school/ai-usage`
- Purpose/content: Tenant AI usage summary.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Storage

- Route(s): `/app/school/storage`
- Purpose/content: Tenant storage usage.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subscription

- Route(s): `/app/school/subscription`
- Purpose/content: Tenant subscription details/renewal request.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Support

- Route(s): `/app/school/support`
- Purpose/content: Respond to support requests.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Audit log

- Route(s): `/app/school/audit-log`
- Purpose/content: Tenant audit events.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Settings

- Route(s): `/app/school/settings`
- Purpose/content: Tenant settings + security link.
- Evidence: `frontend/src/pages/school/SchoolAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

### System Admin

#### System dashboard

- Route(s): `/app/system`
- Purpose/content: Platform roll-up metrics.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Tenants

- Route(s): `/app/system/tenants`
- Purpose/content: Tenant list and onboard button.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Tenant detail

- Route(s): `/app/system/tenants/:tenantId`
- Purpose/content: Tenant status, subscription, usage, school admin, data export/deletion request.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Onboarding

- Route(s): `/app/system/onboarding`
- Purpose/content: Create tenant, assign plan, create admin, activate.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Plans

- Route(s): `/app/system/plans`
- Purpose/content: Plan list.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Subscriptions

- Route(s): `/app/system/subscriptions`
- Purpose/content: Subscription roll-up.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Usage

- Route(s): `/app/system/usage`
- Purpose/content: Platform usage analytics.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### AI usage

- Route(s): `/app/system/ai-usage`
- Purpose/content: Platform AI tokens/cost records.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Storage

- Route(s): `/app/system/storage`
- Purpose/content: Platform storage roll-up.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Support

- Route(s): `/app/system/support`
- Purpose/content: Cross-tenant support inbox/respond.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Feature flags

- Route(s): `/app/system/feature-flags`
- Purpose/content: Save feature flags.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Announcements

- Route(s): `/app/system/announcements`
- Purpose/content: Create platform announcements.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Health

- Route(s): `/app/system/health`
- Purpose/content: Operational status.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Error monitoring

- Route(s): `/app/system/errors`
- Purpose/content: Audit-derived error/security posture.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Audit

- Route(s): `/app/system/audit`
- Purpose/content: Platform audit events.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Security

- Route(s): `/app/system/security`
- Purpose/content: Security events/posture cards.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Backups

- Route(s): `/app/system/backups`
- Purpose/content: Backup/restore operational posture.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

#### Settings

- Route(s): `/app/system/settings`
- Purpose/content: System settings save form.
- Evidence: `frontend/src/pages/system/SystemAdminPortalPage.jsx`
- Required states for prototype: loading where data-backed; empty list/form success; API error; permission/unauthorized where protected; success state for mutations.
- Prototype/Figma status: use Figma for visual design; build this screen if present in route inventory.

## 8. Button and Action Inventory

Actions documented: **136**. These are the actions the redesigned prototype must make visible and non-dead. Backend-supported actions without a dedicated current page are marked so the prototype owner can decide whether to include them in presentation scope.

| Role | Screen | Action/Button | Type | Target/result | API/state dependency | Evidence | Prototype note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Guest/Public | Public navbar | Open Home | Navigation | / | React Router Link | PublicNavBar.jsx | Needs Figma styling |
| Guest/Public | Public navbar | Request a demo | Navigation CTA | /request-demo | Local form state only | PublicNavBar.jsx / RequestDemoPage.jsx | Needs Figma styling |
| Guest/Public | Public navbar | Sign in | Navigation CTA | /login | Account login API later | PublicNavBar.jsx | Needs Figma styling |
| Guest/Public | Home | FAQ button | Button | Inline FAQ affordance | No backend call | FaqButton.jsx | Needs Figma styling |
| Guest/Public | Request demo | Submit demo request | Submit | Success state on same page | Local submitted state; no backend call found | RequestDemoPage.jsx | Needs backend/form decision if production contact capture is required |
| Anonymous | Login | Sign in | Submit | Role-based redirect | POST /api/v1/account/login | LoginPage.jsx / authApi.js | Must be clickable in prototype |
| All authenticated | Header | Open/close sidebar | Icon button | Toggle AppShell sidebar | Local UI state | Header.jsx | Prototype should include menu behavior |
| All authenticated | Header | Sign out | Button | /login or anonymous state | POST /api/v1/account/logout | Header.jsx / authApi.js | Must be represented |
| All authenticated | Profile | View profile | Read | /app/profile | Auth context / account data | ProfilePage.jsx | Profile page frame needed |
| All authenticated | Security | Change password | Submit | Success alert/toast | POST /api/v1/account/change-password | SecurityPage.jsx / authApi.js | Security form frame needed |
| All authenticated | Forbidden/status pages | Go home | Navigation | /app | React Router Link | StatusPages.jsx | Status pages needed |
| All/auth expired | Session expired/suspended | Sign in | Navigation | /login | React Router Link | StatusPages.jsx | Status pages needed |
| Student | Shared notifications center | Open preferences | Navigation | /app/notifications/preferences | GET/PUT /api/v1/notification-preferences | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Student | Shared notifications center | Mark all read | Mutation | Unread count updates | PATCH /api/v1/notifications/read-all | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Student | Shared notifications center | Mark one read | Mutation | Notification row updates | PATCH /api/v1/notifications/{id}/read | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Student | Shared messages | New conversation | Modal/form | Conversation thread | POST /api/v1/conversations | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Student | Shared messages | Open thread | Navigation | /app/messages/:conversationId | GET /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Student | Shared messages thread | Send reply | Submit | Message appended | POST /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Teacher | Shared notifications center | Open preferences | Navigation | /app/notifications/preferences | GET/PUT /api/v1/notification-preferences | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Teacher | Shared notifications center | Mark all read | Mutation | Unread count updates | PATCH /api/v1/notifications/read-all | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Teacher | Shared notifications center | Mark one read | Mutation | Notification row updates | PATCH /api/v1/notifications/{id}/read | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Teacher | Shared messages | New conversation | Modal/form | Conversation thread | POST /api/v1/conversations | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Teacher | Shared messages | Open thread | Navigation | /app/messages/:conversationId | GET /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Teacher | Shared messages thread | Send reply | Submit | Message appended | POST /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Shared notifications center | Open preferences | Navigation | /app/notifications/preferences | GET/PUT /api/v1/notification-preferences | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Shared notifications center | Mark all read | Mutation | Unread count updates | PATCH /api/v1/notifications/read-all | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Shared notifications center | Mark one read | Mutation | Notification row updates | PATCH /api/v1/notifications/{id}/read | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Shared messages | New conversation | Modal/form | Conversation thread | POST /api/v1/conversations | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Shared messages | Open thread | Navigation | /app/messages/:conversationId | GET /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Shared messages thread | Send reply | Submit | Message appended | POST /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| School Admin | Shared notifications center | Open preferences | Navigation | /app/notifications/preferences | GET/PUT /api/v1/notification-preferences | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| School Admin | Shared notifications center | Mark all read | Mutation | Unread count updates | PATCH /api/v1/notifications/read-all | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| School Admin | Shared notifications center | Mark one read | Mutation | Notification row updates | PATCH /api/v1/notifications/{id}/read | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| School Admin | Shared messages | New conversation | Modal/form | Conversation thread | POST /api/v1/conversations | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| School Admin | Shared messages | Open thread | Navigation | /app/messages/:conversationId | GET /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| School Admin | Shared messages thread | Send reply | Submit | Message appended | POST /api/v1/conversations/{id}/messages | CommunicationPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Student | Dashboard | Open metric cards | Navigation | Role-specific pages | Various GET calls | StudentPortalPage.jsx | Needs Figma/prototype build |
| Student | Subjects | Open subject details | Navigation | /app/student/subjects/:subjectId | GET /api/Subjects/GetSubjectById/{id} | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Units | Open unit lessons | Navigation | /app/student/units/:unitId | GET /api/Lessons/GetLessonsByUnitId | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Lessons | Open lesson details | Navigation | /app/student/lessons/:lessonId | GET lesson/materials endpoints | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Lesson detail | Complete lesson | Mutation | Lesson progress updated | POST /api/v1/student/lessons/{lessonId}/complete | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Materials | Open material | Navigation/detail | /app/student/materials/:materialId | GET /api/LessonMaterial/GetMaterialByLessonId | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Material comments | Submit comment | Submit | Comment list updated | POST /api/v1/lesson-materials/{materialId}/comments | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Homework detail | Submit homework | Submit | My submission saved | POST /api/v1/homework/{id}/submit | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Quiz detail | Start quiz | Mutation/navigation | /app/student/quiz-attempts/:attemptId | POST /api/v1/quizzes/{id}/attempts | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Quiz attempt | Save answers | Submit secondary | Answers persisted | PUT /api/v1/attempts/{attemptId}/answers | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Quiz attempt | Submit attempt | Mutation/navigation | /app/student/quiz-attempts/:attemptId/result | POST /api/v1/attempts/{attemptId}/submit | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | AI tutor | Send question | Submit | AI answer with source/citations | POST /api/chat via backend | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Communities | Join community | Mutation | Membership updated | POST /api/v1/communities/{id}/join | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Communities | Leave community | Mutation | Membership removed | POST /api/v1/communities/{id}/leave | studentApi.js | Needs Figma/prototype build |
| Student | Community detail | Submit post | Submit | Post appears in feed | POST /api/v1/communities/{id}/posts | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Competitions | Enter competition | Mutation | Entry created | POST /api/v1/competitions/{id}/entries | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Competition detail | Submit/update competition work | Submit | Durable submission saved | POST /api/v1/competitions/{id}/submissions | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Office hours | Book office-hour slot | Mutation | Booking created | POST /api/v1/office-hours/{id}/bookings | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Office hours | Cancel booking | Mutation | Booking cancelled | POST /api/v1/bookings/{bookingId}/cancel | studentApi.js | Needs Figma/prototype build |
| Student | Student notifications | Mark all read | Mutation | Unread count reduced | PATCH /api/v1/notifications/read-all | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Student notifications | Mark single read | Mutation | Item marked read | PATCH /api/v1/notifications/{id}/read | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Suggestions | Submit suggestion | Submit | Suggestion saved | POST /api/v1/suggestions | StudentPortalPage.jsx / studentApi.js | Needs Figma/prototype build |
| Student | Settings | Change password | Navigation | /app/security | Account password form | StudentPortalPage.jsx | Needs Figma/prototype build |
| Student | CV engagement | View own engagement summary | Read | Engagement summary card | GET /api/v1/student/vision/engagement-summary | StudentVisionPage.jsx / visionApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Dashboard | Open teacher metric cards | Navigation | Classes/subjects/students/quizzes | GET /api/v1/teacher/dashboard | TeacherPortalPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | Classes | Open class detail | Navigation | /app/teacher/classes/:classId | GET /api/v1/teacher/classes/{id}/students | TeacherPortalPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | Subjects | Open subject units | Navigation | /app/teacher/subjects/:subjectId | GET /api/Units/GetUnitsBySubjectId | TeacherPortalPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | Units | Open lesson detail | Navigation | /app/teacher/lessons/:lessonId | GET lesson materials | TeacherPortalPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | Students | Open student record | Navigation | /app/teacher/students/:studentId | GET progress endpoints | TeacherPortalPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | Student record | Award points | Mutation | Points ledger updated | POST /api/v1/students/{studentId}/points | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Student record | Save gamification rule | Mutation | Rule updated | PUT /api/v1/gamification/rules | teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Quizzes | Open quiz detail | Navigation | /app/teacher/quizzes/:quizId | GET /api/v1/quizzes/{id} | TeacherPortalPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | Quiz detail | Add question | Mutation | Question added | POST /api/v1/quizzes/{id}/questions | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Quiz detail | Update question | Mutation | Question saved | PUT /api/v1/quizzes/{id}/questions/{questionId} | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Quiz detail | Delete question | Destructive | Question removed | DELETE /api/v1/quizzes/{id}/questions/{questionId} | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Quiz detail | Publish quiz | Mutation | Quiz becomes available | POST /api/v1/quizzes/{id}/publish | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Quiz detail | Archive quiz | Destructive/secondary | Quiz archived | POST /api/v1/quizzes/{id}/archive | teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Quiz detail | Assign quiz | Mutation | Assignment created | POST /api/v1/quizzes/{id}/assignments | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | AI quiz generation | Generate draft | AI mutation | Draft quiz created | POST /api/v1/ai/quiz/draft | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Submission detail | Grade submission | Submit | Grades saved | POST /api/v1/submissions/{attemptId}/grade | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Submission detail | Send feedback | Submit | Feedback saved | POST /api/v1/submissions/{attemptId}/feedback | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | Office hours | Mark attendance | Mutation | Booking attendance saved | POST /api/v1/bookings/{bookingId}/attendance | TeacherPortalPage.jsx / teacherApi.js | Needs Figma/prototype build |
| Teacher / School Admin | CV session list | Start session | Submit | New active session | POST /api/v1/vision/sessions | VisionStaffPage.jsx / visionApi.js | Needs Figma/prototype build |
| Teacher / School Admin | CV session list | Open session | Navigation | /app/vision/sessions/:sessionId | GET /api/v1/vision/sessions/{id} | VisionStaffPage.jsx | Needs Figma/prototype build |
| Teacher / School Admin | CV session detail | Analyze frame | Upload/AI mutation | Candidates generated | POST /api/v1/vision/sessions/{id}/analyze | VisionStaffPage.jsx / visionApi.js | Needs Figma/prototype build |
| Teacher / School Admin | CV session detail | End session | Mutation | Session status ended | POST /api/v1/vision/sessions/{id}/end | VisionStaffPage.jsx / visionApi.js | Needs Figma/prototype build |
| Teacher / School Admin | CV candidate review | Confirm candidate | Approve | Attendance confirmed | POST /api/v1/vision/candidates/{id}/confirm | VisionStaffPage.jsx / visionApi.js | Needs Figma/prototype build |
| Teacher / School Admin | CV candidate review | Reject candidate | Reject | Candidate rejected | POST /api/v1/vision/candidates/{id}/reject | VisionStaffPage.jsx / visionApi.js | Needs Figma/prototype build |
| Teacher / School Admin | CV candidate review | Override candidate | Override | Attendance manually overridden | POST /api/v1/vision/candidates/{id}/override | VisionStaffPage.jsx / visionApi.js | Needs Figma/prototype build |
| Parent | Dashboard | Open child metric cards | Navigation | Child/progress/attendance/documents | GET /api/v1/parent/dashboard | ParentPortalPage.jsx | Needs Figma/prototype build |
| Parent | Children | Open child detail | Navigation | /app/parent/children/:childId | GET /api/v1/parent/children/{childId} | ParentPortalPage.jsx | Needs Figma/prototype build |
| Parent | Child detail | Open progress | Navigation | /app/parent/children/:childId/progress | GET progress endpoints | ParentPortalPage.jsx / parentApi.js | Needs Figma/prototype build |
| Parent | Child detail | Open attendance | Navigation | /app/parent/children/:childId/attendance | GET /api/v1/parent/children/{childId}/attendance | ParentPortalPage.jsx / parentApi.js | Needs Figma/prototype build |
| Parent | Child detail | Open points | Navigation | /app/parent/children/:childId/points | GET points ledger | ParentPortalPage.jsx / parentApi.js | Needs Figma/prototype build |
| Parent | Documents | Create document request | Submit | Request created | POST /api/v1/parent-requests | ParentPortalPage.jsx / parentApi.js | Needs Figma/prototype build |
| Parent | Documents | Attach request document | Upload | Attachment uploaded | POST /api/v1/parent-requests/{id}/attachment | parentApi.js | Needs Figma/prototype build |
| Parent | Documents | Download response document | Download | Browser download | GET /api/v1/parent-requests/{id}/responses/{responseId}/document/download | ParentPortalPage.jsx / parentApi.js | Needs Figma/prototype build |
| Parent | Conversations | Open shared messages | Navigation | /app/messages or /app/parent/conversations | GET /api/v1/conversations | ParentPortalPage.jsx / notificationsApi.js | Needs Figma/prototype build |
| Parent | Notifications | Mark notification read | Mutation | Item marked read | PATCH /api/v1/notifications/{id}/read | ParentPortalPage.jsx / parentApi.js | Needs Figma/prototype build |
| Parent | CV child engagement | View linked-child engagement | Read | Engagement summary | GET /api/v1/parent/vision/children/{childId}/engagement-summary | ParentVisionPage.jsx / visionApi.js | Needs Figma/prototype build |
| School Admin | Dashboard | Open school metric cards | Navigation | School sections | GET /api/v1/school-admin/dashboard | SchoolAdminPortalPage.jsx | Needs Figma/prototype build |
| School Admin | Academic years | Create academic year | Submit | Academic year created | POST /api/v1/academic-years | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Terms | Create term | Submit | Term created | POST /api/v1/terms | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Grades | Create grade | Submit | Grade created | POST /api/Grades/AddGrade | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Classes | Create class | Submit | Class created | POST /api/v1/classes | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Subjects | Create subject | Submit | Subject created | POST /api/Subjects/AddSubject | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Users/students/teachers/parents | Create user | Submit | Tenant user created | POST /api/v1/tenant-users | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Users/students/teachers/parents | Enable/disable user | Mutation | User active state changes | POST /api/v1/tenant-users/{id}/enable or disable | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Relationships | Link parent to student | Submit | Relationship created | POST /api/v1/school-admin/relationships | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Relationships | Deactivate relationship | Secondary/destructive | Relationship inactive | POST /api/v1/school-admin/relationships/{id}/deactivate | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Teacher assignments | Create class assignment | Submit | Teacher assigned to class | POST /api/v1/school-admin/teacher-class-assignments | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Teacher assignments | Create subject assignment | Submit | Teacher assigned to subject | POST /api/v1/teacher-subject-assignments | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Announcements | Create announcement | Submit | Announcement draft/record created | POST /api/v1/announcements | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Announcements | Publish/unpublish announcement | Mutation | Announcement visibility changes | POST /api/v1/announcements/{id}/publish | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Document requests | Respond to request | Submit | Response saved | POST /api/v1/parent-requests/{id}/responses | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Document requests | Mark resolved | Mutation | Request status updated | POST /api/v1/parent-requests/{id}/status | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Support | Respond support request | Submit | Support response saved | POST /api/v1/support-requests/{id}/respond | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| School Admin | Settings | Save setting | Submit | Tenant setting upserted | PUT /api/v1/tenant-settings | SchoolAdminPortalPage.jsx / schoolApi.js | Needs Figma/prototype build |
| System Admin | Dashboard | Open platform metric cards | Navigation | System sections | GET /api/v1/system-admin/dashboard | SystemAdminPortalPage.jsx | Needs Figma/prototype build |
| System Admin | Tenants | Open tenant detail | Navigation | /app/system/tenants/:tenantId | GET /api/v1/tenants/{id} | SystemAdminPortalPage.jsx | Needs Figma/prototype build |
| System Admin | Tenant detail | Activate tenant | Mutation | Tenant active | POST /api/v1/tenants/{id}/activate | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Tenant detail | Suspend tenant | Mutation | Tenant suspended | POST /api/v1/tenants/{id}/suspend | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Tenant detail | Reactivate tenant | Mutation | Tenant active | POST /api/v1/tenants/{id}/reactivate | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Tenant detail | Create school admin | Submit | One-time credential returned | POST /api/v1/system-admin/tenants/{id}/school-admins | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Tenant detail | Export tenant data | Safe operation | Data export request created | POST /api/v1/system-admin/tenants/{id}/data-export | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Tenant detail | Request tenant deletion | Safe request | Deletion request created | POST /api/v1/system-admin/tenants/{id}/data-deletion-request | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Onboarding | Create tenant | Submit | Tenant ID stored for next steps | POST /api/v1/tenants | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Onboarding | Assign plan | Submit | Subscription created | POST /api/v1/tenants/subscriptions | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Onboarding | Create initial school admin | Submit | Credential returned | POST /api/v1/system-admin/tenants/{id}/school-admins | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Onboarding | Activate tenant | Mutation | Tenant active | POST /api/v1/tenants/{id}/activate | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Support | Respond support ticket | Submit | Ticket response/status updated | POST /api/v1/system-admin/support-tickets/{id}/respond | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Feature flags | Save feature flag | Submit | Flag upserted | PUT /api/v1/feature-flags | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Announcements | Create platform announcement | Submit | Announcement created | POST /api/v1/system-admin/announcements | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| System Admin | Settings | Save system setting | Submit | Setting upserted | PUT /api/v1/system-settings | SystemAdminPortalPage.jsx / systemApi.js | Needs Figma/prototype build |
| Teacher / School Admin | AI analysis/prediction | Run analysis / prediction / review pain point | Backend-supported action | Teacher analytics / review UI if designed | POST/GET/PUT /api/v1/ai/analysis and /api/v1/ai/prediction | AiAnalysisController.cs / AiPredictionController.cs | Backend-supported; ensure prototype represents analytics review if presentation flow needs it |
| Teacher / School Admin | AI documents | Upload/delete AI reference document | Backend-supported action | Document corpus changed | POST/DELETE /api/v1/ai/documents | AiDocumentsController.cs | Backend-supported; page not explicit in current frontend route inventory |
| School Admin | Suggestion moderation | Moderate student suggestions | Backend-supported action | Suggestion moderated | POST /api/v1/suggestions/{id}/moderate | SuggestionsController.cs | Backend-supported; route maps suggestions only for student create |
| Tenant roles | Files | Upload/download/delete/archive files | Shared file action | File stored/downloaded/archived | /api/v1/files/* | FilesController.cs / FileUpload.jsx / FileDownloadButton.jsx | Use where attachment/material/document flows need it |
| Teacher / School Admin | Office hours | Create/update/cancel office-hour slot | Backend-supported action | Office hours updated | POST/PUT/POST /api/v1/office-hours | OfficeHoursController.cs | Frontend currently focuses list/attendance; prototype may need creation if discussion includes office-hour management |
| School Admin / System Admin | Audit | View audit/platform audit | Read | Audit table | GET /api/v1/audit or /api/v1/platform-audit | AuditController.cs | Included in admin pages |

## 9. Modal / Dialog / Drawer / Bottom Sheet Inventory

| Role | Trigger screen | Surface | Content/buttons | Evidence | Prototype requirement |
| --- | --- | --- | --- | --- | --- |
| All authenticated | Header menu button | Sidebar/drawer toggle | Opens/collapses AppShell navigation on small screens | Header.jsx / Sidebar.jsx | Must be represented in responsive prototype. |
| All authenticated | Shared messages | New conversation modal/form | Fields: participant user ID, optional student ID, subject, first message; buttons Send and Cancel | CommunicationPage.jsx | Critical for messaging demo. |
| Student | Material comments / community posts / suggestion forms | Inline form states | Not modal in current implementation, but prototype can use card or sheet on mobile if Figma uses that pattern | StudentPortalPage.jsx | Do not remove submit state. |
| Student | Quiz attempt | Question answer blocks | Inline interactive form; Save answers and Submit attempt actions | StudentPortalPage.jsx | Critical for student demo. |
| Teacher / School Admin | Quiz detail | Question editor / assignment card | Inline cards in source; may map to modal/sheet in Figma if needed | TeacherPortalPage.jsx | Must expose add/update/delete/publish/assign. |
| Teacher / School Admin | CV candidate row | Candidate review controls | Inline per-row confirm/reject/override controls with student/status/notes fields | VisionStaffPage.jsx | Critical for CV demo if shown. |
| Parent | Documents | Create document request form | Inline current form; may be bottom sheet in mobile prototype | ParentPortalPage.jsx | Critical for parent flow. |
| School Admin | Entity create forms | Academic year/term/grade/class/subject/user/link/assignment/announcement/settings forms | Inline cards in source; may be modal/sheet in prototype | SchoolAdminPortalPage.jsx | Critical for admin setup demo. |
| System Admin | Tenant onboarding forms | Create tenant, assign plan, create admin, activate | Inline multi-step forms | SystemAdminPortalPage.jsx | Critical for platform demo. |
| Status pages | Forbidden/session/suspended/not found | Status card | Single CTA: Go home or Sign in | StatusPages.jsx | Required fallback coverage. |

## 10. Form Inventory

| Role | Form | Fields | Submit/result | Evidence |
| --- | --- | --- | --- | --- |
| Guest | Request demo | firstName, lastName, company, jobTitle, country, city, email, phone, message | Submit; success state is local only | RequestDemoPage.jsx |
| Anonymous | Login | userId, password | Submit to login; success redirects by role | LoginPage.jsx / authApi.js |
| All authenticated | Security | currentPassword, newPassword, confirmNewPassword | Submit to change password; validation/errors/success | SecurityPage.jsx / authApi.js |
| Shared tenant roles | New conversation | participantUserId, studentId, subject, firstMessage | Creates conversation | CommunicationPage.jsx |
| Shared tenant roles | Thread reply | draft message body | Posts message to conversation | CommunicationPage.jsx |
| Student | Resource comment | body | Adds material comment | StudentPortalPage.jsx |
| Student | Homework submission | content | Submits homework | StudentPortalPage.jsx |
| Student | Quiz answers | per-question answer values | Save answers; submit attempt | StudentPortalPage.jsx |
| Student | AI tutor | message | Sends tutor prompt | StudentPortalPage.jsx |
| Student | Community post | content | Creates community post | StudentPortalPage.jsx |
| Student | Competition submission | content | Submits/updates entry work | StudentPortalPage.jsx |
| Student | Suggestion | title, body | Submits suggestion | StudentPortalPage.jsx |
| Teacher / School Admin | AI quiz generation | payload derived from class/subject/unit/options | Generates draft quiz | TeacherPortalPage.jsx / teacherApi.js |
| Teacher / School Admin | Question editor | question text/options/answer/score | Add/update/delete question | TeacherPortalPage.jsx |
| Teacher / School Admin | Assign quiz | class/student assignment inputs | Assigns quiz | TeacherPortalPage.jsx |
| Teacher / School Admin | Submission grading | grades and feedback | Grades attempt and sends feedback | TeacherPortalPage.jsx |
| Teacher / School Admin | Vision start session | session title | Starts CV session | VisionStaffPage.jsx |
| Teacher / School Admin | Vision analyze frame | frame/file/base64 | Analyzes classroom frame | VisionStaffPage.jsx / visionApi.js |
| Teacher / School Admin | Vision candidate review | studentId, status, notes | Confirm/reject/override candidate | VisionStaffPage.jsx |
| Parent | Document request | child/student, request type/title/body; optional attachment | Creates and tracks parent request | ParentPortalPage.jsx / parentApi.js |
| School Admin | User/entity/settings forms | varies by academic year/term/grade/class/subject/user/link/assignment/announcement/support/settings | Creates/updates/responds | SchoolAdminPortalPage.jsx / schoolApi.js |
| System Admin | Tenant onboarding | tenantId/name/plan/admin fullName/loginCode | Creates tenant subscription/admin and activates | SystemAdminPortalPage.jsx / systemApi.js |
| System Admin | Feature flag/settings/announcement/support forms | key/value/status/body/title | Saves platform configuration or response | SystemAdminPortalPage.jsx / systemApi.js |

## 11. Data and API Dependency Map

### Backend controller endpoints

| Controller | Base route | Policy hint | HTTP actions |
| --- | --- | --- | --- |
| AcademicYearsController.cs | api/v1/academic-years | TenantMember, SchoolAdminOnly | GET; GET {id}; POST; PUT {id}; DELETE {id} |
| AccountController.cs | api/v1/account | SelfAccount | POST login; POST refresh; POST logout; POST revoke; POST change-password; POST forgot-password; POST reset-password |
| AiAnalysisController.cs | api/v1/ai/analysis | TenantMember, TeacherOrSchoolAdmin | POST; GET {studentId}/history; PUT {painPointId}/review |
| AiDocumentsController.cs | api/v1/ai/documents | TeacherOrSchoolAdmin | POST; DELETE {documentId} |
| AiPredictionController.cs | api/v1/ai/prediction | TenantMember, TeacherOrSchoolAdmin | POST; GET {studentId}/history; PUT {studentId}/learning-profile |
| AiQuizController.cs | api/v1/ai/quiz | TeacherOrSchoolAdmin | POST draft |
| AiTutorController.cs | api | TenantMember | POST chat |
| AiUsageController.cs | api/v1/ai-usage | SchoolAdminOnly | POST; GET; GET summary |
| AnnouncementsController.cs | api/v1/announcements | TenantMember, SchoolAdminOnly | GET; POST; POST {id}/publish |
| AuditController.cs | api/v1 | SchoolAdminOnly, SystemAdminOnly | GET audit; GET platform-audit |
| BadgesController.cs | api/v1 | TenantMember | GET badges; GET students/{studentId}/badges; POST students/{studentId}/badges; GET students/{studentId}/streak; POST students/{studentId}/streak |
| ClassesController.cs | api/v1/classes | TenantMember, SchoolAdminOnly | GET; GET {id}; POST; PUT {id}; DELETE {id} |
| ClassroomVisionController.cs | api/v1/vision | TeacherOrSchoolAdmin | POST sessions; GET sessions; GET sessions/{id}; POST sessions/{id}/end; POST sessions/{id}/analyze; GET sessions/{id}/frames; GET sessions/{id}/candidates; GET sessions/{id}/summary; POST candidates/{candidateId}/confirm; POST candidates/{candidateId}/reject; POST candidates/{candidateId}/override; POST enrollments; GET enrollments; GET enrollment-assets/status; POST enrollments/{enrollmentId}/asset; GET enrollments/{enrollmentId}/asset/download |
| CommunitiesController.cs | api/v1 | TenantMember | GET communities; GET communities/{id}; POST communities; PUT communities/{id}; POST communities/{id}/archive; GET communities/{id}/members; POST communities/{id}/join; POST communities/{id}/leave; POST communities/{id}/members; POST communities/{id}/posts; GET communities/{id}/posts; DELETE posts/{postId}; POST posts/{postId}/comments; DELETE comments/{commentId}; POST posts/{postId}/reports; POST posts/{postId}/moderate |
| CompetitionsController.cs | api/v1/competitions | TenantMember | GET; GET {id}; POST; PUT {id}; POST {id}/publish; POST {id}/archive; POST {id}/close; POST {id}/entries; POST {id}/entries/{entryId}/score; GET {id}/leaderboard; POST {id}/submissions; GET {id}/submissions/me; GET {id}/submissions |
| ConversationsController.cs | api/v1/conversations | TenantMember | POST; GET; GET {id}; GET {id}/participants; POST {id}/messages; GET {id}/messages; POST {id}/messages/{messageId}/read |
| DevE2EController.cs | api/v1/dev/e2e | Mixed/none at controller level | POST reset |
| EnrollmentsController.cs | api/v1/enrollments | TenantMember, SchoolAdminOnly | GET; POST; POST {id}/withdraw |
| FilesController.cs | api/v1/files | TenantMember | POST upload; GET {id}/metadata; GET {id}/download; POST {id}/signed-download; GET download; DELETE {id}; POST; GET; GET {id}; POST {id}/archive |
| GamificationController.cs | api/v1 | TenantMember | GET students/{studentId}/points; GET students/{studentId}/points/ledger; POST students/{studentId}/points; GET gamification/leaderboard; GET gamification/rules; PUT gamification/rules |
| GradesController.cs | api/[controller] | TenantMember, TeacherOrSchoolAdmin | GET GetAllGrades; GET GetGradeById; POST AddGrade; PUT UpdateGrade; DELETE DeleteGrade |
| HomeworkController.cs | api/v1/homework | TenantMember, TeacherOrSchoolAdmin, StudentOnly | POST; GET; PUT {id}; POST {id}/publish; GET {id}; GET {id}/submissions; POST submissions/{submissionId}/grade; GET assigned; POST {id}/submit; GET {id}/my-submission |
| LessonMaterialController.cs | api/[controller] | TenantMember, TeacherOrSchoolAdmin | POST UploadMaterial; GET GetMaterialByLessonId; POST AddMaterial; PUT UpdateMaterial; DELETE DeleteMaterial |
| LessonsController.cs | api/[controller] | TenantMember, TeacherOrSchoolAdmin | GET GetLessonsByUnitId; POST AddLesson; PUT UpdateLesson; DELETE DeleteLesson |
| MyTenantController.cs | api/v1/my-tenant | SchoolAdminOnly | GET; GET subscription; GET usage; POST renewal-requests |
| NotificationPreferencesController.cs | api/v1/notification-preferences | TenantMember | GET; PUT |
| NotificationsController.cs | api/v1/notifications | Mixed/none at controller level | GET; GET unread-count; PATCH {id}/read; PATCH read-all |
| OfficeHoursController.cs | api/v1 | TenantMember | POST office-hours; PUT office-hours/{id}; POST office-hours/{id}/cancel; GET office-hours/mine; GET office-hours/available; POST office-hours/{id}/bookings; GET office-hours/{id}/bookings; POST bookings/{bookingId}/cancel; POST bookings/{bookingId}/attendance |
| ParentController.cs | api/v1/parent | ParentOnly | GET dashboard; GET children; GET children/{childId}; GET children/{childId}/attendance |
| ParentRequestsController.cs | api/v1/parent-requests | TenantMember, ParentOnly, SchoolAdminOnly | POST; GET; GET {id}; POST {id}/responses; POST {id}/status; POST {id}/attachment; GET {id}/attachment/download; POST {id}/response-document; GET {id}/responses/{responseId}/document/download |
| ParentVisionController.cs | api/v1/parent/vision | ParentOnly | GET children/{childId}/engagement-summary |
| PerformanceController.cs | api/v1 | TenantMember, TenantStaff | GET me/students; GET performance/class/{classId}; GET performance/subject/{subjectId} |
| QuizAttemptsController.cs | api/v1 | StudentOnly | GET assigned-quizzes; POST quizzes/{id}/attempts; GET attempts/{attemptId}; PUT attempts/{attemptId}/answers; POST attempts/{attemptId}/submit; GET quizzes/{id}/my-attempts; GET attempts/{attemptId}/result |
| QuizController.cs | api/[controller] | TenantMember, TeacherOrSchoolAdmin | GET GetAllQuizzes; GET GetQuizById; GET GetQuizzesByType; POST AddQuiz; PUT UpdateQuiz; DELETE DeleteQuiz |
| QuizGradingController.cs | api/v1/submissions | TeacherOrSchoolAdmin | GET {attemptId}; POST {attemptId}/grade; POST {attemptId}/feedback |
| QuizzesController.cs | api/v1/quizzes | TeacherOrSchoolAdmin | GET; GET {id}; POST; PUT {id}; POST {id}/questions; PUT {id}/questions/{questionId}; DELETE {id}/questions/{questionId}; POST {id}/publish; POST {id}/archive; POST {id}/assignments; GET {id}/assignments; GET {id}/submissions; GET {id}/analytics |
| ReportsController.cs | api/v1/reports | SchoolAdminOnly | GET tenant-users; GET assessment-summary; GET audit-activity; GET ai-usage-activity |
| ResourceCommentsController.cs | api/v1/lesson-materials/{materialId}/comments | TenantMember | POST; GET; PUT {commentId}; DELETE {commentId} |
| SchoolAdminController.cs | api/v1/school-admin | SchoolAdminOnly | GET dashboard; GET relationships; POST relationships; POST relationships/{id}/deactivate; GET teacher-class-assignments; POST teacher-class-assignments; POST teacher-class-assignments/{id}/deactivate |
| SettingsController.cs | api/v1 | SchoolAdminOnly, SystemAdminOnly, TenantMember | GET tenant-settings; PUT tenant-settings; GET system-settings; PUT system-settings; GET feature-flags; PUT feature-flags; GET feature-flags/{key}/evaluate |
| StudentAttendanceController.cs | api/v1/student/attendance | StudentOnly | GET |
| StudentLessonsController.cs | api/v1/student/lessons | StudentOnly | POST {lessonId}/complete |
| StudentProgressController.cs | api/v1/students/{studentId} | TenantMember | GET lesson-progress; GET subject-progress; GET progress-summary; GET metric-history; GET attempt-history; GET insights; GET pain-points; GET recommendations; GET predictions |
| StudentVisionController.cs | api/v1/student/vision | StudentOnly | GET engagement-summary |
| SubjectsController.cs | api/[controller] | TenantMember, TeacherOrSchoolAdmin | GET GetSubjects; GET GetSubjectById/{id}; GET GetSubjectsByGradeIdAsync; POST AddSubject; PUT UpdateSubject; DELETE DeleteSubject/{id} |
| SuggestionsController.cs | api/v1/suggestions | TenantMember, SchoolAdminOnly | POST; GET; POST {id}/moderate |
| SupportRequestsController.cs | api/v1/support-requests | TenantMember, SchoolAdminOnly | POST; GET; GET {id}; POST {id}/respond |
| SystemAdminController.cs | api/v1/system-admin | SystemAdminOnly | GET dashboard; GET usage; GET ai-usage; GET storage; GET subscriptions; GET support-tickets; POST support-tickets/{id}/respond; GET announcements; POST announcements; POST tenants/{id}/school-admins; GET operational-status; POST tenants/{id}/data-export; POST tenants/{id}/data-deletion-request |
| TeacherAssignmentsController.cs | api/v1/teacher-subject-assignments | TenantMember, SchoolAdminOnly | GET; POST; DELETE {id} |
| TeacherController.cs | api/v1/teacher | TeacherOrSchoolAdmin | GET dashboard; GET classes; GET subjects; GET classes/{classId}/students |
| TenantUsersController.cs | api/v1/tenant-users | SchoolAdminOnly | POST; GET; GET {id}; POST {id}/enable; POST {id}/disable; POST {id}/reset-credential |
| TenantsController.cs | api/v1/tenants | SystemAdminOnly | GET; GET {id}; POST; POST {id}/activate; POST {id}/suspend; POST {id}/reactivate; POST {id}/archive; GET plans; POST subscriptions; GET {id}/subscription; POST renewals/{renewalId}/process; GET {id}/usage |
| TermsController.cs | api/v1/terms | TenantMember, SchoolAdminOnly | GET; GET {id}; POST; PUT {id}; DELETE {id} |
| UnitsController.cs | api/[controller] | TenantMember, TeacherOrSchoolAdmin | GET GetUnitsBySubjectId; POST AddUnit; PUT UpdateUnit; DELETE DeleteUnit |

### AI service internal endpoints

| AI file | Routes |
| --- | --- |
| ai/app/api.py | GET /; GET /health/live; GET /health/ready |
| ai/app/internal/v1/router.py | POST /tutor; POST /quiz/draft; POST /analysis; POST /prediction; POST /documents; POST /documents/{document_id}/reindex; DELETE /documents/{document_id}; GET /documents/{document_id}/status; GET /metrics |

## 12. End-to-End Flow Inventory

| Flow | Role(s) | Start route | Step sequence | States required |
| --- | --- | --- | --- | --- |
| Guest request demo then sign in | Guest | / | Home → Request demo → fill form → submit success → Sign in → Login page. | Use loading, empty, error, success, and unauthorized states where applicable |
| Login role redirect | Anonymous + all roles | /login | Enter user ID/password → POST login → redirect to /app/student, /app/teacher, /app/parent, /app/school, or /app/system according to role. | Use loading, empty, error, success, and unauthorized states where applicable |
| Student learning with AI tutor | Student | /app/student | Dashboard → Subjects → Subject details → Unit lessons → Lesson detail → Complete lesson → AI Tutor → send question → view answer/sources. | Use loading, empty, error, success, and unauthorized states where applicable |
| Student quiz attempt | Student | /app/student/quizzes | Quiz list → Quiz detail → Start → Attempt → Save answers → Submit → Result. | Use loading, empty, error, success, and unauthorized states where applicable |
| Student homework submission | Student | /app/student/homework | Homework list → Homework detail → enter answer → Submit → view my submission/status. | Use loading, empty, error, success, and unauthorized states where applicable |
| Student community/competition/gamification | Student | /app/student/communities | Communities → community detail → Join → post; Competitions → detail → Enter → submit work → leaderboard; Badges/Streaks/Points views. | Use loading, empty, error, success, and unauthorized states where applicable |
| Teacher AI quiz and grading | Teacher / School Admin | /app/teacher | Teacher dashboard → AI quiz generation → generate draft → quiz detail → edit/add questions → publish → assign → submissions → grade → feedback. | Use loading, empty, error, success, and unauthorized states where applicable |
| Teacher student analytics and points | Teacher / School Admin | /app/teacher/students | Students → student detail → progress/pain points/recommendations/predictions → award points → ledger updates. | Use loading, empty, error, success, and unauthorized states where applicable |
| Teacher CV attendance review | Teacher / School Admin | /app/vision | CV sessions → start session → open session → upload/analyze frame → review candidates → confirm/reject/override → end session. | Use loading, empty, error, success, and unauthorized states where applicable |
| Parent monitoring and document request | Parent | /app/parent | Dashboard → Children → child detail → progress/attendance/points/vision → Documents → create request → track status → download response. | Use loading, empty, error, success, and unauthorized states where applicable |
| Parent/teacher messaging | Parent + Teacher | /app/messages | Messages → New conversation or existing thread → send message → notification/read states. | Use loading, empty, error, success, and unauthorized states where applicable |
| School admin setup | School Admin | /app/school | Dashboard → create academic year/term/grade/class/subject → create users → link parent/student → assign teacher to class/subject. | Use loading, empty, error, success, and unauthorized states where applicable |
| School admin operations | School Admin | /app/school | Announcements create/publish → document requests respond/resolve → reports → AI usage/storage/subscription/support/audit/settings. | Use loading, empty, error, success, and unauthorized states where applicable |
| System admin tenant onboarding | System Admin | /app/system/onboarding | Create tenant → assign plan → create school admin → activate → open tenant detail. | Use loading, empty, error, success, and unauthorized states where applicable |
| System admin tenant operations | System Admin | /app/system/tenants | Tenant list → detail → activate/suspend/reactivate → export data → request deletion → usage/subscription/admin credentials. | Use loading, empty, error, success, and unauthorized states where applicable |
| System admin platform operations | System Admin | /app/system | Dashboard → usage/AI/storage/support/respond → feature flags/settings → health/errors/security/backups/audit. | Use loading, empty, error, success, and unauthorized states where applicable |
| Notification deep link | Tenant roles | /app/notifications | Notification center → open actionUrl if present → mark read; preferences control in-app/email categories. | Use loading, empty, error, success, and unauthorized states where applicable |
| Unauthorized/wrong role | All roles | Any protected route | Navigate to route outside allowed role → RoleGuard redirects/renders forbidden status. | Use loading, empty, error, success, and unauthorized states where applicable |
| Expired or suspended session | All authenticated users | Any app route | Expired auth → session-expired; suspended tenant → suspended; both link to login. | Use loading, empty, error, success, and unauthorized states where applicable |
| 404 fallback | All users | Unknown route | Unknown URL → Not found → Go home. | Use loading, empty, error, success, and unauthorized states where applicable |

## 13. Role Permission Matrix

| Feature/screen group | Guest | Student | Teacher | Parent | School Admin | System Admin |
| --- | --- | --- | --- | --- | --- | --- |
| Public marketing pages | View | No access | No access | No access | No access | No access |
| Login | View/submit | Anonymous only | Anonymous only | Anonymous only | Anonymous only | Anonymous only |
| Profile/security | No access | Own account | Own account | Own account | Own account | Own account |
| Notifications/messages | No access | Tenant member | Tenant member | Tenant member | Tenant member | No shared tenant inbox route in nav |
| Student learning/AI tutor/quizzes/homework | No access | Own only | No access | No access | No access | No access |
| Student progress/attendance/engagement | No access | Own only | Assigned class/student view via teacher analytics | Linked child only | Tenant reports/admin where applicable | No access |
| Teacher classes/students/quizzes/grading | No access | No access | Assigned class only | No access | Tenant staff/admin access | No access |
| CV attendance sessions | No access | Own summary only | Create/review assigned/tenant sessions | Linked child summary only | Create/review tenant sessions | No access |
| Parent child monitoring/documents | No access | No access | Respond/message where applicable | Linked child only; create requests | Tenant manage/respond | No access |
| School admin tenant management | No access | No access | No access | No access | Tenant only create/edit/respond/manage | No access |
| System admin platform management | No access | No access | No access | No access | No access | Platform-wide |
| Feature flags/system settings/platform audit | No access | No access | No access | No access | No access | Platform-wide |

## 14. Screen State Matrix

| Screen | Route | Loading | Empty | Error | Permission | Success |
| --- | --- | --- | --- | --- | --- | --- |
| Home public landing | / | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Request demo | /request-demo | Not required or form pending state | N/A or no-data copy | API error state / validation errors | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Events | /events | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Activities | /activities | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| News | /news | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Login | /login | Not required or form pending state | N/A or no-data copy | API error state / validation errors | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| App home / role redirect shell | /app | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Profile | /app/profile | Not required or form pending state | N/A or no-data copy | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Security | /app/security | Not required or form pending state | N/A or no-data copy | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Shared notifications | /app/notifications | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Notification preferences | /app/notifications/preferences | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Messages list | /app/messages | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Message thread | /app/messages/:conversationId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Forbidden | /forbidden | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Session expired | /session-expired | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Suspended tenant | /suspended | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Not found | * | Not required or form pending state | N/A or no-data copy | Static page fallback only | Public access | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Student dashboard | /app/student | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subjects list | /app/student/subjects | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subject details | /app/student/subjects/:subjectId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Units list | /app/student/units | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Unit details/lessons | /app/student/units/:unitId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Lessons list | /app/student/lessons | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Lesson detail | /app/student/lessons/:lessonId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Material detail | /app/student/materials/:materialId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Material comments | /app/student/comments/:materialId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Homework list | /app/student/homework | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Homework detail | /app/student/homework/:homeworkId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Quiz list | /app/student/quizzes | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Quiz detail | /app/student/quizzes/:quizId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Quiz attempt | /app/student/quiz-attempts/:attemptId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Quiz result | /app/student/quiz-attempts/:attemptId/result | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| AI tutor | /app/student/ai-tutor | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Recommendations | /app/student/recommendations | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Progress | /app/student/progress | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Attendance | /app/student/attendance | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Engagement | /app/student/engagement | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Communities | /app/student/communities | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Community detail | /app/student/communities/:communityId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Competitions | /app/student/competitions | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Competition detail | /app/student/competitions/:competitionId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Leaderboard | /app/student/leaderboard/:competitionId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Office hours | /app/student/office-hours | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Student notifications | /app/student/notifications | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Announcements | /app/student/announcements | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Suggestions | /app/student/suggestions | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Badges | /app/student/badges | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Streaks | /app/student/streaks | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Settings | /app/student/settings | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Student profile | /app/student/profile | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Student CV engagement | /app/student/vision | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Teacher dashboard | /app/teacher | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Classes | /app/teacher/classes | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Class detail | /app/teacher/classes/:classId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subjects | /app/teacher/subjects | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subject units | /app/teacher/subjects/:subjectId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Unit lessons | /app/teacher/units/:unitId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Lesson detail | /app/teacher/lessons/:lessonId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Students | /app/teacher/students | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Student detail | /app/teacher/students/:studentId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Quizzes | /app/teacher/quizzes | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Quiz detail | /app/teacher/quizzes/:quizId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| AI quiz generation | /app/teacher/quiz-generate | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Submission detail | /app/teacher/submissions/:attemptId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Assignments alias | /app/teacher/assignments | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Teacher notifications | /app/teacher/notifications | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Teacher office hours | /app/teacher/office-hours | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Teacher settings | /app/teacher/settings | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| CV sessions | /app/vision | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| CV session detail | /app/vision/sessions/:sessionId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parent dashboard | /app/parent | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Children | /app/parent/children | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Child overview | /app/parent/children/:childId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Child progress | /app/parent/children/:childId/progress | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Child attendance | /app/parent/children/:childId/attendance | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Child points | /app/parent/children/:childId/points | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Document requests | /app/parent/documents | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parent conversations | /app/parent/conversations | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parent notifications | /app/parent/notifications | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parent announcements | /app/parent/announcements | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parent settings | /app/parent/settings | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parent profile | /app/parent/profile | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Child engagement | /app/parent/children/:childId/vision | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| School dashboard | /app/school | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| School profile | /app/school/profile | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Academic years | /app/school/academic-years | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Terms | /app/school/terms | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Grades | /app/school/grades | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Classes | /app/school/classes | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subjects | /app/school/subjects | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Users | /app/school/users | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Students | /app/school/students | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Teachers | /app/school/teachers | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Parents | /app/school/parents | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Relationships | /app/school/relationships | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Teacher assignments | /app/school/teacher-assignments | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Curriculum | /app/school/curriculum | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Announcements | /app/school/announcements | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Document requests | /app/school/document-requests | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Communities | /app/school/communities | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Competitions | /app/school/competitions | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Reports | /app/school/reports | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| AI usage | /app/school/ai-usage | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Storage | /app/school/storage | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subscription | /app/school/subscription | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Support | /app/school/support | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Audit log | /app/school/audit-log | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Settings | /app/school/settings | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| System dashboard | /app/system | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Tenants | /app/system/tenants | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Tenant detail | /app/system/tenants/:tenantId | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Onboarding | /app/system/onboarding | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Plans | /app/system/plans | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Subscriptions | /app/system/subscriptions | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Usage | /app/system/usage | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| AI usage | /app/system/ai-usage | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Storage | /app/system/storage | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Support | /app/system/support | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Feature flags | /app/system/feature-flags | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Announcements | /app/system/announcements | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Health | /app/system/health | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Error monitoring | /app/system/errors | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Audit | /app/system/audit | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Security | /app/system/security | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Backups | /app/system/backups | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |
| Settings | /app/system/settings | Skeleton/spinner required | Empty list/card state required for list/detail lookups | API error state / validation errors | Forbidden/wrong-role fallback for protected routes | Mutation success alert/toast where actions exist; otherwise normal page loaded |

## 15. Existing Prototype Coverage Audit

No existing prototype files were present inside the uploaded `drasa-x-main.zip`. The scan found no `cloud-design-prototype-spec`, no `.dc.html`, and no Figma export. Therefore, prototype coverage cannot be verified from this ZIP. For the next Claude Design step, treat every route and action above as required coverage unless the separately supplied prototype/figma file proves otherwise.

### 15.1 Prototype screens found

| Prototype screen | Matching real screen | Coverage |
| --- | --- | --- |
| None found in uploaded ZIP | N/A | Not verifiable |

### 15.2 Required screens that must be built or verified in Claude Design

| Required screen/route | Role/access | Priority |
| --- | --- | --- |
| / | Guest/Public | Critical for complete prototype |
| /request-demo | Guest/Public | High |
| /events | Guest/Public | High |
| /activities | Guest/Public | High |
| /news | Guest/Public | High |
| /login | Anonymous only | Critical for complete prototype |
| /app | Any authenticated role | Critical for complete prototype |
| /app/profile | Any authenticated role | Critical for complete prototype |
| /app/security | Any authenticated role | Critical for complete prototype |
| /app/notifications | Any authenticated role | Critical for complete prototype |
| /app/notifications/preferences | Any authenticated role | Critical for complete prototype |
| /app/messages | Any authenticated role | Critical for complete prototype |
| /app/messages/:conversationId | Any authenticated role | Critical for complete prototype |
| /app/vision | Teacher, School Admin | Critical for complete prototype |
| /app/vision/sessions/:sessionId | Teacher, School Admin | Critical for complete prototype |
| /app/student/vision | Student | Critical for complete prototype |
| /app/parent/children/:childId/vision | Parent | Critical for complete prototype |
| /app/student | Student | Critical for complete prototype |
| /app/student/subjects | Student | Critical for complete prototype |
| /app/student/subjects/:subjectId | Student | Critical for complete prototype |
| /app/student/units | Student | Critical for complete prototype |
| /app/student/units/:unitId | Student | Critical for complete prototype |
| /app/student/lessons | Student | Critical for complete prototype |
| /app/student/lessons/:lessonId | Student | Critical for complete prototype |
| /app/student/materials/:materialId | Student | Critical for complete prototype |
| /app/student/comments/:materialId | Student | Critical for complete prototype |
| /app/student/homework | Student | Critical for complete prototype |
| /app/student/homework/:homeworkId | Student | Critical for complete prototype |
| /app/student/quizzes | Student | Critical for complete prototype |
| /app/student/quizzes/:quizId | Student | Critical for complete prototype |
| /app/student/quiz-attempts/:attemptId | Student | Critical for complete prototype |
| /app/student/quiz-attempts/:attemptId/result | Student | Critical for complete prototype |
| /app/student/ai-tutor | Student | Critical for complete prototype |
| /app/student/recommendations | Student | Critical for complete prototype |
| /app/student/progress | Student | Critical for complete prototype |
| /app/student/attendance | Student | Critical for complete prototype |
| /app/student/engagement | Student | Critical for complete prototype |
| /app/student/communities | Student | Critical for complete prototype |
| /app/student/communities/:communityId | Student | Critical for complete prototype |
| /app/student/competitions | Student | Critical for complete prototype |
| /app/student/competitions/:competitionId | Student | Critical for complete prototype |
| /app/student/leaderboard/:competitionId | Student | Critical for complete prototype |
| /app/student/office-hours | Student | Critical for complete prototype |
| /app/student/notifications | Student | Critical for complete prototype |
| /app/student/announcements | Student | Critical for complete prototype |
| /app/student/suggestions | Student | Critical for complete prototype |
| /app/student/badges | Student | Critical for complete prototype |
| /app/student/streaks | Student | Critical for complete prototype |
| /app/student/settings | Student | Critical for complete prototype |
| /app/student/profile | Student | Critical for complete prototype |
| /app/teacher | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/classes | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/classes/:classId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/subjects | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/subjects/:subjectId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/units/:unitId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/lessons/:lessonId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/students | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/students/:studentId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/quizzes | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/quizzes/:quizId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/quiz-generate | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/submissions/:attemptId | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/assignments | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/notifications | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/office-hours | Teacher, School Admin | Critical for complete prototype |
| /app/teacher/settings | Teacher, School Admin | Critical for complete prototype |
| /app/parent | Parent | Critical for complete prototype |
| /app/parent/children | Parent | Critical for complete prototype |
| /app/parent/children/:childId | Parent | Critical for complete prototype |
| /app/parent/children/:childId/progress | Parent | Critical for complete prototype |
| /app/parent/children/:childId/attendance | Parent | Critical for complete prototype |
| /app/parent/children/:childId/points | Parent | Critical for complete prototype |
| /app/parent/documents | Parent | Critical for complete prototype |
| /app/parent/conversations | Parent | Critical for complete prototype |
| /app/parent/notifications | Parent | Critical for complete prototype |
| /app/parent/announcements | Parent | Critical for complete prototype |
| /app/parent/settings | Parent | Critical for complete prototype |
| /app/parent/profile | Parent | Critical for complete prototype |
| /app/school | School Admin | Critical for complete prototype |
| /app/school/profile | School Admin | Critical for complete prototype |
| /app/school/academic-years | School Admin | Critical for complete prototype |
| /app/school/terms | School Admin | Critical for complete prototype |
| /app/school/grades | School Admin | Critical for complete prototype |
| /app/school/classes | School Admin | Critical for complete prototype |
| /app/school/subjects | School Admin | Critical for complete prototype |
| /app/school/users | School Admin | Critical for complete prototype |
| /app/school/students | School Admin | Critical for complete prototype |
| /app/school/teachers | School Admin | Critical for complete prototype |
| /app/school/parents | School Admin | Critical for complete prototype |
| /app/school/relationships | School Admin | Critical for complete prototype |
| /app/school/teacher-assignments | School Admin | Critical for complete prototype |
| /app/school/curriculum | School Admin | Critical for complete prototype |
| /app/school/announcements | School Admin | Critical for complete prototype |
| /app/school/document-requests | School Admin | Critical for complete prototype |
| /app/school/communities | School Admin | Critical for complete prototype |
| /app/school/competitions | School Admin | Critical for complete prototype |
| /app/school/reports | School Admin | Critical for complete prototype |
| /app/school/ai-usage | School Admin | Critical for complete prototype |
| /app/school/storage | School Admin | Critical for complete prototype |
| /app/school/subscription | School Admin | Critical for complete prototype |
| /app/school/support | School Admin | Critical for complete prototype |
| /app/school/audit-log | School Admin | Critical for complete prototype |
| /app/school/settings | School Admin | Critical for complete prototype |
| /app/system | System Admin | Critical for complete prototype |
| /app/system/tenants | System Admin | Critical for complete prototype |
| /app/system/tenants/:tenantId | System Admin | Critical for complete prototype |
| /app/system/onboarding | System Admin | Critical for complete prototype |
| /app/system/plans | System Admin | Critical for complete prototype |
| /app/system/subscriptions | System Admin | Critical for complete prototype |
| /app/system/usage | System Admin | Critical for complete prototype |
| /app/system/ai-usage | System Admin | Critical for complete prototype |
| /app/system/storage | System Admin | Critical for complete prototype |
| /app/system/support | System Admin | Critical for complete prototype |
| /app/system/feature-flags | System Admin | Critical for complete prototype |
| /app/system/announcements | System Admin | Critical for complete prototype |
| /app/system/health | System Admin | Critical for complete prototype |
| /app/system/errors | System Admin | Critical for complete prototype |
| /app/system/audit | System Admin | Critical for complete prototype |
| /app/system/security | System Admin | Critical for complete prototype |
| /app/system/backups | System Admin | Critical for complete prototype |
| /app/system/settings | System Admin | Critical for complete prototype |
| /forbidden | Any role after failed guard | High |
| /session-expired | Any user with expired auth | High |
| /suspended | Tenant users when tenant is suspended | High |
| * | All users | High |

### 15.3 Prototype actions that must not be dead buttons

| Action | Screen | Role | Expected result |
| --- | --- | --- | --- |
| Open Home | Public navbar | Guest/Public | / |
| Request a demo | Public navbar | Guest/Public | /request-demo |
| Sign in | Public navbar | Guest/Public | /login |
| FAQ button | Home | Guest/Public | Inline FAQ affordance |
| Submit demo request | Request demo | Guest/Public | Success state on same page |
| Sign in | Login | Anonymous | Role-based redirect |
| Open/close sidebar | Header | All authenticated | Toggle AppShell sidebar |
| Sign out | Header | All authenticated | /login or anonymous state |
| View profile | Profile | All authenticated | /app/profile |
| Change password | Security | All authenticated | Success alert/toast |
| Go home | Forbidden/status pages | All authenticated | /app |
| Sign in | Session expired/suspended | All/auth expired | /login |
| Open preferences | Shared notifications center | Student | /app/notifications/preferences |
| Mark all read | Shared notifications center | Student | Unread count updates |
| Mark one read | Shared notifications center | Student | Notification row updates |
| New conversation | Shared messages | Student | Conversation thread |
| Open thread | Shared messages | Student | /app/messages/:conversationId |
| Send reply | Shared messages thread | Student | Message appended |
| Open preferences | Shared notifications center | Teacher | /app/notifications/preferences |
| Mark all read | Shared notifications center | Teacher | Unread count updates |
| Mark one read | Shared notifications center | Teacher | Notification row updates |
| New conversation | Shared messages | Teacher | Conversation thread |
| Open thread | Shared messages | Teacher | /app/messages/:conversationId |
| Send reply | Shared messages thread | Teacher | Message appended |
| Open preferences | Shared notifications center | Parent | /app/notifications/preferences |
| Mark all read | Shared notifications center | Parent | Unread count updates |
| Mark one read | Shared notifications center | Parent | Notification row updates |
| New conversation | Shared messages | Parent | Conversation thread |
| Open thread | Shared messages | Parent | /app/messages/:conversationId |
| Send reply | Shared messages thread | Parent | Message appended |
| Open preferences | Shared notifications center | School Admin | /app/notifications/preferences |
| Mark all read | Shared notifications center | School Admin | Unread count updates |
| Mark one read | Shared notifications center | School Admin | Notification row updates |
| New conversation | Shared messages | School Admin | Conversation thread |
| Open thread | Shared messages | School Admin | /app/messages/:conversationId |
| Send reply | Shared messages thread | School Admin | Message appended |
| Open metric cards | Dashboard | Student | Role-specific pages |
| Open subject details | Subjects | Student | /app/student/subjects/:subjectId |
| Open unit lessons | Units | Student | /app/student/units/:unitId |
| Open lesson details | Lessons | Student | /app/student/lessons/:lessonId |
| Complete lesson | Lesson detail | Student | Lesson progress updated |
| Open material | Materials | Student | /app/student/materials/:materialId |
| Submit comment | Material comments | Student | Comment list updated |
| Submit homework | Homework detail | Student | My submission saved |
| Start quiz | Quiz detail | Student | /app/student/quiz-attempts/:attemptId |
| Save answers | Quiz attempt | Student | Answers persisted |
| Submit attempt | Quiz attempt | Student | /app/student/quiz-attempts/:attemptId/result |
| Send question | AI tutor | Student | AI answer with source/citations |
| Join community | Communities | Student | Membership updated |
| Leave community | Communities | Student | Membership removed |
| Submit post | Community detail | Student | Post appears in feed |
| Enter competition | Competitions | Student | Entry created |
| Submit/update competition work | Competition detail | Student | Durable submission saved |
| Book office-hour slot | Office hours | Student | Booking created |
| Cancel booking | Office hours | Student | Booking cancelled |
| Mark all read | Student notifications | Student | Unread count reduced |
| Mark single read | Student notifications | Student | Item marked read |
| Submit suggestion | Suggestions | Student | Suggestion saved |
| Change password | Settings | Student | /app/security |
| View own engagement summary | CV engagement | Student | Engagement summary card |
| Open teacher metric cards | Dashboard | Teacher / School Admin | Classes/subjects/students/quizzes |
| Open class detail | Classes | Teacher / School Admin | /app/teacher/classes/:classId |
| Open subject units | Subjects | Teacher / School Admin | /app/teacher/subjects/:subjectId |
| Open lesson detail | Units | Teacher / School Admin | /app/teacher/lessons/:lessonId |
| Open student record | Students | Teacher / School Admin | /app/teacher/students/:studentId |
| Award points | Student record | Teacher / School Admin | Points ledger updated |
| Save gamification rule | Student record | Teacher / School Admin | Rule updated |
| Open quiz detail | Quizzes | Teacher / School Admin | /app/teacher/quizzes/:quizId |
| Add question | Quiz detail | Teacher / School Admin | Question added |
| Update question | Quiz detail | Teacher / School Admin | Question saved |
| Delete question | Quiz detail | Teacher / School Admin | Question removed |
| Publish quiz | Quiz detail | Teacher / School Admin | Quiz becomes available |
| Archive quiz | Quiz detail | Teacher / School Admin | Quiz archived |
| Assign quiz | Quiz detail | Teacher / School Admin | Assignment created |
| Generate draft | AI quiz generation | Teacher / School Admin | Draft quiz created |
| Grade submission | Submission detail | Teacher / School Admin | Grades saved |
| Send feedback | Submission detail | Teacher / School Admin | Feedback saved |
| Mark attendance | Office hours | Teacher / School Admin | Booking attendance saved |
| Start session | CV session list | Teacher / School Admin | New active session |
| Open session | CV session list | Teacher / School Admin | /app/vision/sessions/:sessionId |
| Analyze frame | CV session detail | Teacher / School Admin | Candidates generated |
| End session | CV session detail | Teacher / School Admin | Session status ended |
| Confirm candidate | CV candidate review | Teacher / School Admin | Attendance confirmed |
| Reject candidate | CV candidate review | Teacher / School Admin | Candidate rejected |
| Override candidate | CV candidate review | Teacher / School Admin | Attendance manually overridden |
| Open child metric cards | Dashboard | Parent | Child/progress/attendance/documents |
| Open child detail | Children | Parent | /app/parent/children/:childId |
| Open progress | Child detail | Parent | /app/parent/children/:childId/progress |
| Open attendance | Child detail | Parent | /app/parent/children/:childId/attendance |
| Open points | Child detail | Parent | /app/parent/children/:childId/points |
| Create document request | Documents | Parent | Request created |
| Attach request document | Documents | Parent | Attachment uploaded |
| Download response document | Documents | Parent | Browser download |
| Open shared messages | Conversations | Parent | /app/messages or /app/parent/conversations |
| Mark notification read | Notifications | Parent | Item marked read |
| View linked-child engagement | CV child engagement | Parent | Engagement summary |
| Open school metric cards | Dashboard | School Admin | School sections |
| Create academic year | Academic years | School Admin | Academic year created |
| Create term | Terms | School Admin | Term created |
| Create grade | Grades | School Admin | Grade created |
| Create class | Classes | School Admin | Class created |
| Create subject | Subjects | School Admin | Subject created |
| Create user | Users/students/teachers/parents | School Admin | Tenant user created |
| Enable/disable user | Users/students/teachers/parents | School Admin | User active state changes |
| Link parent to student | Relationships | School Admin | Relationship created |
| Deactivate relationship | Relationships | School Admin | Relationship inactive |
| Create class assignment | Teacher assignments | School Admin | Teacher assigned to class |
| Create subject assignment | Teacher assignments | School Admin | Teacher assigned to subject |
| Create announcement | Announcements | School Admin | Announcement draft/record created |
| Publish/unpublish announcement | Announcements | School Admin | Announcement visibility changes |
| Respond to request | Document requests | School Admin | Response saved |
| Mark resolved | Document requests | School Admin | Request status updated |
| Respond support request | Support | School Admin | Support response saved |
| Save setting | Settings | School Admin | Tenant setting upserted |
| Open platform metric cards | Dashboard | System Admin | System sections |
| Open tenant detail | Tenants | System Admin | /app/system/tenants/:tenantId |
| Activate tenant | Tenant detail | System Admin | Tenant active |
| Suspend tenant | Tenant detail | System Admin | Tenant suspended |
| Reactivate tenant | Tenant detail | System Admin | Tenant active |
| Create school admin | Tenant detail | System Admin | One-time credential returned |
| Export tenant data | Tenant detail | System Admin | Data export request created |
| Request tenant deletion | Tenant detail | System Admin | Deletion request created |
| Create tenant | Onboarding | System Admin | Tenant ID stored for next steps |
| Assign plan | Onboarding | System Admin | Subscription created |
| Create initial school admin | Onboarding | System Admin | Credential returned |
| Activate tenant | Onboarding | System Admin | Tenant active |
| Respond support ticket | Support | System Admin | Ticket response/status updated |
| Save feature flag | Feature flags | System Admin | Flag upserted |
| Create platform announcement | Announcements | System Admin | Announcement created |
| Save system setting | Settings | System Admin | Setting upserted |
| Run analysis / prediction / review pain point | AI analysis/prediction | Teacher / School Admin | Teacher analytics / review UI if designed |
| Upload/delete AI reference document | AI documents | Teacher / School Admin | Document corpus changed |
| Moderate student suggestions | Suggestion moderation | School Admin | Suggestion moderated |
| Upload/download/delete/archive files | Files | Tenant roles | File stored/downloaded/archived |
| Create/update/cancel office-hour slot | Office hours | Teacher / School Admin | Office hours updated |
| View audit/platform audit | Audit | School Admin / System Admin | Audit table |

## 16. Figma Mapping Preparation

| Real route/screen | Role | Title key | Figma frame/component needed | Visual priority |
| --- | --- | --- | --- | --- |
| / | Public | app.name | Needs matching Figma frame/component inspection | High |
| /request-demo | Public | public.requestDemo.title | Needs matching Figma frame/component inspection | Medium |
| /events | Public | public.events.title | Needs matching Figma frame/component inspection | Medium |
| /activities | Public | public.activities.title | Needs matching Figma frame/component inspection | Medium |
| /news | Public | public.news.title | Needs matching Figma frame/component inspection | Medium |
| /login | Auth | auth.title | Needs matching Figma frame/component inspection | High |
| /app | Shared app | nav.dashboard | Needs matching Figma frame/component inspection | Medium |
| /app/profile | Shared app | nav.profile | Needs matching Figma frame/component inspection | Medium |
| /app/security | Shared app | nav.security | Needs matching Figma frame/component inspection | Medium |
| /app/notifications | Shared app | notifications.title | Needs matching Figma frame/component inspection | Medium |
| /app/notifications/preferences | Shared app | notifications.preferencesTitle | Needs matching Figma frame/component inspection | Medium |
| /app/messages | Shared app | messages.title | Needs matching Figma frame/component inspection | Medium |
| /app/messages/:conversationId | Shared app | messages.thread | Needs matching Figma frame/component inspection | Medium |
| /app/vision | Teacher / School Admin | vision.title | Needs matching Figma frame/component inspection | Medium |
| /app/vision/sessions/:sessionId | Teacher / School Admin | vision.sessionTitleFallback | Needs matching Figma frame/component inspection | Medium |
| /app/student/vision | Student | vision.studentTitle | Needs matching Figma frame/component inspection | Medium |
| /app/parent/children/:childId/vision | Parent | vision.parentTitle | Needs matching Figma frame/component inspection | Medium |
| /app/student | Student | student.dashboard.title | Needs matching Figma frame/component inspection | High |
| /app/student/subjects | Student | student.subjects.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/subjects/:subjectId | Student | student.subjects.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/units | Student | student.units.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/units/:unitId | Student | student.units.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/lessons | Student | student.lessons.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/lessons/:lessonId | Student | student.lessons.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/materials/:materialId | Student | student.materials.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/comments/:materialId | Student | student.comments.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/homework | Student | student.homework.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/homework/:homeworkId | Student | student.homework.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/quizzes | Student | student.quizzes.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/quizzes/:quizId | Student | student.quizzes.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/quiz-attempts/:attemptId | Student | student.quizzes.attempt | Needs matching Figma frame/component inspection | Medium |
| /app/student/quiz-attempts/:attemptId/result | Student | student.quizzes.result | Needs matching Figma frame/component inspection | Medium |
| /app/student/ai-tutor | Student | student.tutor.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/recommendations | Student | student.recommendations.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/progress | Student | student.progress.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/attendance | Student | student.attendance.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/engagement | Student | student.engagement.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/communities | Student | student.communities.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/communities/:communityId | Student | student.communities.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/competitions | Student | student.competitions.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/competitions/:competitionId | Student | student.competitions.details | Needs matching Figma frame/component inspection | Medium |
| /app/student/leaderboard/:competitionId | Student | student.leaderboard.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/office-hours | Student | student.officeHours.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/notifications | Student | student.notifications.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/announcements | Student | student.announcements.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/suggestions | Student | student.suggestions.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/badges | Student | student.badges.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/streaks | Student | student.streaks.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/settings | Student | student.settings.title | Needs matching Figma frame/component inspection | Medium |
| /app/student/profile | Student | nav.profile | Needs matching Figma frame/component inspection | Medium |
| /app/teacher | Teacher / School Admin | teacher.dashboard.title | Needs matching Figma frame/component inspection | High |
| /app/teacher/classes | Teacher / School Admin | teacher.classes.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/classes/:classId | Teacher / School Admin | teacher.classes.details | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/subjects | Teacher / School Admin | teacher.subjects.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/subjects/:subjectId | Teacher / School Admin | teacher.curriculum.units | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/units/:unitId | Teacher / School Admin | teacher.curriculum.lessons | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/lessons/:lessonId | Teacher / School Admin | teacher.curriculum.lessonDetails | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/students | Teacher / School Admin | teacher.students.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/students/:studentId | Teacher / School Admin | teacher.students.details | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/quizzes | Teacher / School Admin | teacher.quizzes.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/quizzes/:quizId | Teacher / School Admin | teacher.quizzes.details | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/quiz-generate | Teacher / School Admin | teacher.generate.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/submissions/:attemptId | Teacher / School Admin | teacher.grading.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/assignments | Teacher / School Admin | nav.assignments | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/notifications | Teacher / School Admin | teacher.notifications.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/office-hours | Teacher / School Admin | teacher.officeHours.title | Needs matching Figma frame/component inspection | Medium |
| /app/teacher/settings | Teacher / School Admin | teacher.settings.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent | Parent | parent.dashboard.title | Needs matching Figma frame/component inspection | High |
| /app/parent/children | Parent | parent.children.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/children/:childId | Parent | parent.child.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/children/:childId/progress | Parent | parent.progress.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/children/:childId/attendance | Parent | parent.attendance.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/children/:childId/points | Parent | parent.points.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/documents | Parent | parent.documents.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/conversations | Parent | parent.conversations.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/notifications | Parent | parent.notifications.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/announcements | Parent | parent.announcements.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/settings | Parent | parent.settings.title | Needs matching Figma frame/component inspection | Medium |
| /app/parent/profile | Parent | nav.profile | Needs matching Figma frame/component inspection | Medium |
| /app/school | School Admin | school.pages.dashboard.title | Needs matching Figma frame/component inspection | High |
| /app/school/profile | School Admin | school.pages.profile.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/academic-years | School Admin | school.pages.academicYears.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/terms | School Admin | school.pages.terms.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/grades | School Admin | school.pages.grades.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/classes | School Admin | school.pages.classes.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/subjects | School Admin | school.pages.subjects.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/users | School Admin | school.pages.users.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/students | School Admin | school.pages.students.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/teachers | School Admin | school.pages.teachers.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/parents | School Admin | school.pages.parents.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/relationships | School Admin | school.pages.relationships.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/teacher-assignments | School Admin | school.pages.teacherAssignments.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/curriculum | School Admin | school.pages.curriculum.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/announcements | School Admin | school.pages.announcements.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/document-requests | School Admin | school.pages.documentRequests.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/communities | School Admin | school.pages.communities.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/competitions | School Admin | school.pages.competitions.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/reports | School Admin | school.pages.reports.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/ai-usage | School Admin | school.pages.aiUsage.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/storage | School Admin | school.pages.storage.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/subscription | School Admin | school.pages.subscription.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/support | School Admin | school.pages.support.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/audit-log | School Admin | school.pages.audit.title | Needs matching Figma frame/component inspection | Medium |
| /app/school/settings | School Admin | school.pages.settings.title | Needs matching Figma frame/component inspection | Medium |
| /app/system | System Admin | system.pages.dashboard.title | Needs matching Figma frame/component inspection | High |
| /app/system/tenants | System Admin | system.pages.tenants.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/tenants/:tenantId | System Admin | system.pages.tenantDetails.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/onboarding | System Admin | system.pages.onboarding.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/plans | System Admin | system.pages.plans.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/subscriptions | System Admin | system.pages.subscriptions.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/usage | System Admin | system.pages.usage.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/ai-usage | System Admin | system.pages.aiUsage.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/storage | System Admin | system.pages.storage.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/support | System Admin | system.pages.support.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/feature-flags | System Admin | system.pages.featureFlags.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/announcements | System Admin | system.pages.announcements.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/health | System Admin | system.pages.health.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/errors | System Admin | system.pages.errorMonitoring.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/audit | System Admin | system.pages.audit.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/security | System Admin | system.pages.security.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/backups | System Admin | system.pages.backups.title | Needs matching Figma frame/component inspection | Medium |
| /app/system/settings | System Admin | system.pages.settings.title | Needs matching Figma frame/component inspection | Medium |
| /forbidden | Public | pages.forbiddenTitle | Needs matching Figma frame/component inspection | Medium |
| /session-expired | Public | pages.sessionExpiredTitle | Needs matching Figma frame/component inspection | Medium |
| /suspended | Public | pages.suspendedTitle | Needs matching Figma frame/component inspection | Medium |
| * | Status | pages.notFoundTitle | Needs matching Figma frame/component inspection | Medium |

## 17. Presentation-Critical Demo Scope

| Demo flow | Exact route sequence | Required demo data | Why it matters |
| --- | --- | --- | --- |
| Student AI learning | /app/student → /app/student/subjects → lesson → /app/student/ai-tutor | Subject, unit, lesson, material, AI answer with citations, complete lesson state | Shows core AI learning value. |
| Student assessment | /app/student/quizzes → detail → attempt → result | Quiz, questions, answers, score/feedback | Shows assessment loop. |
| Teacher AI quiz + grading | /app/teacher/quiz-generate → quiz detail → publish/assign → submission grading | Draft quiz, editable questions, assignment, attempt submission, feedback | Shows teacher-side AI and human review. |
| Parent monitoring + documents | /app/parent → child detail/progress/attendance/points → documents | Linked child, progress/attendance, request status, response download | Shows parent value. |
| School admin operations | /app/school → users/classes/relationships/assignments/reports/document requests | Tenant users, school structure, reports, request response | Shows tenant administration. |
| System admin platform | /app/system → tenants → tenant detail/onboarding/health/support | Tenant, plan, subscription, support ticket, operational status | Shows SaaS/platform layer. |
| CV attendance optional highlight | /app/vision → session detail candidate review | Session, candidate rows, confirm/reject/override, engagement summary | Use if discussion includes computer vision. |

## 18. Final Build Requirements for Later Claude Design Prototype

- Use this Markdown for page/flow completeness and the supplied `.fig` file for visual style.
- Build all 126 route/screen entries or intentionally map dynamic/detail routes to reusable frames with clear states.
- Every role must have dashboard, navigation, profile/settings/security, notifications where applicable, and realistic data.
- Every action in the inventory must have visible feedback: navigation, modal, state change, success, or error.
- No “coming soon”, no lorem ipsum, no dead buttons, no pages without role context.
- Include public, auth, status, and protected-role error states, not just portal dashboards.
- Include Arabic/English and RTL/LTR visual handling because frontend i18n includes `en.js` and `ar.js` with direction support.
- Use mobile/prototype-friendly variants if Claude Design is generating a presentation prototype, but do not delete low-frequency admin and status pages.

## 19. Gaps, Conflicts, and Assumptions

| Gap/conflict | Evidence | Decision/impact |
| --- | --- | --- |
| Prototype files missing from uploaded ZIP | No `.dc.html` or cloud-design folder found | Coverage cannot be compared; later Claude Design must build/verify all routes from this report. |
| Figma file not supplied in this turn | User will provide Figma separately to Claude Design | This report uses route/product truth only, not visual decisions. |
| Some backend-supported features do not have explicit dedicated current frontend routes | Example: AI documents upload/delete, office-hour creation, suggestion moderation | Mark these as backend-supported; include in prototype only if presentation scope needs them or Figma has frames. |
| Several dynamic routes share the same React page/view prop | Example: subject details, unit details, lesson details reuse portal page components | Claude Design can reuse frame templates, but each route state/flow must be represented. |
| No source docs folder inside supplied ZIP | Only frontend/AI README plus code/tests/controllers were available | The inventory is source-code backed; if external docs conflict, prefer current route registry unless user supplies newer docs. |

## 20. Completeness Checklist

- [x] Frontend route registry inspected.
- [x] Frontend nav/sidebar configuration inspected.
- [x] Public/auth/app/status routes listed.
- [x] Student routes and actions listed.
- [x] Teacher routes and actions listed.
- [x] Parent routes and actions listed.
- [x] School Admin routes and actions listed.
- [x] System Admin routes and actions listed.
- [x] Shared notifications/messages/profile/security/status pages listed.
- [x] Backend API controllers scanned and mapped.
- [x] AI service route files scanned.
- [x] i18n English/Arabic resources noted.
- [x] Existing prototype scan performed; no prototype files found in uploaded ZIP.
- [x] Figma mapping section prepared for later `.fig` handoff.
- [x] Presentation-critical demo flows listed.
- [x] No application source code changed.
- [x] No Git history changed.

## 21. Claude Design Handoff Instruction

When sending this file to Claude Design, use this instruction: “Use the attached Figma file as the visual source of truth, and use `DERASAX_PROJECT_PAGES_AND_FLOWS_SOURCE_OF_TRUTH.md` as the product completeness source of truth. Rebuild the prototype so every route, role, screen, state, action, and presentation flow listed here is represented. Do not omit dynamic/detail/admin/status screens. Do not create dead buttons. Do not show placeholders or coming soon pages.”

---

Static inspection summary: 126 routes/screen entries, 63 navigation entries, 136 actions, 20 end-to-end flows, 54 backend controllers with endpoints, 2 AI files with routed endpoints.
