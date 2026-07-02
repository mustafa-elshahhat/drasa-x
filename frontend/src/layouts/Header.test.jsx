import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../i18n'
import { createQueryClient } from '../lib/query/queryClient'
import { Header } from './Header'

// Regression test for F-07 / P3-2: the student header search input had no
// onChange/submit handler — typing did nothing. Removed rather than faked.

vi.mock('../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'user-1', fullName: 'Sara' } }),
}))

vi.mock('../features/notifications/notificationsApi', () => ({
  notificationsApi: { unreadCount: vi.fn().mockResolvedValue(0) },
}))

function renderHeader(role) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter>
          <Header user={{ fullName: 'Sara', userName: 'sara01' }} role={role} onToggleNav={() => {}} onSignOut={() => {}} />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('Header', () => {
  it('renders no dead search input for a Student', () => {
    renderHeader('Student')
    expect(screen.queryByPlaceholderText(/search/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('searchbox')).not.toBeInTheDocument()
  })

  it('renders the account menu regardless of role', () => {
    renderHeader('Teacher')
    expect(screen.getByRole('button', { name: 'Account menu' })).toBeInTheDocument()
  })

  // SchoolAdmin Teacher-portal removal: the header account menu must never send
  // SchoolAdmin to a /app/teacher/* page — only Teacher gets that settings link.
  it('sends a Teacher to /app/teacher/settings', async () => {
    const user = userEvent.setup()
    renderHeader('Teacher')
    await user.click(screen.getByRole('button', { name: 'Account menu' }))
    expect(screen.getByRole('menuitem', { name: /settings/i })).toHaveAttribute('href', '/app/teacher/settings')
  })

  it('sends a SchoolAdmin to /app/school/settings, never /app/teacher/settings', async () => {
    const user = userEvent.setup()
    renderHeader('SchoolAdmin')
    await user.click(screen.getByRole('button', { name: 'Account menu' }))
    const settingsLink = screen.getByRole('menuitem', { name: /settings/i })
    expect(settingsLink).toHaveAttribute('href', '/app/school/settings')
    expect(settingsLink).not.toHaveAttribute('href', '/app/teacher/settings')
  })
})
