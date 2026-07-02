import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentQuizzesPage from './StudentQuizzesPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for the P3-8 repo-wide sweep: opening a quiz detail page
// (direct link / refresh) before the shared assigned-quizzes query resolved
// rendered a dash-placeholder title with a live "Start quiz" button and, on a
// genuine list-fetch failure, no error UI at all — the user was stuck with no
// indication anything was wrong and no retry affordance.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { assignedQuizzes: vi.fn(), previousAttempts: vi.fn(), startQuiz: vi.fn() },
}))

function renderDetail(path = '/app/student/quizzes/quiz-1') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/student/quizzes/:quizId" element={<StudentQuizzesPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentQuizzesPage detail loading state', () => {
  it('shows a loading indicator instead of a live "Start quiz" button while the quiz list is still in flight', async () => {
    let resolveList
    studentApi.assignedQuizzes.mockReturnValue(new Promise((r) => { resolveList = r }))
    studentApi.previousAttempts.mockResolvedValue([])
    renderDetail()

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /start attempt/i })).not.toBeInTheDocument()

    resolveList([{ id: 'quiz-1', title: 'Algebra basics', questionCount: 10, timeLimitMinutes: 20 }])
    expect(await screen.findByRole('heading', { name: 'Algebra basics' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /start attempt/i })).toBeInTheDocument()
  })

  it('shows a real error state with retry instead of a stuck dash-placeholder page when the quiz list fails to load', async () => {
    const { ApiError } = await import('../../../lib/api/problemDetails')
    studentApi.assignedQuizzes.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden' }))
    studentApi.previousAttempts.mockResolvedValue([])
    renderDetail()

    expect(await screen.findByText('You do not have permission to perform this action.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /start attempt/i })).not.toBeInTheDocument()
  })
})
