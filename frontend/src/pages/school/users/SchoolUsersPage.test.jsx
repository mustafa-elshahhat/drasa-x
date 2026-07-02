import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolUsersPage from './SchoolUsersPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for §6 row 1 / P1-1: the user lists had no row action at all
// (list-only, "no detail, no enable/disable"). Each role-filtered list must now
// link into its own detail route.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { users: vi.fn(), grades: vi.fn(), createUser: vi.fn() },
}))

function renderUsers(props) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/students']}>
          <SchoolUsersPage {...props} />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SchoolUsersPage row actions', () => {
  it('links each row to its role-scoped detail route', async () => {
    schoolApi.users.mockResolvedValue([{ id: 'student-1', fullName: 'Sara Ali', loginCode: 'sara01', role: 'Student' }])
    renderUsers({ role: 'Student' })

    const link = await screen.findByRole('link', { name: 'Open' })
    expect(link).toHaveAttribute('href', '/app/school/students/student-1')
  })

  it('links the "all users" list to /app/school/users/:id', async () => {
    schoolApi.users.mockResolvedValue([{ id: 'teacher-1', fullName: 'Ali Hassan', loginCode: 'ali01', role: 'Teacher' }])
    renderUsers({ role: null, canCreate: true })

    const link = await screen.findByRole('link', { name: 'Open' })
    expect(link).toHaveAttribute('href', '/app/school/users/teacher-1')
  })
})

// Ported from the retired SchoolAdminPortalPage facade ("Phase 11 contracts"):
// role-filtered lists are scoped to the backend's role query, with an honest
// empty state when there are no accounts.
describe('SchoolUsersPage role-scoped listing', () => {
  it('lists the tenant students returned by the backend', async () => {
    schoolApi.users.mockResolvedValue([{ id: 's1', fullName: 'Tenant1 Student', loginCode: 'STU-T1', role: 'Student' }])
    renderUsers({ role: 'Student' })
    expect(await screen.findByText(/Tenant1 Student/)).toBeInTheDocument()
    await waitFor(() => expect(schoolApi.users).toHaveBeenCalledWith('Student', expect.anything()))
  })

  it('shows an empty state when there are no accounts', async () => {
    schoolApi.users.mockResolvedValue([])
    renderUsers({ role: 'Teacher' })
    expect(await screen.findByText('No accounts yet.')).toBeInTheDocument()
  })
})
