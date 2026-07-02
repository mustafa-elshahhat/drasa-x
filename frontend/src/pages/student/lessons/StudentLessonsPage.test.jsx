import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentLessonsPage from './StudentLessonsPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for §9 row 3 / P3-4: the "video player" was a decorative
// gradient Thumb with a fake play-button overlay — no real video exists at the
// lesson level. It must not present itself as a working player.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: {
    lessonDetail: vi.fn(), lessons: vi.fn(), materials: vi.fn(), progress: vi.fn(), completeLesson: vi.fn(),
  },
}))

function renderAt() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/lessons/lesson-1']}>
          <Routes>
            <Route path="/app/student/lessons/:lessonId" element={<StudentLessonsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  studentApi.progress.mockResolvedValue({})
  studentApi.lessons.mockResolvedValue([])
})

describe('StudentLessonsPage hero honesty', () => {
  it('never claims to be a video player (no "play" affordance)', async () => {
    studentApi.lessonDetail.mockResolvedValue({ id: 'lesson-1', title: 'Fractions', subjectId: 'subj-1', unitId: 'unit-1' })
    studentApi.materials.mockResolvedValue([{ id: 'mat-1', title: 'Handout' }])
    renderAt()

    await screen.findByRole('heading', { name: 'Fractions' })
    // No lucide "play-circle" icon anywhere in the hero — it must not imply video playback.
    expect(document.querySelector('.lucide-play-circle')).not.toBeInTheDocument()
    expect(screen.getByText('View lesson materials')).toBeInTheDocument()
  })

  it('does not offer a fake clickable affordance when there is no material to open', async () => {
    studentApi.lessonDetail.mockResolvedValue({ id: 'lesson-1', title: 'Fractions', subjectId: 'subj-1', unitId: 'unit-1' })
    studentApi.materials.mockResolvedValue([])
    renderAt()

    await screen.findByRole('heading', { name: 'Fractions' })
    expect(screen.queryByText('View lesson materials')).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /view lesson materials/i })).not.toBeInTheDocument()
  })
})

// Ported from the retired StudentPortalPage facade ("Phase 8 contracts"): the
// explicit lesson-completion command must be sent to the backend and the page
// must wait for server confirmation before showing a saved state.
describe('StudentLessonsPage completion command', () => {
  it('calls the explicit lesson completion command and waits for server success', async () => {
    studentApi.materials.mockResolvedValue([])
    studentApi.progress.mockResolvedValue({ lessons: { status: 'fulfilled', value: { data: [] } } })
    studentApi.lessonDetail.mockResolvedValue({ lessonId: 'lesson-1', title: 'Intro lesson', content: 'Lesson body', unitId: 'u1', unitTitle: 'Unit 1', subjectId: 's1', subjectName: 'Science', isCompleted: false, completionPercentage: 0 })
    studentApi.completeLesson.mockResolvedValue({ id: 'p1', lessonId: 'lesson-1', isCompleted: true, completionPercentage: 100 })
    const user = userEvent.setup()

    renderAt()

    await user.click(await screen.findByRole('button', { name: /mark lesson complete/i }))
    await waitFor(() => expect(studentApi.completeLesson).toHaveBeenCalledWith('lesson-1'))
    expect(await screen.findByText('Progress saved')).toBeInTheDocument()
  })
})
