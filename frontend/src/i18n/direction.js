// Direction helpers (Phase 7 §14). Pure functions so they are trivially testable.
export const RTL_LANGUAGES = ['ar', 'he', 'fa', 'ur']

export function directionForLanguage(lng) {
  const base = (lng || 'en').split('-')[0].toLowerCase()
  return RTL_LANGUAGES.includes(base) ? 'rtl' : 'ltr'
}

/** Apply <html dir/lang> for the given language. Safe to call in any env. */
export function applyDocumentDirection(lng) {
  if (typeof document === 'undefined') return
  const dir = directionForLanguage(lng)
  document.documentElement.setAttribute('dir', dir)
  document.documentElement.setAttribute('lang', (lng || 'en').split('-')[0])
}
