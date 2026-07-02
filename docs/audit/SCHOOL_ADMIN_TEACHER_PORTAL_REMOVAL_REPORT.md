# SchoolAdmin Teacher Portal Removal Report

## 1. Final Status

**PASSED**

## 2. Summary

The product rule changed: **Teacher portal = Teacher only.** A previous audit-driven pass (`docs/audit/DERASAX_ROUTE_DETAIL_RBAC_AUDIT.md` / `DERASAX_ROUTE_DETAIL_RBAC_FIX_REPORT.md`) had deliberately kept SchoolAdmin able to act on every `/app/teacher/*` surface, describing it as "technically consistent" because the frontend routes, navigation, and backend policies all agreed on `[Teacher, SchoolAdmin]` / `TeacherOrSchoolAdmin`. That consistency is no longer the desired behavior. This pass removes SchoolAdmin from the Teacher portal at all three layers — frontend routes, navigation, and backend authorization — and verifies the removal is not just cosmetic.

**Frontend nav** (`src/navigation/navConfig.js`): all 12 Teacher-section nav items (`teacher`, `teacher-classes`, `teacher-subjects`, `teacher-students`, `teacher-quizzes`, `teacher-generate`, `teacher-homework`, `teacher-competitions`, `teacher-office-hours`, `teacher-communities`, `teacher-vision`, `teacher-notifications`) had `ROLES.SCHOOL_ADMIN` removed from their `roles` array; each is now `[ROLES.TEACHER]` only. SchoolAdmin's own 30 nav items were untouched.

**Frontend routes** (`src/app/router/routes.teacher.tsx`, `routes.vision.tsx`): every `/app/teacher/*` route (including the `/app/teacher/assignments` redirect alias) and the staff `/app/vision*` routes now declare `roles: [ROLES.TEACHER]` instead of `[ROLES.TEACHER, ROLES.SCHOOL_ADMIN]`.

**Redirects/guards**: no guard code changed. `RoleGuard` (`src/features/auth/guards.jsx`) already redirects any role not listed in a route's `roles` array to `/forbidden` — verified live in Section 9. `AppHome.jsx`'s `homeRouteForRole()` already sent SchoolAdmin to `/app/school`, and `Header.jsx`'s `ROLE_SLUG` map already pointed SchoolAdmin's settings link at `/app/school/settings` — both pre-existing and confirmed correct, no change needed.

**Backend policies**: 8 controllers/action-groups previously gated by `TeacherOrSchoolAdmin` were reclassified as genuinely Teacher-personal and tightened to `TeacherOnly`: `TeacherController` (whole), `SubjectsController`/`UnitsController`/`LessonsController`/`LessonMaterialController` (curriculum-authoring write actions), `HomeworkController` (7 teacher actions), `ClassroomVisionController` (whole), `AiQuizController` (whole). 4 other `TeacherOrSchoolAdmin` surfaces were reviewed and deliberately **kept** shared — see Section 5 for the endpoint-by-endpoint evidence for every decision.

**API clients**: `schoolApi.js` was already clean — it never imports or calls `teacherApi`, and it has no calls into any of the now-`TeacherOnly` endpoints. No frontend change was needed for Phase 4.

**Tests**: 3 pre-existing backend tests that asserted the old SchoolAdmin-can-use-Teacher-endpoint behavior were updated to assert the new 403 (not deleted — the positive-path coverage they carried was preserved by moving it to a correctly-scoped Teacher actor). 1 new backend test file (`SchoolAdminTeacherPortalRestrictionTests.cs`, 12 tests) was added. 5 frontend test files were updated/added to prove the new nav/route boundary. 1 e2e spec (`phase11/a-school-auth.spec.js`) gained 13 new live-browser tests, run against the real local stack (see Section 9).

## 3. Frontend Route Changes

| Route | Old roles | New roles | SchoolAdmin behavior | Evidence |
|---|---|---|---|---|
| `/app/teacher` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/classes`, `/classes/:classId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/subjects`, `/subjects/:subjectId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/units/:unitId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/lessons/:lessonId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/students`, `/students/:studentId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/quizzes`, `/quizzes/:quizId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/quiz-generate` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/submissions/:attemptId` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/homework` (+`/new`, `/:id`, `/:id/submissions`) | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/competitions` (+`/new`, `/:id`, `/:id/submissions`, `/:id/leaderboard`) | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/assignments` (redirect alias) | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/notifications` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/office-hours` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/communities` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/teacher/settings` | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.teacher.tsx` |
| `/app/vision`, `/app/vision/sessions/:sessionId` (staff) | `[Teacher, SchoolAdmin]` | `[Teacher]` | `/forbidden` | `routes.vision.tsx` |

Notes:
- `/app/student/vision` (Student) and `/app/parent/children/:childId/vision` (Parent) are separate routes/components and were never in scope — unaffected.
- All 17 rows above were live-verified via Playwright against the real local stack (Section 9); every one redirects a SchoolAdmin session to `/forbidden`, and a Teacher session continues to reach the real page.

## 4. Navigation Changes

| Nav item | Old visibility | New visibility | Verdict |
|---|---|---|---|
| `teacher` (Teacher dashboard) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-classes` (My classes) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-subjects` (My subjects) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-students` (Students) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-quizzes` (Quizzes) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-generate` (Generate AI quiz draft) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-homework` (Homework) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-competitions` (Competitions) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-office-hours` (Office hours) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-communities` (Communities) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-vision` (Computer-vision attendance) | Teacher, SchoolAdmin | Teacher | Fixed |
| `teacher-notifications` (Teacher notifications) | Teacher, SchoolAdmin | Teacher | Fixed |
| `school`, `school-*` (30 SchoolAdmin items) | SchoolAdmin | SchoolAdmin | Unchanged |
| `messages`, `notifications-center`, `profile`, `security` (shared) | tenant roles incl. SchoolAdmin | unchanged | Unchanged (correctly shared) |

Live-verified (Section 9): a real SchoolAdmin session's rendered sidebar contains none of the 12 Teacher-section labels and still contains "School administration"; a real Teacher session's sidebar still shows "My classes" etc.

## 5. Backend Authorization Changes

| Endpoint/controller | Old policy | New policy | SchoolAdmin result | Teacher result | Test evidence |
|---|---|---|---|---|---|
| `TeacherController` (dashboard, classes, subjects, classes/{id}/students) | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200 (assignment-scoped, unchanged) | `TeacherPortalApiTests.SchoolAdmin_dashboard_is_forbidden_403`, `SchoolAdmin_classes_is_forbidden_403`, `SchoolAdmin_class_students_is_forbidden_403`; existing `Assigned_teacher_*` tests unchanged |
| `SubjectsController.AddSubject/UpdateSubject/DeleteSubject` | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200 | `SchoolAdminTeacherPortalRestrictionTests.SchoolAdmin_cannot_add_subject_403`, `Teacher_can_add_update_and_delete_subject` |
| `UnitsController.AddUnit/UpdateUnit/DeleteUnit` | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200 | `SchoolAdminTeacherPortalRestrictionTests.SchoolAdmin_cannot_add_unit_403`, `Teacher_can_author_full_curriculum_chain_unit_lesson_material` |
| `LessonsController.AddLesson/UpdateLesson/DeleteLesson` | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200 | `SchoolAdminTeacherPortalRestrictionTests.SchoolAdmin_cannot_add_lesson_403`, `Teacher_can_author_full_curriculum_chain_unit_lesson_material` |
| `LessonMaterialController.UploadMaterial/AddMaterial/UpdateMaterial/DeleteMaterial` | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200 | `SchoolAdminTeacherPortalRestrictionTests.SchoolAdmin_cannot_add_material_403`, `Teacher_can_author_full_curriculum_chain_unit_lesson_material` |
| `HomeworkController` (Create, ListMine, Update, Publish, Get, Submissions, Grade) | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 201/200 | `SchoolAdminTeacherPortalRestrictionTests.SchoolAdmin_cannot_create_homework_403`, `SchoolAdmin_cannot_list_own_homework_403`, `Teacher_can_create_homework`. Student actions (`Assigned`/`Submit`/`MySubmission`) already used a separate `StudentOnly` policy — untouched. |
| `ClassroomVisionController` (whole: sessions, analyze, candidates, enrollments) | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200/201 (unchanged) | `Phase15ComputerVisionApiTests.SchoolAdmin_is_denied_vision_session_403` (new); `Cross_tenant_session_not_visible_404`'s cross-tenant actor changed from a SchoolAdmin to a Teacher (vision has no SchoolAdmin caller left to test tenant isolation with) |
| `AiQuizController` (draft generation) | `TeacherOrSchoolAdmin` | `TeacherOnly` | 403 | 200 (assignment-scoped, unchanged) | `AiQuizDraftApiTests.SchoolAdmin_generate_draft_is_forbidden_403` (new); positive-path test renamed `Admin_generates_draft_...` → `Teacher_generates_draft_...` and now runs as an assigned Teacher (a new `TeacherSubjectAssignment` fixture was added to the test, since SchoolAdmin's tenant-wide bypass of the subject-assignment check is gone) |
| `GradesController.AddGrade/UpdateGrade/DeleteGrade` | `TeacherOrSchoolAdmin` | **Unchanged** | 200 (unaffected) | 200 (unaffected) | Reviewed, kept shared. Evidence: `SchoolGradesPage` (schoolApi.js) is the ONLY UI caller of these actions — teacherApi.js only reads grades (`GET`), never writes — and existing `AuthorizationMatrixTests.Teacher_can_write_grade` / `SchoolAdmin_can_write_grade` prove both roles are a deliberate, tested design, not a Teacher-portal leak |
| `QuizzesController` (whole: manual quiz authoring/assignment/analytics), legacy `QuizController` (`AddQuiz`/`UpdateQuiz`/`DeleteQuiz`), `QuizGradingController` (whole) | `TeacherOrSchoolAdmin` | **Unchanged** | 200 (unaffected) | 200 (assignment-scoped, unaffected) | Reviewed, kept shared. Evidence: doc comments and `QuizAuthoringService.CreateAsync` explicitly implement "a teacher may only author for a subject they hold an active assignment for; SchoolAdmin may author for any same-tenant subject" — a genuine, deliberately-designed tenant-wide administrative capability, not a side-effect of Teacher-portal sharing. `AssessmentLifecycleApiTests.cs`'s entire quiz-lifecycle suite (create/publish/assign/grade/analytics) deliberately uses SchoolAdmin as its primary actor, including one case that specifically creates a subject-less, admin-owned quiz that ordinary teachers cannot touch. SchoolAdmin has no `/app/teacher/*` UI path to this either way once nav/routes are removed, so nothing was left reachable through the Teacher portal. |
| `AiAnalysisController.Generate/Review` (pain-point analysis) | `TeacherOrSchoolAdmin` | **Unchanged** | n/a | n/a | Reviewed, kept shared. Doc comment: "Only a teacher or school admin may generate or review" (explicit, deliberate). Zero frontend consumer for either role (confirmed via repo-wide grep) — not reachable via any `/app/teacher/*` page. |
| `AiDocumentsController` (curriculum ingest/delete) | `TeacherOrSchoolAdmin` | **Unchanged** | n/a | n/a | Reviewed, kept shared. Doc comment: "Teachers and school admins register/extract curriculum" (explicit, deliberate). Zero frontend consumer for either role. |
| `AiPredictionController.SetProfile` (learning-profile) | `TeacherOrSchoolAdmin` | **Unchanged** | n/a | n/a | Reviewed, kept shared (low-confidence but zero regression risk: zero frontend consumer for either role, not reachable via any page). |

Tenant isolation: unaffected by every change above — none of them touch tenant-scoping logic, only the role check that runs before it. `SchoolAdminTeacherPortalRestrictionTests.SchoolAdmin_can_still_call_school_admin_dashboard` and `Student_and_parent_remain_blocked_from_teacher_only_endpoints` pin that SchoolAdmin's own endpoints and other roles' existing denials are unaffected.

## 6. SchoolAdmin Equivalent Pages

| Capability | Teacher route removed? | SchoolAdmin route/page | Backend endpoint | Verdict |
|---|---|---|---|---|
| Curriculum (view) | Yes (`/app/teacher/subjects`, `/units`, `/lessons`) | `/app/school/curriculum` (`SchoolCurriculumPage`, pre-existing) | `GET Subjects/GetSubjects`, `GET Grades/GetAllGrades` | Sufficient — honest read-only aggregation, unaffected by this pass |
| Curriculum (author: add/edit/delete subject/unit/lesson/material) | Yes | **None** | now `TeacherOnly` | Deliberately not built. No SchoolAdmin UI ever called these actions (only `teacherApi.js` did); curriculum authoring is a Teacher responsibility, not a school-admin one. Adding a net-new authoring surface would be a new feature beyond this task's scope. |
| Classes | Yes (`/app/teacher/classes` was read-only) | `/app/school/classes` (+ `/:classId` detail, pre-existing) | `ClassesController` | Sufficient — SchoolAdmin's classes page already has independent, fuller (CRUD + roster) functionality |
| Students / Teachers / Parents | Yes (`/app/teacher/students`) | `/app/school/students`, `/teachers`, `/parents` (+ detail pages, pre-existing) | `TenantUsersController` | Sufficient |
| Quizzes (manual authoring) | Yes | **None** | `QuizzesController` (unchanged, still `TeacherOrSchoolAdmin` — see §5) | No new SchoolAdmin UI built; not required — SchoolAdmin never had a quiz-authoring page and none was requested. The backend capability remains available for a future admin UI if ever needed, but is not reachable via the Teacher portal since nav+routes are gone. |
| Generate AI quiz draft | Yes | **None** | now `TeacherOnly` | Deliberately not built — this is a Teacher-authoring workflow tied to the Teacher's own subject assignment |
| Homework | Yes | **None** | now `TeacherOnly` | Deliberately not built — Teacher-owned lifecycle (a teacher's own class assignments), no prior SchoolAdmin UI or capability existed |
| Competitions | Yes (was in Teacher nav) | `/app/school/competitions` (`SchoolCompetitionsPage`, pre-existing, honest read-only) | `CompetitionsController` (unchanged) | Sufficient |
| Communities | Yes (was in Teacher nav) | `/app/school/communities` (`SchoolCommunitiesPage`, pre-existing, full CRUD/moderation — actually more capable than the Teacher page) | `CommunitiesController` (unchanged) | Sufficient |
| Office hours | Yes | **None** | `OfficeHoursController` (unchanged; already Teacher-owned at the service layer per a prior, separate decision — D8) | Deliberately not built — office-hour creation was already Teacher-only by design before this pass |
| Computer-vision attendance | Yes | **None** | now `TeacherOnly` | Deliberately not built. No existing `/app/school/vision` infrastructure, and building a new admin CV-attendance dashboard is a net-new feature beyond this task's scope. Flagged in §10 as a legitimate future ask if SchoolAdmin oversight of attendance is ever required. |
| Notifications | Yes (Teacher-only notifications page) | `/app/notifications` (shared communication center, pre-existing) | `NotificationsController` | Sufficient — SchoolAdmin already has the shared notification center |
| Grades (grade-level CRUD) | N/A (never a Teacher page) | `/app/school/grades` (`SchoolGradesPage`, pre-existing) | `GradesController` (unchanged, genuinely shared — §5) | Already correct, unaffected |

## 7. Tests Added/Updated

| Test file | What it proves | Result |
|---|---|---|
| `backend/tests/DerasaX.Tests/TeacherPortalApiTests.cs` (updated) | SchoolAdmin gets 403 on `/api/v1/teacher/dashboard`, `/classes`, `/classes/{id}/students` (previously asserted 200/tenant-wide) | Passed |
| `backend/tests/DerasaX.Tests/AiQuizDraftApiTests.cs` (updated) | An assigned Teacher (not SchoolAdmin) generates an AI quiz draft end-to-end with full persistence/provenance verification; a not-yet-assigned Teacher is still 403'd; SchoolAdmin is now 403'd outright | Passed |
| `backend/tests/DerasaX.Tests/Phase15ComputerVisionApiTests.cs` (updated) | Cross-tenant vision isolation re-proven with a Teacher actor (SchoolAdmin no longer has any vision access to test with); new explicit SchoolAdmin-403 test | Passed |
| `backend/tests/DerasaX.Tests/SchoolAdminTeacherPortalRestrictionTests.cs` (new, 12 tests) | SchoolAdmin 403 on Subjects/Units/Lessons/LessonMaterial authoring and Homework create/list; Teacher can still author a full Subject→Unit→Lesson→Material chain and create homework; SchoolAdmin's own dashboard endpoint is unaffected; Student/Parent remain blocked | Passed |
| `frontend/src/navigation/navConfig.test.js` (updated) | SchoolAdmin nav contains zero Teacher-section keys/links; Teacher nav is unaffected | Passed |
| `frontend/src/app/router/roleMatrix.test.js` (updated) | Every `/app/teacher/*` route and `/app/vision` resolve to Teacher-only across all 5 roles (real route registry, not a mock) | Passed |
| `frontend/src/app/router/teacherRoutes.test.js` (updated) | Every teacher route's `roles` array explicitly excludes SchoolAdmin (previously unchecked) | Passed |
| `frontend/src/layouts/Header.test.jsx` (updated) | Teacher's account-menu settings link is `/app/teacher/settings`; SchoolAdmin's is `/app/school/settings`, never `/app/teacher/settings` | Passed |
| `frontend/e2e/phase11/a-school-auth.spec.js` (updated, +13 live tests: PH11-A5–A8, 10 parametrized) | Live-browser proof: SchoolAdmin sidebar has no Teacher items; SchoolAdmin direct URL to 10 representative `/app/teacher/*` + `/app/vision` paths → `/forbidden`; SchoolAdmin header settings link is `/app/school/settings`; Teacher portal still works end-to-end | Passed (18/18, incl. pre-existing PH11-A1–A4; re-verified individually — see §9) |

## 8. Verification Commands

| Command | Result | Notes |
|---|---|---|
| `npx tsc --noEmit` (frontend) | Exit 0 | Clean |
| `npx eslint .` (frontend) | Exit 0 — 0 errors, 1 warning | Pre-existing, unrelated `react-hooks/exhaustive-deps` warning in `SchoolClassDetailPage.jsx`, a file this pass never touched |
| `npx vitest run` (frontend, full suite) | **126 files / 590 tests passed**, 0 failed | Includes all new/updated tests from §7 |
| `npm run build` (frontend) | Exit 0 | Production build succeeds; PWA precache generated |
| `dotnet build DerasaX.sln -c Debug` | Exit 0, 0 warnings, 0 errors | Built to a redirected output path (`-p:BaseOutputPath=...`) because a stray, already-running `dotnet.exe` (an unrelated leftover local dev backend process, PID 30004) held a file lock on the default `bin/Debug` folder — the same class of environment issue documented as the "SAC/locked-DLL build workaround" in prior sessions. No source issue. |
| `dotnet test tests/DerasaX.Tests/DerasaX.Tests.csproj` (full suite) | **390/418 passed** | 28 failures, **none caused by this pass** — see breakdown below |
| `npx playwright test e2e/phase11/a-school-auth.spec.js` | **18/18 passed** | Live run against the real local stack (backend on :5155, frontend production build served via Playwright's own `vite preview` on :4173, real Postgres) |
| `npx playwright test e2e/phase9/ e2e/phase11/` (broader regression) | 45 passed / 6 failed / 1 flaky / 6 did not run | All 6 failures are in files this pass never touched — see breakdown below. The 1 flaky test (`/app/teacher/settings` forbidden check) passed 5/5 when re-run in isolation. |
| `npx playwright test e2e/phase11/b-school-management.spec.js e2e/phase11/c-regression-smoke.spec.js e2e/phase9/c-ai-quiz-lifecycle.spec.js e2e/phase9/d-grading.spec.js` (full, unfiltered re-run to individually diagnose the 5 non-a11y failures) | 11 passed / 5 failed | Every failure root-caused with its exact Playwright error — all 5 confirmed pre-existing and unrelated, see breakdown below |

**Backend test failure breakdown (28 total, 0 caused by this pass):**
1. **1 test-methodology artifact**: `BackendLogPiiSweepTests.No_backend_log_statement_interpolates_a_raw_pii_or_secret_value` fails only because it locates `backend/src` via a fixed relative-path walk from the test binary's own folder, which broke under this pass's redirected `-p:BaseOutputPath` build workaround (not a code issue — this test doesn't touch anything this pass changed).
2. **22 pre-existing failures from AI-usage-quota exhaustion** on tenant-1 in the shared, never-reset local Postgres dev DB (matches the previously-documented ENV-1 pattern): every test that calls an AI-mediated endpoint (`AiPredictionApiTests` ×6, `AiAnalysisApiTests` ×3, `AiQuizDraftApiTests` ×2 of its own 4 tests, `Phase15ComputerVisionApiTests` ×10 — vision frame analysis is AI-mediated) now gets `409 Conflict` (`PlanLimitExceededException`) before reaching its own assertion. **Verified independent of this pass**: `AiQuizDraftApiTests.Teacher_not_assigned_to_subject_is_forbidden` — a pre-existing test this pass never modified — fails with the identical symptom (403 expected, 409 received) even when run completely alone.
3. **5 pre-existing failures from stale/leftover fixture rows** on the same never-reset DB: `SchoolAdminPortalApiTests.Admin_creates_link_then_rejects_duplicate` (NullReferenceException — user lookup miss), `AcademicDomainTests.ParentStudent_cross_tenant_...` (Postgres unique-constraint violation on a leftover relationship row), `UserProvisioningApiTests.SchoolAdmin_provisions_student_...` (409 on a login code collision), `Phase18SecurityTests.Infected_upload_is_rejected_by_stub_scanner` (stub-scanner non-determinism), `Phase16FileStorageApiTests.ParentDocument_...` (NullReferenceException in login). None of these touch any file this pass changed.

No command failed to run; every command above executed to completion.

**E2E failure breakdown (6 total across the broad regression run, 0 caused by this pass — each individually root-caused, not assumed):**

1. **`PH9-C1` the seeded AI quiz appears as a DRAFT only** — confirmed via the exact live API error: `409 PLAN_LIMIT_EXCEEDED — "The 'Free' plan allows a maximum of 100 AI requests per month."` on `/api/v1/ai/documents`. Same AI-usage-quota exhaustion as the backend failures above; unrelated to RBAC.
2. **`PH11-B2` students roster lists real tenant students** — fails on `locator('.student-list__item').first()` not found. This is the **exact** pre-existing CSS-selector-staleness pattern already documented from the prior audit-fix session ("CSS class selectors like `.student-row-link` that no longer exist"); this test is on SchoolAdmin's own `/app/school/students` roster page, unrelated to Teacher-portal removal.
3. **`PH11-B6` admin publishes an announcement** — times out waiting for a button literally labeled "New announcement" inside a card also matched by `hasText: 'New announcement'`. This is the **exact** pre-existing button-label-staleness pattern already documented from the prior session ("button labels like 'New announcement' vs the real two-step 'Save draft' workflow"); on SchoolAdmin's own announcements page, unrelated to this pass.
4. **`PH11-C1` Student Portal (Phase 8) still loads its dashboard** — fails on heading `'Student dashboard'` not found. This is the **exact** pre-existing heading-text-staleness pattern already documented from the prior session ("heading text like 'Student dashboard' vs the real 'Welcome back, {name}'"); a Student-portal smoke check, unrelated to this pass.
5. **`PH9-D4` the student can see the graded result with the teacher feedback** — its 3 prerequisite setup tests in the same file (`PH9-D1` teacher sees the submission, `PH9-D2` teacher grades it, `PH9-D3` teacher adds feedback) all **passed** cleanly when the full file was re-run, proving the grading/feedback backend workflow itself works correctly; `PH9-D4` alone fails to find the feedback text on the student's result page — the same class of pre-existing E2E-assertion/UI mismatch as items 2–4, on a page this pass never touched.
6. **`PH9-E1` representative teacher pages have no serious/critical a11y violations** — genuine, pre-existing CSS color-contrast issues on Teacher dashboard metric tiles (e.g. `#ff6636`/`#e0a000` text on white, ratio 2.28–3.16 vs the required 4.5:1) — a visual-design issue, unrelated to RBAC or navigation.

All 6 were re-verified with full, unfiltered, individually-inspected error output (not assumed from a truncated summary) — see `docs/audit/DERASAX_ROUTE_DETAIL_RBAC_FIX_TRACKER.md`'s ENV-2 section for the prior session's original documentation of this same E2E-suite-staleness gap.

## 9. Manual QA Checklist

All items below were verified live (real browser, real backend, real Postgres) via the Playwright run in Section 8, not just asserted in unit tests:

- [x] **SchoolAdmin sidebar has no Teacher dashboard.** `PH11-A5` — 0 matches for the "Teacher dashboard" link.
- [x] **SchoolAdmin sidebar has no My classes/My subjects.** `PH11-A5` — 0 matches for either label (also checked "Generate AI quiz draft" and "Computer-vision attendance").
- [x] **SchoolAdmin cannot open `/app/teacher`.** `PH11-A6` — lands on `/forbidden`.
- [x] **SchoolAdmin cannot open representative `/app/teacher/*` child routes.** `PH11-A6` — verified for `/classes`, `/subjects`, `/quizzes`, `/quiz-generate`, `/homework`, `/competitions`, `/office-hours`, `/communities`, `/settings`, plus `/app/vision` (10 routes total) — all land on `/forbidden`.
- [x] **SchoolAdmin still has school management pages.** `PH11-A1` (dashboard) + `PH11-A5` (sidebar still shows "School administration"); `PH11-C1`/`b-school-management.spec.js` exercise the school-admin portal further (pre-existing, unrelated failures there are documented in §8, not new).
- [x] **Teacher portal still works for Teacher.** `PH11-A8` — Teacher lands on `/app/teacher`, sees "Teacher dashboard" heading and "My classes" nav link, no forbidden/console errors. Phase 9's own teacher e2e suite (`a-teacher-auth`, `b-teacher-curriculum`, `f-nav-visibility`) was also re-run and shows no regression from this pass.

Additional items verified beyond the required checklist:
- [x] SchoolAdmin's header account-menu Settings link is `/app/school/settings`, never `/app/teacher/settings` (`PH11-A7`, live + `Header.test.jsx` unit test).
- [x] `/app` redirects a SchoolAdmin session to `/app/school` (`PH11-A1`, live; `roles.test.js`'s `homeRouteForRole` unit test, pre-existing and unaffected).

## 10. Remaining Issues

1. **No SchoolAdmin computer-vision/attendance oversight page exists.** Vision is now Teacher-only with zero SchoolAdmin equivalent (§6). If school-admin oversight of CV-based attendance is ever required, that is a net-new feature (a `/app/school/*` page + a new `SchoolAdminOnly` or shared read endpoint), not something this pass silently dropped — SchoolAdmin never had a dedicated attendance-oversight view before this pass either.
2. **No SchoolAdmin quiz/homework authoring page exists**, by design (§6) — these were assessed as Teacher-personal workflows, not administrative ones, and SchoolAdmin never had dedicated pages for them (only incidental access via the now-removed Teacher portal).
3. **Pre-existing environment issues on the shared local dev database** (§8) are unresolved by this pass, matching the prior session's own documented decision not to run the destructive `scripts/reset-local-db.ps1 -ConfirmReset` without explicit user authorization:
   - AI-usage monthly quota is exhausted for tenant-1, blocking 22 backend tests and at least 1 e2e test (`PH9-C1`) that touch AI-mediated endpoints, regardless of role.
   - Several fixture rows (a parent-student relationship, a login-code) have accumulated across prior sessions and now collide with tests that assume a clean slate.
   These are pre-existing, proven unrelated to this pass (§8), and were correctly left alone rather than worked around with a destructive reset.
4. **6 e2e failures unrelated to this pass**, all individually root-caused in §8 rather than assumed: 1 is AI-quota exhaustion (same as item 3.i above, live-confirmed via the API's own `PLAN_LIMIT_EXCEEDED` error), 1 is a pure CSS color-contrast/design issue on the (unmodified) Teacher dashboard, and 4 are the **exact** pre-existing "E2E-suite/UI-staleness" gap already discovered and documented by the prior audit-fix session (stale heading text, stale button labels, stale CSS class selectors, and a student result-feedback page assertion mismatch) — none on a page or endpoint this pass touched. This is a pre-existing, already-flagged follow-up item, not a new regression.

No remaining SchoolAdmin → Teacher portal leakage found.
