import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import CommunicationNotificationsPage from './CommunicationNotificationsPage'
import { notificationsApi } from '../../../features/notifications/notificationsApi'

// Regression test for F-11 / P2-13: a backend-supplied `actionUrl` was rendered
// as a Link with no allow-listing (Open redirect risk if a notification carries
// an absolute/external URL).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'user-1', fullName: 'User One' } }),
}))

vi.mock('../../../features/notifications/notificationsApi', () => ({
  notificationsApi: { list: vi.fn(), unreadCount: vi.fn(), markRead: vi.fn(), markAllRead: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/notifications']}>
          <CommunicationNotificationsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  notificationsApi.unreadCount.mockResolvedValue(0)
})

describe('CommunicationNotificationsPage actionUrl sanitization', () => {
  it('renders an Open link for a safe internal actionUrl', async () => {
    notificationsApi.list.mockResolvedValue([
      { id: 'n1', title: 'Homework graded', body: 'b', category: 4, isRead: true, createdAt: new Date().toISOString(), actionUrl: '/app/student/homework/hw-1' },
    ])
    renderPage()
    const link = await screen.findByRole('link', { name: 'Open' })
    expect(link).toHaveAttribute('href', '/app/student/homework/hw-1')
  })

  it('does not render a Link for an external actionUrl', async () => {
    notificationsApi.list.mockResolvedValue([
      { id: 'n2', title: 'Suspicious', body: 'b', category: 4, isRead: true, createdAt: new Date().toISOString(), actionUrl: 'https://evil.example/phish' },
    ])
    renderPage()
    await screen.findByText('Suspicious')
    expect(screen.queryByRole('link', { name: 'Open' })).not.toBeInTheDocument()
  })

  it('does not render a Link for a non-/app/ internal path', async () => {
    notificationsApi.list.mockResolvedValue([
      { id: 'n3', title: 'API leak', body: 'b', category: 4, isRead: true, createdAt: new Date().toISOString(), actionUrl: '/api/v1/notifications' },
    ])
    renderPage()
    await screen.findByText('API leak')
    expect(screen.queryByRole('link', { name: 'Open' })).not.toBeInTheDocument()
  })
})

// Regression test for F-12 / P3-5 (ported from the retired StudentNotificationsPage
// facade coverage): no loading branch meant a brief false "No notifications" flash
// while the real request was still in flight.
describe('CommunicationNotificationsPage loading state', () => {
  it('shows a loading indicator, not a false-empty state, while the request is in flight', async () => {
    let resolveFn
    notificationsApi.list.mockReturnValue(new Promise((resolve) => { resolveFn = resolve }))
    renderPage()

    // The info Alert is also role="status" (aria-live="polite"), so scope by content.
    const statuses = screen.getAllByRole('status')
    expect(statuses.some((el) => el.textContent.includes('Loading'))).toBe(true)
    expect(screen.queryByText('No notifications')).not.toBeInTheDocument()

    resolveFn([])
    expect(await screen.findByText('No notifications')).toBeInTheDocument()
  })
})

// Ported from the retired CommunicationPage facade: the notification centre shows
// the real unread count from the backend, and marking a notification read calls
// the real backend mutation.
describe('CommunicationNotificationsPage — notification centre', () => {
  it('shows the real unread count and the inbox items from the backend', async () => {
    notificationsApi.unreadCount.mockResolvedValue(2)
    notificationsApi.list.mockResolvedValue([
      { id: 'n1', title: 'Your geometry result is ready', body: 'Your quiz was graded', category: 4, isRead: false, createdAt: new Date().toISOString() },
    ])
    renderPage()
    expect(await screen.findByText('Your geometry result is ready')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByText(/Unread: 2/)).toBeInTheDocument())
  })

  it('marks a notification as read through the backend mutation', async () => {
    notificationsApi.unreadCount.mockResolvedValue(1)
    notificationsApi.list.mockResolvedValue([
      { id: 'n1', title: 'Quiz graded', body: 'b', category: 4, isRead: false, createdAt: new Date().toISOString() },
    ])
    notificationsApi.markRead.mockResolvedValue(null)
    renderPage()
    const btn = await screen.findByRole('button', { name: /mark read/i })
    await userEvent.click(btn)
    await waitFor(() => expect(notificationsApi.markRead).toHaveBeenCalledWith('n1'))
  })
})
