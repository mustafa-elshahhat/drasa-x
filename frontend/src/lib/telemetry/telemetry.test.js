import { describe, it, expect } from 'vitest'
import { redact } from './telemetry'

describe('telemetry redaction (no sensitive data leaves the browser)', () => {
  it('redacts sensitive keys', () => {
    const out = redact({
      token: 'abc',
      password: 'hunter2',
      newPassword: 'x',
      authorization: 'Bearer y',
      message: 'private chat',
      email: 'a@b.com',
      keep: 'visible',
    })
    expect(out.token).toBe('[redacted]')
    expect(out.password).toBe('[redacted]')
    expect(out.newPassword).toBe('[redacted]')
    expect(out.authorization).toBe('[redacted]')
    expect(out.message).toBe('[redacted]')
    expect(out.email).toBe('[redacted]')
    expect(out.keep).toBe('visible')
  })

  it('redacts bearer tokens / JWTs found in string values', () => {
    expect(redact('Bearer abc.def.ghi')).toBe('[redacted-token]')
    expect(redact('eyJhbGciOiJIUzI1Niative.payloadpart.signature')).toBe('[redacted-token]')
  })

  it('recurses into nested objects and arrays', () => {
    const out = redact({ nested: { password: 'p', ok: 1 }, list: [{ token: 't' }] })
    expect(out.nested.password).toBe('[redacted]')
    expect(out.nested.ok).toBe(1)
    expect(out.list[0].token).toBe('[redacted]')
  })
})
