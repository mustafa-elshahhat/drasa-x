import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, within, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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
        <MemoryRouter initialEntries={[`/app/school/${props.role.toLowerCase()}s`]}>
          <SchoolUsersPage {...props} />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.grades.mockResolvedValue([])
})

describe('SchoolUsersPage row actions', () => {
  it('links each row to its role-scoped detail route', async () => {
    schoolApi.users.mockResolvedValue([{ id: 'student-1', fullName: 'Sara Ali', loginCode: 'sara01', role: 'Student' }])
    renderUsers({ role: 'Student' })

    const link = await screen.findByRole('link', { name: 'Open' })
    expect(link).toHaveAttribute('href', '/app/school/students/student-1')
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

// The generic School Admin "Users" page (role dropdown + inline create form) was
// removed. Each role page (Students/Teachers/Parents) now shows its own "Add {role}"
// primary action, which opens the same create-account modal with the role fixed —
// no role selector, no way to submit a different role than the page it was opened from.
describe('SchoolUsersPage role-specific account creation', () => {
  it.each([
    ['Student', 'Add Student'],
    ['Teacher', 'Add Teacher'],
    ['Parent', 'Add Parent'],
  ])('shows "%s" and opens the create-account modal with the role fixed to %s', async (role, label) => {
    schoolApi.users.mockResolvedValue([])
    const user = userEvent.setup()
    renderUsers({ role, canCreate: true })

    const trigger = await screen.findByRole('button', { name: label })
    await user.click(trigger)

    const dialog = await screen.findByRole('dialog', { name: label })
    expect(within(dialog).queryByLabelText('Role', { exact: false })).not.toBeInTheDocument()
    expect(within(dialog).getByLabelText('Name')).toBeInTheDocument()
  })

  it('does not render a role dropdown or any password/login-code input inside the create modal', async () => {
    schoolApi.users.mockResolvedValue([])
    const user = userEvent.setup()
    renderUsers({ role: 'Teacher', canCreate: true })

    await user.click(await screen.findByRole('button', { name: 'Add Teacher' }))
    const dialog = await screen.findByRole('dialog', { name: 'Add Teacher' })
    expect(within(dialog).queryByText('Role')).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText(/password/i)).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText('Login code')).not.toBeInTheDocument()
  })

  it('keeps the grade field for Student creation but not for Teacher/Parent', async () => {
    schoolApi.users.mockResolvedValue([])
    schoolApi.grades.mockResolvedValue([{ id: 'grade-1', name: 'Grade 7' }])
    const user = userEvent.setup()
    renderUsers({ role: 'Student', canCreate: true })

    await user.click(await screen.findByRole('button', { name: 'Add Student' }))
    const dialog = await screen.findByRole('dialog', { name: 'Add Student' })
    expect(within(dialog).getByLabelText('Grade')).toBeInTheDocument()
  })

  it('rejects a non-English full name and blocks submit', async () => {
    schoolApi.users.mockResolvedValue([])
    const user = userEvent.setup()
    renderUsers({ role: 'Parent', canCreate: true })

    await user.click(await screen.findByRole('button', { name: 'Add Parent' }))
    const dialog = await screen.findByRole('dialog', { name: 'Add Parent' })
    await user.type(within(dialog).getByLabelText('Name'), 'محمد أحمد')
    expect(await within(dialog).findByText('Full name must be written in English letters only.')).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: 'Add Parent' })).toBeDisabled()
    expect(schoolApi.createUser).not.toHaveBeenCalled()
  })

  it('creates a Teacher with a valid English name, submits the fixed role, and shows the generated credentials', async () => {
    schoolApi.users.mockResolvedValue([])
    schoolApi.createUser.mockResolvedValue({ userId: 'u1', loginCode: 'teacher.ali.hassan.4821', role: 'Teacher', temporaryPassword: 'Fresh#Pass9000' })
    const user = userEvent.setup()
    renderUsers({ role: 'Teacher', canCreate: true })

    await user.click(await screen.findByRole('button', { name: 'Add Teacher' }))
    const dialog = await screen.findByRole('dialog', { name: 'Add Teacher' })
    await user.type(within(dialog).getByLabelText('Name'), 'Ali Hassan')
    await user.click(within(dialog).getByRole('button', { name: 'Add Teacher' }))

    await waitFor(() => expect(schoolApi.createUser).toHaveBeenCalledWith({ fullName: 'Ali Hassan', role: 'Teacher', gradeId: null }))
    expect(await screen.findByText('teacher.ali.hassan.4821')).toBeInTheDocument()
    expect(screen.getByText('Fresh#Pass9000')).toBeInTheDocument()
  })
})
