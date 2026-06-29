import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { apiFetch, apiJson, setAccessToken, setOnAuthLost, ApiError, __resetClientState } from './client'

function res(status, body = {}, headers = {}) {
  return new Response(status === 204 ? null : JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json', ...headers },
  })
}

describe('canonical API client', () => {
  beforeEach(() => {
    __resetClientState()
  })
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('blocks absolute URLs (backend-only)', async () => {
    await expect(apiFetch('https://evil.example/api')).rejects.toMatchObject({
      errorCode: 'CLIENT_ABSOLUTE_URL_BLOCKED',
    })
  })

  it('attaches the bearer token and a correlation id', async () => {
    setAccessToken('tok-123')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(res(200, { ok: true }))
    await apiFetch('/api/v1/grades')
    const [, init] = fetchMock.mock.calls[0]
    expect(init.headers.get('Authorization')).toBe('Bearer tok-123')
    expect(init.headers.get('X-Correlation-Id')).toBeTruthy()
    expect(init.credentials).toBe('include')
  })

  it('refreshes once on 401 then retries the original request', async () => {
    setAccessToken('expired')
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(res(401, { errorCode: 'UNAUTHENTICATED' })) // original
      .mockResolvedValueOnce(res(200, { token: 'fresh' })) // refresh
      .mockResolvedValueOnce(res(200, { data: 1 })) // retried original
    const r = await apiFetch('/api/v1/student/dashboard')
    expect(r.status).toBe(200)
    // 3 calls: original, refresh, retry
    expect(fetchMock).toHaveBeenCalledTimes(3)
    const refreshCall = fetchMock.mock.calls[1][0]
    expect(refreshCall).toContain('/api/v1/account/refresh')
  })

  it('deduplicates concurrent 401 refreshes into a single refresh', async () => {
    setAccessToken('expired')
    let refreshCalls = 0
    vi.spyOn(globalThis, 'fetch').mockImplementation((url) => {
      if (typeof url === 'string' && url.includes('/account/refresh')) {
        refreshCalls++
        return Promise.resolve(res(200, { token: 'fresh' }))
      }
      // Two distinct endpoints: first response 401, retry 200.
      return Promise.resolve(res(refreshCalls > 0 ? 200 : 401, { ok: true }))
    })
    await Promise.all([apiFetch('/api/a'), apiFetch('/api/b')])
    expect(refreshCalls).toBe(1)
  })

  it('invokes onAuthLost (logout) when the refresh fails', async () => {
    setAccessToken('expired')
    const onAuthLost = vi.fn()
    setOnAuthLost(onAuthLost)
    vi.spyOn(globalThis, 'fetch').mockImplementation((url) => {
      if (typeof url === 'string' && url.includes('/account/refresh')) return Promise.resolve(res(401))
      return Promise.resolve(res(401))
    })
    await apiFetch('/api/secure')
    expect(onAuthLost).toHaveBeenCalledTimes(1)
  })

  it('apiJson throws a normalized ApiError on non-2xx', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      res(409, { title: 'Conflict', errorCode: 'CONFLICT', correlationId: 'cid-9' }, { 'X-Correlation-Id': 'cid-9' })
    )
    await expect(apiJson('/api/x', { method: 'POST', _retried: true })).rejects.toMatchObject({
      status: 409,
      errorCode: 'CONFLICT',
      correlationId: 'cid-9',
    })
  })

  it('surfaces a network error as a retryable ApiError', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'))
    await expect(apiFetch('/api/x', { _retried: true })).rejects.toBeInstanceOf(ApiError)
  })
})
