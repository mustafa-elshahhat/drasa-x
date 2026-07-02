import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherQuizzesPage from './TeacherQuizzesPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Regression test for §8.3 "Quizzes" / P2-5: teacherApi.quizAnalytics/archiveQuiz/
// deleteQuestion were dead client methods, and quiz creation was AI-draft-only
// (no manual create UI).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: 'Teacher' }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    quizzes: vi.fn(),
    quiz: vi.fn(),
    quizSubmissions: vi.fn(),
    quizAssignments: vi.fn(),
    quizAnalytics: vi.fn(),
    classes: vi.fn(),
    subjects: vi.fn(),
    createQuiz: vi.fn(),
    archiveQuiz: vi.fn(),
    deleteQuestion: vi.fn(),
    publishQuiz: vi.fn(),
  },
}))

function renderAt(path) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/teacher/quizzes" element={<TeacherQuizzesPage />} />
            <Route path="/app/teacher/quizzes/:quizId" element={<TeacherQuizzesPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('TeacherQuizzesPage — manual create', () => {
  it('creates a quiz manually (not via the AI generator) and navigates to its detail page', async () => {
    teacherApi.quizzes.mockResolvedValue([])
    teacherApi.subjects.mockResolvedValue([{ id: 'subj-1', name: 'Algebra' }])
    teacherApi.createQuiz.mockResolvedValue({ id: 'quiz-new' })
    const user = userEvent.setup()
    renderAt('/app/teacher/quizzes')

    await screen.findByText('Algebra')
    await user.type(screen.getByLabelText('Title', { exact: false }), 'Chapter 3 Quiz')
    await user.selectOptions(screen.getByLabelText('My subjects', { exact: false }), 'subj-1')
    await user.click(screen.getByRole('button', { name: 'Create a quiz manually' }))

    await waitFor(() => expect(teacherApi.createQuiz).toHaveBeenCalledWith(expect.objectContaining({ title: 'Chapter 3 Quiz', subjectId: 'subj-1' })))
  })
})

describe('TeacherQuizzesPage — detail actions', () => {
  beforeEach(() => {
    teacherApi.quiz.mockResolvedValue({ id: 'quiz-1', title: 'Fractions', status: 0, questions: [{ id: 'q-1', text: 'What is 1/2 + 1/2?', type: 2, points: 5, options: [{ text: 'Yes', isCorrect: true }] }] })
    teacherApi.quizSubmissions.mockResolvedValue([])
  })

  it('archives a quiz', async () => {
    teacherApi.archiveQuiz.mockResolvedValue({})
    const user = userEvent.setup()
    renderAt('/app/teacher/quizzes/quiz-1')

    await user.click(await screen.findByRole('button', { name: 'Archive' }))
    await waitFor(() => expect(teacherApi.archiveQuiz).toHaveBeenCalledWith('quiz-1'))
  })

  it('deletes a question from a draft quiz', async () => {
    teacherApi.deleteQuestion.mockResolvedValue({})
    const user = userEvent.setup()
    renderAt('/app/teacher/quizzes/quiz-1')

    await user.click(await screen.findByRole('button', { name: 'Delete question' }))
    await waitFor(() => expect(teacherApi.deleteQuestion).toHaveBeenCalledWith('quiz-1', 'q-1'))
  })

  it('shows real analytics data once the quiz is published', async () => {
    teacherApi.quiz.mockResolvedValue({ id: 'quiz-2', title: 'Fractions', status: 4, questions: [] })
    teacherApi.quizAnalytics.mockResolvedValue({ quizId: 'quiz-2', totalSubmissions: 12, averageScorePercentage: 76.5, questions: [{ questionId: 'q-1', text: 'Q1', answered: 12, correctCount: 9, correctRate: 0.75 }] })
    renderAt('/app/teacher/quizzes/quiz-2')

    expect(await screen.findByText('12')).toBeInTheDocument()
    expect(await screen.findByText('76.5%')).toBeInTheDocument()
    expect(await screen.findByText(/75% correct/)).toBeInTheDocument()
  })
})

// Ported from the retired TeacherPortalPage facade ("Phase 9 contracts"): a draft
// quiz shows draft messaging and exposes Publish; publishing calls the real
// backend command.
describe('TeacherQuizzesPage — publish lifecycle', () => {
  it('a draft quiz shows draft messaging and exposes Publish; published quizzes do not', async () => {
    teacherApi.quiz.mockResolvedValue({ id: 'Q1', title: 'Draft Quiz', status: 'AiGenerated', questions: [{ id: 'q1', text: 'What is 2+2?' }] })
    teacherApi.quizSubmissions.mockResolvedValue([])
    renderAt('/app/teacher/quizzes/Q1')
    expect(await screen.findByText(/This is a draft/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /publish quiz/i })).toBeInTheDocument()
  })

  it('publishes a quiz through the backend command', async () => {
    teacherApi.quiz.mockResolvedValue({ id: 'Q1', title: 'Draft Quiz', status: 'AiGenerated', questions: [] })
    teacherApi.quizSubmissions.mockResolvedValue([])
    teacherApi.publishQuiz.mockResolvedValue({ id: 'Q1', status: 'Published' })
    const user = userEvent.setup()
    renderAt('/app/teacher/quizzes/Q1')
    await user.click(await screen.findByRole('button', { name: /publish quiz/i }))
    await waitFor(() => expect(teacherApi.publishQuiz).toHaveBeenCalledWith('Q1'))
  })
})
