# Frontend ↔ Backend Contract Map (v2 audit baseline)

Generated for `derasax_frontback_state_lifecycle_fix_plan_v2.md` Phase 0. Statuses:
`matched | mismatch | missing-ui | missing-client-method | fake-fallback | backend-only | frontend-only | partially-fixed`.

## Quizzes

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Quizzes | StudentQuizzesPage | studentApi.assignedQuizzes | GET /api/v1/assigned-quizzes | - | AssignedQuizDto | mismatch | DTO lacks status/latestAttemptId/latestAttemptStatus/questionCount/score/percentage → tabs + result link broken |
| Quizzes | StudentQuizAttemptPage | studentApi.attempt/startQuiz | GET /api/v1/attempts/{id}, POST /api/v1/quizzes/{id}/attempts | - | AttemptDetailDto | mismatch | DTO lacks quizTitle/timeLimitMinutes/questionCount → timer fakes 30 min |
| Quizzes | StudentQuizResultPage | studentApi.attemptResult | GET /api/v1/attempts/{id}/result | - | AttemptDetailDto | mismatch+fake-fallback | answers[] lack questionText/correctAnswer/explanation/pointsPossible and Id not populated; page fakes 88%/18/20/i!==2 |
| Quizzes | StudentQuizzesPage (graded link) | - | GET /api/v1/attempts/{id}/result | - | - | mismatch | "View Result" link uses quizId not attemptId → 404 |
| Quizzes | TeacherQuizzesPage | teacherApi.quiz/updateQuestion/publishQuiz/assignQuiz | /api/v1/quizzes/* | UpdateQuestionDto/AddQuestionDto | QuizDetailDto | partially-fixed | edit question text only; no add-question UI, no metadata edit UI, no create-quiz method |

## Homework

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Homework | StudentHomeworkPage (list) | studentApi.homework | GET /api/v1/homework/assigned | - | AssignedHomeworkDto | partially-fixed | derives status from submissionStatus/hasSubmitted; missing submissionId/gradedAt/canSubmit/attachmentFileId; fakes 20-point fallback |
| Homework | StudentHomeworkPage (submit) | studentApi.submitHomework | POST /api/v1/homework/{id}/submit | SubmitHomeworkDto | HomeworkSubmissionDto | fake-fallback | file never uploaded; `[Attached file: name]` injected into content; AttachmentFileId never sent |
| Homework | StudentHomeworkPage (my-sub) | studentApi.homeworkSubmission | GET /api/v1/homework/{id}/my-submission | - | HomeworkSubmissionDto | mismatch | 404 not normalized to null → query errors+retries |
| Homework | StudentHomeworkPage (draft) | - | - | - | - | fake-fallback | "Save Draft" only alert()s; no backend draft |
| Homework | (Teacher) MISSING | none | POST/PUT /api/v1/homework, /{id}/publish, /{id}/submissions, /submissions/{id}/grade | CreateHomeworkDto etc | HomeworkDto | missing-ui+missing-client-method | backend complete; no teacher pages or teacherApi methods |
| Homework | /app/teacher/assignments | - | - | - | - | mismatch | route points to TeacherQuizzesPage |

## Competitions

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Competitions | StudentCompetitionsPage (list) | studentApi.competitions | GET /api/v1/competitions | - | CompetitionDto | mismatch | no role filter → Draft/Archived visible; DTO lacks hasEntered/canEnter/canSubmit/canViewLeaderboard/score/rank |
| Competitions | StudentCompetitionsPage (enter) | studentApi.enterCompetition | POST /api/v1/competitions/{id}/entries | - | CompetitionEntryDto | mismatch | Enter shown after entry (no hasEntered) |
| Competitions | StudentCompetitionsPage (leaderboard) | studentApi.leaderboard | GET /api/v1/competitions/{id}/leaderboard | - | LeaderboardRowDto | mismatch+fake-fallback | called unconditionally (403 while Published); reads totalPoints/points not Score |
| Competitions | StudentCompetitionsPage (submit) | studentApi.submitCompetition | POST /api/v1/competitions/{id}/submissions | SubmitCompetitionDto | CompetitionSubmissionDto | mismatch | submit card shown before entry |
| Competitions | (Teacher) MISSING | none | POST/PUT/publish/archive/close/score/submissions | Create/Update/RecordScoreDto | CompetitionDto | missing-ui+missing-client-method | backend complete; no teacher pages/methods |
| Competitions | SchoolCompetitionsPage | schoolApi.competitions | GET /api/v1/competitions | - | CompetitionDto | backend-only | read-only banner |

## Messages

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Messages | CommunicationThreadPage | notificationsApi.messages | GET /api/v1/conversations/{id}/messages | - | MessageDto | mismatch | MessageDto has no read-state; receipts persisted but never returned |
| Messages | CommunicationThreadPage (read) | notificationsApi.markMessageRead | POST .../messages/{messageId}/read | - | - | missing-ui | method exists but never called on thread open |
| Messages | CommunicationThreadPage (send) | notificationsApi.postMessage | POST .../messages | PostMessageDto | MessageDto | partially-fixed | invalidates messages only, not conversation list |
| Messages | CommunicationMessagesPage (new) | notificationsApi.startConversation | POST /api/v1/conversations | StartConversationDto | ConversationDto | frontend-only | free-text participant GUID; no picker/search endpoint |

## Announcements

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Announcements | SchoolAnnouncementsPage | schoolApi.createAnnouncement | POST /api/v1/announcements | CreateAnnouncementDto | AnnouncementDto | mismatch | create sets IsActive=true immediately (no draft) but never notifies |
| Announcements | SchoolAnnouncementsPage | schoolApi.publishAnnouncement | POST /api/v1/announcements/{id}/publish?publish= | - | - | partially-fixed | publish/unpublish via flag; notifications only on publish=true |

## Lessons / Dashboard

| Area | Page/Component | API Client Method | Backend Endpoint | Request DTO | Response DTO | Status | Notes |
|---|---|---|---|---|---|---|---|
| Lessons | StudentLessonsPage | studentApi.subjects/units/lessons | GET /Lessons/GetLessonsByUnitId | - | LessonDto | fake-fallback | brute-force subject→unit→lesson walk; fakes Mathematics/Integration/18:24/8 pages |
| Lessons | StudentLessonsPage (detail) | none | (no direct GET /student/lessons/{id}) | - | - | missing-client-method | no single-lesson detail endpoint |
| Dashboard | StudentDashboardPage | studentApi.* (composed) | many | - | many | fake-fallback | fakes 58 progress, 7 streak, 2580 points, Calculus Midterm Quiz, 20/30 |
</content>
</invoke>
