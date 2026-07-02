import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolClassDetailPage from './SchoolClassDetailPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for §4 "Class (school)" / P1-2 / P1-3 (enrollment management),
// D9: school-admin classes had no detail/roster/edit/enrollment surface at all.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    getClass: vi.fn(), updateClass: vi.fn(),
    enrollments: vi.fn(), enrollStudent: vi.fn(), withdrawEnrollment: vi.fn(),
    users: vi.fn(),
  },
}))

function renderDetail() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/classes/class-1']}>
          <Routes>
            <Route path="/app/school/classes/:classId" element={<SchoolClassDetailPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.getClass.mockResolvedValue({ id: 'class-1', name: '7A', code: '7A-2026', capacity: 30, enrolledCount: 1 })
  schoolApi.users.mockResolvedValue([
    { id: 'student-1', fullName: 'Sara Ali' },
    { id: 'student-2', fullName: 'Omar Khan' },
  ])
})

describe('SchoolClassDetailPage', () => {
  it('renders the class and its roster with a withdraw action for active enrollments', async () => {
    schoolApi.enrollments.mockResolvedValue([{ id: 'enr-1', studentId: 'student-1', status: 0 }])
    renderDetail()

    expect(await screen.findByRole('heading', { name: '7A' })).toBeInTheDocument()
    expect(await screen.findByText('Sara Ali')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Withdraw' })).toBeInTheDocument()
  })

  it('only offers not-yet-enrolled students in the enroll picker', async () => {
    schoolApi.enrollments.mockResolvedValue([{ id: 'enr-1', studentId: 'student-1', status: 0 }])
    renderDetail()

    await screen.findByText('Sara Ali')
    const select = screen.getByLabelText('Student')
    const optionLabels = Array.from(select.querySelectorAll('option')).map((o) => o.textContent)
    expect(optionLabels).toContain('Omar Khan')
    expect(optionLabels).not.toContain('Sara Ali')
  })

  it('enrolls the selected student and refreshes the roster', async () => {
    schoolApi.enrollments.mockResolvedValue([])
    schoolApi.enrollStudent.mockResolvedValue({ id: 'enr-2', studentId: 'student-2' })
    const user = userEvent.setup()
    renderDetail()

    await screen.findByRole('heading', { name: '7A' })
    await user.selectOptions(screen.getByLabelText('Student'), 'student-2')
    await user.click(screen.getByRole('button', { name: 'Enroll' }))

    await waitFor(() => expect(schoolApi.enrollStudent).toHaveBeenCalledWith({ studentId: 'student-2', schoolClassId: 'class-1' }))
  })

  it('withdraws an active enrollment', async () => {
    schoolApi.enrollments.mockResolvedValue([{ id: 'enr-1', studentId: 'student-1', status: 0 }])
    schoolApi.withdrawEnrollment.mockResolvedValue({})
    const user = userEvent.setup()
    renderDetail()

    await user.click(await screen.findByRole('button', { name: 'Withdraw' }))
    await waitFor(() => expect(schoolApi.withdrawEnrollment).toHaveBeenCalledWith('enr-1'))
  })

  it('edits class name/code/capacity', async () => {
    schoolApi.enrollments.mockResolvedValue([])
    schoolApi.updateClass.mockResolvedValue({ id: 'class-1', name: '7A - Renamed', code: '7A-2026', capacity: 32 })
    const user = userEvent.setup()
    renderDetail()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const nameInput = screen.getByLabelText('Name')
    await user.clear(nameInput)
    await user.type(nameInput, '7A - Renamed')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(schoolApi.updateClass).toHaveBeenCalledWith('class-1', expect.objectContaining({ name: '7A - Renamed' })))
  })
})
