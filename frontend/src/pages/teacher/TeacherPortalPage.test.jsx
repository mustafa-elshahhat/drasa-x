import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import TeacherPortalPage from './TeacherPortalPage'
import { teacherApi } from '../../features/teacher/teacherApi'
import { ApiError } from '../../lib/api/problemDetails'

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' } }),
}))

vi.mock('../../features/teacher/teacherApi', () => ({
  teacherApi: {
    dashboard: vi.fn(),
    classes: vi.fn(),
    subjects: vi.fn(),
    classStudents: vi.fn(),
    myStudents: vi.fn(),
    subjectUnits: vi.fn(),
    unitLessons: vi.fn(),
    lessonMaterials: vi.fn(),
    quizzes: vi.fn(),
    quiz: vi.fn(),
    quizSubmissions: vi.fn(),
    quizAssignments: vi.fn(),
    quizAnalytics: vi.fn(),
    generateDraft: vi.fn(),
    addQuestion: vi.fn(),
    updateQuestion: vi.fn(),
    deleteQuestion: vi.fn(),
    publishQuiz: vi.fn(),
    archiveQuiz: vi.fn(),
    assignQuiz: vi.fn(),
    submission: vi.fn(),
    gradeSubmission: vi.fn(),
    feedbackSubmission: vi.fn(),
    studentProgress: vi.fn(),
    studentPoints: vi.fn(),
    studentPointsLedger: vi.fn(),
    awardPoints: vi.fn(),
    notifications: vi.fn(),
    markNotificationRead: vi.fn(),
    officeHours: vi.fn(),
  },
}))

function renderTeacher(view, path = '/app/teacher') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/teacher" element={<TeacherPortalPage view={view} />} />
            <Route path="/app/teacher/quizzes/:quizId" element={<TeacherPortalPage view={view} />} />
            <Route path="/app/teacher/submissions/:attemptId" element={<TeacherPortalPage view={view} />} />
            <Route path="/app/teacher/students/:studentId" element={<TeacherPortalPage view={view} />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('TeacherPortalPage Phase 9 contracts', () => {
  it('renders the assignment-scoped dashboard metrics from the backend', async () => {
    teacherApi.dashboard.mockResolvedValue({ assignedClassCount: 2, assignedSubjectCount: 1, studentCount: 3, draftQuizCount: 1, publishedQuizCount: 1, pendingGradingCount: 0 })
    renderTeacher('dashboard')
    expect(await screen.findByText('Teacher dashboard')).toBeInTheDocument()
    // assignedClassCount value rendered
    expect(await screen.findByText('Assigned classes')).toBeInTheDocument()
    await waitFor(() => expect(teacherApi.dashboard).toHaveBeenCalled())
  })

  it('lists only the teacher\'s assigned classes', async () => {
    teacherApi.classes.mockResolvedValue([{ classId: 'PH8-CLASS-T1', name: 'Phase 8 Class', studentCount: 2 }])
    renderTeacher('classes')
    expect(await screen.findByText(/Phase 8 Class/)).toBeInTheDocument()
  })

  it('shows an empty state when the teacher has no classes', async () => {
    teacherApi.classes.mockResolvedValue([])
    renderTeacher('classes')
    expect(await screen.findByText('No assigned classes')).toBeInTheDocument()
  })

  it('surfaces an error state when a teacher read fails', async () => {
    teacherApi.dashboard.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'boom' }))
    renderTeacher('dashboard')
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })

  it('AI quiz generation is presented as draft-only and posts a draft request', async () => {
    teacherApi.subjects.mockResolvedValue([{ subjectId: 'PH8-SUBJECT-T1', name: 'Phase 8 Mathematics' }])
    teacherApi.generateDraft.mockResolvedValue({ quizId: 'NEW-DRAFT', status: 'AiGenerated' })
    const user = userEvent.setup()
    renderTeacher('quiz-generate')
    expect(await screen.findByText('Draft-only generation')).toBeInTheDocument()
    await screen.findByText('Phase 8 Mathematics')
    await user.selectOptions(screen.getByLabelText('Subject'), 'PH8-SUBJECT-T1')
    await user.click(screen.getByRole('button', { name: /generate draft/i }))
    await waitFor(() => expect(teacherApi.generateDraft).toHaveBeenCalled())
    const arg = teacherApi.generateDraft.mock.calls[0][0]
    expect(arg.subjectId).toBe('PH8-SUBJECT-T1')
  })

  it('a draft quiz shows draft messaging and exposes Publish; published quizzes do not', async () => {
    teacherApi.quiz.mockResolvedValue({ id: 'Q1', title: 'Draft Quiz', status: 'AiGenerated', questions: [{ id: 'q1', text: 'What is 2+2?' }] })
    teacherApi.quizSubmissions.mockResolvedValue([])
    renderTeacher('quizzes', '/app/teacher/quizzes/Q1')
    expect(await screen.findByText(/This is a draft/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /publish quiz/i })).toBeInTheDocument()
  })

  it('publishes a quiz through the backend command', async () => {
    teacherApi.quiz.mockResolvedValue({ id: 'Q1', title: 'Draft Quiz', status: 'AiGenerated', questions: [] })
    teacherApi.quizSubmissions.mockResolvedValue([])
    teacherApi.publishQuiz.mockResolvedValue({ id: 'Q1', status: 'Published' })
    const user = userEvent.setup()
    renderTeacher('quizzes', '/app/teacher/quizzes/Q1')
    await user.click(await screen.findByRole('button', { name: /publish quiz/i }))
    await waitFor(() => expect(teacherApi.publishQuiz).toHaveBeenCalledWith('Q1'))
  })

  it('grades a submission and persists the grade via the backend', async () => {
    teacherApi.submission.mockResolvedValue({ id: 'A1', studentId: 'stu-1', answers: [{ answerId: 'ans-1', answerText: 'x=2', pointsEarned: 0, isCorrect: false }] })
    teacherApi.gradeSubmission.mockResolvedValue({ id: 'A1' })
    const user = userEvent.setup()
    renderTeacher('submission', '/app/teacher/submissions/A1')
    await user.click(await screen.findByRole('button', { name: /save grade/i }))
    await waitFor(() => expect(teacherApi.gradeSubmission).toHaveBeenCalled())
    const [attemptId, grades] = teacherApi.gradeSubmission.mock.calls[0]
    expect(attemptId).toBe('A1')
    expect(Array.isArray(grades)).toBe(true)
    expect(grades[0].answerId).toBe('ans-1')
  })

  it('Phase 14 closure — awards points to an assigned student via the tested gamification API', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One', averageQuizPercentage: 80 }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 15 })
    teacherApi.studentPointsLedger.mockResolvedValue([{ id: 't1', points: 15, reason: 'Seed' }])
    teacherApi.awardPoints.mockResolvedValue({ id: 'tx1' })
    const user = userEvent.setup()
    renderTeacher('students', '/app/teacher/students/stu-1')

    expect(await screen.findByText('Gamification points')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Points'), '25')
    await user.type(screen.getByLabelText('Reason'), 'Excellent participation')
    await user.click(screen.getByRole('button', { name: /award points/i }))
    await waitFor(() => expect(teacherApi.awardPoints).toHaveBeenCalled())
    const [studentId, body] = teacherApi.awardPoints.mock.calls[0]
    expect(studentId).toBe('stu-1')
    expect(body.points).toBe(25)
    expect(body.reason).toBe('Excellent participation')
    expect(typeof body.idempotencyKey).toBe('string')
  })

  it('Phase 14 closure — the award form rejects an out-of-range / empty-reason submission client-side', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One' }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 0 })
    teacherApi.studentPointsLedger.mockResolvedValue([])
    const user = userEvent.setup()
    renderTeacher('students', '/app/teacher/students/stu-1')
    await screen.findByText('Gamification points')
    // Out of range (>1000) with no reason → submit stays disabled, award never called.
    await user.type(screen.getByLabelText('Points'), '5000')
    expect(screen.getByRole('button', { name: /award points/i })).toBeDisabled()
    expect(teacherApi.awardPoints).not.toHaveBeenCalled()
  })

  it('sends teacher feedback to the backend', async () => {
    teacherApi.submission.mockResolvedValue({ id: 'A1', answers: [] })
    teacherApi.feedbackSubmission.mockResolvedValue({ id: 'A1' })
    const user = userEvent.setup()
    renderTeacher('submission', '/app/teacher/submissions/A1')
    await user.type(await screen.findByLabelText('Feedback to the student'), 'Great work overall.')
    await user.click(screen.getByRole('button', { name: /send feedback/i }))
    await waitFor(() => expect(teacherApi.feedbackSubmission).toHaveBeenCalledWith('A1', 'Great work overall.'))
  })
})
