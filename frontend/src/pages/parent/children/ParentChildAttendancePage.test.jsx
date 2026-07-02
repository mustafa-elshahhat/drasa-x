import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentChildAttendancePage from './ParentChildAttendancePage'
import { parentApi } from '../../../features/parent/parentApi'

// Ported from the retired ParentPortalPage facade ("Phase 10 contracts"): a
// linked child's attendance summary renders from authoritative backend records.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' } }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { childAttendance: vi.fn() },
}))

function renderAt(childId) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/app/parent/children/${childId}/attendance`]}>
          <Routes>
            <Route path="/app/parent/children/:childId/attendance" element={<ParentChildAttendancePage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('ParentChildAttendancePage', () => {
  it('renders the child attendance summary from authoritative records', async () => {
    parentApi.childAttendance.mockResolvedValue({
      summary: { total: 3, present: 1, absent: 1, late: 1, excused: 0, attendancePercentage: 66.67 },
      records: [{ id: 'A1', attendanceDate: '2031-01-05', status: 'Present', source: 'Manual' }],
    })
    renderAt('stu-guid-1')
    expect(await screen.findByText('Attendance summary')).toBeInTheDocument()
    expect(await screen.findByText('66.67')).toBeInTheDocument()
  })
})
