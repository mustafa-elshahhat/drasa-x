import { describe, it, expect } from 'vitest'
import { envSchema, config } from './env'

describe('environment configuration validation', () => {
  it('requires a valid absolute backend URL', () => {
    expect(envSchema.safeParse({ VITE_DOTNET_URL: 'http://localhost:5155' }).success).toBe(true)
    expect(envSchema.safeParse({ VITE_DOTNET_URL: '' }).success).toBe(false)
    expect(envSchema.safeParse({ VITE_DOTNET_URL: 'not-a-url' }).success).toBe(false)
  })

  it('rejects a missing backend URL', () => {
    expect(envSchema.safeParse({}).success).toBe(false)
  })

  it('exposes a frozen, normalized config with a backend URL and no trailing slash', () => {
    expect(config.backendUrl).toBeTruthy()
    expect(config.backendUrl.endsWith('/')).toBe(false)
    expect(Object.isFrozen(config)).toBe(true)
  })

  it('never exposes an AI-service URL field', () => {
    expect(config).not.toHaveProperty('aiUrl')
    expect(config).not.toHaveProperty('ragUrl')
  })
})
