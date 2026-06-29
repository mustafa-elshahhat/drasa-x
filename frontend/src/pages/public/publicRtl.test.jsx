import { describe, it, expect, afterEach } from 'vitest'
import { screen, act } from '@testing-library/react'
import { renderWithProviders, i18n } from '../../test/utils'
import HomePage from './HomePage'

// i18n / RTL smoke for a PUBLIC page: switching to Arabic flips the document
// direction to RTL (via the shared i18n direction handler) and the marketing
// hero renders translated Arabic copy. Language changes are wrapped in act()
// because they re-render the mounted tree via the languageChanged event.
describe('public marketing i18n/RTL', () => {
  afterEach(async () => {
    await act(async () => {
      await i18n.changeLanguage('en')
    })
  })

  it('flips document direction to RTL and renders Arabic hero copy', async () => {
    await act(async () => {
      await i18n.changeLanguage('ar')
    })
    renderWithProviders(<HomePage />)
    expect(document.documentElement.getAttribute('dir')).toBe('rtl')
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(/مرحبًا/)
  })
})
