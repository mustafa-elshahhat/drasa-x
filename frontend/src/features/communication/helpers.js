// Shared communication helpers (Phase 13 surfaces), extracted from the former
// CommunicationPage dispatcher. Pure functions/constants — no fabricated data.
export const CATEGORY_NAMES = ['General', 'Warning', 'Informational', 'QuizAssigned', 'QuizGraded', 'DeadlineReminder', 'Announcement']

export const categoryName = (c) => (typeof c === 'string' ? c : CATEGORY_NAMES[c] ?? 'General')

export function formatWhen(value) {
  if (!value) return ''
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleString()
}
