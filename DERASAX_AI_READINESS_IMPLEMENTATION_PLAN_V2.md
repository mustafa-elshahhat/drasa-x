# DerasaX AI Readiness Implementation Plan V2

> Version: V2 — updated against the latest `drasa-x-main.zip` review  
> Scope: AI Tutor, Books/RAG, Quiz AI, Pain-point Analysis, Prediction, Computer Vision Attendance, and production verification.  
> Project rule: **do not create Git commits, do not rewrite history, do not push, do not tag. Keep all work as local uncommitted changes unless explicitly instructed otherwise.**

---

## 0. Current Assessment Summary

The current codebase has a solid AI architecture:

```text
school-ai-frontend → DerasaX-backend → school-ai-rag
```

The frontend does not call the AI service directly. The backend remains the system of record and calls the internal AI endpoints using service authentication and tenant-scoped context. This is the correct security boundary.

However, the current code is not yet fully production-complete for AI. Several capabilities have frontend/backend/AI wiring, but they still need missing data pipelines, real model assets, context propagation, and end-to-end verification.

### What improved since V1

```text
✅ Student Tutor now supports optional subject selection.
✅ AI document internal endpoints are stronger: ingest/delete/reindex/status.
✅ Quiz AI backend and AI validation are stronger.
✅ Pain-point backend/review flow is mostly present.
✅ Prediction backend feature extraction exists partially.
✅ Computer Vision staff workflow exists: session/frame/analyze/candidates/confirm/reject/override.
```

### What still blocks production AI

```text
❌ LessonMaterial upload does not automatically extract text and index into RAG.
❌ Tutor still lacks full student/grade/lesson/history context.
❌ Quiz generation still depends on indexed content but the UI does not clearly show indexing readiness.
❌ Pain-point analysis needs a clear frontend run/review workflow.
❌ Prediction needs real model files, model metadata, and frontend operation/profile UI.
❌ CV needs real local model assets, enrollment images → embeddings pipeline, and no runtime model downloads.
❌ AI readiness needs per-capability status, not only one global health result.
```

---

## 1. Target End State

The AI system is considered ready only when these flows work end-to-end:

```text
Teacher uploads lesson material/book
→ backend extracts text
→ backend indexes it into AI RAG
→ indexing status becomes Indexed
→ student asks a question without manually typing the subject when possible
→ tutor answers from the indexed material with citations
→ teacher generates a quiz from the same indexed content
→ student solves the quiz
→ pain-point analysis can be generated and human-reviewed
→ prediction uses real model files or clearly reports unavailable
→ CV can enroll student images, generate embeddings, analyze frames, suggest candidates, and only mark attendance after teacher confirmation
```

---

## 2. Hard Constraints

These constraints apply to every phase:

```text
- Do not commit, push, rebase, amend, squash, tag, or alter Git history.
- Do not make the frontend call the AI service directly.
- Do not bypass DerasaX-backend as the system of record.
- Do not store permanent business data only in the AI service.
- Do not fake AI success when models, documents, embeddings, or provider configuration are missing.
- Do not silently fall back to stub behavior in production.
- Do not skip, delete, weaken, or mark tests flaky to get green results.
- Do not introduce runtime model downloads for CV or prediction.
- Do not expose cross-tenant data in retrieval, citations, analysis, quiz generation, prediction, or vision results.
```

---

## 3. Capability Matrix — Current vs Required

| Capability | Current State | Main Gap | Required End State |
|---|---|---|---|
| AI Tutor | Front/Back/AI present; subject optional | Missing grade/lesson/history/context and query rewrite | Student can ask naturally; system uses context and citations |
| Books/RAG | AI ingest/delete/reindex/status exists | LessonMaterial upload not connected to extraction/indexing | Uploading material makes it searchable by AI |
| Quiz AI | Backend/AI strong; draft generation exists | UI lacks indexing awareness and source visibility | Generate grounded quiz only from indexed content |
| Pain-point | Backend/review mostly present | Frontend run/review flow and signal aggregation incomplete | Teacher reviews AI-generated insights before exposure |
| Prediction | Backend feature extraction exists partially | Missing real model files, model metadata, frontend operation UI | Real prediction or honest unavailable state |
| CV Attendance | Workflow present; staff can confirm | Missing real models, enrollment embeddings, no-runtime-download guard | Real recognition candidates, teacher-confirmed attendance |
| Readiness | Global health exists | Needs per-capability readiness matrix | Admin-visible truthful readiness by capability |

---

# Phase 0 — AI Audit & Readiness Baseline

## Objective

Create a truthful, per-capability readiness baseline for AI. The system must clearly distinguish between:

```text
Ready
Degraded
Stub
Unavailable
Misconfigured
```

This phase must not implement major product behavior. It should make the true state visible and testable.

## Current Code Context

Known existing areas:

```text
ai/app/api.py
ai/app/internal/v1/*
ai/app/security/service_auth.py
backend/src/DerasaX.Application/Services/Ai/*
backend/src/DerasaX.Api/Controllers/Ai*Controller.cs
backend/src/DerasaX.Api/Controllers/ClassroomVisionController.cs
frontend/src/pages/student/tutor/StudentTutorPage.jsx
frontend/src/pages/teacher/quizzes/TeacherQuizGeneratePage.jsx
frontend/src/pages/vision/VisionStaffPage.jsx
```

## Required Work

### Backend

Add or extend an admin/system-admin AI readiness endpoint, for example:

```text
GET /api/v1/system-admin/ai/readiness
```

Return a per-capability matrix:

```json
{
  "tutor": { "status": "ready|degraded|unavailable", "reasons": [] },
  "rag": { "status": "ready|degraded|unavailable", "reasons": [] },
  "quiz": { "status": "ready|degraded|unavailable", "reasons": [] },
  "analysis": { "status": "ready|degraded|unavailable", "reasons": [] },
  "prediction": { "status": "ready|degraded|unavailable", "reasons": [] },
  "vision": { "status": "ready|stub|degraded|unavailable", "reasons": [] }
}
```

The backend should call or derive readiness from the AI service, but it must not collapse all AI status into one global pass/fail result.

### AI Service

Enhance readiness checks to report per capability:

```text
RAG:
- Chroma/vectorstore import available
- vectorstore path writable
- embedding model available
- retrieval can initialize

Tutor:
- LLM provider configured where required
- prompt registry available
- RAG dependency ready or degraded clearly reported

Quiz:
- LLM provider configured
- RAG dependency ready or degraded clearly reported

Analysis:
- LLM provider configured
- non-diagnostic guard available

Prediction:
- rf_regressor.pkl exists
- rf_classifier.pkl exists
- feature schema/version available

Vision:
- selected engine: stub/torch/auto
- torch import status
- opencv import status
- detector dependency status
- local model files status
- no runtime download requirement satisfied
```

### Frontend

Add or update admin/system-admin health UI to show AI capability status. Do not show one vague “AI healthy” label.

## New Checks Required

```text
- Missing prediction models → prediction status = unavailable, not ready.
- Missing CV weights → vision status = stub/degraded/unavailable, not ready.
- Stub mode enabled → visible in readiness response and UI.
- AI auth/signing mismatch → AI service connection/auth status fails clearly.
```

## Acceptance Criteria

```text
✅ Admin/system-admin can see per-capability AI readiness.
✅ Missing .pkl prediction models do not look like a successful prediction system.
✅ Missing CV model assets do not look like production vision readiness.
✅ Stub/degraded modes are visible and test-covered.
✅ No direct frontend-to-AI calls are introduced.
```

## Tests

```text
Backend:
- Readiness endpoint returns all expected capability keys.
- Missing AI service returns degraded/unavailable safely.
- Missing prediction models maps to prediction unavailable.
- Stub CV maps to vision stub/degraded.

AI:
- Capability readiness tests for missing/present model files.
- No runtime download allowed in readiness.

Frontend:
- Readiness UI renders ready/degraded/unavailable/stub states.
```

---

# Phase 1 — Books/RAG Ingestion Pipeline

## Objective

Make uploaded lesson materials/books become searchable AI knowledge automatically.

Current gap: the AI document endpoints exist, but normal LessonMaterial upload does not reliably perform:

```text
file upload → text extraction → document ingest → indexing status → tutor/quiz retrieval
```

This is the highest-priority phase.

## Current Code Context

Likely relevant areas:

```text
backend/src/DerasaX.Api/Controllers/LessonMaterialController.cs
backend/src/DerasaX.Application/Services/Ai/AiDocumentService.cs
backend/src/DerasaX.Api/Controllers/AiDocumentsController.cs
ai/app/internal/v1/documents*.py
ai/app/rag/*
frontend/src/pages/teacher/*materials*
frontend/src/features/*materials*
```

## Required Work

### Backend — Material Indexing State

Add durable indexing status for lesson materials. Use existing entities if already present; otherwise add fields/entities through EF migrations.

Required status values:

```text
NotIndexed
Indexing
Indexed
Failed
NeedsReindex
Deleted
```

Track at minimum:

```text
MaterialId
TenantId
SchoolId
GradeId
SubjectId
UnitId
LessonId
OriginalFileName
ContentType
FileChecksum
AiDocumentId
IndexedChunkCount
IndexingStatus
IndexingError
IndexedAt
LastIndexAttemptAt
```

### Backend — Text Extraction

Implement a server-side text extraction pipeline for supported types:

```text
PDF text extraction
DOCX text extraction
TXT/plain text
```

Do not claim scanned PDF/OCR support unless OCR is actually implemented.

If OCR is not implemented, scanned PDFs must fail with a clear message such as:

```text
The file was uploaded, but no extractable text was found. OCR is not enabled.
```

### Backend — Upload → Ingest Integration

When a teacher/admin uploads lesson material:

```text
1. Store the file using the existing file storage abstraction.
2. Extract text.
3. Create or update indexing state = Indexing.
4. Call AI document ingest via backend AI client.
5. Store AiDocumentId, chunk count, status = Indexed.
6. If extraction or AI ingest fails, store status = Failed and the safe error message.
```

### Backend — Reindex/Delete

Add or wire endpoints:

```text
POST   /api/v1/lesson-materials/{id}/ai/reindex
GET    /api/v1/lesson-materials/{id}/ai/status
DELETE /api/v1/lesson-materials/{id}/ai-index
```

On material update:

```text
- If file/checksum changes → NeedsReindex or automatic reindex.
```

On material delete:

```text
- Delete/disable indexed AI document.
- Do not leave stale chunks retrievable.
```

### AI Service

Ensure document ingest metadata includes:

```text
tenantId
schoolId
gradeId
subjectId
subjectName
unitId
lessonId
materialId
sourceTitle
contentType
checksum
pageNumber where available
```

Support:

```text
- ingest document
- delete document by material/document id
- reindex document
- status by document/material id
```

Retrieval must filter by tenant and, where provided, grade/subject/unit/lesson.

### Frontend

In material upload/list/detail screens:

```text
- Show AI indexing status.
- Show Indexed / Indexing / Failed / Needs Reindex.
- Add Re-index action for allowed roles.
- Show safe indexing error.
- Prevent misleading “AI-ready” labels before Indexed status.
```

## Acceptance Criteria

```text
✅ Teacher uploads a PDF/DOCX/TXT lesson material.
✅ Backend extracts text and indexes it into AI RAG.
✅ UI shows Indexed when complete.
✅ Student asks a question from that material and receives an answer with citation.
✅ Deleting the material prevents future answers from citing it.
✅ Failed extraction is shown honestly.
✅ Cross-tenant retrieval is impossible.
```

## Tests

```text
Backend:
- Upload supported file → indexing state transitions to Indexed.
- Empty/scanned/no-text file → Failed with safe message.
- Delete material → AI document delete called.
- Reindex endpoint updates status and AiDocumentId/chunk count.
- Tenant isolation tests.

AI:
- Ingest stores metadata.
- Retrieval filters by tenant/grade/subject/lesson.
- Delete removes or disables retrievable chunks.

Frontend:
- Indexing status renders.
- Reindex action works.
- Failed status is visible.

E2E:
- Upload material → ask tutor → cited answer from uploaded material.
```

---

# Phase 2 — Smart Tutor Context

## Objective

Make the tutor work naturally when the student writes only the question, with no need to manually type the subject in most cases.

The current code has improved because the frontend now supports optional subject selection, but it still needs full context propagation and intelligent retrieval behavior.

## Current Gap

```text
Current: message + language + optional subject
Missing: grade, lesson context, unit context, material context, chat history, enrolled-subject filtering, query rewrite/typo correction
```

## Required Work

### Frontend

Update tutor payload to include available context:

```text
message
language
subjectId or subject name when selected
current route context if opened from lesson/material
recent conversation history summary or last N messages
```

When the student opens tutor from a subject/lesson/material page, preserve and send that context.

### Backend

Do not trust the frontend as the source of truth for grade/enrollment. Derive server-side context:

```text
studentId from auth
tenantId from auth
gradeId from student profile
enrolled/allowed subjects from backend data
schoolId from tenant/school context
```

Build a TutorContext object before calling AI:

```text
TenantId
StudentId
GradeId
AllowedSubjectIds
SelectedSubjectId
UnitId
LessonId
MaterialId
Language
ConversationHistory
```

Enforce:

```text
- Student can only retrieve from own tenant.
- Student retrieval should prioritize own grade and enrolled subjects.
- Lesson/material context should narrow retrieval when present.
```

### AI Service

Add or improve query processing:

```text
- Light query rewrite for typos and incomplete questions.
- Preserve original language.
- Do not change factual intent.
- Use chat history for follow-up questions such as “اشرحها تاني” or “why?”.
```

Retrieval order:

```text
1. Exact lesson/material context if provided.
2. Selected subject if provided.
3. Student grade + enrolled subjects.
4. Safe fallback within tenant only.
```

If the query is ambiguous and retrieval confidence is low, return a clarification question instead of hallucinating.

## Acceptance Criteria

```text
✅ Student can ask a clear question without manually typing subject and get the right answer when indexed material exists.
✅ Tutor uses subject/lesson context when available.
✅ Follow-up questions use recent conversation context.
✅ Typo-heavy but understandable questions are rewritten or handled safely.
✅ Low-confidence retrieval returns clarification/no-answer, not fabricated content.
✅ Citations are preserved.
```

## Tests

```text
Backend:
- Tutor derives grade/enrolled subjects server-side.
- Student cannot inject another grade/tenant context.
- Selected subject narrows retrieval.

AI:
- Query rewrite test for Arabic/English typo examples.
- Follow-up question test with chat history.
- Low-confidence no-answer/clarification test.

Frontend:
- Tutor sends subject when selected.
- Tutor sends context when opened from lesson/material.

E2E:
- Student opens lesson → asks “اشرح دي” → answer uses current lesson context.
```

---

# Phase 3 — Quiz AI Hardening

## Objective

Keep the existing Quiz AI flow, but make it production-safe, source-aware, and indexing-aware.

The backend and AI validation are already stronger than before, so this phase is not a full rebuild. It is hardening and UX completion.

## Required Work

### Frontend

Update teacher quiz generation UI to include:

```text
grade
subject
unit optional
lesson optional
material optional
difficulty
question count
question types
```

Show AI readiness for the selected scope:

```text
- Indexed content exists
- Not enough indexed content
- Material not indexed
- AI unavailable/degraded
```

Show citations/sources after generation.

### Backend

Before calling AI:

```text
- Validate teacher permission for grade/subject.
- Check indexed content availability for selected scope.
- Reject generation if no indexed content exists unless a deliberate non-RAG mode exists and is clearly labeled.
```

After AI response:

```text
- Validate strict JSON structure.
- Validate question count.
- Validate options and correct answers.
- Validate duplicate questions/options.
- Validate points.
- Validate no empty explanation where explanation is required.
- Persist quiz only as Draft.
- Persist source references if supported by schema.
```

### AI Service

Quiz generation must:

```text
- Use retrieved context only.
- Return strict JSON only.
- Include source references per question if possible.
- Refuse or return insufficient_context when RAG context is inadequate.
```

## Acceptance Criteria

```text
✅ Teacher cannot generate grounded quiz from unindexed material.
✅ Generated quiz remains Draft.
✅ Questions are tied to indexed content/sources.
✅ Malformed AI output is rejected.
✅ Duplicate or invalid options are rejected.
✅ UI shows why generation is unavailable when material is not indexed.
```

## Tests

```text
Backend:
- No indexed content → quiz generation rejected.
- Malformed AI response → rejected.
- Duplicate options → rejected.
- Unauthorized teacher → forbidden.

AI:
- Insufficient context returns explicit insufficient_context.
- Generated JSON schema tests.

Frontend:
- Not-indexed warning shown.
- Source/citation rendering after draft generation.

E2E:
- Upload material → indexed → generate quiz → draft saved with valid questions.
```

---

# Phase 4 — Pain-point Analysis Flow

## Objective

Complete the product workflow around pain-point analysis. The backend/review flow is mostly present; the main missing piece is frontend operation, review UX, and richer signal aggregation.

## Required Work

### Backend

Ensure analysis can use multiple signals:

```text
tutor conversations
quiz attempts
homework submissions
attendance
grades
engagement observations where available
```

Expose safe endpoints for:

```text
POST generate pain-point analysis
GET pending reviews
PUT approve/reject/edit recommendation
GET approved insights for student/parent views
```

The current code may already contain much of this; do not duplicate endpoints unnecessarily. Reuse and harden existing controllers/services.

### AI Service

Maintain non-diagnostic behavior:

```text
- No medical/psychological diagnosis.
- No sensitive labeling.
- Always mark humanReviewRequired = true for generated insights.
```

Return structured output:

```text
category
evidence/signals
recommendation
confidence
humanReviewRequired
```

### Frontend

Add or complete teacher/admin UI:

```text
Student AI Insights page
Generate analysis action
Pending pain-point review list
Approve / Reject / Edit recommendation
Evidence display
Confidence display
```

Student and parent views must show approved insights only.

## Acceptance Criteria

```text
✅ Teacher can generate an analysis for a student.
✅ Result is PendingReview by default.
✅ Teacher/admin can approve/reject/edit.
✅ Student/parent only see approved insight projections.
✅ AI output contains evidence and recommendation.
✅ No diagnostic/medical language is displayed.
```

## Tests

```text
Backend:
- Generate stores PendingReview.
- Approve makes visible to allowed projections.
- Reject hides from student/parent.
- Parent cannot see unapproved results.

AI:
- Guard rejects/removes diagnostic language.
- Output schema validation.

Frontend:
- Pending review list renders.
- Approve/reject actions work.
- Student/parent approved-only rendering.
```

---

# Phase 5 — Prediction Real Model Flow

## Objective

Make prediction either real and model-backed, or honestly unavailable. No fake fallback.

The backend now appears to have partial feature extraction, but real model files are still missing from the reviewed zip.

## Required Work

### AI Service

Provide and load real model assets:

```text
rf_regressor.pkl
rf_classifier.pkl
```

Add model metadata:

```text
modelVersion
trainedAt
featureSchemaVersion
expectedFeatureNames
expectedFeatureRanges
trainingDatasetNote or model card
```

Prediction behavior:

```text
- If models are missing: return model_unavailable.
- If features are invalid: return validation_error.
- If models load: return prediction with modelVersion and confidence/risk fields.
```

### Backend

Review and complete feature extraction from real DerasaX data:

```text
attendanceRate
quizAverage
homeworkCompletionRate
recentGradeTrend
studyTime
engagementScore when available
learningProfile
```

Store prediction results with:

```text
studentId
predictedScore
riskLevel
confidence
factors
modelVersion
featureSchemaVersion
createdAt
```

### Frontend

Add/complete UI for teacher/admin:

```text
Run prediction
Latest prediction
Prediction history
Risk level
Contributing factors
Learning profile editor
Unavailable state when model files are missing
```

Use careful wording:

```text
Prediction
Risk indicator
Requires review
Not a final judgment
```

## Acceptance Criteria

```text
✅ Missing model files show Prediction Unavailable, not fake numbers.
✅ Present model files produce a prediction with modelVersion.
✅ Invalid feature shape is rejected.
✅ Prediction result is stored in backend DB.
✅ Teacher/admin can view latest prediction and factors.
✅ Student-facing wording does not overstate certainty.
```

## Tests

```text
AI:
- Missing model returns model_unavailable.
- Invalid features return validation_error.
- Valid model fixture returns valid prediction schema.

Backend:
- Feature extraction uses real student data.
- Prediction result persisted.
- Unauthorized roles cannot run prediction.

Frontend:
- Unavailable state visible.
- Prediction result and factors render.
```

---

# Phase 6 — Computer Vision Attendance Real Flow

## Objective

Move CV from workflow/stub/degraded capability to real recognition readiness, while preserving human confirmation and privacy boundaries.

The current workflow is good, but real recognition requires local models and enrollment embeddings.

## Hard Rule

Attendance must not be marked automatically by AI.

```text
AI suggests candidate(s)
Teacher confirms/rejects/overrides
Backend records attendance
```

## Required Work

### Privacy / Consent Gate

Before enabling enrollment asset upload by default, define and enforce:

```text
- Who can upload student images?
- Has the school/parent consented?
- How long images are retained?
- Can images be deleted?
- Are raw images stored, or only embeddings?
- Who can view/download assets?
```

Keep `Cv:EnrollmentAssetsEnabled = false` unless the consent/product policy is implemented.

### AI Service — No Runtime Downloads

Remove or guard any behavior that downloads model weights at runtime.

Required:

```text
- Face detection/recognition weights must be local.
- Readiness must fail/degrade if local weights are missing.
- Do not call external model download URLs during app startup or frame analysis.
```

Pay special attention to any use of:

```text
InceptionResnetV1(pretrained="vggface2")
```

This must not trigger runtime downloads in production.

### AI Service — Real Model Assets

Define required local assets, for example:

```text
face detector weights/config
face embedding model weights
recognition index/classifier
emotion.pth
engagement.pth
normalizer.pkl where applicable
centroids/index files where applicable
```

The exact set should match the final engine implementation. Do not claim readiness for assets that are not actually used.

### AI Service — Enrollment Pipeline

Implement:

```text
student enrollment images
→ face detection/cropping
→ embedding generation
→ embedding quality validation
→ recognition index update
→ status persisted/returned
```

Return enrollment processing status:

```text
Uploaded
Processing
EmbeddingGenerated
ModelUpdated
Failed
```

### Backend

Complete enrollment asset and embedding workflow:

```text
- Upload enrollment image only when enabled and authorized.
- Store consent/status metadata.
- Trigger AI enrollment processing.
- Store mapping between externalLabelId/embedding identity and studentId.
- Allow deletion/revocation.
- Audit all confirm/reject/override actions.
```

### Frontend

Add/complete staff/admin enrollment UI:

```text
Student enrollment image upload
Enrollment status
Processing failure reason
Delete/revoke enrollment asset
Consent/status indicator
```

Vision session UI should show:

```text
engine mode: real/stub/degraded
candidate confidence
unknown detections
confirm/reject/override
attendance saved result
```

## Acceptance Criteria

```text
✅ Missing local CV model files make vision status stub/degraded/unavailable.
✅ No runtime download occurs during startup or frame analysis.
✅ Staff can enroll student images only when enabled and authorized.
✅ Enrollment creates usable embeddings/index entries.
✅ Frame analysis suggests known students from enrollment data.
✅ Teacher confirmation creates attendance record.
✅ Reject/override are audited.
✅ Raw image privacy policy is enforced.
```

## Tests

```text
AI:
- Missing weights → degraded/unavailable.
- Runtime download is not attempted.
- Enrollment image → embedding fixture test.
- Frame with enrolled identity → candidate returned.

Backend:
- Enrollment upload disabled by default.
- Unauthorized upload forbidden.
- Confirm attendance creates record.
- Reject does not create attendance.
- Override creates record with audit.

Frontend:
- Enrollment UI states.
- Vision degraded banner.
- Confirm/reject/override flows.

E2E:
- Enroll student → analyze frame → candidate appears → teacher confirms → attendance saved.
```

---

# Phase 7 — Full AI Verification & Production Gates

## Objective

Prove the full AI stack works end-to-end without fake success.

## Required Verification Matrix

### Tutor + RAG

```text
- Upload supported material.
- Indexing status becomes Indexed.
- Ask from same material.
- Answer includes citation.
- Ask outside indexed content.
- System returns no-answer/clarification.
- Delete material.
- Deleted material is no longer cited.
```

### Tutor Context

```text
- Ask without subject but with grade/enrolled subject context.
- Ask with selected subject.
- Ask from lesson page using lesson context.
- Ask typo-heavy Arabic/English question.
- Ask follow-up question using history.
```

### Quiz AI

```text
- Generate quiz from indexed lesson.
- Reject generation for unindexed material.
- Reject malformed AI JSON.
- Validate duplicate options/questions.
- Save as Draft only.
```

### Pain-point Analysis

```text
- Generate analysis from student data/conversation.
- Result is pending review.
- Approve makes visible to student/parent.
- Reject keeps hidden.
- Diagnostic language is blocked.
```

### Prediction

```text
- Missing models → unavailable state.
- Present fixture models → prediction result saved.
- Invalid features → validation error.
- UI displays risk/factors carefully.
```

### Vision

```text
- Missing model files → stub/degraded/unavailable, not ready.
- Enrollment disabled by default unless configured.
- Enrollment image processing creates embedding/index entry.
- Frame analysis returns candidate.
- Confirm creates attendance.
- Reject does not create attendance.
- Override creates attendance with audit.
```

## Required Commands

Use project-appropriate commands. Do not weaken tests to pass.

Expected verification categories:

```text
Backend:
- dotnet restore
- dotnet build
- dotnet test
- EF pending model changes check if available

Frontend:
- npm install / existing package manager install if needed
- lint
- unit tests
- build
- Playwright e2e relevant suites

AI:
- pytest
- readiness tests
- RAG tests
- prediction tests
- vision tests

Local orchestration:
- start local stack
- verify-local script if available
- smoke test full AI flows
```

## Final Done Criteria

```text
✅ No build errors.
✅ No lint errors.
✅ No failing tests.
✅ No tests skipped/deleted/softened to pass.
✅ No Git commits created.
✅ AI readiness shows truthful per-capability status.
✅ Tutor/RAG/Quiz flow works from uploaded material.
✅ Prediction is real or honestly unavailable.
✅ Vision is real only with local models/enrollment; otherwise honestly degraded/stub.
✅ All AI flows remain backend-mediated and tenant-isolated.
```

---

# Updated Execution Order

Follow this order:

```text
1. Phase 0 — Patch readiness first so the team can see what is real vs degraded.
2. Phase 1 — Implement LessonMaterial extraction/indexing. This unlocks Tutor and Quiz.
3. Phase 2 — Add Smart Tutor context/history/query rewrite.
4. Phase 3 — Harden Quiz AI around indexed content and sources.
5. Phase 4 — Complete Pain-point frontend/review/product flow.
6. Phase 5 — Add prediction model files/metadata/UI or honest unavailable state.
7. Phase 6 — Add real CV enrollment/embeddings/local model readiness; no runtime downloads.
8. Phase 7 — Run full AI verification and document evidence.
```

---

# Anti-Goals

Do not do any of these:

```text
- Do not replace the backend-mediated AI architecture with direct frontend calls.
- Do not claim scanned PDF/OCR support unless OCR is implemented and tested.
- Do not allow AI quiz generation from unindexed content while labeling it grounded.
- Do not expose unreviewed pain-point analysis to students or parents.
- Do not generate fake prediction numbers when models are missing.
- Do not treat CV stub detections as real attendance intelligence.
- Do not download CV weights at runtime.
- Do not auto-mark attendance without teacher confirmation.
- Do not add broad unrelated rewrites.
```

---

# Suggested Final Report Format After Implementation

When implementation is complete, produce a report with this structure:

```markdown
# DerasaX AI Readiness V2 — Completion Report

## Final Status
PASSED / FAILED / PARTIAL

## Capability Matrix
| Capability | Status | Evidence | Remaining Gap |
|---|---|---|---|

## Implemented Changes
- Backend
- Frontend
- AI service
- Database/migrations
- Tests

## Verification Evidence
- Backend commands + result
- Frontend commands + result
- AI commands + result
- E2E flows + result

## Known Limitations
- OCR
- Prediction model readiness
- CV model readiness
- Any environment blockers

## Git Status
No commits created. Local changes only.
```

---

# Short Implementation Prompt

Use this prompt to execute the plan in a coding agent:

```text
You are a senior full-stack + AI systems engineer working on DerasaX.

Objective: Implement DERASAX_AI_READINESS_IMPLEMENTATION_PLAN_V2.md across the existing DerasaX repository, preserving the architecture: school-ai-frontend → DerasaX-backend → school-ai-rag. The backend remains the system of record. The frontend must never call the AI service directly.

Context:
- DerasaX is a multi-tenant smart-school platform.
- Current AI areas include Tutor, RAG documents, Quiz generation, Pain-point analysis, Prediction, and Computer Vision attendance.
- Some AI wiring already exists; do not duplicate existing controllers/services. Reuse and harden them.
- Current gaps include LessonMaterial upload not automatically indexing into RAG, incomplete Tutor context/history/query rewrite, quiz indexing awareness, pain-point frontend/review flow, missing prediction model files/metadata/UI, and incomplete real CV enrollment/embeddings/local-model readiness.

Hard constraints:
- Do not create Git commits.
- Do not push, rebase, amend, squash, tag, or alter Git history.
- Do not delete/skip/weaken tests.
- Do not fake AI readiness or fake predictions.
- Do not treat CV stub mode as real recognition.
- Do not add runtime model downloads.
- Do not bypass tenant isolation.
- Do not introduce direct frontend-to-AI calls.

Execution:
1. First inspect the current code and map existing files/endpoints to the plan.
2. Implement phases in order: Phase 0 through Phase 7.
3. Keep each change minimal and aligned with current project style.
4. Add or update tests for backend, frontend, and AI.
5. Run the relevant verification commands.
6. Produce a completion report using the report format in the plan.

Definition of done:
- Per-capability AI readiness exists and is truthful.
- Uploading supported lesson material extracts text and indexes it into RAG.
- Tutor uses student/grade/subject/lesson/history context and handles low-confidence questions safely.
- Quiz generation is grounded in indexed content and remains Draft.
- Pain-point analysis has human review before student/parent exposure.
- Prediction is either model-backed or honestly unavailable.
- CV is real only with local model assets and enrollment embeddings; otherwise clearly stub/degraded.
- All tests pass without weakened coverage.
- No Git commit is created.
```
