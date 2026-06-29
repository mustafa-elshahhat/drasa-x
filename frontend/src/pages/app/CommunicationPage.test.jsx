import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import CommunicationPage from './CommunicationPage'
import { notificationsApi } from '../../features/notifications/notificationsApi'

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'user-1', fullName: 'User One' }, isAuthenticated: true }),
}))

vi.mock('../../features/notifications/notificationsApi', () => ({
  notificationsApi: {
    list: vi.fn(),
    unreadCount: vi.fn(),
    markRead: vi.fn(),
    markAllRead: vi.fn(),
    preferences: vi.fn(),
    updatePreference: vi.fn(),
    conversations: vi.fn(),
    conversation: vi.fn(),
    messages: vi.fn(),
    postMessage: vi.fn(),
    startConversation: vi.fn(),
  },
}))

function renderPage(view) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/notifications']}>
          <Routes>
            <Route path="/app/notifications" element={<CommunicationPage view={view} />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => vi.clearAllMocks())

describe('CommunicationPage — notification centre', () => {
  it('shows the real unread count and the inbox items from the backend', async () => {
    notificationsApi.unreadCount.mockResolvedValue(2)
    notificationsApi.list.mockResolvedValue([
      { id: 'n1', title: 'Your geometry result is ready', body: 'Your quiz was graded', category: 4, isRead: false, createdAt: new Date().toISOString() },
    ])
    renderPage('notifications')
    expect(await screen.findByText('Your geometry result is ready')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText(/Unread: 2/)).toBeInTheDocument())
  })

  it('marks a notification as read through the backend mutation', async () => {
    notificationsApi.unreadCount.mockResolvedValue(1)
    notificationsApi.list.mockResolvedValue([
      { id: 'n1', title: 'Quiz graded', body: 'b', category: 4, isRead: false, createdAt: new Date().toISOString() },
    ])
    notificationsApi.markRead.mockResolvedValue(null)
    renderPage('notifications')
    const btn = await screen.findByRole('button', { name: /mark read/i })
    await userEvent.click(btn)
    await waitFor(() => expect(notificationsApi.markRead).toHaveBeenCalledWith('n1'))
  })
})

describe('CommunicationPage — preferences', () => {
  it('locks mandatory categories so they cannot be disabled', async () => {
    notificationsApi.preferences.mockResolvedValue([
      { category: 1, categoryName: 'Warning', inAppEnabled: true, emailEnabled: false, mandatory: true, emailConfigured: false },
      { category: 6, categoryName: 'Announcement', inAppEnabled: true, emailEnabled: false, mandatory: false, emailConfigured: false },
    ])
    renderPage('preferences')
    const warningRow = await screen.findByTestId('pref-Warning')
    const inAppToggle = warningRow.querySelector('input[type="checkbox"]')
    expect(inAppToggle).toBeDisabled()
    const announceRow = screen.getByTestId('pref-Announcement')
    expect(announceRow.querySelector('input[type="checkbox"]')).not.toBeDisabled()
  })
})
