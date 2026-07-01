# DerasaX Route, Detail Page, and RBAC Audit

Audit date: 2026-07-01 · Read-only audit (no code changed). Evidence cites exact files; line numbers reflect the working tree at audit time.

Verification runs (all executed during this audit, frontend `frontend/`):

| Command | Result |
|---|---|
| `npm run typecheck` (tsc --noEmit) | exit 0 |
| `npm run lint` (eslint .) | exit 0 — **but see §10: core `no-undef` does not check JSX element names and `eslint-plugin-react` is not installed, so two confirmed render-crash bugs pass lint** (`npx eslint src/pages/system/errors/SystemErrorMonitoringPage.jsx src/pages/system/backups/SystemBackupsPage.jsx src/pages/student/units/StudentUnitsPage.jsx` → exit 0) |
| `npx vitest run src/app/router src/navigation src/layouts` | 11 files, 59 tests passed |

---

## 1. Executive Summary

| Metric | Count |
|---|---|
| Frontend routes registered in `src/app/router/routes.tsx` registry | **131** (asserted by `routeArchitecture.test.js` `EXPECTED_ROUTE_COUNT = 131`) + 4 status/fallback routes wired directly in `App.tsx:81-84` (`/forbidden`, `/session-expired`, `/suspended`, `*`) |
| Navigation items (`src/navigation/navConfig.js`) | **66** (Student 14, Teacher 8, Parent 6, SchoolAdmin 17, SystemAdmin 17, shared tenant 2, all-roles 2) + header account menu (Profile/Settings/Security) + NotificationBell + sidebar help card |
| Backend controllers / endpoints inventoried | **55 controllers**, ~230 actions (full inventory used for cross-check; §8) |
| Orphan routes (registered, unreachable from any in-app link/nav) | **28** route paths (~21% of the registry) — §2, §5 |
| Confirmed broken interactions (crash or unreachable UI action) | **9** (3 render crashes, 4 flows killed by the `rowActions` drop, breadcrumb 404 links, dead header search) — §5 |
| RBAC/authorization findings | **4 backend gaps** (legacy files endpoints), **1 FE-allowed/BE-blocked** mismatch (`/app/messages` for SystemAdmin), **2 data over-disclosure projections** — §7, §8 |
| Frontend calls to nonexistent backend endpoints | **0** — every literal URL in the five feature API clients matches a live controller action |
| Backend endpoints with no frontend consumer | **~40** (entire flows missing: curriculum authoring, enrollments, office-hour creation, suggestions moderation, renewals, forgot/reset password…) — §8 |

**Overall risk: HIGH for admin-portal completeness and click-through UX; MODERATE for security.**
The route/guard architecture itself is sound (single typed registry, consistent RoleGuards, backend policies verified to match on every consumed endpoint, and service-layer role/ownership checks confirmed on the broad `TenantMember` controllers). The dominant defects are: (a) one shared table component silently dropping every row action, which severs the System-Admin tenant detail flow and the School-Admin publish/deactivate flows; (b) three registered routes that crash on render; (c) a large "dark" surface of registered-but-unlinked routes, including the school admin's only user-creation page and the entire vision feature; (d) legacy file-metadata endpoints readable/archivable by any tenant member.

---

## 2. Route Inventory

Legend — Nav: linked from sidebar/nav config; Link: reachable via in-app `Link`/`navigate` only; ORPHAN: registered but no in-app entry point (URL-only). Guard evidence: `routes.*.tsx` per-area files; guards applied in `src/app/App.tsx:20-25` via `RoleGuard`/`PermissionGuard` (`src/features/auth/guards.jsx`).

### 2.1 Public / auth / status

| Area | Role(s) | Route | Component | Linked from nav? | Detail route? | Status | Evidence |
|---|---|---|---|---|---|---|---|
| Public | anon | `/` | `pages/public/HomePage.jsx` | PublicNavBar | — | OK (FaqButton dead — §5) | `routes.public.tsx:13` |
| Public | anon | `/request-demo` | `RequestDemoPage.jsx` | yes | — | Client-only form, no backend by design | `RequestDemoPage.jsx:19-32` |
| Public | anon | `/events`, `/activities`, `/news` | `PublicListingPage` wrappers | yes + HomePage cards | — | Intentional empty-state placeholders (no content feed) | `PublicListingPage.jsx:24-26` |
| Auth | anon-only | `/login` | `pages/auth/LoginPage.jsx` | — | — | OK; return-path sanitized via `safeReturnPath` | `LoginPage.jsx:50-51` |
| Status | any | `/forbidden`, `/session-expired`, `/suspended`, `*` (NotFound) | `pages/status/StatusPages.jsx` | — | — | OK; link-backs valid | `App.tsx:81-84`, `StatusPages.jsx:36-76` |

### 2.2 Shared authenticated (`routes.shared.tsx`)

| Role(s) | Route | Component | Nav? | Status | Evidence |
|---|---|---|---|---|---|
| any auth | `/app` | `AppHome.jsx` (pure role redirect) | header brand | OK | `AppHome.jsx:10`, `roles.ts:82-97` |
| any (perm `manage_own_account`) | `/app/profile` | `ProfilePage.jsx` | sidebar `all` + header menu | OK | `routes.shared.tsx:20-25`, `navConfig.js:116` |
| any (perm `manage_own_account`) | `/app/security` | `SecurityPage.jsx` | sidebar `all` + header menu | OK | `routes.shared.tsx:27-32` |
| any auth | `/app/notifications` | `CommunicationNotificationsPage` | sidebar (tenant roles) + NotificationBell (all incl. SystemAdmin) | OK backend-wise for all roles (`NotificationsController` is plain `[Authorize]`) | `navConfig.js:114`, `NotificationBell.jsx:36` |
| any auth | `/app/notifications/preferences` | `CommunicationPreferencesPage` | Link from notifications page | OK | `CommunicationNotificationsPage.jsx:47` |
| any auth | `/app/messages` | `CommunicationMessagesPage` | sidebar (tenant roles only) | OK for tenant roles; **SystemAdmin reachable by URL but backend `TenantMember` policy blocks → §7** | `navConfig.js:113`, `ConversationsController.cs` class policy |
| any auth | `/app/messages/:conversationId` | `CommunicationThreadPage` | Link from messages list | OK | `CommunicationMessagesPage.jsx:41` |

### 2.3 Student (`routes.student.tsx`, all `roles: [Student]`)

| Route | Component | Nav?/Linked? | Status | Evidence |
|---|---|---|---|---|
| `/app/student` | StudentDashboardPage | Nav | OK, but no loading/error states (renders `—` on failure); over-fetches homework/unread/office data it never renders | `StudentDashboardPage.jsx:83,92,93` |
| `/app/student/subjects` | StudentSubjectsPage | Nav | OK | card Link `:128` |
| `/app/student/subjects/:subjectId` | StudentSubjectDetailsPage | Link (subject cards) | OK, but **fabricated fallbacks in prod**: `grade \|\| 'Grade 11'`, `avgScore \|\| '86%'` | `StudentSubjectDetailsPage.jsx:117,122` |
| `/app/student/units` | StudentUnitsPage | **ORPHAN + CRASH** | `ReferenceError: SubjectsPage is not defined` when no `:unitId` | `StudentUnitsPage.jsx:61` (no such import, lines 1-15) |
| `/app/student/units/:unitId` | StudentUnitsPage | Link (subject details `:274`) | OK | |
| `/app/student/lessons` | StudentLessonsPage | no in-app link found | Not linked; bare-list render behavior unverified (queries gated on `lessonId`) | `routes.student.tsx:43` |
| `/app/student/lessons/:lessonId` | StudentLessonsPage | Link (unit lesson rows `:176`) | OK; "video player" is a decorative Thumb, not a player | `StudentLessonsPage.jsx:119-130` |
| `/app/student/materials/:materialId` | StudentMaterialsPage | Link (lesson material rows `:155`) | **Incomplete: never fetches the material itself** — renders decorative thumb + comments only; no content/download | `StudentMaterialsPage.jsx` (no material fetch; only `resourceComments` `:20`) |
| `/app/student/comments/:materialId` | StudentMaterialsPage | **ORPHAN** (never linked; identical to materials) | Duplicate surface | `routes.student.tsx:46` |
| `/app/student/homework` / `:homeworkId` | StudentHomeworkPage | Nav / row "Open" Link `:149` | OK | |
| `/app/student/quizzes` / `:quizId` | StudentQuizzesPage | Nav / cards `:166` | OK | |
| `/app/student/quiz-attempts/:attemptId` (+`/result`) | StudentQuizAttempt/ResultPage | Link (start/continue/result buttons) | OK | `StudentQuizzesPage.jsx:148-173` |
| `/app/student/ai-tutor` | StudentTutorPage | Nav | OK (static suggestion chips) | |
| `/app/student/recommendations` | StudentProgressPage (mode) | Only link is conditional (dashboard `:268`, rendered only when a recommendation exists) | Weakly reachable | `StudentDashboardPage.jsx:251,268` |
| `/app/student/progress` | StudentProgressPage | Nav | OK | |
| `/app/student/engagement` | StudentProgressPage (mode) | **ORPHAN** | URL-only | `routes.student.tsx:57` |
| `/app/student/attendance` | StudentAttendancePage | Nav | OK | |
| `/app/student/communities` / `:communityId` | StudentCommunitiesPage | Nav / cards `:30` | OK; leave/comment API methods unwired | `studentApi.js:113,119` |
| `/app/student/competitions` / `:competitionId` | StudentCompetitionsPage | Nav / cards `:30` | OK | |
| `/app/student/leaderboard/:competitionId` | StudentCompetitionsPage | **ORPHAN** | Duplicate of competition detail | `routes.student.tsx:62` |
| `/app/student/office-hours` | StudentOfficeHoursPage | Nav | OK; cancel-booking API unwired | `studentApi.js:158` |
| `/app/student/notifications` | StudentNotificationsPage | **ORPHAN** (bell → `/app/notifications` instead) | Redundant with shared center | `NotificationBell.jsx:36` |
| `/app/student/announcements`, `/suggestions`, `/badges`, `/settings` | respective pages | Nav | OK; settings page is static (§9) | |
| `/app/student/streaks` | StudentBadgesPage | **ORPHAN** | Identical to `/badges` | `routes.student.tsx:68` |
| `/app/student/profile` | shared ProfilePage | header menu → `/app/profile` (this alias unlinked) | Redundant alias | `routes.student.tsx:70` |
| `/app/student/vision` | StudentVisionPage | **ORPHAN** | Whole student CV surface URL-only | `routes.vision.tsx:13`; no link in `navConfig.js:49-62` |

### 2.4 Teacher (`routes.teacher.tsx`, all `roles: [Teacher, SchoolAdmin]`)

| Route | Component | Nav?/Linked? | Status | Evidence |
|---|---|---|---|---|
| `/app/teacher` | TeacherDashboardPage | Nav | OK | Metric links `:23-28` |
| `/app/teacher/classes` / `:classId` | TeacherClassesPage | Nav / cards `:29` | OK (deep-link shows no class metadata until list cache fills — minor) | `TeacherClassesPage.jsx:49` |
| `/app/teacher/subjects` / `:subjectId` | TeacherSubjectsPage | Nav / cards `:26` | OK (read-only; no authoring — §8) | |
| `/app/teacher/units/:unitId` | TeacherUnitsPage | Link `:44` | OK | |
| `/app/teacher/lessons/:lessonId` | TeacherLessonPage | Link `:24` | Materials are static rows — no open/download action | `TeacherLessonPage.jsx:26-29` |
| `/app/teacher/students` / `:studentId` | TeacherStudentsPage | Nav / cards `:35` | OK (progress, points, award-points all work) | |
| `/app/teacher/quizzes` / `:quizId` | TeacherQuizzesPage | Nav / cards `:35` | OK; analytics/archive/delete-question APIs unwired | `teacherApi.js:62,83,89` |
| `/app/teacher/quiz-generate` | TeacherQuizGeneratePage | Nav + Link | OK | |
| `/app/teacher/submissions/:attemptId` | TeacherSubmissionPage | Link (quiz detail rows `:123`) | OK | |
| `/app/teacher/homework`, `/new`, `/:homeworkId`, `/:homeworkId/submissions` | TeacherHomeworkPage | **ORPHAN surface** — not in navConfig; only self-links inside the page | Fully built lifecycle invisible to users | `navConfig.js:63-70` (absent); internal links `TeacherHomeworkPage.jsx:38,49` |
| `/app/teacher/competitions` (+`/new`, `:id`, `:id/submissions`, `:id/leaderboard`) | TeacherCompetitionsPage | **ORPHAN surface** — same | Fully built lifecycle invisible; scoring requires hand-typing entry id | `TeacherCompetitionsPage.jsx:233` |
| `/app/teacher/assignments` | TeacherHomeworkPage (alias) | **ORPHAN** | Alias, no link anywhere | `routes.teacher.tsx:56` |
| `/app/teacher/notifications` | TeacherNotificationsPage | Nav | OK (no loading spinner; brief false-empty) | |
| `/app/teacher/office-hours` | TeacherOfficeHoursPage | Nav | Read-only dump: no create/edit/cancel UI, no attendance marking (backend + API client ready) | `teacherApi.js:189` unwired; `OfficeHoursController.cs` POST/PUT/cancel |
| `/app/teacher/settings` | TeacherSettingsPage | **ORPHAN** (header Settings menu does link `/app/teacher/settings` for teachers — via `Header.jsx:39`) | Reachable via header menu only; content is a single password link | `Header.jsx:10,39` |
| `/app/vision`, `/app/vision/sessions/:sessionId` | VisionStaffPage | **ORPHAN surface** (no nav entry for Teacher/SchoolAdmin) | Fully functional CV attendance flow, URL-only | `routes.vision.tsx:11-12`; absent from `navConfig.js` |

Note: `/app/teacher/settings` is reachable through the header account menu (`Header.jsx:39` builds `/app/{roleSlug}/settings`); the same applies to each role's settings page. The teacher-agent orphan flag is corrected accordingly — true orphans in this area remain homework, competitions, assignments, and vision.

### 2.5 Parent (`routes.parent.tsx` + vision, all `roles: [Parent]`)

| Route | Component | Nav?/Linked? | Status | Evidence |
|---|---|---|---|---|
| `/app/parent` | ParentDashboardPage | Nav | OK | |
| `/app/parent/children` / `:childId` | ParentChildrenPage / ParentChildOverviewPage | Nav / child cards | OK | `ParentChildrenPage.jsx:24` |
| `/app/parent/children/:childId/progress` `/attendance` `/points` | respective pages | Metric links from overview `:46-48` | OK (progress silently drops `pain-points` fetch; partial failures render as empty cards) | `parentApi.js:44`; `ParentChildProgressPage.jsx:19-23` |
| `/app/parent/children/:childId/vision` | ParentVisionPage | **ORPHAN** — overview links only progress/attendance/points | URL-only | `ParentChildOverviewPage.jsx:46-48` |
| `/app/parent/documents` | ParentDocumentsPage | Nav | OK (create/upload/download work) | |
| `/app/parent/conversations` | ParentConversationsPage | Nav | **Dead-end list**: rows have no link to `/app/messages/:conversationId` | `ParentConversationsPage.jsx:24-25` |
| `/app/parent/notifications` | ParentNotificationsPage | Nav | Ignores `actionUrl` (shared page honors it) — no deep-links | vs `CommunicationNotificationsPage.jsx:82-86` |
| `/app/parent/announcements`, `/settings`, `/profile` | respective | Nav / header | OK (settings thin) | |

### 2.6 School Admin (`routes.school.tsx`, all `roles: [SchoolAdmin]`) — 25 routes, **no detail routes exist**

| Route | Nav?/Linked? | Status | Evidence |
|---|---|---|---|
| `/app/school` | Nav | OK — 12 Metric tiles | `SchoolDashboardPage.jsx:29-40` |
| `/app/school/profile` | **ORPHAN** | URL-only | grep: only `routes.school.tsx:39` + tests |
| `/app/school/academic-years` | Nav | OK (create works; no row actions, no edit/archive UI) | |
| `/app/school/terms` | **ORPHAN** | Terms manageable only by URL | `routes.school.tsx:41` |
| `/app/school/grades` | **ORPHAN** | URL-only | `routes.school.tsx:42` |
| `/app/school/classes` | Nav | OK (create only; no roster/detail/edit) | |
| `/app/school/subjects` | Not in nav; dashboard tile links it | Reachable; read-only | `SchoolDashboardPage.jsx:33` |
| `/app/school/users` | **ORPHAN — the only user-creation surface** (`canCreate: true`) | Critical reachability gap | `routes.school.tsx:45`; grep: no links |
| `/app/school/students` `/teachers` `/parents` | Nav | Lists only — **no row actions, no detail, no enable/disable** (`setUserEnabled` API dead) | `schoolApi.js:82-84`; §5 |
| `/app/school/relationships` | Nav | Create works; **deactivate button never renders** (rowActions drop) | `SchoolRelationshipsPage.jsx:51-53` + §5 |
| `/app/school/teacher-assignments` | Nav | Create works; no deactivate UI | |
| `/app/school/curriculum` | Nav | Read-only aggregation | |
| `/app/school/announcements` | Nav | Create works; **publish/unpublish never renders** (rowActions drop) → drafts can never go live from UI | `SchoolAnnouncementsPage.jsx:43-51` + §5 |
| `/app/school/document-requests` | Nav | OK — functional respond / mark-resolved card actions | `SchoolDocumentRequestsPage.jsx:46-47` |
| `/app/school/communities` | **ORPHAN** | Read-only, URL-only | `routes.school.tsx:54` |
| `/app/school/competitions` | **ORPHAN** | Read-only, URL-only | `routes.school.tsx:55` |
| `/app/school/reports` | Nav | OK (2 of 4 report endpoints consumed) | §8 |
| `/app/school/ai-usage` | Nav | OK | |
| `/app/school/storage` | **ORPHAN** | URL-only | `routes.school.tsx:58` |
| `/app/school/subscription` | Nav | OK read-only; renewal-request endpoint unconsumed | §8 |
| `/app/school/support` | Nav | OK — functional respond action | `SchoolSupportPage.jsx:40` |
| `/app/school/audit-log` | Nav | OK read-only | |
| `/app/school/settings` | Nav | OK (upsert form) | |

### 2.7 System Admin (`routes.system.tsx`, all `roles: [SystemAdmin]`) — 18 routes

| Route | Nav?/Linked? | Status | Evidence |
|---|---|---|---|
| `/app/system` | Nav | OK — 9 Metric tiles | `SystemDashboardPage.jsx:30-38` |
| `/app/system/tenants` | Nav | List renders; **"Open →" row link never renders** (rowActions drop) | `SystemTenantsPage.jsx:42` + §5 |
| `/app/system/tenants/:tenantId` | Only working entry: Onboarding success Link | Page itself fully functional (status/plan/admin/export/deletion); archive API has no button | `SystemOnboardingPage.jsx:96`; `systemApi.js:53-56` |
| `/app/system/onboarding` | Nav | OK — 4-step wizard works end-to-end | |
| `/app/system/plans` | Nav | OK — uses `Crud`, row Edit modal WORKS (only table with working row actions) | `Crud.jsx:58-59`; `SystemPlansPage.jsx:160-175` |
| `/app/system/subscriptions` | Nav | OK read-only | |
| `/app/system/usage`, `/ai-usage`, `/storage` | Nav | OK (storage shows honest backend deferral note) | |
| `/app/system/support` | Nav | OK — functional respond | |
| `/app/system/feature-flags` | Nav | Upsert form works; **per-row toggle never renders** (rowActions drop) | `SystemFeatureFlagsPage.jsx:47-58` + §5 |
| `/app/system/announcements` | Nav | OK | |
| `/app/system/health` | Nav | OK — real operational-status data | |
| `/app/system/errors` | Nav | **CRASH**: `PostureCard` not imported → ReferenceError when data loads | `SystemErrorMonitoringPage.jsx:21` (imports lines 1-8) |
| `/app/system/audit`, `/security` | Nav | OK (security = filtered audit, honest note) | |
| `/app/system/backups` | Nav | **CRASH**: same `PostureCard` ReferenceError | `SystemBackupsPage.jsx:21` |
| `/app/system/settings` | Nav | OK | |

### 2.8 Pages not referenced by any route (dead in production)

| File | Evidence |
|---|---|
| `pages/student/StudentPortalPage.jsx`, `pages/teacher/TeacherPortalPage.jsx`, `pages/parent/ParentPortalPage.jsx`, `pages/school/SchoolAdminPortalPage.jsx`, `pages/system/SystemAdminPortalPage.jsx`, `pages/app/CommunicationPage.jsx` | Legacy view-dispatcher facades; imported only by their own `*.test.jsx` harnesses (self-documented "compatibility facade" comments) |

---

## 3. Navigation Visibility Matrix

Guard model: sidebar filters by `navItemsForRole` (`navConfig.js:128-130`); direct-URL access is enforced by `RoleGuard` → `/forbidden` (`guards.jsx:44-45`); backend enforces per-endpoint policies (verified inventory, §8). "Backend allows?" = the policies behind the page's API calls.

| Role | Visible page/nav item | Expected? | Actual access by URL | Backend allows? | Status | Evidence |
|---|---|---|---|---|---|---|
| Student | 14 student items + messages + notifications + profile + security | yes | all allowed | yes (StudentOnly/TenantMember endpoints) | OK | `navConfig.js:49-62,113-117` |
| Student | any `/app/teacher\|parent\|school\|system/*` URL | no | **/forbidden** (RoleGuard) | no (403; e2e-verified per portal) | OK | `phase8/a-auth.spec.js` A3-A6 etc. |
| Teacher | 8 teacher items + shared 4 | yes | allowed | yes (TeacherOrSchoolAdmin) | OK | `navConfig.js:63-70` |
| Teacher | homework / competitions / vision surfaces | **should be visible** | allowed by URL (routes registered) | yes (backend fully supports) | **Missing nav items — features invisible** | §5 F-04 |
| Parent | 6 parent items + shared 4 | yes | allowed | yes (ParentOnly + relationship checks) | OK | `navConfig.js:71-76` |
| Parent | child vision page | should be linked | allowed by URL | yes (`ParentVisionController` ParentOnly + linkage) | **Missing link** | `ParentChildOverviewPage.jsx:46-48` |
| SchoolAdmin | 17 school items + **8 teacher items** + shared 4 | teacher-surface access is intentional (routes + backend both allow SchoolAdmin) | allowed | yes (TeacherOrSchoolAdmin) | OK — consistent across all three layers | `routes.teacher.tsx:26`, `TeacherController.cs` |
| SchoolAdmin | profile, terms, grades, users, communities, competitions, storage | pages exist, presumably intended | allowed by URL only | yes (SchoolAdminOnly/TenantMember) | **7 orphan routes** (incl. the only create-user page) | §2.6 |
| SystemAdmin | 17 system items + profile + security | yes | allowed | yes (SystemAdminOnly) | OK | `navConfig.js:95-111` |
| SystemAdmin | NotificationBell → `/app/notifications` | shown to all roles | allowed | yes (`NotificationsController` plain `[Authorize]`) | OK, but inconsistent: the `notifications-center` nav item deliberately excludes SystemAdmin while the bell includes them | `navConfig.js:114` vs `Header.jsx:76` |
| SystemAdmin | `/app/messages` (+thread) | not in nav (correct) | **allowed by URL** (route has no role restriction) | **no** — `ConversationsController` requires `TenantMember` (tenantId claim) → 403 | FE-allowed/BE-blocked mismatch; page shows error state | `routes.shared.tsx:37-38`; `ServiceCollectionExtensions.cs:292-293` |
| any tenant role | `/app/vision*` student/staff surfaces | teacher/school-admin & student & parent variants all registered | allowed for the right roles | yes | Whole Phase-15 feature has **zero nav entries** for any role | `routes.vision.tsx`; `navConfig.js` (no vision keys) |

Unit/e2e verification of this matrix: `navConfig.test.js` (role filtering, no leakage), `authFlows.test.jsx` (student → teacher route = forbidden), phase8-12 `a-*.spec.js` per-portal denial suites. **Gap:** no test asserts nav `to:` targets exist in the route registry (§10).

---

## 4. Detail Page Coverage Matrix

| Entity | List route | Detail route | Row/link action exists? | Backend detail endpoint | Status | Required fix |
|---|---|---|---|---|---|---|
| Subject (student) | `/app/student/subjects` | `/subjects/:subjectId` | yes (card Link) | `GET api/v1/Subjects/GetSubjectById/{id}` | ✅ working | — |
| Unit (student) | — (bare route crashes) | `/units/:unitId` | yes from subject detail | `GET api/v1/Units/GetUnitsBySubjectId` | ✅ detail works; ❌ list route crashes | Fix `StudentUnitsPage.jsx:61` |
| Lesson (student) | `/app/student/lessons` (unlinked) | `/lessons/:lessonId` | yes from unit | `GET api/v1/student/lessons/{id}` | ✅ | — |
| Material (student) | (inside lesson) | `/materials/:materialId` | yes | **none consumed** — page never fetches the material | ⚠️ placeholder detail (comments only, no content/download) | Wire material fetch + `filesApi.download` |
| Homework (student) | `/app/student/homework` | `/:homeworkId` | yes ("Open") | `GET api/v1/homework/assigned` + `{id}/my-submission` | ✅ | — |
| Quiz/attempt (student) | `/app/student/quizzes` | `/:quizId`, `/quiz-attempts/:attemptId(/result)` | yes | QuizAttempts endpoints | ✅ | — |
| Community (student) | `/communities` | `/:communityId` | yes | `GET api/v1/communities/{id}` | ✅ (leave/comment missing) | P2 |
| Competition (student) | `/competitions` | `/:competitionId` | yes | `GET api/v1/competitions/{id}` | ✅ | — |
| Class (teacher) | `/app/teacher/classes` | `/:classId` | yes (card) | `GET api/v1/teacher/classes/{classId}/students` | ✅ | — |
| Student (teacher) | `/app/teacher/students` | `/:studentId` | yes (card) | `students/{id}/progress-summary` etc. | ✅ | — |
| Quiz (teacher) | `/quizzes` | `/:quizId` | yes | `GET api/v1/quizzes/{id}` | ✅ | — |
| Submission (teacher) | quiz detail | `/submissions/:attemptId` | yes | `GET api/v1/submissions/{attemptId}` | ✅ | — |
| Homework (teacher) | `/homework` | `/:homeworkId` | yes (internal) | `GET api/v1/homework/{id}` | ✅ page; ❌ surface orphaned from nav | Add nav item |
| Competition (teacher) | `/competitions` | `/:competitionId` | yes (internal) | `GET api/v1/competitions/{id}` | ✅ page; ❌ orphaned; submissions rows not click-to-score | Add nav + pass entryId to score form |
| Office-hour booking (teacher) | `/office-hours` | none | none | `GET office-hours/{id}/bookings`, `POST bookings/{id}/attendance` | ❌ list has no actions; attendance API unwired | Build session/booking UI |
| Vision session (staff) | `/app/vision` | `/sessions/:sessionId` | yes (row Link) | `GET api/v1/vision/sessions/{id}` | ✅ page; ❌ surface orphaned | Add nav item |
| Child (parent) | `/children` | `/:childId` (+progress/attendance/points) | yes | `GET api/v1/parent/children/{childId}` | ✅ | — |
| Child vision (parent) | overview | `/:childId/vision` | **no link** | `GET parent/vision/children/{childId}/engagement-summary` | ❌ detail route unlinked | Add Metric link |
| Conversation (parent page) | `/app/parent/conversations` | `/app/messages/:conversationId` exists | **no link from parent list** | `GET conversations/{id}` | ❌ dead-end list | Link rows to thread route |
| Document request (parent/school) | both list pages | none (inline cards) | yes (inline respond/upload/download) | `GET parent-requests/{id}` exists, unconsumed | ✅ inline pattern; detail endpoint unused | optional |
| Tenant user / student / teacher / parent (school) | `/app/school/students` etc. | **none** | **none** (no row actions at all; `setUserEnabled` dead) | `GET api/v1/tenant-users/{id}` exists, **unconsumed** | ❌ list-only; backend detail ready | Build detail page or row drawer + enable/disable/reset-credential |
| Class (school) | `/app/school/classes` | none | none | `GET api/v1/classes/{id}` exists, unconsumed | ❌ | P1 |
| Academic year / term / grade (school) | list pages | none | none (create-only) | `GET {id}` + PUT/DELETE exist, unconsumed | ⚠️ create-only CRUD | P2 |
| Relationship / class-assignment (school) | list pages | n/a | **deactivate defined but never rendered** (rowActions drop) | deactivate endpoints exist | ❌ broken action | Fix ResourceTable |
| Announcement (school) | list | n/a | **publish defined but never rendered** | `POST announcements/{id}/publish` | ❌ broken action — drafts unpublishable via UI | Fix ResourceTable |
| Support ticket (school/system) | card lists | none needed | yes (inline respond) | respond endpoints | ✅ | — |
| Suggestion (school) | **no page** | — | — | `GET api/v1/suggestions` + moderate (SchoolAdminOnly) | ❌ students submit; school admin has no inbox | New page |
| Tenant (system) | `/app/system/tenants` | `/tenants/:tenantId` | **defined but never rendered** (rowActions drop); only Onboarding success links there | `GET api/v1/tenants/{id}` (+sub/usage) | ❌ detail page fine, list→detail broken | Fix ResourceTable |
| Plan (system) | `/plans` | modal edit | yes (Crud works) | `PUT tenants/plans/{id}` | ✅ | — |
| Subscription (system) | `/subscriptions` | none | none | list endpoint only | ✅ read-only by design | — |
| Feature flag (system) | `/feature-flags` | n/a | **toggle defined but never rendered** | `PUT api/v1/feature-flags` | ❌ broken action | Fix ResourceTable |
| Audit / security event (school/system) | list pages | none | none | list endpoints only | ✅ read-only | — |
| Health / errors / backups (system) | single pages | — | — | `operational-status` | ❌ errors & backups **crash** | Import/extract `PostureCard` |
| File/document (generic) | **no page anywhere** | — | — | `GET api/v1/files` (+{id}, metadata, archive…) | ❌ backend-only surface (and over-permissive, §8) | Product decision + lock down |
| Notification (all) | list pages | none (actionUrl deep-link on shared page only) | partial | list/read endpoints | ✅/⚠️ parent page ignores actionUrl | P2 |
| Badges/points/streaks (student) | `/badges` | none needed | — | badges/points endpoints | ✅ | — |

---

## 5. Broken Click / Dead Navigation Findings

| # | Severity | Page | UI element | Current behavior | Expected behavior | Evidence | Suggested fix |
|---|---|---|---|---|---|---|---|
| F-01 | **P0** | All `List`-based tables in school + system portals | Every `rowActions` (tenant "Open →" link, feature-flag Toggle, relationship Deactivate, announcement Publish/Unpublish) | **Never rendered** — `List` forwards `rowActions` to `ResourceTable`, which doesn't accept the prop | Row actions render in an actions column | `features/school/components.jsx:24` & `features/system/components.jsx:24` pass it; `components/data/ResourceTable.jsx:15-26` drops it; `components/ui/DataTable.jsx` has no actions column. Victims: `SystemTenantsPage.jsx:42`, `SystemFeatureFlagsPage.jsx:47-58`, `SchoolRelationshipsPage.jsx:51-53`, `SchoolAnnouncementsPage.jsx:43-51` | Add `rowActions` support to `ResourceTable`/`DataTable` (mirror `Crud.jsx:58-59` `__actions` column) |
| F-02 | **P0** | `/app/system/errors`, `/app/system/backups` | Whole page | `ReferenceError: PostureCard is not defined` when data loads (component defined only as a local in `SystemSecurityPage.jsx:26`, never imported) | Render posture card | `SystemErrorMonitoringPage.jsx:21`, `SystemBackupsPage.jsx:21` (import lists lines 1-8 contain no PostureCard) | Extract `PostureCard` to a shared module and import it (also remove the now-unused local copy in Security) |
| F-03 | **P0** | `/app/student/units` (registered route) | Whole page | `ReferenceError: SubjectsPage is not defined` — bare-list branch returns an unimported component | Render subjects list or redirect | `StudentUnitsPage.jsx:61`; imports lines 1-15 | Import `StudentSubjectsPage` or `<Navigate to="/app/student/subjects">` |
| F-04 | **P0 (feature invisibility)** | Sidebar (Teacher/SchoolAdmin) | Missing nav entries for homework, competitions, vision | Fully built pages unreachable except by URL | Nav entries per feature | `navConfig.js:63-70` vs `routes.teacher.tsx:45-54`, `routes.vision.tsx:11-12` | Add nav items (or consciously feature-flag; §12) |
| F-05 | P1 | Breadcrumbs on all detail pages whose parent list path isn't registered | Intermediate breadcrumb links | Link to unregistered cumulative paths → NotFound page. Affected parents: `/app/teacher/units`, `/app/teacher/submissions`, `/app/teacher/lessons`, `/app/student/quiz-attempts`, `/app/student/materials`, `/app/student/comments`, `/app/student/leaderboard`, `/app/vision/sessions` | Skip or de-link crumbs with no registered route | `layouts/Breadcrumbs.jsx:13-17` builds cumulative paths; `App.tsx:84` catch-all; registry lacks those list paths | Only render a crumb as `Link` when `findRouteByPath` matches |
| F-06 | P1 | `/app/parent/conversations` | Conversation rows | Display-only; no way to open the thread | Row links to `/app/messages/:conversationId` | `ParentConversationsPage.jsx:24-25` | Add Link per row |
| F-07 | P2 | Header (Student only) | Search input | No onChange/submit handler — typing does nothing | Search or remove | `layouts/Header.jsx:61-70` | Wire or remove |
| F-08 | P2 | Public home | FAQ button | Label-only `<button>`, no onClick/modal | Opens FAQ | `pages/public/.../FaqButton.jsx:10-12` (self-documented) | Implement or remove |
| F-09 | P2 | `/app/teacher/competitions/:id` | Score form | Teacher must hand-type an "Entry id"; submissions list doesn't pass entry ids | Click-to-score from submission row | `TeacherCompetitionsPage.jsx:204-209, 233` | Pass entryId from row into ScoreCard |
| F-10 | P2 | `/app/school/announcements` (consequence of F-01) | Publish | Created announcements remain drafts forever via UI | Publish action available | `AnnouncementsController.cs` Publish is SchoolAdminOnly and never called from UI | Covered by F-01 |
| F-11 | P3 | `CommunicationNotificationsPage` | `Link to={n.actionUrl}` | Backend-supplied path rendered as Link without allow-listing | Validate with `safeReturnPath`-style guard | `CommunicationNotificationsPage.jsx:83` | Sanitize actionUrl |
| F-12 | P3 | Teacher/Student notifications pages | No loading state | Brief false "empty" flash while loading | Spinner | `TeacherNotificationsPage.jsx:24-25`, `StudentNotificationsPage.jsx:25-27` | Add isLoading branch |

---

## 6. Missing Pages by Role

| Role | Missing page/detail | Why it should exist | Existing backend support? | Existing frontend partial? | Suggested fix |
|---|---|---|---|---|---|
| SchoolAdmin | User detail (student/teacher/parent) with enable/disable/reset-credential | Lists exist with zero actions; account lifecycle is a core admin task | `GET/POST api/v1/tenant-users/{id}(/enable\|disable\|reset-credential)` — all live | `schoolApi.setUserEnabled` exists, dead (`schoolApi.js:82-84`) | Detail page or row drawer |
| SchoolAdmin | Enrollment management (student ↔ class) | Classes/students exist; no way to enroll/withdraw | `EnrollmentsController` list/enroll/withdraw (SchoolAdmin writes) | none | New page or classes-page section |
| SchoolAdmin | Suggestions moderation inbox | Students submit suggestions; list/moderate is SchoolAdminOnly | `GET api/v1/suggestions`, `POST {id}/moderate` | none | New page + nav |
| SchoolAdmin | Subscription renewal request | Subscription page is read-only | `POST api/v1/my-tenant/renewal-requests` | none | Button on subscription page |
| SchoolAdmin | Response-document attach on document requests | Backend supports document responses; UI is text-only | `POST parent-requests/{id}/response-document` | respond text works | Add upload to respond card |
| Teacher (+SchoolAdmin) | Curriculum authoring (add/edit subject, unit, lesson, upload material) | Teacher curriculum surfaces are read-only end-to-end; content can only enter via seeds/API | `Subjects/AddSubject`, `Units/AddUnit`, `Lessons/AddLesson`, `LessonMaterial/UploadMaterial` (+update/delete) — all `TeacherOrSchoolAdmin` | none | Authoring UI on teacher subject/unit/lesson pages |
| Teacher | Office-hours create/edit/cancel + booking attendance | Page is a read-only dump; students can book sessions that teachers can't create via UI | `POST/PUT api/v1/office-hours`, `{id}/cancel`, `POST bookings/{id}/attendance` (service enforces teacher-only create: `OfficeHourService.cs:29`) | `teacherApi.markAttendance` exists, dead (`teacherApi.js:189`) | Build session management UI |
| Teacher | Community creation/moderation UI | Service allows Teacher/SchoolAdmin create & moderate; no UI anywhere creates a community | `POST api/v1/communities` (+moderate/archive/members; `CommunityService.cs:56`) | student join/post UI only | Teacher communities page |
| Teacher | Quiz analytics view; archive quiz; delete question | Endpoints + client methods exist | `GET quizzes/{id}/analytics`, `POST {id}/archive`, `DELETE questions/{questionId}` | `teacherApi.quizAnalytics/archiveQuiz/deleteQuestion` dead | Wire into quiz detail |
| Student | Material content/download on `/materials/:materialId` | Page shows only comments + decorative thumb | `FilesController` download; `LessonMaterial` metadata | comments thread works | Fetch material + download link |
| Parent | Vision engagement link; response-document download | Registered page unlinked; response docs undownloadable | vision summary + `responses/{responseId}/document/download` | vision page complete | Link + download button |
| All (anon) | Forgot/reset password flow | Backend flow is complete; login page has no entry | `POST account/forgot-password`, `reset-password` (AllowAnonymous) | none (`authApi.js` lacks methods) | Login-page link + 2 small pages |
| SystemAdmin | Renewal processing; tenant archive button | Backend supports; no UI | `POST tenants/renewals/{renewalId}/process`; archive in `systemApi.js:53-56` | client ready | Tenant detail additions |

---

## 7. Extra/Wrongly Visible Pages by Role

| Role | Page visible incorrectly | Why incorrect | Frontend guard | Backend policy | Suggested fix |
|---|---|---|---|---|---|
| SystemAdmin | `/app/messages`, `/app/messages/:id` reachable by direct URL | Route registered with `requiresAuth` only (no roles); SystemAdmin has no `tenantId` claim | none beyond auth | `TenantMember` → 403 | Add tenant-role restriction to the shared communication routes, or give SystemAdmin a platform messaging surface (§12) |
| SystemAdmin | NotificationBell → `/app/notifications` while nav item excludes SystemAdmin | Inconsistent intent (works backend-wise; `NotificationsController` is plain `[Authorize]`) | none | allows | Align: either add nav item for SystemAdmin or scope the bell |
| Student | `/app/student/units` (registered → crashes), `/streaks`, `/leaderboard/:id`, `/comments/:id`, `/engagement`, `/app/student/notifications` | Registered surfaces that are broken or duplicates — visible to URL guessing, inconsistent UX | RoleGuard(Student) fine | backend fine | Fix crash; decide keep/remove duplicates (§12) |
| Any tenant member (backend, not a page) | Legacy files endpoints (`GET /api/v1/files`, `GET /files/{id}`, `GET /files/{id}/metadata`, `POST /files/{id}/archive`) | A Student token can enumerate all tenant file metadata (incl. `StorageKey`) and **soft-archive any tenant file record** | n/a (no frontend consumer) | `TenantMember` with **no owner/role check in service** | Lock to owner/admin like the Phase-16 paths (see evidence in §8) |
| Student (data projection) | Own AI analysis history returns full staff projection (evidence, model/prompt versions); pain-points endpoint returns unreviewed items to students/parents | Parent-safe filtering keyed on `Role == Parent` only; no Approved-only filter on pain-points | n/a | access allowed by design; projection over-broad | Product/privacy decision (§12) |

No case was found of a sidebar item visible to a role whose backend rejects it (each nav target's endpoints were cross-checked against the policy inventory).

---

## 8. Frontend/Backend Contract Gaps

### 8.1 Confirmed authorization gaps (backend, verified in service code)

| Endpoint | Problem | Evidence |
|---|---|---|
| `POST /api/v1/files/{id}/archive` | Any tenant member (incl. Student) can soft-archive ANY tenant `FileRecord` — no owner/role check (contrast guarded Phase-16 `DELETE /files/{id}`) | `Services/Operations/FileMetadataService.cs:75-85` vs `Services/Storage/FileStorageService.cs:209-211` |
| `GET /api/v1/files`, `GET /files/{id}`, `GET /files/{id}/metadata` | Any tenant member can enumerate/read metadata (FileName, ContentType, size, `StorageKey`, purpose) of every tenant file; no owner/visibility gate (byte download IS gated) | `FileMetadataService.cs:55-73`; `FileStorageService.cs:142-146` vs `EnsureBaselineRead` `:228-235` |
| `POST /api/v1/ai/prediction` | No role gate — a Student can generate/persist predictions for their own id and consume tenant AI quota (ownership enforced; role not) | `Services/Ai/PredictionService.cs:70-71` |
| (verified NOT gaps) | Communities/Competitions/Gamification/Badges/OfficeHours writes, cross-student progress reads, conversations — all enforce role/ownership in services (Student → 403/404) | e.g. `CompetitionService.cs:345-348`, `GamificationService.cs:81`, `BadgeService.cs:54`, `OfficeHourService.cs:29,238`, `StudentAccessAuthorizer.cs:32-65`, `ConversationService.cs:229-241` |

### 8.2 Frontend → backend call verification

Every literal URL in `studentApi.js`, `teacherApi.js`, `parentApi.js`, `schoolApi.js`, `systemApi.js`, `visionApi.js`, `notificationsApi.js`, `filesApi.js`, `authApi.js` was matched against the controller inventory: **no missing endpoints, no wrong verbs, no param-shape mismatches** (legacy verb-style paths like `Subjects/GetSubjectById/{id}` and `Units/GetUnitsBySubjectId?id=` match the backend's dual-route legacy controllers exactly). Policy vs frontend-guard alignment holds on every consumed endpoint.

### 8.3 Backend endpoints with no frontend consumer (flow gaps)

| Area | Unconsumed endpoints | Impact |
|---|---|---|
| Curriculum authoring | `Subjects/AddSubject|UpdateSubject|DeleteSubject`, `Units/AddUnit|UpdateUnit|DeleteUnit`, `Lessons/AddLesson|UpdateLesson|DeleteLesson`, `LessonMaterial/UploadMaterial|AddMaterial|UpdateMaterial|DeleteMaterial` | **No UI can author curriculum content** |
| Enrollment | `GET/POST api/v1/enrollments`, `POST {id}/withdraw` | No UI manages class membership |
| Office hours | `POST/PUT office-hours`, `{id}/cancel`, `GET {id}/bookings`, `POST bookings/{id}/cancel`, `POST bookings/{id}/attendance` | Teacher lifecycle + student cancel missing |
| Account | `POST account/forgot-password`, `reset-password`, `revoke` | No password recovery UI |
| Suggestions | `GET suggestions`, `POST {id}/moderate` | Admin inbox missing |
| Renewals | `POST my-tenant/renewal-requests`; `POST tenants/renewals/{id}/process` | Renewal flow has no UI on either side |
| Reports | `GET reports/assessment-summary`, `GET reports/audit-activity` | School reports page consumes only 2 of 4 |
| Quizzes | `GET quizzes/{id}/analytics`, `POST {id}/archive`, `DELETE {id}/questions/{qid}`, manual `POST quizzes` create (UI is AI-draft-only), legacy `QuizController/*` | Teacher tooling gaps; legacy duplicates dormant |
| Communities | `POST communities` (+update/archive/members/moderation/report set) | No creation/moderation UI (school page read-only, student join/post only) |
| Badges/Gamification | `POST students/{id}/badges`, `POST students/{id}/streak`, `GET/PUT gamification/rules` | No award-badge or rules UI (`teacherApi` methods dead) |
| Parent requests | `GET parent-requests/{id}`, `POST {id}/response-document`, `GET {id}/responses/{responseId}/document/download` | Response documents unusable end-to-end |
| Vision | `GET vision/sessions/{id}/frames`, `POST vision/enrollments`, `GET vision/enrollments`, enrollment-assets | No face-enrollment UI |
| Files | most of `FilesController` (metadata/list/get/archive/signed-download/token download) | No file-manager UI; several endpoints over-permissive (§8.1) |
| Misc | `AiDocumentsController` ingest/delete (no curriculum-ingestion UI), `AiAnalysisController` generate/review (teacher insights read via progress endpoints instead), `AiPredictionController` generate/history/learning-profile, `PerformanceController` class/{id} & subject/{id}, `SettingsController` feature-flags evaluate, `AiUsageController` POST record, tenant-users `GET {id}` | Backend-ready capabilities invisible |

### 8.4 Route-shape caveats (backend)

| Item | Evidence |
|---|---|
| `GET api/v1/files/download` (signed-token) coexists with `GET api/v1/files/{id}` — literal wins, but fragile | `FilesController.cs` |
| Dual legacy route bases on `Quiz`, `Subjects`, `Units`, `Lessons`, `Grades`, `LessonMaterial`, `Notifications` controllers (`api/v1/...` + `api/...`) double the exposed surface | controller `[Route]` attributes |
| `AiTutorController` exposes both `POST api/chat` and `POST /api/v1/ai/tutor` | `AiTutorController.cs` |

---

## 9. Placeholder or Incomplete Pages

| Route | Component | What is incomplete | User impact | Suggested fix |
|---|---|---|---|---|
| `/app/student/materials/:materialId` | StudentMaterialsPage | Material never fetched; decorative thumb + comments only | Students can't view/download materials | Wire material fetch + download |
| `/app/student/subjects/:subjectId` | StudentSubjectDetailsPage | `'Grade 11'` / `'86%'` fabricated fallbacks shown in prod when fields absent | Fake stats presented as real | Remove fallbacks (`:117,:122`) |
| `/app/student/lessons/:lessonId` | StudentLessonsPage | "Video player" is a decorative gradient Thumb | Misleading affordance | Real player or remove |
| `/app/student/settings` | StudentSettingsPage | Static; only a change-password link; language card is inert text | Settings do nothing | Build or trim |
| `/app/teacher/settings`, `/app/parent/settings` | Teacher/ParentSettingsPage | Single password link only | Same | Same |
| `/app/teacher/lessons/:lessonId` | TeacherLessonPage | Materials rows have no open/download | Content not accessible | Add actions |
| `/app/teacher/office-hours` | TeacherOfficeHoursPage | Read-only dump; no create/cancel/attendance | Feature unusable for teachers | §6 |
| `/app/system/storage`, `/security`, `/errors`, `/backups` | system pages | Honest deferral notes (storage byte-accounting, security=filtered audit, errors/backups posture-only) — and errors/backups crash (F-02) | Partial observability | Fix crash; backend work tracked separately |
| `/request-demo`, `/events`, `/activities`, `/news` | public pages | Intentional client-only/empty-state placeholders (self-documented) | Marketing-only | Product decision |
| Header (student) | search input | No handler | Dead control | F-07 |
| Home page | FaqButton | No handler | Dead control | F-08 |

---

## 10. Testing Gaps

Existing coverage is strong (59 route/nav unit tests; 20+ e2e suites incl. per-role denial matrices; ~100 backend 401/403 assertions across 33 files). Missing:

1. **Nav ↔ route registry cross-check test** — `navConfig.test.js` never imports `ROUTES`; a nav item pointing at an unregistered path would pass CI. Add: every `NAV_ITEMS[].to` (and Header/Sidebar/help-card targets) resolves via route matching.
2. **Row-click → detail navigation unit tests** — none exist for any list page; the F-01 `rowActions` drop is invisible to the entire suite. Add render tests asserting the action column/link exists (tenants list, feature flags, relationships, announcements).
3. **Route render smoke test over the real registry** — mounting every registered route with a mocked session would have caught F-02/F-03 (`/app/student/units`, `/app/system/errors`, `/app/system/backups` crash). None exists today.
4. **Lint rule for undefined JSX components** — core `no-undef` does not check JSX element names; `eslint-plugin-react` (`react/jsx-no-undef`) is absent from `eslint.config.js`. Verified: `npx eslint` on the three crashing files → exit 0.
5. **School-admin e2e row-to-detail coverage** — `phase11/b-school-management.spec.js` B2 only asserts the roster lists rows; no e2e clicks any school entity into a detail/action anywhere.
6. **Direct-URL matrix per role over real routes** — `authFlows.test.jsx` covers one cross-role case with inline test routes; per-portal denial exists only in e2e. A parametrized unit test over `ROUTES` × roles would be cheap.
7. **Backend authorization tests for the legacy files endpoints** — no test asserts owner/role enforcement on `GET/POST /api/v1/files*` legacy paths (the §8.1 gaps are untested precisely where unguarded).
8. **Tenant 403-vs-404 contract** — `TenantIsolationTests.cs` pins 404-hide behavior but no test pins the 403 half on tenant routes for cross-role (SystemAdmin) access besides `AuthorizationMatrixTests`.
9. **Breadcrumb integrity** — no test asserts intermediate crumbs resolve to registered routes (F-05).
10. **E2e for notification deep-links and message-thread opening from parent surfaces** (phase13 covers shared surfaces only).

---

## 11. Prioritized Fix Plan

### P0 — broken navigation / security / RBAC

| Priority | Fix | Files likely affected | Backend needed? | Frontend needed? | Tests needed | Acceptance criteria |
|---|---|---|---|---|---|---|
| P0 | Render `rowActions` in shared table | `components/data/ResourceTable.jsx`, `components/ui/DataTable.jsx` | no | yes | unit tests per affected page (tenants Open link, flag toggle, relationship deactivate, announcement publish) | All four flows operable from UI; tenant list navigates to `/app/system/tenants/:id` |
| P0 | Fix `PostureCard` ReferenceError | extract to `features/system/components` (or shared), import in `SystemErrorMonitoringPage.jsx`, `SystemBackupsPage.jsx`; remove dead local in `SystemSecurityPage.jsx` | no | yes | route render smoke test | `/app/system/errors` and `/backups` render with data |
| P0 | Fix `SubjectsPage` ReferenceError | `pages/student/units/StudentUnitsPage.jsx:61` | no | yes | route render smoke test | `/app/student/units` renders subjects (or redirects) |
| P0 | Lock down legacy files endpoints | `Services/Operations/FileMetadataService.cs` (List/Get/Archive), `Services/Storage/FileStorageService.cs` (GetMetadata) | yes | no | backend 403 tests (student vs foreign file archive/list/metadata) | Non-owner/non-admin gets 403; archive requires owner/admin |
| P0 | Surface teacher homework/competitions (+decide vision) in nav | `navigation/navConfig.js` (+i18n keys) | no | yes | navConfig tests updated; nav↔routes cross-check | Teacher sidebar exposes homework & competitions; vision per §12 decision |

### P1 — missing detail pages for core entities

| Priority | Fix | Files | Backend? | Frontend? | Tests | Acceptance |
|---|---|---|---|---|---|---|
| P1 | School user detail/drawer with enable/disable/reset-credential | `pages/school/users/*`, `schoolApi.js` (wire `setUserEnabled`, add `GET tenant-users/{id}`, reset) | no (endpoints live) | yes | render + e2e row-click | Admin can open a user and toggle status |
| P1 | Enrollment management UI | new `pages/school/enrollments` or classes-page section; `schoolApi.js` | no | yes | e2e enroll/withdraw | Admin enrolls/withdraws student in class |
| P1 | Curriculum authoring UI (units/lessons/materials upload) | teacher subject/unit/lesson pages; `teacherApi.js` | no | yes | e2e author→student sees | Teacher creates lesson + uploads material end-to-end |
| P1 | Office-hours lifecycle UI (create/cancel/attendance) | `TeacherOfficeHoursPage.jsx`; wire `markAttendance` | no | yes | e2e create→book→attend | Full loop in UI |
| P1 | Student material page fetches real content/download | `StudentMaterialsPage.jsx`, `filesApi.js` | no | yes | e2e download | Student opens/downloads material |
| P1 | Add `/app/school/users` (+profile/terms/grades/storage as decided) to nav | `navConfig.js` | no | yes | navConfig tests | Create-user page reachable |
| P1 | Breadcrumb 404 links | `layouts/Breadcrumbs.jsx` | no | yes | unit test | Crumbs to unregistered paths are plain text |

### P2 — missing links/actions where backend already exists

| Priority | Fix | Files | Tests | Acceptance |
|---|---|---|---|---|
| P2 | Parent conversations → link rows to `/app/messages/:id`; honor `actionUrl` in parent notifications (sanitized) | `ParentConversationsPage.jsx`, `ParentNotificationsPage.jsx` | render tests | Parent opens threads/deep-links |
| P2 | Link parent child-vision page from child overview; add student/staff vision nav (per §12) | `ParentChildOverviewPage.jsx`, `navConfig.js` | render | Vision reachable |
| P2 | Competition click-to-score (pass entryId) | `TeacherCompetitionsPage.jsx` | render | Score from submission row |
| P2 | Quiz analytics/archive/delete-question; manual quiz create | `TeacherQuizzesPage.jsx` | render | Wired |
| P2 | Suggestions moderation page (school) | new page + nav + `schoolApi` | e2e | Admin sees/moderates suggestions |
| P2 | Forgot/reset password flow | `LoginPage.jsx`, new pages, `authApi.js` | e2e | Recovery works |
| P2 | Renewal request (school) + renewal processing & archive button (system) | subscription/tenant-detail pages | e2e | Renewal loop closes |
| P2 | Student cancel-booking; community leave/comment | student pages | render | Wired |
| P2 | Response-document attach (school) + download (parent) | document-request pages, `parentApi`/`schoolApi` | e2e | Docs round-trip |

### P3 — polish, empty states, coverage

- Remove fabricated fallbacks (`'Grade 11'`/`'86%'`); dead header search; FaqButton; decorative video thumb honesty.
- Add loading states to teacher/student notifications; ledger spinner on parent points.
- Add `eslint-plugin-react` with `react/jsx-no-undef`; nav↔routes cross-check test; route render smoke test; school-admin row-click e2e; backend 403 tests for files.
- Delete dead production code: 6 portal facades (§2.8), dead API methods (`setUserEnabled` if not wired, `quizAnalytics` etc. once wired or dropped), duplicate student routes per §12 decisions.
- Decide fate of dormant legacy dual-route controllers (`api/Quiz`, `api/Subjects`, …) — narrow the attack surface.
- Sanitize `actionUrl` links (F-11); unify SystemAdmin bell/nav inconsistency.

---

## 12. Needs Product Decision

1. **Vision feature exposure** — routes, pages, backend, and e2e (phase15) are complete for staff/student/parent, yet zero nav entries exist for any role. Deliberate soft launch or oversight? (If deliberate, gate the routes too; currently URL-reachable.)
2. **Teacher homework/competitions absent from nav** — same question as (1); pages are production-quality.
3. **Duplicate/vestigial student routes** — `/app/student/streaks` (≡ badges), `/leaderboard/:competitionId` (≡ competition detail), `/comments/:materialId` (≡ materials), `/engagement` (progress mode, unlinked), `/app/student/notifications` (≡ shared center), `/app/student/profile` (≡ `/app/profile`): keep, differentiate, or remove?
4. **`/app/teacher/assignments` alias** — keep as a synonym of homework or drop?
5. **SystemAdmin messaging** — should platform admins have any messaging/notification surface? Today: bell works, messages 403s at the backend, nav hides both.
6. **`TenantInternal` file visibility** — any tenant member (incl. students) may download `TenantInternal` files by design of `EnsureBaselineRead`. Intended?
7. **AI transparency projections** — students see full staff projection of their own analysis history (evidence, model/prompt versions), and unreviewed pain-points flow to students/parents via `StudentProgressService.PainPointsAsync`. Privacy/product call before changing.
8. **Office-hour creation is Teacher-only in the service** (`OfficeHourService.cs:29`) while every other teacher surface admits SchoolAdmin. Intentional asymmetry?
9. **School-admin entity detail pages** — current design is flat list+create with no detail routes anywhere in the school portal. If detail pages are wanted (students/teachers/classes), that's net-new routing (`/app/school/students/:id` …), not a bug fix.
10. **Public marketing placeholders** — request-demo posts nowhere; events/activities/news have no content feed. Fine for now?
11. **Legacy dual routes + legacy `QuizController`** — retire or keep for compatibility?
12. **Leftover orphan school routes** (profile/terms/grades/storage/communities/competitions) — promote to nav, merge into other pages, or delete?

---

*End of audit. No source files were modified; the only artifact created is this report.*
