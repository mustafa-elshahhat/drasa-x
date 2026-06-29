import { describe, it, expect, vi, beforeEach } from 'vitest'

const calls = []
vi.mock('../../lib/api/client', () => ({
  api: {
    get: (p, o) => { calls.push(['GET', p, o]); return Promise.resolve({ data: { id: 'm1' } }) },
    post: (p, b) => { calls.push(['POST', p, b]); return Promise.resolve({ data: { token: 't', downloadUrl: '/api/v1/files/download?token=t' } }) },
    del: (p) => { calls.push(['DELETE', p]); return Promise.resolve(true) },
    upload: (p, body) => { calls.push(['UPLOAD', p, body]); return Promise.resolve({ data: { id: 'f1', fileName: 'a.pdf' } }) },
  },
  apiFetch: vi.fn(),
}))

import { apiFetch } from '../../lib/api/client'
import { filesApi, filenameFromResponse, downloadToBrowser } from './filesApi'

beforeEach(() => {
  calls.length = 0
  vi.clearAllMocks()
})

describe('filesApi (Phase 16, backend-only)', () => {
  it('uploads multipart form data to the backend upload endpoint', async () => {
    const file = new File([new Uint8Array(4)], 'a.pdf', { type: 'application/pdf' })
    const result = await filesApi.upload({ file, purpose: 'CommunityAttachment' })
    const [verb, path, body] = calls[0]
    expect(verb).toBe('UPLOAD')
    expect(path).toBe('/api/v1/files/upload')
    expect(body).toBeInstanceOf(FormData)
    expect(body.get('Purpose')).toBe('CommunityAttachment')
    expect(result).toEqual({ id: 'f1', fileName: 'a.pdf' })
  })

  it('requests a signed download token from the backend', async () => {
    const signed = await filesApi.signedDownload('f1')
    expect(calls[0]).toEqual(['POST', '/api/v1/files/f1/signed-download', null])
    expect(signed.token).toBe('t')
  })

  it('parses the filename from a Content-Disposition header', () => {
    const res = { headers: { get: () => 'attachment; filename="report card.pdf"' } }
    expect(filenameFromResponse(res)).toBe('report card.pdf')
    const star = { headers: { get: () => "attachment; filename*=UTF-8''r%C3%A9sum%C3%A9.pdf" } }
    expect(filenameFromResponse(star)).toBe('résumé.pdf')
    const none = { headers: { get: () => '' } }
    expect(filenameFromResponse(none, 'fallback.bin')).toBe('fallback.bin')
  })

  it('downloads through apiFetch (never a raw storage URL)', async () => {
    apiFetch.mockResolvedValue({
      ok: true,
      blob: async () => new Blob(['bytes']),
      headers: { get: () => 'attachment; filename="lesson.pdf"' },
    })
    const name = await downloadToBrowser('/api/v1/files/f1/download')
    expect(apiFetch).toHaveBeenCalledWith('/api/v1/files/f1/download', { method: 'GET' })
    expect(name).toBe('lesson.pdf')
  })

  it('rejects absolute (non-backend) download URLs', async () => {
    await expect(downloadToBrowser('https://evil.example/x')).rejects.toThrow()
  })
})
