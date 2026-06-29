export function displayValue(item, keys = ['title', 'Title', 'name', 'Name', 'id', 'Id']) {
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

export function formatDate(value, locale) {
  if (!value) return '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(date)
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
