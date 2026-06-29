import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { Routes, Route } from 'react-router-dom'
import { renderWithProviders } from '../test/utils'
import { PublicLayout } from './PublicLayout'

function renderLayout() {
  return renderWithProviders(
    <Routes>
      <Route element={<PublicLayout />}>
        <Route path="/" element={<div>page body</div>} />
      </Route>
    </Routes>,
    { route: '/' }
  )
}

describe('PublicLayout', () => {
  it('shows the DerasaX brand wordmark', () => {
    renderLayout()
    expect(screen.getAllByText('DerasaX').length).toBeGreaterThan(0)
  })

  it('links "Sign in" to the real /login and "Request a demo" to /request-demo', () => {
    renderLayout()
    expect(screen.getByRole('link', { name: /sign in/i })).toHaveAttribute('href', '/login')
    expect(screen.getByRole('link', { name: /request a demo/i })).toHaveAttribute(
      'href',
      '/request-demo'
    )
  })

  it('wraps page content in marketing nav + footer landmarks', () => {
    renderLayout()
    expect(screen.getByRole('contentinfo')).toBeInTheDocument()
    expect(screen.getByText('page body')).toBeInTheDocument()
  })
})
