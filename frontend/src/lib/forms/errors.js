// =============================================================================
// Form error mapping (Phase 7 §9). Translates backend RFC 9457 validation
// Problem Details (422 with `errors: { Field: [msgs] }`) into react-hook-form
// setError calls, and exposes a form-level message for non-field errors.
// =============================================================================
import { ApiError } from '../api/problemDetails'

/**
 * Apply a backend ApiError to a react-hook-form instance.
 * @param {ApiError} error
 * @param {import('react-hook-form').UseFormSetError<any>} setError
 * @param {{ fieldMap?: Record<string,string> }} [opts] maps backend field names to form field names
 * @returns {string|null} a form-level message when the error is not field-specific
 */
export function applyServerErrors(error, setError, opts = {}) {
  if (!(error instanceof ApiError)) return 'Something went wrong. Please try again.'
  const fieldMap = opts.fieldMap || {}

  if (error.fieldErrors && typeof setError === 'function') {
    let applied = 0
    for (const [rawField, messages] of Object.entries(error.fieldErrors)) {
      // Backend uses PascalCase DTO names; map (or lowercase first letter) to form fields.
      const field = fieldMap[rawField] || rawField.charAt(0).toLowerCase() + rawField.slice(1)
      setError(field, { type: 'server', message: messages[0] })
      applied++
    }
    if (applied > 0) return null
  }

  // Non-field error: return a form-level message (caller renders it in an Alert).
  return error.title || error.detail || `Request failed (${error.status}).`
}
