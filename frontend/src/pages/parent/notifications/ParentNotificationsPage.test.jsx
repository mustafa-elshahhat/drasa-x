import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentNotificationsPage from './ParentNotificationsPage'
import { parentApi } from '../../../features/parent/parentApi'

// Regression test for §6 "Parent notifications" / P2-2: this page ignored
// actionUrl entirely (the shared communication page honored it, this one
// didn't) — no deep-links for parents.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' }, role: 'Parent' }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { notifications: vi.fn(), markNotificationRead: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/parent/notifications']}>
          <ParentNotificationsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => vi.clearAllMocks())

describe('ParentNotificationsPage deep links', () => {
  it('renders a safe deep link when actionUrl is a real /app/ path', async () => {
    parentApi.notifications.mockResolvedValue({
      items: { status: 'fulfilled', value: [{ id: 'n1', title: 'Progress update', body: 'b', isRead: true, createdAt: new Date().toISOString(), actionUrl: '/app/parent/children/child-1/progress' }] },
      count: { status: 'fulfilled', value: { unreadCount: 0 } },
    })
    renderPage()
    const link = await screen.findByRole('link', { name: 'Open' })
    expect(link).toHaveAttribute('href', '/app/parent/children/child-1/progress')
  })

  it('does not render a link when actionUrl is unsafe/external', async () => {
    parentApi.notifications.mockResolvedValue({
      items: { status: 'fulfilled', value: [{ id: 'n2', title: 'Progress update', body: 'b', isRead: true, createdAt: new Date().toISOString(), actionUrl: 'https://evil.example' }] },
      count: { status: 'fulfilled', value: { unreadCount: 0 } },
    })
    renderPage()
    await screen.findByText('Progress update')
    expect(screen.queryByRole('link', { name: 'Open' })).not.toBeInTheDocument()
  })
})
