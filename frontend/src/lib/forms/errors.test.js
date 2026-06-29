import { describe, it, expect, vi } from 'vitest'
import { applyServerErrors } from './errors'
import { ApiError } from '../api/problemDetails'

describe('applyServerErrors (backend validation mapping)', () => {
  it('maps PascalCase field errors onto camelCase form fields', () => {
    const setError = vi.fn()
    const err = new ApiError({ status: 422, fieldErrors: { CurrentPassword: ['Required'] } })
    const formLevel = applyServerErrors(err, setError, { fieldMap: { CurrentPassword: 'currentPassword' } })
    expect(formLevel).toBeNull()
    expect(setError).toHaveBeenCalledWith('currentPassword', { type: 'server', message: 'Required' })
  })

  it('lowercases the first letter when no explicit map entry exists', () => {
    const setError = vi.fn()
    const err = new ApiError({ status: 422, fieldErrors: { NewPassword: ['Too short'] } })
    applyServerErrors(err, setError)
    expect(setError).toHaveBeenCalledWith('newPassword', { type: 'server', message: 'Too short' })
  })

  it('returns a form-level message for non-field errors', () => {
    const setError = vi.fn()
    const err = new ApiError({ status: 409, title: 'Conflict' })
    const formLevel = applyServerErrors(err, setError)
    expect(formLevel).toBe('Conflict')
    expect(setError).not.toHaveBeenCalled()
  })
})
