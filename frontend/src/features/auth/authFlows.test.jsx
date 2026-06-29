import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import { ToastProvider } from '../../components/feedback/ToastProvider'
import { AuthProvider, useAuth } from './AuthContext'
import { ProtectedRoute, RoleGuard } from './guards'
import LoginPage from '../../pages/auth/LoginPage'

// authApi is mocked so the flows run against the contract shape without a server.
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
  }),
}))
import * as authApi from './authApi'

function studentSession() {
  return { user: { id: 's1', userName: 'stud', fullName: 'Stud One' }, role: 'Student', token: 't', expiresOn: null }
}

function LogoutButton() {
  const { logout } = useAuth()
  return (
    <button onClick={() => logout()}>do-logout</button>
  )
}

function TestApp({ entries, queryClient }) {
  const client = queryClient || createQueryClient()
  return (
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ToastProvider>
          <MemoryRouter initialEntries={entries}>
            <AuthProvider onSessionEnd={() => client.clear()}>
              <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/forbidden" element={<div>FORBIDDEN PAGE</div>} />
                <Route path="/suspended" element={<div>SUSPENDED PAGE</div>} />
                <Route
                  path="/app"
                  element={
                    <ProtectedRoute>
                      <LogoutButton />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/app/student"
                  element={
                    <RoleGuard roles={['Student']}>
                      <div>STUDENT HOME</div>
                    </RoleGuard>
                  }
                />
                <Route
                  path="/app/teacher"
                  element={
                    <RoleGuard roles={['Teacher']}>
                      <div>TEACHER HOME</div>
                    </RoleGuard>
                  }
                />
              </Routes>
            </AuthProvider>
          </MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>
  )
}

describe('auth + guard integration flows', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('redirects an anonymous user from a protected route to login', async () => {
    authApi.restore.mockResolvedValue(null)
    render(<TestApp entries={['/app']} />)
    expect(await screen.findByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('restores a session and renders the role route', async () => {
    authApi.restore.mockResolvedValue(studentSession())
    render(<TestApp entries={['/app/student']} />)
    expect(await screen.findByText('STUDENT HOME')).toBeInTheDocument()
  })

  it('blocks a student from a teacher-only route (cross-role rejection)', async () => {
    authApi.restore.mockResolvedValue(studentSession())
    render(<TestApp entries={['/app/teacher']} />)
    expect(await screen.findByText('FORBIDDEN PAGE')).toBeInTheDocument()
  })

  it('logs in via the backend contract and lands on the student home', async () => {
    authApi.restore.mockResolvedValue(null)
    authApi.login.mockResolvedValue({ ok: true, session: studentSession() })
    const user = userEvent.setup()
    render(<TestApp entries={['/login']} />)
    await screen.findByRole('button', { name: /sign in/i })
    await user.type(screen.getByLabelText(/Login code/i), 'stud')
    await user.type(screen.getByLabelText(/^Password/), 'pw')
    await user.click(screen.getByRole('button', { name: /Sign in/i }))
    expect(await screen.findByText('STUDENT HOME')).toBeInTheDocument()
  })

  it('clears the query cache on logout', async () => {
    authApi.restore.mockResolvedValue(studentSession())
    authApi.logout.mockResolvedValue(undefined)
    const client = createQueryClient()
    client.setQueryData(['u', 's1', 'profile'], { secret: 'data' })
    const user = userEvent.setup()
    render(<TestApp entries={['/app']} queryClient={client} />)
    await screen.findByText('do-logout')
    expect(client.getQueryData(['u', 's1', 'profile'])).toBeTruthy()
    await user.click(screen.getByText('do-logout'))
    await waitFor(() => expect(client.getQueryData(['u', 's1', 'profile'])).toBeUndefined())
  })

  it('routes a suspended tenant to the suspended page on login', async () => {
    authApi.restore.mockResolvedValue(null)
    authApi.login.mockResolvedValue({ ok: false, suspended: true, error: { status: 403, errorCode: 'TENANT_SUSPENDED' } })
    const user = userEvent.setup()
    render(<TestApp entries={['/login']} />)
    await screen.findByRole('button', { name: /sign in/i })
    await user.type(screen.getByLabelText(/Login code/i), 'x')
    await user.type(screen.getByLabelText(/^Password/), 'y')
    await user.click(screen.getByRole('button', { name: /Sign in/i }))
    expect(await screen.findByText('SUSPENDED PAGE')).toBeInTheDocument()
  })
})
