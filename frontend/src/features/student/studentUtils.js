// Default keys deliberately EXCLUDE id/Id: a user-facing title/name must never
// silently fall back to a raw identifier. Callers that genuinely want an id as a
// last resort use `itemId` or pass explicit keys.
export function displayValue(item, keys = ['title', 'Title', 'name', 'Name']) {
  if (!item || typeof item !== 'object') return ''
  for (const key of keys) {
    const value = item[key]
    if (value !== undefined && value !== null && value !== '') return String(value)
  }
  return ''
}

export function itemId(item, keys = ['id', 'Id', 'quizId', 'QuizId', 'assignmentId', 'AssignmentId']) {
  return displayValue(item, keys)
}

export function statusLabel(value) {
  if (value === undefined || value === null || value === '') return '—'
  return String(value)
}

// Lesson-material attachment type (backend AttachmentType enum: Video=1, Document=2, Image=3,
// Slides=4, Audio=5), serialized as its numeric value. Used by material detail/list views (student
// + teacher) to render an honest type label/icon instead of assuming every material is a video.
const ATTACHMENT_TYPE = { 1: 'Video', 2: 'Document', 3: 'Image', 4: 'Slides', 5: 'Audio' }
export function attachmentTypeName(value) {
  if (value === undefined || value === null || value === '') return ''
  if (typeof value === 'number') return ATTACHMENT_TYPE[value] || String(value)
  if (/^\d+$/.test(String(value))) return ATTACHMENT_TYPE[Number(value)] || String(value)
  return String(value)
}

export function formatDate(value, locale) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
}

export function formatDateOnly(value, locale) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(date)
}

// Backend DTOs are not typed and mix PascalCase / camelCase keys, so every field
// read goes through a dual-case lookup. Returns undefined when absent.
export function getField(item, key) {
  if (!item || typeof item !== 'object' || !key) return undefined
  if (item[key] !== undefined) return item[key]
  const pascal = key.charAt(0).toUpperCase() + key.slice(1)
  if (item[pascal] !== undefined) return item[pascal]
  const camel = key.charAt(0).toLowerCase() + key.slice(1)
  if (item[camel] !== undefined) return item[camel]
  return undefined
}

// Formats a raw value for display. Translation of enums/booleans is handled by
// the rendering component (which has `t`); this covers date/number/text only.
export function formatField(value, format, { locale } = {}) {
  if (value === undefined || value === null || value === '') return '—'
  switch (format) {
    case 'date':
      return formatDate(value, locale)
    case 'dateOnly':
      return formatDateOnly(value, locale)
    case 'number': {
      const n = Number(value)
      return Number.isFinite(n) ? new Intl.NumberFormat(locale).format(n) : String(value)
    }
    default:
      return String(value)
  }
}

// Resolves an enum value (numeric or string) against a label map to a chip
// descriptor { tone, labelKey }. The map is keyed by both the numeric value and
// the canonical name where applicable. Returns null when unmapped.
export function resolveStatus(value, map) {
  if (!map || value === undefined || value === null) return null
  if (map[value]) return map[value]
  const asString = String(value)
  return map[asString] || null
}

// Turns an untyped DTO key (camelCase / PascalCase / snake_case) into a readable
// label, e.g. "startDate" → "Start date", "AttendancePercentage" → "Attendance
// percentage". Used as the honest fallback for detail records whose exact field
// set is backend-defined and not individually translated. Never shows the raw key.
export function humanizeKey(key) {
  if (!key) return ''
  const spaced = String(key)
    .replace(/[_-]+/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim()
    .toLowerCase()
  return spaced.charAt(0).toUpperCase() + spaced.slice(1)
}

// Generic status tone from a value string/boolean, used when a typed enum map is
// not available so status cells still read with meaningful color (honest — the
// label is the backend value itself).
const TONE_BY_VALUE = {
  active: 'success', approved: 'success', published: 'success', resolved: 'success', completed: 'success', healthy: 'success', up: 'success', enabled: 'success', current: 'success', online: 'success',
  pending: 'warning', inprogress: 'warning', 'in progress': 'warning', open: 'warning', degraded: 'warning', draft: 'muted', scheduled: 'info',
  rejected: 'danger', suspended: 'danger', archived: 'muted', closed: 'muted', down: 'danger', failed: 'danger', disabled: 'danger', error: 'danger', offline: 'danger',
}
export function genericStatusTone(value) {
  if (typeof value === 'boolean') return value ? 'success' : 'muted'
  const v = String(value ?? '').trim().toLowerCase()
  return TONE_BY_VALUE[v] || 'muted'
}

// Derives a curated, ordered set of table columns from untyped list rows. Display
// fields (name/title) come first, then codes, then status/role (rendered as
// chips), then a date — humanized headers, never raw keys. Capped to avoid
// horizontal overflow.
export function autoColumns(rows, { limit = 6, hide = [] } = {}) {
  const first = (rows || []).find((r) => r && typeof r === 'object')
  if (!first) return []
  const hidden = new Set(['id', 'Id', 'tenantId', 'TenantId', ...hide])
  // Foreign-key identifiers (anything ending in `Id`, e.g. gradeId/subjectId/
  // studentId) are raw GUIDs and must never auto-surface as a table column.
  const isIdKey = (k) => /Id$/.test(k)
  const score = (k) => {
    const lk = k.toLowerCase()
    if (/(fullname|^name$|title|displayname)/.test(lk)) return 0
    if (/(code|email|^key$)/.test(lk)) return 1
    if (/(role)/.test(lk)) return 2
    if (/(status|state)/.test(lk)) return 3
    if (lk.startsWith('is') || /(active|enabled|published)/.test(lk)) return 4
    if (lk.endsWith('date') || lk.endsWith('at')) return 7
    return 6
  }
  return Object.entries(first)
    .filter(([k, v]) => !hidden.has(k) && !isIdKey(k) && v !== null && v !== undefined && typeof v !== 'object')
    .map(([k, v]) => ({ k, v }))
    .sort((a, b) => score(a.k) - score(b.k))
    .slice(0, limit)
    .map(({ k, v }) => {
      const lk = k.toLowerCase()
      let kind
      if (typeof v === 'boolean' || lk.startsWith('is')) kind = 'bool'
      else if (/(role)/.test(lk)) kind = 'role'
      else if (/(status|state)/.test(lk)) kind = 'status'
      const format = lk.endsWith('date') || lk.endsWith('at') ? 'date' : undefined
      return { key: k, header: humanizeKey(k), kind, format }
    })
}

// Derives display fields from an untyped record: primitive (non-object) values
// with humanized labels and date formatting for *date/*at keys. Hides ids and
// noisy keys by default so detail views read cleanly.
export function autoFields(item, { limit = 16, hide = [] } = {}) {
  if (!item || typeof item !== 'object') return []
  const hidden = new Set(['id', 'Id', 'tenantId', 'TenantId', ...hide])
  return Object.entries(item)
    .filter(([key, value]) => !hidden.has(key) && !/Id$/.test(key) && value !== null && value !== undefined && typeof value !== 'object')
    .slice(0, limit)
    .map(([key]) => {
      const lower = key.toLowerCase()
      const format = lower.endsWith('date') || lower.endsWith('at') ? 'date' : undefined
      return { key, label: humanizeKey(key), format }
    })
}

export function settledData(result, mapper = (v) => v) {
  return result?.status === 'fulfilled' ? mapper(result.value) : null
}

export function settledError(result) {
  return result?.status === 'rejected' ? result.reason : null
}

export function safeInternalPath(value) {
  if (!value || typeof value !== 'string') return null
  if (!value.startsWith('/app/')) return null
  if (/^https?:\/\//i.test(value)) return null
  return value
}

export function answerPayloadFromForm(formData) {
  // Selected-option answers (MCQ / true-false) use the `q:` prefix and carry an
  // option id; free-text answers (essay/short-answer) use the `qt:` prefix and
  // carry the typed text. They map to distinct backend fields so essay text is
  // never sent as an option id.
  const answers = []
  for (const [key, value] of formData.entries()) {
    if (key.startsWith('qt:')) {
      const questionId = key.slice(3)
      if (value && String(value).trim()) answers.push({ questionId, answerText: String(value) })
    } else if (key.startsWith('q:')) {
      const questionId = key.slice(2)
      if (value) answers.push({ questionId, selectedOptionId: String(value) })
    }
  }
  return answers
}
