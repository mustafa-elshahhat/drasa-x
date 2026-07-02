import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentQuizResultPage from './StudentQuizResultPage'
import { studentApi } from '../../../features/student/studentApi'

// Ported from the retired StudentPortalPage facade ("Phase 8 contracts"): the quiz
// result page must render the REAL backend score/correctness (never fabricated
// fallback values), and show an honest pending state before grading completes.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { attemptResult: vi.fn() },
}))

function renderAt(attemptId) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/app/student/quiz-attempts/${attemptId}/result`]}>
          <Routes>
            <Route path="/app/student/quiz-attempts/:attemptId/result" element={<StudentQuizResultPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentQuizResultPage', () => {
  it('renders the REAL backend score and correctness (no fabricated values)', async () => {
    studentApi.attemptResult.mockResolvedValue({
      id: 'att-9', quizId: 'q-9', quizTitle: 'Algebra Quiz', status: 'Graded',
      achievedScore: 1, totalScore: 2, percentage: 50, questionCount: 2,
      answers: [
        { questionId: 'q1', questionText: '2+2?', isCorrect: true, pointsPossible: 1, pointsEarned: 1 },
        { questionId: 'q2', questionText: '3+3?', isCorrect: false, pointsPossible: 1, pointsEarned: 0, correctAnswer: '6' },
      ],
    })
    renderAt('att-9')
    expect(await screen.findByText('Algebra Quiz')).toBeInTheDocument()
    expect(screen.getByText('50%')).toBeInTheDocument()
    expect(screen.getByText('1/2')).toBeInTheDocument()
    // The old fabricated fallbacks must never appear.
    expect(screen.queryByText('88%')).not.toBeInTheDocument()
    expect(screen.queryByText(/Calculus Midterm Quiz/)).not.toBeInTheDocument()
  })

  it('shows a pending state (no fake score) when the attempt is not yet graded', async () => {
    studentApi.attemptResult.mockResolvedValue({ id: 'att-7', quizId: 'q-7', quizTitle: 'Essay Quiz', status: 'Submitted', answers: [] })
    renderAt('att-7')
    expect(await screen.findByText('Essay Quiz')).toBeInTheDocument()
    expect(screen.queryByText('88%')).not.toBeInTheDocument()
    expect(screen.queryByText('50%')).not.toBeInTheDocument()
  })
})
