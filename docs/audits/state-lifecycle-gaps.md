# State Lifecycle Gaps (v2 audit baseline)

| Entity | Expected Lifecycle | Backend Support | Frontend Support | Gap |
|---|---|---|---|---|
| Quiz (teacher) | Draft → Published → Archived | full (QuizStatus Draft/…/Published/Archived; publish/archive/assign endpoints) | partial | no add-question UI, no metadata-edit UI, no manual create; cache invalidation incomplete |
| Quiz attempt (student) | Available → InProgress → Submitted → Graded | full (QuizSubmission status InProgress/Submitted/Graded/Late) | broken | AssignedQuizDto exposes no status/latestAttemptId → tabs always "available", result link uses quizId |
| Quiz result | by attemptId | full (GET /attempts/{id}/result) | broken | list "View Result" passes quizId; result page fabricates score/correctness when fields absent |
| Homework (teacher) | Draft → Published → (submissions) → Graded | full (create/publish/submissions/grade) | missing | no teacher pages/routes/API methods at all |
| Homework (student) | NotSubmitted → Submitted/Late → Graded | full (assigned/submit/my-submission, AttachmentFileId) | partial | fake file upload, fake draft, 404 not normalized, fake 20-point fallback |
| Competition | Draft → Published → Closed → Archived | partial | partial | `Active` enum value never set; student leaderboard gated to Active/Closed only; list returns Draft/Archived to students; no student-facing flags; no teacher UI |
| Competition entry/submit | Enter → Submit → Score → Leaderboard | full (entries/submissions/score/leaderboard) | broken | student page shows Enter after entry, Submit before entry, calls leaderboard unconditionally, reads wrong score field |
| Message | Sent → Read | partial | missing | receipts persisted but MessageDto has no read-state; markRead never called on thread open |
| Announcement | Draft → Published → Unpublished | partial | partial | create sets IsActive=true immediately; no draft state; notifications only on publish |
| Lesson | Open → Completed (100%) | functional (complete endpoint + progress) | functional+fake | works, but no single-lesson detail endpoint and page renders fake lesson metadata when data missing |
| Student dashboard | real metrics | composed (no aggregate endpoint) | fake-fallback | renders fabricated 58/7/2580/Calculus Midterm Quiz when data missing |
</content>
