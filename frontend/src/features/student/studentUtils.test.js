import { describe, expect, it } from 'vitest'
import { answerPayloadFromForm, displayValue, formatDate, itemId, safeInternalPath } from './studentUtils'

describe('student utilities', () => {
  it('uses only server-provided display fields', () => {
    expect(displayValue({ Title: 'Algebra' })).toBe('Algebra')
    expect(displayValue({})).toBe('')
    expect(itemId({ QuizId: 'q1' })).toBe('q1')
  })

  it('validates notification deep links as internal app paths only', () => {
    expect(safeInternalPath('/app/student/homework')).toBe('/app/student/homework')
    expect(safeInternalPath('https://evil.test/app/student')).toBeNull()
    expect(safeInternalPath('/api/v1/notifications')).toBeNull()
  })

  it('formats dates with the active locale', () => {
    expect(formatDate('2026-06-26T10:00:00Z', 'en')).toContain('2026')
  })

  it('builds quiz answer payloads without client-side scoring', () => {
    const form = new FormData()
    form.set('q:q1', 'o1')
    form.set('score', '100')
    expect(answerPayloadFromForm(form)).toEqual([{ questionId: 'q1', selectedOptionId: 'o1' }])
  })

  it('maps free-text (essay) answers to answerText, never selectedOptionId', () => {
    const form = new FormData()
    form.set('q:mcq', 'optionB')
    form.set('qt:essay', 'A linear equation graphs as a straight line.')
    form.set('qt:blank', '   ') // whitespace-only text is omitted
    expect(answerPayloadFromForm(form)).toEqual([
      { questionId: 'mcq', selectedOptionId: 'optionB' },
      { questionId: 'essay', answerText: 'A linear equation graphs as a straight line.' },
    ])
  })
})
