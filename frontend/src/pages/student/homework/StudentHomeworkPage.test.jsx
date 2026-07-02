import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentHomeworkPage from './StudentHomeworkPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for the P3-8 repo-wide sweep: opening a homework detail page
// (direct link / refresh) before the shared homework-list query resolved
// rendered a live, fully-enabled submission form against an unknown item
// (blank title, no due date, no indication anything was already submitted)
// instead of a loading state.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { homework: vi.fn(), homeworkSubmission: vi.fn(), submitHomework: vi.fn() },
}))

function renderDetail(path = '/app/student/homework/hw-1') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/student/homework/:homeworkId" element={<StudentHomeworkPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentHomeworkPage detail loading state', () => {
  it('shows a loading indicator instead of a live submission form while the homework list is still in flight', async () => {
    let resolveList
    studentApi.homework.mockReturnValue(new Promise((r) => { resolveList = r }))
    studentApi.homeworkSubmission.mockResolvedValue(null)
    renderDetail()

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('Submit homework')).not.toBeInTheDocument()
    expect(screen.queryByText('Click to upload your file')).not.toBeInTheDocument()

    resolveList([{ id: 'hw-1', title: 'Fractions worksheet', dueDate: '2031-01-05', submissionStatus: 0 }])
    expect(await screen.findByRole('heading', { name: 'Fractions worksheet' })).toBeInTheDocument()
    expect(screen.getByText('Submit homework')).toBeInTheDocument()
  })

  it('shows a real error state instead of a blank/live form when the homework list fails to load', async () => {
    const { ApiError } = await import('../../../lib/api/problemDetails')
    studentApi.homework.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden' }))
    studentApi.homeworkSubmission.mockResolvedValue(null)
    renderDetail()

    expect(await screen.findByText('You do not have permission to perform this action.')).toBeInTheDocument()
    expect(screen.queryByText('Submit homework')).not.toBeInTheDocument()
  })
})
