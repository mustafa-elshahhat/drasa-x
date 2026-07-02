import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentOfficeHoursPage from './StudentOfficeHoursPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for §2.3 / P1-5 / P2-12: cancel-booking API existed but was
// never wired to any button (studentApi.js:158).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { officeHours: vi.fn(), bookOfficeHour: vi.fn(), cancelBooking: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/office-hours']}>
          <StudentOfficeHoursPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('StudentOfficeHoursPage cancel booking', () => {
  it('shows a Cancel booking action and calls cancelBooking with the booking id (not the session id)', async () => {
    studentApi.officeHours.mockResolvedValue({
      available: { status: 'fulfilled', value: [] },
      mine: { status: 'fulfilled', value: [{ id: 'session-1', myBookingId: 'booking-1', teacherName: 'Mr. Ali', startsAt: '2030-01-01T10:00:00Z' }] },
    })
    studentApi.cancelBooking.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    const cancelBtn = await screen.findByRole('button', { name: 'Cancel booking' })
    await user.click(cancelBtn)

    await waitFor(() => expect(studentApi.cancelBooking).toHaveBeenCalledWith('booking-1'))
  })

  it('does not show a Cancel booking action when myBookingId is absent', async () => {
    studentApi.officeHours.mockResolvedValue({
      available: { status: 'fulfilled', value: [] },
      mine: { status: 'fulfilled', value: [{ id: 'session-1', teacherName: 'Mr. Ali', startsAt: '2030-01-01T10:00:00Z' }] },
    })
    renderPage()

    await screen.findByText('Mr. Ali')
    expect(screen.queryByRole('button', { name: 'Cancel booking' })).not.toBeInTheDocument()
  })
})
