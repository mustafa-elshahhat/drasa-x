import { describe, it, expect } from 'vitest'
import { ApiError, problemFromResponse, networkError, userMessageFor } from './problemDetails'

function jsonResponse(status, body, headers = {}) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/problem+json', ...headers },
  })
}

describe('Problem Details normalization', () => {
  it('extracts errorCode + correlationId from the body and header', async () => {
    const res = jsonResponse(
      403,
      { title: 'Forbidden', detail: 'no', errorCode: 'FORBIDDEN', correlationId: 'cid-1' },
      { 'X-Correlation-Id': 'cid-1' }
    )
    const err = await problemFromResponse(res)
    expect(err).toBeInstanceOf(ApiError)
    expect(err.status).toBe(403)
    expect(err.errorCode).toBe('FORBIDDEN')
    expect(err.correlationId).toBe('cid-1')
  })

  it('falls back to the X-Correlation-Id header when absent from the body', async () => {
    const res = jsonResponse(500, { title: 'Server error' }, { 'X-Correlation-Id': 'cid-hdr' })
    const err = await problemFromResponse(res)
    expect(err.correlationId).toBe('cid-hdr')
  })

  it('maps 422 validation errors into fieldErrors', async () => {
    const res = jsonResponse(422, {
      title: 'Validation failed',
      errors: { NewPassword: ['Too short'], CurrentPassword: ['Required'] },
    })
    const err = await problemFromResponse(res)
    expect(err.fieldErrors.NewPassword).toEqual(['Too short'])
    expect(err.fieldErrors.CurrentPassword).toEqual(['Required'])
  })

  it('builds a retryable network error', () => {
    const err = networkError(new Error('boom'), 'cid-x')
    expect(err.status).toBe(0)
    expect(err.isNetworkError).toBe(true)
    expect(err.retryable).toBe(true)
    expect(err.correlationId).toBe('cid-x')
  })

  it('produces safe, status-specific user messages', () => {
    expect(userMessageFor(new ApiError({ status: 403 }))).toMatch(/permission/i)
    expect(userMessageFor(new ApiError({ status: 429 }))).toMatch(/too many/i)
    expect(userMessageFor(new ApiError({ status: 503 }))).toMatch(/server/i)
    expect(userMessageFor(new ApiError({ status: 404 }))).toMatch(/not be found/i)
  })
})
