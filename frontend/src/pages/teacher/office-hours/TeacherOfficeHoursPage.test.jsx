import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherOfficeHoursPage from './TeacherOfficeHoursPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Regression test for §6 row 7 / P1-5 / D8: office hours was a read-only dump
// (no create/edit/cancel UI, no attendance marking) and creation must stay
// hidden from SchoolAdmin since the backend rejects it (OfficeHourService.
// CreateAsync throws Forbidden unless IsTeacher).

let mockRole = 'Teacher'
vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: mockRole }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    officeHours: vi.fn(),
    createOfficeHour: vi.fn(),
    updateOfficeHour: vi.fn(),
    cancelOfficeHour: vi.fn(),
    officeHourBookings: vi.fn(),
    markAttendance: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher/office-hours']}>
          <TeacherOfficeHoursPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  mockRole = 'Teacher'
})

describe('TeacherOfficeHoursPage', () => {
  it('shows the create-session form for a Teacher', async () => {
    teacherApi.officeHours.mockResolvedValue([])
    renderPage()
    expect(await screen.findByRole('button', { name: 'Schedule a session' })).toBeInTheDocument()
  })

  it('hides the create-session form for SchoolAdmin (backend forbids it)', async () => {
    mockRole = 'SchoolAdmin'
    teacherApi.officeHours.mockResolvedValue([])
    renderPage()
    await screen.findByText('No office-hour sessions')
    expect(screen.queryByRole('button', { name: 'Schedule a session' })).not.toBeInTheDocument()
  })

  it('cancels a scheduled session', async () => {
    teacherApi.officeHours.mockResolvedValue([{ id: 'sess-1', title: 'Algebra help', startsAt: '2030-01-01T10:00:00Z', endsAt: '2030-01-01T11:00:00Z', capacity: 3, bookedCount: 0, status: 0 }])
    teacherApi.cancelOfficeHour.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Cancel session' }))
    await waitFor(() => expect(teacherApi.cancelOfficeHour).toHaveBeenCalledWith('sess-1'))
  })

  it('does not offer edit/cancel for an already-cancelled session', async () => {
    teacherApi.officeHours.mockResolvedValue([{ id: 'sess-2', title: 'Old session', startsAt: '2030-01-01T10:00:00Z', endsAt: '2030-01-01T11:00:00Z', capacity: 3, bookedCount: 0, status: 1 }])
    renderPage()
    await screen.findByText('Cancelled')
    expect(screen.queryByRole('button', { name: 'Cancel session' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
  })

  it('shows bookings and marks attendance', async () => {
    teacherApi.officeHours.mockResolvedValue([{ id: 'sess-3', title: 'Geometry', startsAt: '2030-01-01T10:00:00Z', endsAt: '2030-01-01T11:00:00Z', capacity: 3, bookedCount: 1, status: 0 }])
    teacherApi.officeHourBookings.mockResolvedValue([{ id: 'book-1', studentId: 'stud-1', status: 1 }])
    teacherApi.markAttendance.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'View bookings' }))
    await user.click(await screen.findByRole('button', { name: 'Mark attended' }))

    await waitFor(() => expect(teacherApi.markAttendance).toHaveBeenCalledWith('book-1', 3))
  })
})
