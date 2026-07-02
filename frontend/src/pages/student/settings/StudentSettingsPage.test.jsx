import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentSettingsPage from './StudentSettingsPage'

// Regression test for §9 "Static settings pages" / P3-7: the language card was
// inert static text (a real, working LanguageSwitcher already renders in the
// global Header on this same screen) — trimmed the duplicate, and replaced it
// with a real link to the already-functional notification-preferences page.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/settings']}>
          <StudentSettingsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentSettingsPage', () => {
  it('links to security and notification preferences, with no inert language card', () => {
    renderPage()

    expect(screen.getByRole('link', { name: /change password/i })).toHaveAttribute('href', '/app/security')
    expect(screen.getByRole('link', { name: /manage notification preferences/i })).toHaveAttribute(
      'href',
      '/app/notifications/preferences',
    )
    expect(screen.queryByText('Language preference remains the approved local UI preference.')).not.toBeInTheDocument()
  })
})
