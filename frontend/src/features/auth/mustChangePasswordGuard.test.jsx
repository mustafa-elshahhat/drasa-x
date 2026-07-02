import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import { ToastProvider } from '../../components/feedback/ToastProvider'
import { AuthProvider } from './AuthContext'
import { ProtectedRoute, RequirePasswordChange } from './guards'

// A forced-first-login password change (mustChangePassword: true) must block every /app route
// (including the shell/nav) and redirect to /change-password instead; once the flag is false,
// /change-password itself sends the user back home rather than rendering the forced form.

vi.mock('./authApi', () => ({
  restore: vi.fn(),
  login: vi.fn(),
  logout: vi.fn(),
  changePassword: vi.fn(),
  toSession: (m) => ({
    user: { id: m.id, userName: m.userName, fullName: m.fullName || m.userName },
    role: m.role,
    token: m.token,
    expiresOn: m.expiresOn,
    mustChangePassword: Boolean(m.mustChangePassword),
  }),
}))
import * as authApi from './authApi'

function sessionWith(mustChangePassword) {
  return { id: 's1', userName: 'stud', fullName: 'Stud One', role: 'Student', token: 't', expiresOn: null, mustChangePassword }
}

function TestApp({ entries }) {
  const client = createQueryClient()
  return (
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ToastProvider>
          <MemoryRouter initialEntries={entries}>
            <AuthProvider>
              <Routes>
                <Route path="/login" element={<div>LOGIN PAGE</div>} />
                <Route
                  path="/change-password"
                  element={
                    <RequirePasswordChange>
                      <div>FORCED CHANGE PASSWORD PAGE</div>
                    </RequirePasswordChange>
                  }
                />
                <Route
                  path="/app"
                  element={
                    <ProtectedRoute>
                      <div>APP SHELL</div>
                    </ProtectedRoute>
                  }
                />
                <Route path="/app/student" element={<div>STUDENT HOME</div>} />
              </Routes>
            </AuthProvider>
          </MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>
  )
}

describe('mustChangePassword guard behaviour', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('redirects an authenticated mustChangePassword session away from /app to /change-password', async () => {
    authApi.restore.mockResolvedValue(sessionWith(true))
    render(<TestApp entries={['/app']} />)
    expect(await screen.findByText('FORCED CHANGE PASSWORD PAGE')).toBeInTheDocument()
    expect(screen.queryByText('APP SHELL')).not.toBeInTheDocument()
  })

  it('renders the app shell normally once mustChangePassword is false', async () => {
    authApi.restore.mockResolvedValue(sessionWith(false))
    render(<TestApp entries={['/app']} />)
    expect(await screen.findByText('APP SHELL')).toBeInTheDocument()
  })

  it('redirects away from /change-password when there is nothing to force', async () => {
    authApi.restore.mockResolvedValue(sessionWith(false))
    render(<TestApp entries={['/change-password']} />)
    // Nothing to force -> sent to the role's home, not the forced page.
    expect(await screen.findByText('STUDENT HOME')).toBeInTheDocument()
    expect(screen.queryByText('FORCED CHANGE PASSWORD PAGE')).not.toBeInTheDocument()
  })

  it('sends an anonymous visitor from /change-password to /login', async () => {
    authApi.restore.mockResolvedValue(null)
    render(<TestApp entries={['/change-password']} />)
    expect(await screen.findByText('LOGIN PAGE')).toBeInTheDocument()
  })
})
