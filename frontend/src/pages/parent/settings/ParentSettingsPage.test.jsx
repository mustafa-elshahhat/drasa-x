import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentSettingsPage from './ParentSettingsPage'

// Regression test for §9 "Static settings pages" / P3-7: previously only a
// single password link — added a real link to the already-functional
// notification-preferences page so Settings is not a dead end.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' }, role: 'Parent' }),
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/parent/settings']}>
          <ParentSettingsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentSettingsPage', () => {
  it('links to security and notification preferences', () => {
    renderPage()

    expect(screen.getByRole('link', { name: /change password/i })).toHaveAttribute('href', '/app/security')
    expect(screen.getByRole('link', { name: /manage notification preferences/i })).toHaveAttribute(
      'href',
      '/app/notifications/preferences',
    )
  })
})
