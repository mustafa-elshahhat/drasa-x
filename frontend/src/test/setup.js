import '@testing-library/jest-dom/vitest'
import { afterEach, expect, vi } from 'vitest'
import { cleanup } from '@testing-library/react'
import { toHaveNoViolations } from 'jest-axe'

// Register the axe accessibility matcher for all tests.
expect.extend(toHaveNoViolations)

// jsdom does not implement matchMedia; provide a no-op so components that read
// prefers-reduced-motion / responsive hooks don't crash under test.
if (!window.matchMedia) {
  window.matchMedia = (query) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  })
}

// scrollTo is referenced by some focus flows.
if (!window.scrollTo) window.scrollTo = () => {}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})
