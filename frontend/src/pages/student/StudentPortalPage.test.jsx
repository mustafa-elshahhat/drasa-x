import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import StudentPortalPage from './StudentPortalPage'
import { studentApi } from '../../features/student/studentApi'

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Student One' } }),
}))

vi.mock('../../features/student/studentApi', () => ({
  studentApi: {
    subjects: vi.fn(),
    subject: vi.fn(),
    units: vi.fn(),
    lessons: vi.fn(),
    attendance: vi.fn(),
    materials: vi.fn(),
    progress: vi.fn(),
    lessonDetail: vi.fn(),
    completeLesson: vi.fn(),
    competition: vi.fn(),
    leaderboard: vi.fn(),
    enterCompetition: vi.fn(),
    competitionSubmission: vi.fn(),
    submitCompetition: vi.fn(),
    attemptResult: vi.fn(),
  },
}))

function renderStudent(view, path = '/app/student') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/student" element={<StudentPortalPage view={view} />} />
            <Route path="/app/student/subjects" element={<StudentPortalPage view={view} />} />
            <Route path="/app/student/units/:unitId" element={<StudentPortalPage view={view} />} />
            <Route path="/app/student/lessons/:lessonId" element={<StudentPortalPage view={view} />} />
            <Route path="/app/student/competitions/:competitionId" element={<StudentPortalPage view={view} />} />
            <Route path="/app/student/quiz-attempts/:attemptId/result" element={<StudentPortalPage view={view} />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentPortalPage Phase 8 contracts', () => {
  it('renders persisted attendance records and summary without CV claims', async () => {
    studentApi.attendance.mockResolvedValue({
      summary: { total: 2, present: 1, absent: 1, late: 0, excused: 0, attendancePercentage: 50 },
      records: [{ id: 'a1', attendanceDate: '2031-01-05T00:00:00Z', status: 'Present', source: 'Manual', sessionKey: 'day' }],
    })

    renderStudent('attendance')

    expect(await screen.findByText('Attendance summary')).toBeInTheDocument()
    expect(screen.getByText('50%')).toBeInTheDocument()
    expect(screen.getByLabelText('Attendance status: Present')).toBeInTheDocument()
    expect(screen.getByText(/facial-recognition attendance remain later-phase work/i)).toBeInTheDocument()
  })

  it('calls the explicit lesson completion command and waits for server success', async () => {
    studentApi.materials.mockResolvedValue([])
    studentApi.progress.mockResolvedValue({ lessons: { status: 'fulfilled', value: { data: [] } } })
    studentApi.lessonDetail.mockResolvedValue({ lessonId: 'lesson-1', title: 'Intro lesson', content: 'Lesson body', unitId: 'u1', unitTitle: 'Unit 1', subjectId: 's1', subjectName: 'Science', isCompleted: false, completionPercentage: 0 })
    studentApi.completeLesson.mockResolvedValue({ id: 'p1', lessonId: 'lesson-1', isCompleted: true, completionPercentage: 100 })
    const user = userEvent.setup()

    renderStudent('lessons', '/app/student/lessons/lesson-1')

    await user.click(await screen.findByRole('button', { name: /mark lesson complete/i }))
    await waitFor(() => expect(studentApi.completeLesson).toHaveBeenCalledWith('lesson-1'))
    expect(await screen.findByText('Progress saved')).toBeInTheDocument()
  })

  it('Phase 14 closure — submits durable competition work through the backend submissions endpoint', async () => {
    // Backend now returns explicit student flags; an entered student in an open competition can submit.
    studentApi.competition.mockResolvedValue({ id: 'comp-1', title: 'Science Fair', hasEntered: true, canSubmit: true, canEnter: false, canViewLeaderboard: false })
    studentApi.leaderboard.mockResolvedValue([])
    studentApi.competitionSubmission.mockResolvedValue(null) // not submitted yet
    studentApi.submitCompetition.mockResolvedValue({ id: 's1', content: 'My project' })
    const user = userEvent.setup()

    renderStudent('competitions', '/app/student/competitions/comp-1')

    expect(await screen.findByText('Your submission')).toBeInTheDocument()
    expect(await screen.findByText('You have not submitted yet.')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Submission'), 'My project')
    await user.click(screen.getByRole('button', { name: /submit work/i }))
    await waitFor(() => expect(studentApi.submitCompetition).toHaveBeenCalledWith('comp-1', 'My project'))
    expect(await screen.findByText('Submission saved')).toBeInTheDocument()
  })

  it('quiz result renders the REAL backend score and correctness (no fabricated values)', async () => {
    studentApi.attemptResult.mockResolvedValue({
      id: 'att-9', quizId: 'q-9', quizTitle: 'Algebra Quiz', status: 'Graded',
      achievedScore: 1, totalScore: 2, percentage: 50, questionCount: 2,
      answers: [
        { questionId: 'q1', questionText: '2+2?', isCorrect: true, pointsPossible: 1, pointsEarned: 1 },
        { questionId: 'q2', questionText: '3+3?', isCorrect: false, pointsPossible: 1, pointsEarned: 0, correctAnswer: '6' },
      ],
    })
    renderStudent('quiz-result', '/app/student/quiz-attempts/att-9/result')
    expect(await screen.findByText('Algebra Quiz')).toBeInTheDocument()
    expect(screen.getByText('50%')).toBeInTheDocument()
    expect(screen.getByText('1/2')).toBeInTheDocument()
    // The old fabricated fallbacks must never appear.
    expect(screen.queryByText('88%')).not.toBeInTheDocument()
    expect(screen.queryByText(/Calculus Midterm Quiz/)).not.toBeInTheDocument()
  })

  it('quiz result shows a pending state (no fake score) when the attempt is not yet graded', async () => {
    studentApi.attemptResult.mockResolvedValue({ id: 'att-7', quizId: 'q-7', quizTitle: 'Essay Quiz', status: 'Submitted', answers: [] })
    renderStudent('quiz-result', '/app/student/quiz-attempts/att-7/result')
    expect(await screen.findByText('Essay Quiz')).toBeInTheDocument()
    expect(screen.queryByText('88%')).not.toBeInTheDocument()
    expect(screen.queryByText('50%')).not.toBeInTheDocument()
  })

  it('renders subjects page with search and filters, and filters client-side', async () => {
    studentApi.subjects.mockResolvedValue([
      { id: 'math', name: 'Mathematics', teacher: 'Mr. Osama Refat', units: 6, progress: 62 },
      { id: 'phys', name: 'Physics', teacher: 'Dr. Ahmed Hassan', units: 5, progress: 48 },
    ])

    renderStudent('subjects', '/app/student/subjects')

    // Expect subjects to render
    expect(await screen.findByText('Mathematics')).toBeInTheDocument()
    expect(screen.getByText('Physics')).toBeInTheDocument()

    // Test search filter
    const searchInput = screen.getByPlaceholderText(/search/i)
    await userEvent.type(searchInput, 'Math')
    expect(screen.getByText('Mathematics')).toBeInTheDocument()
    expect(screen.queryByText('Physics')).not.toBeInTheDocument()
  })

  it('renders units page with breadcrumbs and redesigned lesson cards list', async () => {
    studentApi.subjects.mockResolvedValue([
      { id: 'math', name: 'Mathematics', teacher: 'Mr. Osama Refat', units: 6, progress: 62 },
    ])
    studentApi.units.mockResolvedValue([
      { id: 'u3', subject: 'math', name: 'Integration', nameAr: 'التكامل', lessonsCount: 4, completedLessonsCount: 1, completionPercentage: 25 },
    ])
    studentApi.lessons.mockResolvedValue([
      { id: 'lesson-1', title: 'Intro to Integration', content: 'Intro text', unitId: 'u3' },
      { id: 'lesson-2', title: 'Definite Integrals', content: 'Definite text', unitId: 'u3' },
    ])
    studentApi.progress.mockResolvedValue({
      lessons: {
        status: 'fulfilled',
        value: {
          data: [
            { lessonId: 'lesson-1', isCompleted: true, completionPercentage: 100 },
            { lessonId: 'lesson-2', isCompleted: false, completionPercentage: 40 },
          ],
        },
      },
    })

    renderStudent('units', '/app/student/units/u3')

    // Breadcrumbs
    expect(await screen.findByText('Mathematics')).toBeInTheDocument()
    expect(screen.getAllByText('Integration').length).toBeGreaterThan(0)

    // Title and progress subtitle
    expect(screen.getByText('1/2 lessons completed')).toBeInTheDocument()

    // Lesson cards
    expect(screen.getByText('Intro to Integration')).toBeInTheDocument()
    expect(screen.getByText('Definite Integrals')).toBeInTheDocument()

    // Status chips/indicators
    expect(screen.getByText('Completed')).toBeInTheDocument()
    expect(screen.getByText('40%')).toBeInTheDocument()
  })
})
