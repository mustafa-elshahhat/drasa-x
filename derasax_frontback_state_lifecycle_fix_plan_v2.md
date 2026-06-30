# DerasaX — Frontend/Backend State Lifecycle Fix Plan v2

**Scope:** latest `drasa-x-main.zip` static audit of frontend/backend state transitions and API contract gaps.  
**Status:** the original plan is still valid as the base plan, but this v2 updates it for the latest code state.  
**Important note:** this is a static code audit plan. It must be verified by backend tests, frontend tests, and E2E flows before marking complete.

---

## 1. Executive Summary

The plan does **not** need to be rewritten from scratch. The latest version shows a small improvement, mainly in **Student Homework status handling**, but the core gaps remain:

- Student quiz state/result flow is still unreliable.
- Teacher quiz authoring is still incomplete.
- Teacher/Admin homework lifecycle is still missing in the frontend.
- Student homework file upload is still fake/incomplete.
- Competition lifecycle is still backend-heavy and frontend-incomplete.
- Message read receipts exist in backend/API client but are not wired in the thread UI.
- Announcements still create as active immediately, which weakens draft/publish semantics.
- Fake fallback/demo data still exists, including Student Dashboard values.

### Problem trend after latest zip

| Area | Previous Status | Latest Status | Decision |
|---|---|---|---|
| Student Homework status tabs | broken/mismatched | partially improved | keep Phase 5 but reduce status scope |
| Homework file upload | fake | still fake | keep P0 |
| Student Quizzes | broken/mismatched | still broken/mismatched | keep P0 |
| Teacher Quiz Authoring | incomplete | still incomplete | keep P1/P0 depending flow |
| Teacher/Admin Homework UI | missing | still missing | keep P0 |
| Competitions lifecycle | incomplete | still incomplete | keep P0 |
| Messages read receipts | not wired | still not wired | keep P1 |
| Announcements lifecycle | ambiguous | still ambiguous | keep P1 |
| Fake fallbacks | present | present, dashboard added | expand Phase 10 |

### Main conclusion

Problems **decreased slightly**, not substantially. The original MD remains suitable, but v2 adds the latest audit delta and clarifies the remaining work.

---

## 2. Root Causes

Most issues come from these root causes:

1. **Frontend/backend DTO mismatch**  
   Examples: frontend expects `status`, `duration`, `points`, `totalPoints`; backend returns `SubmissionStatus`, `TimeLimitMinutes`, `Score`, `MaxScore`.

2. **Backend endpoints exist without complete frontend usage**  
   Examples: homework create/publish/grade, competition create/publish/close/score, message read receipts.

3. **Frontend shows realistic-looking fallback data**  
   Examples: fake quiz scores, fake lesson names, fake dashboard metrics, default question counts/durations.

4. **Lifecycle is not fully represented in the UI**  
   Examples: homework draft/publish/submit/grade, competition draft/publish/submit/score/close, message sent/read.

5. **Student-facing visibility is not strict enough**  
   Examples: competitions list can expose statuses that should not be visible to students unless a student-specific endpoint or server-side filter is enforced.

---

## 3. Mandatory Implementation Rules

- Do not start by redesigning UI. Fix contracts and state semantics first.
- Do not introduce fake fallback values that look like real production data.
- Do not delete, skip, weaken, or silence tests to make verification pass.
- Do not change Git history. No commits, no pushes, no rebases, no tags, no amend.
- Do not make broad unrelated rewrites.
- Do not add dependencies unless strictly required and documented.
- Every status shown in the frontend must be returned by the backend or derived from backend fields in a documented adapter.
- Every important lifecycle must be covered by backend tests, frontend tests, and at least one E2E critical path.
- Every endpoint used by a page must go through the project API client layer, not one-off component fetch calls.

---

## 4. Updated Execution Order

1. Phase 0 — Latest Front/Back Contract Audit Baseline
2. Phase 1 — DTO Alignment & State Contracts
3. Phase 2 — Student Quizzes Flow
4. Phase 3 — Teacher Quizzes Authoring
5. Phase 4 — Teacher/Admin Homework Lifecycle
6. Phase 5 — Student Homework Completion
7. Phase 6 — Competitions Full Lifecycle
8. Phase 7 — Messages & Read Receipts
9. Phase 8 — Announcements Lifecycle
10. Phase 9 — Lessons Cleanup
11. Phase 10 — Remove Fake Fallbacks & Demo Data
12. Phase 11 — API Client Completion
13. Phase 12 — Role Guards & Visibility Rules
14. Phase 13 — Full Verification & Regression Gates

---

# Phase 0 — Latest Front/Back Contract Audit Baseline

## Goal

Create a precise current-state map before modifying code. This phase prevents random fixes and makes sure every change targets a known mismatch.

## Required files

Create or update:

```text
docs/audits/frontend-backend-contract-map.md
docs/audits/state-lifecycle-gaps.md
docs/audits/latest-zip-delta.md
```

## `frontend-backend-contract-map.md` format

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Quizzes | StudentQuizzesPage | studentApi.quizzes | GET /api/v1/student/quizzes | - | AssignedQuizDto | mismatch | frontend expects status/latestAttemptId/questionCount but backend does not return them |

Allowed statuses:

```text
matched
mismatch
missing-ui
missing-client-method
fake-fallback
backend-only
frontend-only
partially-fixed
```

## `state-lifecycle-gaps.md` format

| Entity | Expected Lifecycle | Backend Support | Frontend Support | Gap |
|---|---|---|---|---|
| Quiz | Draft → Published → Attempted → Submitted/Graded | partial/full | partial | student result link uses quizId instead of attemptId |

## `latest-zip-delta.md` must include

- What improved since the prior audit.
- What stayed the same.
- What became newly visible.
- Which plan phases changed because of the latest zip.

## Current latest delta to record

- Student Homework list/status handling improved partially because the frontend now derives status from `submissionStatus` and `hasSubmitted` in the list flow.
- Student Homework still needs real file upload, `AttachmentFileId`, removal of fake draft, 404 normalization, and remaining fallback cleanup.
- Student Dashboard fake fallback values must now be tracked explicitly in Phase 10.
- Competition leaderboard mapping must explicitly use backend `Score` instead of `points/totalPoints`.
- Message invalidation after send/read must be made explicit.

## Definition of Done

- Every major page has a mapped backend endpoint or is marked missing.
- Every fake fallback is listed.
- Every lifecycle has a backend/frontend status.
- No implementation changes are made before the audit files are updated.

---

# Phase 1 — DTO Alignment & State Contracts

## Goal

Make frontend state deterministic by aligning backend response DTOs and frontend adapters.

---

## 1. Quizzes DTOs

### Required `AssignedQuizDto`

The student assigned quizzes endpoint must return enough data for the list, tabs, details, attempt entry, and result link.

```text
quizId
title
subjectName
type
status
timeLimitMinutes
maxAttempts
attemptsUsed
canAttempt
questionCount
latestAttemptId
latestAttemptStatus
score
percentage
dueDate
```

### Student quiz status values

Use stable lower-case values at the frontend boundary:

```text
available
in_progress
submitted
graded
closed
```

If backend uses enum names, normalize once in the API client or a typed adapter, not inside every component.

### Required `AttemptDetailDto`

The quiz attempt detail response must provide the metadata required by the attempt UI and timer:

```text
attemptId
quizId
quizTitle
timeLimitMinutes
startedAt
expiresAt
questions[]
```

Alternative acceptable approach: the attempt page may fetch quiz metadata before starting the timer, but it must not fall back to `30 minutes` unless that is the actual backend value.

### Required `AttemptSummaryDto`

```text
attemptId
quizId
quizTitle
achievedScore
totalScore
percentage
status
submittedAt
gradedAt
answers[]
```

Each answer row should provide enough data for review without fake correctness:

```text
questionId
questionText
studentAnswer
correctAnswer
isCorrect
pointsEarned
pointsPossible
explanation
```

## Problems closed

- Student quizzes shown as Available by default.
- Graded/submitted tabs not reliable.
- View Result uses `quizId` instead of `attemptId`.
- Question count fallback `20`.
- Duration fallback `30`.
- Result page fake values.
- Fake `isCorrect` fallback.

---

## 2. Homework DTOs

### Current latest state

Student homework list status has partially improved in the latest zip because the frontend derives state from `submissionStatus` and `hasSubmitted` in the list flow.

### Required `AssignedHomeworkDto`

Keep and standardize these fields:

```text
homeworkId
title
description
subjectName
dueDate
maxScore
hasSubmitted
submissionId
submissionStatus
score
gradedAt
canSubmit
attachmentRequired
attachmentFileId
```

### Submission status values

```text
not_submitted
submitted
graded
late
closed
```

## Remaining problems closed by later phases

- Real file upload still missing.
- `AttachmentFileId` still not wired from frontend submission.
- Fake draft save still exists.
- `my-submission` 404 must be normalized to null.
- Remaining score fallback values must be removed.

---

## 3. Competitions DTOs

### Required student-facing competition DTO

Do not make the student UI infer entry/submission permissions from raw status only. Return explicit flags.

```text
competitionId
title
description
status
startAt
endAt
hasEntered
hasSubmitted
entryId
submissionId
canEnter
canSubmit
canViewLeaderboard
score
rank
```

### Required leaderboard row fields

```text
rank
studentId
studentName
score
submittedAt
```

The frontend must map `score`/`Score`, not `points` or `totalPoints`.

## Problems closed

- Student sees Draft/Archived competitions.
- Enter button appears after entry.
- Submit card appears before entry.
- Leaderboard called before allowed.
- Leaderboard points display undefined because frontend reads wrong field.

---

# Phase 2 — Student Quizzes Flow

## Goal

Make student quiz list, attempt, submission, and result flow use real state and correct IDs.

## Required changes

1. Update backend DTO or frontend adapter so assigned quizzes return a real student-facing `status`.
2. Update Student Quizzes tabs:
   - Available = can attempt and not submitted/graded.
   - In Progress = latest attempt in progress.
   - Submitted = attempt submitted but not graded, if this state exists.
   - Graded = result available.
3. Fix `View Result` to use `latestAttemptId`, not `quizId`.
4. Hide/disable result button when `latestAttemptId` is missing.
5. Make attempt page timer use `timeLimitMinutes` or `expiresAt` from backend.
6. Remove fake result fallbacks:
   - `88%`
   - `18`
   - `20`
   - `Calculus Midterm Quiz`
   - fake answer correctness like `i !== 2`
7. Use backend fields:
   - `achievedScore`
   - `totalScore`
   - `percentage`
   - `answers[]`
8. Show a clear empty/error state if result data is incomplete.

## Required backend tests

- Assigned quiz returns `status` and `latestAttemptId` after submission.
- Assigned quiz returns `questionCount` and `timeLimitMinutes`.
- Attempt detail returns timer metadata.
- Result endpoint requires and returns by `attemptId`.

## Required frontend tests

- Available quiz appears in Available tab.
- Submitted/graded quiz moves out of Available.
- Result link uses attempt ID.
- Result page never renders fake score if backend response is missing score.

## Required E2E

Teacher publishes quiz → assigns quiz → student starts attempt → submits attempt → student opens result using attempt ID.

## Definition of Done

- No student quiz page relies on default `available`, `20 questions`, or `30 minutes` unless returned by backend.
- No result page shows fabricated score or correctness.

---

# Phase 3 — Teacher Quizzes Authoring

## Goal

Make teacher quiz creation/editing/publishing usable and safe for MCQ questions.

## Required changes

1. Fix question update payload to include full `options` when question type requires options.
2. Add UI to add a new question manually.
3. Add UI to edit quiz metadata:
   - title
   - description
   - type
   - timeLimitMinutes
   - maxAttempts
   - dueDate
4. Ensure publish validation is visible in UI:
   - cannot publish quiz without questions
   - cannot publish MCQ without options
   - cannot publish a question without correct answer
5. Improve cache invalidation after:
   - publish
   - archive/delete
   - question add/update/delete
   - assign

## Required invalidations

After quiz publish/update/assign, invalidate:

```text
teacher.quiz.detail
teacher.quizzes
teacher.assignments
teacher.dashboard
student.assignedQuizzes, if same runtime cache can be affected
```

## Required tests

- Editing an MCQ preserves options.
- Adding a question works.
- Publish fails with visible validation if invalid.
- Assign updates teacher-facing list/dashboard counts.

## Definition of Done

Teacher can create/edit/publish/assign a quiz without manual backend calls and without breaking MCQ options.

---

# Phase 4 — Teacher/Admin Homework Lifecycle

## Goal

Add the missing frontend lifecycle for homework. Backend support exists; frontend is incomplete.

## Required routes

Add or correct:

```text
/app/teacher/homework
/app/teacher/homework/new
/app/teacher/homework/:id
/app/teacher/homework/:id/edit
/app/teacher/homework/:id/submissions
/app/teacher/homework/submissions/:submissionId/grade
```

For school admin, either add equivalent routes or clearly restrict homework management to teachers based on product decision.

## Required route fix

`/app/teacher/assignments` must not point to `TeacherQuizzesPage`.

Choose one:

1. Redirect `/app/teacher/assignments` to homework/assignments page.
2. Split clearly:
   - `/app/teacher/quizzes`
   - `/app/teacher/homework`

## Required API client methods

```text
createHomework
updateHomework
publishHomework
getHomework
listHomework
getHomeworkSubmissions
gradeHomeworkSubmission
```

## Required UI features

- Create homework as draft.
- Edit draft homework.
- Publish homework.
- View submissions.
- Grade submission with score and feedback.
- Show status badges for draft/published/submitted/graded.

## Required tests

- Teacher creates homework draft.
- Teacher publishes homework.
- Published homework appears to assigned students.
- Teacher sees submissions.
- Teacher grades submission.

## Definition of Done

Homework lifecycle can be completed from UI without Swagger/manual API calls.

---

# Phase 5 — Student Homework Completion

## Goal

Finish student homework flow. Latest zip partially improved list status, but file upload/draft/error handling remain incomplete.

## Current improved area

Student list/tab status now partially uses:

```text
submissionStatus
hasSubmitted
```

Keep this improvement, but normalize it through a single helper/adapter and test it.

## Remaining required changes

1. Replace fake file attachment behavior.
2. Upload selected file through the real files API.
3. Send returned `AttachmentFileId` in homework submission.
4. Remove `[Attached file: filename]` text injection from submission content.
5. Remove or implement Save Draft.
6. Normalize `my-submission` 404 to `null` in the API client or query function.
7. Remove score fallback such as `20 points` if not returned by backend.
8. Show clear states:
   - not submitted
   - submitted
   - graded
   - late/closed, if backend supports it

## Recommended file upload flow

```text
student selects file
frontend calls filesApi.upload(file)
backend returns fileId
frontend submits homework with content + attachmentFileId
backend stores submission with attachment reference
student page displays uploaded attachment from backend response
```

## Save Draft decision

Choose one product behavior:

### Preferred for now

Remove the Save Draft button until backend draft submissions exist.

### Alternative

Add real backend draft submission endpoints and tests.

Do not keep a fake button that only shows an alert or invalidates queries.

## Required tests

- Submitted homework appears in Submitted tab.
- Graded homework appears in Graded tab.
- File upload calls real file API and submits `AttachmentFileId`.
- 404 from my-submission is treated as no submission.
- Save Draft is either absent or real.

## Definition of Done

Student homework flow shows real state, real files, and no fake draft behavior.

---

# Phase 6 — Competitions Full Lifecycle

## Goal

Make competitions a complete feature across backend, teacher/admin UI, and student UI.

## Backend state decision

Resolve `Published` vs `Active` before UI fixes.

### Recommended simpler model

Use:

```text
Draft → Published → Closed → Archived
```

Interpret `Published` as visible/enterable for students, and `Closed` as results/leaderboard visible.

### Alternative model

Use:

```text
Draft → Published → Active → Closed → Archived
```

Only choose this if there is an endpoint/background job that moves `Published` to `Active` based on dates.

Do not keep `Active` checks in one path while publish only sets `Published`.

## Student endpoint

Add or use a student-specific endpoint:

```text
GET /api/v1/student/competitions
```

It must not return Draft/Archived competitions.

It must return:

```text
hasEntered
hasSubmitted
canEnter
canSubmit
canViewLeaderboard
score
rank
```

## Student UI changes

1. Do not show Draft/Archived competitions.
2. Show Enter button only when `canEnter` is true.
3. Show Submit card only when `canSubmit` is true.
4. Do not call leaderboard unless `canViewLeaderboard` is true.
5. If leaderboard is not available, show “Results are not available yet.”
6. Map leaderboard `score`/`Score`, not `points` or `totalPoints`.
7. After enter/submit, invalidate competition detail/list/submission state.

## Teacher/Admin routes

Add:

```text
/app/teacher/competitions
/app/teacher/competitions/new
/app/teacher/competitions/:id
/app/teacher/competitions/:id/edit
/app/teacher/competitions/:id/submissions
/app/teacher/competitions/:id/leaderboard
```

For school admin, either share the same components with role-aware permissions or add equivalent school routes.

## API client methods

```text
createCompetition
updateCompetition
publishCompetition
archiveCompetition
closeCompetition
getCompetitionSubmissions
scoreCompetitionEntry
getCompetitionLeaderboard
getStudentCompetitions
enterCompetition
submitCompetition
getMyCompetitionSubmission
```

## Remove read-only blocker

`SchoolCompetitionsPage` must not remain read-only if the product requires school admin management.

## Required tests

- Student list excludes Draft/Archived.
- Student cannot see leaderboard before allowed.
- Enter button disappears after entry.
- Submit appears only after entry and before submission/deadline.
- Teacher/admin creates competition.
- Teacher/admin publishes competition.
- Student submits.
- Teacher/admin scores entry.
- Teacher/admin closes competition.
- Student sees leaderboard after close.

## Definition of Done

Competition lifecycle is usable from UI end-to-end and does not expose invalid actions to students.

---

# Phase 7 — Messages & Read Receipts

## Goal

Wire existing backend read receipt support into the conversation UI.

## Required changes

1. When opening a conversation thread, identify incoming unread messages.
2. Call read endpoint for those messages:

```text
POST /api/v1/conversations/{conversationId}/messages/{messageId}/read
```

3. Use existing API client method if present; otherwise add it.
4. After marking read, invalidate:

```text
conversation.messages
conversation.list
unread.counts
notifications, if linked
```

5. After sending a message, invalidate:

```text
conversation.messages
conversation.list
unread.counts, if affected
```

6. Add read state display where possible:

```text
Sent
Read
```

7. Replace manual `participantUserId` input with a role-aware participant picker.

## Participant picker rules

Examples:

- Teacher can choose allowed students, parents, or school staff.
- Student can choose allowed teachers/support channels.
- Parent can choose school staff/teachers linked to their children.

Respect backend authorization. The picker should not imply access beyond what backend allows.

## Required tests

- Opening a thread marks incoming unread messages as read.
- Conversation list unread count updates.
- Sending a message updates conversation list preview/order.
- New conversation can be created without manually typing raw IDs.

## Definition of Done

Messaging is not only “send message”; it also reflects read state and conversation list freshness.

---

# Phase 8 — Announcements Lifecycle

## Goal

Make announcement create/publish semantics honest and notification behavior predictable.

## Current issue

Announcement creation currently creates an active announcement immediately, while publish is the path that triggers notification fan-out. This can create active announcements without publish-time notifications.

## Required product decision

Choose one model.

### Recommended model: Draft then Publish

- Create announcement with `IsActive = false`.
- Publish sets `IsActive = true`.
- Publish triggers notification fan-out.
- Unpublish sets `IsActive = false`.

Frontend buttons:

```text
Save Draft
Publish
Unpublish
```

### Alternative model: Create & Publish

- Rename UI action to `Create & Publish`.
- Creation sets active and sends notifications.
- Draft creation must be a separate action if drafts are needed.

## Required backend tests

- Create announcement does not notify if draft model chosen.
- Publish announcement notifies intended recipients.
- Unpublish hides announcement.
- Re-publish does not duplicate notifications unless explicitly intended.

## Required frontend tests

- Draft announcement is not shown as published.
- Publish changes visible status.
- Unpublish hides/inactivates it.

## Definition of Done

Announcement status and notifications match the user action name.

---

# Phase 9 — Lessons Cleanup

## Goal

Keep lesson completion flow, but remove fake lesson metadata and improve fetching.

## Current good state

Lesson completion is functionally valid:

- Student completes lesson.
- Backend creates/updates progress.
- Completion becomes 100%.

## Required cleanup

1. Remove hardcoded lesson fallback content such as:
   - `Mathematics`
   - `Integration`
   - `Intro to Integration`
   - static descriptions
   - static media durations like `18:24`, `8 pages`, `22 slides`
2. If lesson is missing, show:
   - loading
   - not found
   - unauthorized
   - unavailable
3. Prefer a direct endpoint for lesson detail:

```text
GET /api/v1/student/lessons/{lessonId}
```

4. Avoid fetching all subjects/units/lessons only to locate one lesson.
5. Improve invalidation after complete:

```text
student.lesson.detail
student.progress
student.subjects
student.units
```

## Required tests

- Completing lesson updates progress.
- Missing lesson does not render fake lesson data.
- Lesson detail is loaded from real backend data.

## Definition of Done

Lessons do not display fake metadata when backend data is missing.

---

# Phase 10 — Remove Fake Fallbacks & Demo Data

## Goal

Remove any frontend value that can make incomplete backend data look real.

## Search targets

Search frontend for hardcoded/fallback values including but not limited to:

```text
88%
18/20
Calculus Midterm Quiz
20 questions
30 minutes
Mathematics
Integration
Intro to Integration
[Attached file:
i !== 2
learning progress fallback
streak fallback
totalPoints fallback
featured quiz fallback
```

## Student Dashboard specific cleanup

Track and remove fake/fallback dashboard values such as:

```text
learning progress fallback = 58
streak fallback = 7
totalPoints fallback = 2580
featured quiz fallback = Calculus Midterm Quiz
question count fallback = 20
duration fallback = 30
```

Dashboard must show one of:

- real backend value
- empty state
- loading skeleton
- unavailable state
- error state

It must not show realistic fake achievements, scores, streaks, or points.

## Replacement rules

| Bad behavior | Correct behavior |
|---|---|
| Show fake score | Show unavailable/result pending |
| Show fake lesson title | Show not found/unavailable |
| Show fake file upload | Use real file upload or remove upload UI |
| Show fake dashboard metric | Show empty/loading/unavailable |
| Invent correctness | Require backend `isCorrect` or hide answer review |

## Required tests

- Result page does not render fake score when score missing.
- Dashboard does not render fake metrics when API returns null/empty.
- Lesson page does not render fake lesson title when lesson missing.
- Homework submission does not inject filename into content as fake attachment.

## Definition of Done

No production route renders realistic fake academic/state data.

---

# Phase 11 — API Client Completion

## Goal

Centralize all backend access through API clients and remove missing method gaps.

## Required API coverage

### Quiz

```text
getAssignedQuizzes
getQuizAttemptDetail
getQuizResultByAttemptId
updateQuizQuestionWithOptions
addQuizQuestion
updateQuizMetadata
publishQuiz
assignQuiz
```

### Homework

```text
createHomework
updateHomework
publishHomework
listHomework
getHomework
getHomeworkSubmissions
gradeHomeworkSubmission
submitHomeworkWithAttachment
getMyHomeworkSubmission
```

### Competitions

```text
createCompetition
updateCompetition
publishCompetition
archiveCompetition
closeCompetition
getCompetitionSubmissions
scoreCompetitionEntry
getCompetitionLeaderboard
getStudentCompetitions
enterCompetition
submitCompetition
getMyCompetitionSubmission
```

### Messages

```text
getConversations
getConversationMessages
postMessage
markMessageRead
createConversation
searchAllowedParticipants
```

### Announcements

```text
createAnnouncementDraft
publishAnnouncement
unpublishAnnouncement
listAnnouncements
```

### Files

```text
uploadFile
getFileMetadata
getFileDownloadUrl
```

## Rules

- API clients should normalize casing once where needed.
- Components should not contain DTO repair logic repeatedly.
- 404-as-null behavior should be centralized where it represents normal absence, such as “my submission does not exist yet.”

## Required tests

- API client methods call correct routes.
- Normalizers map backend DTOs to frontend stable fields.
- 404 normalization works for my-submission flows.

## Definition of Done

No required UI feature is blocked by missing API client methods.

---

# Phase 12 — Role Guards & Visibility Rules

## Goal

Ensure every role sees only valid pages, valid data, and valid actions.

## Student must not see

```text
Draft competitions
Archived competitions
Unpublished homework
Unpublished quizzes
Admin-only pages
Teacher-only grading controls
```

## Teacher should see

```text
owned/allowed quizzes
owned/allowed homework
allowed competition management
submissions for allowed classes/students
message participants allowed by school rules
```

## SchoolAdmin should see

```text
school-wide announcements
school competitions, if product allows
school homework/quizzes, if product allows
school-level audit/management pages
```

## Required changes

- Add route guards where missing.
- Add action guards in UI.
- Ensure backend authorization still enforces every rule.
- Do not rely on frontend hiding only.

## Required tests

- Student cannot access teacher homework pages.
- Student list does not include hidden competition states.
- Teacher cannot grade submissions outside allowed scope.
- SchoolAdmin sees only school-scoped data.

## Definition of Done

Visibility rules are enforced in both frontend and backend.

---

# Phase 13 — Full Verification & Regression Gates

## Goal

Prove all lifecycle fixes work end-to-end.

## Backend test requirements

Add/verify tests for:

```text
Quiz publish changes status
Assigned quiz returns student status/latestAttemptId/questionCount/timeLimitMinutes
Quiz result uses attemptId
Homework create draft
Homework publish
Homework submit with AttachmentFileId
Homework grade changes submission status
Competition student list excludes Draft/Archived
Competition enter/submit lifecycle
Competition leaderboard permission
Competition score mapping uses Score
Message read receipt
Announcement create draft then publish notification
Lesson complete updates progress
```

## Frontend test requirements

Add/verify tests for:

```text
Student quiz tabs
Student quiz result link uses attemptId
Quiz result has no fake score/correctness
Teacher MCQ edit preserves options
Teacher homework lifecycle pages
Student homework status tabs
Student homework file upload flow
Competition enter/submit/leaderboard states
Leaderboard displays score from backend Score field
Message read on thread open
Announcement draft/publish UI
Student dashboard no fake fallback values
Lesson page no fake fallback content
```

## E2E critical paths

### Flow 1 — Quiz

```text
Teacher creates quiz
Teacher adds valid questions
Teacher publishes quiz
Teacher assigns quiz
Student sees quiz as available
Student starts attempt
Student submits attempt
Student opens result by attemptId
Result shows real backend score
```

### Flow 2 — Homework

```text
Teacher creates homework draft
Teacher publishes homework
Student sees homework
Student uploads real file
Student submits homework with AttachmentFileId
Teacher views submission
Teacher grades submission
Student sees graded status and score
```

### Flow 3 — Competition

```text
Teacher/Admin creates competition
Teacher/Admin publishes competition
Student sees competition
Student enters competition
Student submits competition
Teacher/Admin scores entry
Teacher/Admin closes competition
Student sees leaderboard when allowed
```

### Flow 4 — Messaging

```text
User A sends message
User B opens conversation
Incoming message is marked read
Conversation list unread count updates
```

### Flow 5 — Announcement

```text
SchoolAdmin creates announcement draft
SchoolAdmin publishes announcement
Announcement becomes active
Notification is created for intended recipients
SchoolAdmin unpublishes announcement
Announcement becomes inactive
```

### Flow 6 — Lesson

```text
Student opens real lesson
Student marks lesson complete
Progress updates to completed/100%
No fake fallback lesson content appears if lesson is missing
```

## Verification commands

Use the project’s existing scripts when available. Suggested verification matrix:

```text
dotnet restore
dotnet build
dotnet test
npm run lint
npm run test
npm run build
pytest
playwright e2e critical flows
```

Adapt exact commands to the repo scripts, but do not mark complete without backend + frontend + E2E verification.

## Definition of Done

- Backend tests pass.
- Frontend tests pass.
- E2E critical flows pass.
- No fake fallback data remains in production routes.
- No lifecycle action requires Swagger/manual API calls.
- No known DTO mismatch remains for the audited modules.
- No Git history changes were made.

---

## 5. Updated Priority Matrix

| Priority | Phase | Reason |
|---|---|---|
| P0 | Phase 1 | DTO contracts are the root of most false UI states |
| P0 | Phase 2 | Student quiz status/result flow can show wrong state/results |
| P0 | Phase 4 | Homework teacher lifecycle missing from frontend |
| P0 | Phase 5 | Homework file upload is fake and must be real or removed |
| P0 | Phase 6 | Competitions lifecycle is incomplete and can expose invalid actions |
| P1 | Phase 3 | Teacher quiz authoring can break MCQ options |
| P1 | Phase 7 | Read receipts exist but are not wired |
| P1 | Phase 8 | Announcement draft/publish semantics are unclear |
| P1 | Phase 10 | Fake dashboard/result/lesson data must be removed before production |
| P2 | Phase 9 | Lesson completion works, but fetching/fallbacks need cleanup |
| P2 | Phase 11 | API client completion prevents future drift |
| P2 | Phase 12 | Role guards harden visibility and action rules |
| P0 final | Phase 13 | Required to prove all fixes |

---

## 6. What Changed From v1

### Reduced scope

- Student Homework status mismatch is now partially improved in latest zip.
- Phase 5 no longer treats status tabs as fully broken; it treats them as partially fixed and requires tests/normalization.

### Added scope

- Student Dashboard fake fallback values are now explicitly included in Phase 10.
- Student quiz attempt metadata now explicitly requires `timeLimitMinutes`/`expiresAt` in Phase 1 and Phase 2.
- Competition leaderboard mapping now explicitly requires backend `Score`/`score` in Phase 1 and Phase 6.
- Message invalidation after send/read is now explicit in Phase 7.
- `latest-zip-delta.md` audit file is added in Phase 0.

### Unchanged major work

- Student quizzes remain P0.
- Teacher/Admin homework lifecycle remains P0.
- Competitions full lifecycle remains P0.
- Homework real file upload remains P0.
- Fake fallbacks remain production blockers.

---

## 7. Final Implementation Guidance

Do not execute this as one giant patch. Execute in this order:

```text
Phase 0 audit files
Phase 1 DTOs/adapters
Phase 2 student quizzes
Phase 4 + Phase 5 homework lifecycle
Phase 6 competitions
Phase 7 messages
Phase 8 announcements
Phase 9 lessons
Phase 10 fake fallback cleanup
Phase 11 API client cleanup
Phase 12 guards
Phase 13 verification
```

The safest cut is:

1. Fix contracts first.
2. Fix student-visible false states second.
3. Add missing teacher/admin management UIs third.
4. Remove fake fallbacks fourth.
5. Prove with E2E last.

Final target: every visible status in DerasaX must reflect real backend state, every lifecycle action must be executable from the correct role UI, and no page should hide backend gaps behind fake/demo values.
