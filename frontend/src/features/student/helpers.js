import { createElement } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ErrorState } from '../../components/ui/states'
import { useAuth } from '../auth/AuthContext'
import { STALE } from '../../lib/query/keys'
import { getField } from './studentUtils'

// =============================================================================
// Shared student-portal helpers, extracted from the former StudentPortalPage
// dispatcher so the split page modules can reuse them. Plain functions/hooks
// only (no JSX/component exports) — components live in their own files.
// =============================================================================

/** Thin wrapper over useQuery with the student-portal defaults. */
export function useStudentQuery(key, fn, options = {}) {
  return useQuery({
    queryKey: key,
    queryFn: ({ signal }) => fn(signal),
    staleTime: options.staleTime ?? STALE.short,
    enabled: options.enabled ?? true,
  })
}

// Common percentage-style fields on untyped progress records. Returns a real
// number where the backend provides one, else null (so callers can show an
// honest "not enough data" state rather than a fabricated percentage).
const PERCENT_KEYS = [
  'completionPercentage', 'CompletionPercentage', 'progress', 'Progress',
  'percentage', 'Percentage', 'masteryPercent', 'MasteryPercent',
  'score', 'Score', 'averageScore', 'AverageScore',
]
export function percentOf(item) {
  for (const k of PERCENT_KEYS) {
    const v = getField(item, k)
    if (v != null && v !== '' && Number.isFinite(Number(v))) return Number(v)
  }
  return null
}

export const ATTENDANCE_TONE = { present: 'success', late: 'warning', absent: 'danger', excused: 'info' }

/** Resolve the authenticated student id + active locale (as the dispatcher did). */
export function useStudentContext() {
  const { i18n } = useTranslation()
  const { user } = useAuth()
  return { userId: user?.id, locale: i18n.language || 'en' }
}

/**
 * Wrap a student page so it resolves { userId, locale } from auth/i18n itself
 * (instead of receiving them from the old dispatcher) and preserves the exact
 * "missing session" guard. Static route props (e.g. `mode`) pass straight through.
 * Uses createElement so this stays a JSX-free, component-free utility module.
 */
export function withStudentContext(Page) {
  function StudentRoutePage(props) {
    const { userId, locale } = useStudentContext()
    if (!userId) {
      return createElement(ErrorState, {
        error: { title: 'Missing session', detail: 'The authenticated user id is unavailable.' },
      })
    }
    return createElement(Page, { userId, locale, ...props })
  }
  return StudentRoutePage
}
