import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { renderWithProviders } from '../../test/utils'
import HomePage from './HomePage'

// The homepage must render for anonymous visitors. renderWithProviders wraps in
// i18n + Router + React Query + Toasts but NOT the AuthProvider, so a successful
// render here proves the page does not require authentication.
describe('public HomePage', () => {
  it('renders the DerasaX marketing hero and the three content cards', () => {
    renderWithProviders(<HomePage />)
    expect(screen.getByRole('heading', { level: 1, name: /DerasaX/i })).toBeInTheDocument()
    expect(screen.getByText('Our Events')).toBeInTheDocument()
    expect(screen.getByText('Our Activities')).toBeInTheDocument()
    expect(screen.getByText('Our News')).toBeInTheDocument()
  })

  it('shows no fabricated statistics', () => {
    const { container } = renderWithProviders(<HomePage />)
    expect(container.textContent).not.toContain('55%')
  })
})
