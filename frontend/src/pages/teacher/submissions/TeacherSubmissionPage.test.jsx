import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherSubmissionPage from './TeacherSubmissionPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Ported from the retired TeacherPortalPage facade ("Phase 9 contracts"): grading
// and feedback both persist through explicit backend commands.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' } }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: { submission: vi.fn(), gradeSubmission: vi.fn(), feedbackSubmission: vi.fn() },
}))

function renderAt(attemptId) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/app/teacher/submissions/${attemptId}`]}>
          <Routes>
            <Route path="/app/teacher/submissions/:attemptId" element={<TeacherSubmissionPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('TeacherSubmissionPage', () => {
  it('grades a submission and persists the grade via the backend', async () => {
    teacherApi.submission.mockResolvedValue({ id: 'A1', studentId: 'stu-1', answers: [{ answerId: 'ans-1', answerText: 'x=2', pointsEarned: 0, isCorrect: false }] })
    teacherApi.gradeSubmission.mockResolvedValue({ id: 'A1' })
    const user = userEvent.setup()
    renderAt('A1')
    await user.click(await screen.findByRole('button', { name: /save grade/i }))
    await waitFor(() => expect(teacherApi.gradeSubmission).toHaveBeenCalled())
    const [attemptId, grades] = teacherApi.gradeSubmission.mock.calls[0]
    expect(attemptId).toBe('A1')
    expect(Array.isArray(grades)).toBe(true)
    expect(grades[0].answerId).toBe('ans-1')
  })

  it('sends teacher feedback to the backend', async () => {
    teacherApi.submission.mockResolvedValue({ id: 'A1', answers: [] })
    teacherApi.feedbackSubmission.mockResolvedValue({ id: 'A1' })
    const user = userEvent.setup()
    renderAt('A1')
    await user.type(await screen.findByLabelText('Feedback to the student'), 'Great work overall.')
    await user.click(screen.getByRole('button', { name: /send feedback/i }))
    await waitFor(() => expect(teacherApi.feedbackSubmission).toHaveBeenCalledWith('A1', 'Great work overall.'))
  })
})
