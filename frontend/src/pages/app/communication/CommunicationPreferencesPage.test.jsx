import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import CommunicationPreferencesPage from './CommunicationPreferencesPage'
import { notificationsApi } from '../../../features/notifications/notificationsApi'

// Ported from the retired CommunicationPage facade: mandatory notification
// categories cannot be disabled by the user, while optional ones can.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'user-1', fullName: 'User One' }, isAuthenticated: true }),
}))

vi.mock('../../../features/notifications/notificationsApi', () => ({
  notificationsApi: { preferences: vi.fn(), updatePreference: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/notifications/preferences']}>
          <CommunicationPreferencesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('CommunicationPreferencesPage', () => {
  it('locks mandatory categories so they cannot be disabled', async () => {
    notificationsApi.preferences.mockResolvedValue([
      { category: 1, categoryName: 'Warning', inAppEnabled: true, emailEnabled: false, mandatory: true, emailConfigured: false },
      { category: 6, categoryName: 'Announcement', inAppEnabled: true, emailEnabled: false, mandatory: false, emailConfigured: false },
    ])
    renderPage()
    const warningRow = await screen.findByTestId('pref-Warning')
    const inAppToggle = warningRow.querySelector('input[type="checkbox"]')
    expect(inAppToggle).toBeDisabled()
    const announceRow = screen.getByTestId('pref-Announcement')
    expect(announceRow.querySelector('input[type="checkbox"]')).not.toBeDisabled()
  })
})
