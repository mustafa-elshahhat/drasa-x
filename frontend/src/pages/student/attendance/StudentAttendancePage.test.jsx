import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentAttendancePage from './StudentAttendancePage'
import { studentApi } from '../../../features/student/studentApi'

// Ported from the retired StudentPortalPage facade ("Phase 8 contracts"): attendance
// summary + records must render from real backend data, alongside the explicit
// "no CV / facial recognition" disclosure (no such claims are made by this page).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { attendance: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/attendance']}>
          <StudentAttendancePage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentAttendancePage', () => {
  it('renders persisted attendance records and summary without CV claims', async () => {
    studentApi.attendance.mockResolvedValue({
      summary: { total: 2, present: 1, absent: 1, late: 0, excused: 0, attendancePercentage: 50 },
      records: [{ id: 'a1', attendanceDate: '2031-01-05T00:00:00Z', status: 'Present', source: 'Manual', sessionKey: 'day' }],
    })

    renderPage()

    expect(await screen.findByText('Attendance summary')).toBeInTheDocument()
    expect(screen.getByText('50%')).toBeInTheDocument()
    expect(screen.getByLabelText('Attendance status: Present')).toBeInTheDocument()
    expect(screen.getByText(/facial-recognition attendance remain later-phase work/i)).toBeInTheDocument()
  })
})
