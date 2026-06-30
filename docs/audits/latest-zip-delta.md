# Latest Audit Delta (v2)

## What improved since the prior audit
- **Student Homework list status**: the frontend now derives status from `submissionStatus` + `hasSubmitted` in the list flow (kept; to be normalized through a single helper + tested).
- Competition durable submissions (`/submissions/me`, 404→null) already exist on the student API client.

## What stayed the same (still broken)
- Student quiz state/result flow: `AssignedQuizDto` lacks `status`/`latestAttemptId`/`questionCount`; result link uses `quizId`; result page fakes 88%/18/20 and `isCorrect = i !== 2`; attempt timer fakes 30 minutes.
- Teacher quiz authoring incomplete (no add-question / metadata-edit UI; MCQ option-preserving update needs verification).
- Teacher/Admin homework lifecycle entirely missing from the frontend.
- Student homework file upload still fake (`[Attached file: name]` text injection); `AttachmentFileId` never sent; fake Save Draft; `my-submission` 404 not normalized; fake 20-point fallback.
- Competitions lifecycle backend-heavy / frontend-incomplete; `Active` status unreachable; student leaderboard 403 while Published; leaderboard reads `totalPoints`/`points` not `Score`.
- Message read receipts persisted but never surfaced or triggered from the thread UI.
- Announcements create as active immediately (weakens draft/publish semantics; notifications only on explicit publish).
- Fake fallbacks remain across Lessons and Student Dashboard.

## What became newly visible
- `MessageDto` returns **no** read-state at all (receipts are write-only from the UI's perspective) — read receipts must be surfaced (per-message `IsRead`/`ReadAt` and/or conversation `unreadCount`).
- Competition `LeaderboardRowDto.Score` is the correct field (decimal); frontend mismatch confirmed.
- No single-lesson detail endpoint exists; student lesson page brute-forces subject→unit→lesson.
- No aggregate student-dashboard endpoint; dashboard is composed client-side and falls back to fabricated values.

## Plan phases changed because of the latest zip
- Phase 5 reduced: status tabs are *partially fixed*; remaining work is real file upload, draft removal, 404 normalization, fallback cleanup, and tests.
- Phase 10 expanded: Student Dashboard fabricated values (58/7/2580/Calculus Midterm Quiz/20/30) explicitly tracked.
- Phase 1/2: attempt metadata must add `timeLimitMinutes`/`expiresAt`; Phase 1/6: leaderboard mapping must use `Score`.
- Phase 7: message invalidation after send/read made explicit; surfacing read-state added as a backend prerequisite.
</content>
