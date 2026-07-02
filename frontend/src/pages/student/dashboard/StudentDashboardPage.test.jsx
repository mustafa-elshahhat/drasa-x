import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentDashboardPage from './StudentDashboardPage'
import { studentApi } from '../../../features/student/studentApi'
import { ApiError } from '../../../lib/api/problemDetails'

// Regression test for §2.3 "StudentDashboardPage" / P3-6: no loading/error
// states (renders '—' on failure) and over-fetches homework/unread/office
// data it never renders.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: {
    subjects: vi.fn(), assignedQuizzes: vi.fn(), progress: vi.fn(), badges: vi.fn(), points: vi.fn(),
    homework: vi.fn(), officeHours: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student']}>
          <StudentDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentDashboardPage loading/error states', () => {
  it('shows a loading indicator before any content, then real content once loaded', async () => {
    let resolveSubjects
    studentApi.subjects.mockReturnValue(new Promise((r) => { resolveSubjects = r }))
    studentApi.assignedQuizzes.mockResolvedValue([])
    studentApi.progress.mockResolvedValue({})
    studentApi.badges.mockResolvedValue({})
    studentApi.points.mockResolvedValue({})
    renderPage()

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('Learning progress')).not.toBeInTheDocument()

    resolveSubjects([])
    expect(await screen.findByText('Learning progress')).toBeInTheDocument()
  })

  it('shows a real error state instead of silent dashes when a critical query fails', async () => {
    studentApi.subjects.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden' }))
    studentApi.assignedQuizzes.mockResolvedValue([])
    studentApi.progress.mockResolvedValue({})
    studentApi.badges.mockResolvedValue({})
    studentApi.points.mockResolvedValue({})
    renderPage()

    expect(await screen.findByText('You do not have permission to perform this action.')).toBeInTheDocument()
    expect(screen.queryByText('Learning progress')).not.toBeInTheDocument()
  })

  it('no longer fetches homework/office-hours data that this page never renders', async () => {
    studentApi.subjects.mockResolvedValue([])
    studentApi.assignedQuizzes.mockResolvedValue([])
    studentApi.progress.mockResolvedValue({})
    studentApi.badges.mockResolvedValue({})
    studentApi.points.mockResolvedValue({})
    renderPage()

    await screen.findByText('Learning progress')
    expect(studentApi.homework).not.toHaveBeenCalled()
    expect(studentApi.officeHours).not.toHaveBeenCalled()
  })
})
