import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherNotificationsPage from './TeacherNotificationsPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Regression test for F-12 / P3-5: no loading branch meant a brief false
// "No notifications" flash while the real request was still in flight.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: 'Teacher' }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: { notifications: vi.fn(), markNotificationRead: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher/notifications']}>
          <TeacherNotificationsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('TeacherNotificationsPage loading state', () => {
  it('shows a loading indicator, not a false-empty state, while the request is in flight', async () => {
    let resolveFn
    teacherApi.notifications.mockReturnValue(new Promise((resolve) => { resolveFn = resolve }))
    renderPage()

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('No notifications')).not.toBeInTheDocument()

    resolveFn({ items: { status: 'fulfilled', value: [] }, count: { status: 'fulfilled', value: { unreadCount: 0 } } })
    expect(await screen.findByText('No notifications')).toBeInTheDocument()
  })
})
