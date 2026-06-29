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
    attendance: vi.fn(),
    materials: vi.fn(),
    progress: vi.fn(),
    completeLesson: vi.fn(),
    competition: vi.fn(),
    leaderboard: vi.fn(),
    enterCompetition: vi.fn(),
    competitionSubmission: vi.fn(),
    submitCompetition: vi.fn(),
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
            <Route path="/app/student/lessons/:lessonId" element={<StudentPortalPage view={view} />} />
            <Route path="/app/student/competitions/:competitionId" element={<StudentPortalPage view={view} />} />
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
    studentApi.completeLesson.mockResolvedValue({ id: 'p1', lessonId: 'lesson-1', isCompleted: true, completionPercentage: 100 })
    const user = userEvent.setup()

    renderStudent('lessons', '/app/student/lessons/lesson-1')

    await user.click(await screen.findByRole('button', { name: /mark lesson complete/i }))
    await waitFor(() => expect(studentApi.completeLesson).toHaveBeenCalledWith('lesson-1'))
    expect(await screen.findByText('Progress saved')).toBeInTheDocument()
  })

  it('Phase 14 closure — submits durable competition work through the backend submissions endpoint', async () => {
    studentApi.competition.mockResolvedValue({ id: 'comp-1', title: 'Science Fair' })
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
})
