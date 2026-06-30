// Phase 9 — Teacher Portal shares the proven Phase 8 presentation helpers. Re-export
// (do not duplicate) so the teacher feature stays consistent with the student one.
export {
  displayValue,
  getField,
  itemId,
  statusLabel,
  formatDate,
  settledData,
  settledError,
  safeInternalPath,
} from '../student/studentUtils'

// The backend serializes QuizStatus as its numeric enum value (and occasionally as
// the name). Map both forms to a stable name so the UI logic and labels are correct.
const QUIZ_STATUS = { 0: 'Draft', 1: 'AiGenerated', 2: 'PendingReview', 3: 'Approved', 4: 'Published', 5: 'Archived' }

export function quizStatusName(value) {
  if (value === undefined || value === null || value === '') return ''
  if (typeof value === 'number') return QUIZ_STATUS[value] || String(value)
  if (/^\d+$/.test(String(value))) return QUIZ_STATUS[Number(value)] || String(value)
  return String(value)
}

export function isQuizPublished(value) {
  return quizStatusName(value).toLowerCase() === 'published'
}

// SubmissionStatus is likewise serialized as its numeric enum value.
const SUBMISSION_STATUS = { 1: 'Submitted', 2: 'Graded', 3: 'Late', 4: 'InProgress' }

export function submissionStatusName(value) {
  if (value === undefined || value === null || value === '') return ''
  if (typeof value === 'number') return SUBMISSION_STATUS[value] || String(value)
  if (/^\d+$/.test(String(value))) return SUBMISSION_STATUS[Number(value)] || String(value)
  return String(value)
}

// AssignmentStatus (homework lifecycle) — Draft/Published/Closed/Archived.
const ASSIGNMENT_STATUS = { 0: 'Draft', 1: 'Published', 2: 'Closed', 3: 'Archived' }
export function assignmentStatusName(value) {
  if (value === undefined || value === null || value === '') return ''
  if (typeof value === 'number') return ASSIGNMENT_STATUS[value] || String(value)
  if (/^\d+$/.test(String(value))) return ASSIGNMENT_STATUS[Number(value)] || String(value)
  return String(value)
}

// CompetitionStatus — Draft/Published/Active/Closed/Archived.
const COMPETITION_STATUS = { 0: 'Draft', 1: 'Published', 2: 'Active', 3: 'Closed', 4: 'Archived' }
export function competitionStatusName(value) {
  if (value === undefined || value === null || value === '') return ''
  if (typeof value === 'number') return COMPETITION_STATUS[value] || String(value)
  if (/^\d+$/.test(String(value))) return COMPETITION_STATUS[Number(value)] || String(value)
  return String(value)
}
