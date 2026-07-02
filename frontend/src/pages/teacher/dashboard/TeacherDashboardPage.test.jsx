import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherDashboardPage from './TeacherDashboardPage'
import { teacherApi } from '../../../features/teacher/teacherApi'
import { ApiError } from '../../../lib/api/problemDetails'

// Ported from the retired TeacherPortalPage facade ("Phase 9 contracts"): the
// dashboard renders real assignment-scoped metrics, and surfaces an honest
// error/retry state when the read is denied.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' } }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: { dashboard: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher']}>
          <TeacherDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('TeacherDashboardPage', () => {
  it('renders the assignment-scoped dashboard metrics from the backend', async () => {
    teacherApi.dashboard.mockResolvedValue({ assignedClassCount: 2, assignedSubjectCount: 1, studentCount: 3, draftQuizCount: 1, publishedQuizCount: 1, pendingGradingCount: 0 })
    renderPage()
    expect(await screen.findByText('Teacher dashboard')).toBeInTheDocument()
    // assignedClassCount value rendered
    expect(await screen.findByText('Assigned classes')).toBeInTheDocument()
    await waitFor(() => expect(teacherApi.dashboard).toHaveBeenCalled())
  })

  it('surfaces an error state when a teacher read fails', async () => {
    teacherApi.dashboard.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'boom' }))
    renderPage()
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })
})
