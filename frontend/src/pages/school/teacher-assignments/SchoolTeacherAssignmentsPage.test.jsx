import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolTeacherAssignmentsPage from './SchoolTeacherAssignmentsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: the class-assignment list had no Deactivate row action; the
// subject-assignment list intentionally has none (no deactivate endpoint exists for it).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    subjectAssignments: vi.fn(),
    createSubjectAssignment: vi.fn(),
    classAssignments: vi.fn(),
    createClassAssignment: vi.fn(),
    deactivateClassAssignment: vi.fn(),
    users: vi.fn(),
    subjects: vi.fn(),
    classes: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/teacher-assignments']}>
          <SchoolTeacherAssignmentsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.users.mockResolvedValue([])
  schoolApi.subjects.mockResolvedValue([])
  schoolApi.classes.mockResolvedValue([])
  schoolApi.subjectAssignments.mockResolvedValue([])
})

describe('SchoolTeacherAssignmentsPage row actions', () => {
  it('renders a Deactivate button only for active class assignments and calls the API on click', async () => {
    schoolApi.classAssignments.mockResolvedValue([
      { id: 'ca-1', teacherId: 't1', schoolClassId: 'c1', isActive: true },
      { id: 'ca-2', teacherId: 't2', schoolClassId: 'c2', isActive: false },
    ])
    schoolApi.deactivateClassAssignment.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    const buttons = await screen.findAllByRole('button', { name: 'Deactivate' })
    expect(buttons).toHaveLength(1)

    await user.click(buttons[0])
    await waitFor(() => expect(schoolApi.deactivateClassAssignment).toHaveBeenCalledWith('ca-1'))
  })

  it('does not render a Deactivate action for subject assignments', async () => {
    schoolApi.classAssignments.mockResolvedValue([])
    schoolApi.subjectAssignments.mockResolvedValue([{ id: 'sa-1', teacherId: 't1', subjectId: 'sub1' }])
    renderPage()

    await screen.findByText('Subject assignments')
    expect(screen.queryByRole('button', { name: 'Deactivate' })).not.toBeInTheDocument()
  })
})
