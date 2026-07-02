import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentUnitsPage from './StudentUnitsPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for F-03 / P0-3: the bare `/app/student/units` route (no
// :unitId) threw `ReferenceError: SubjectsPage is not defined` because the
// component referenced an unimported component. It must now redirect instead
// of crashing.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: {
    subjects: vi.fn().mockResolvedValue([]),
    units: vi.fn().mockResolvedValue([]),
    lessons: vi.fn().mockResolvedValue([]),
    progress: vi.fn().mockResolvedValue({}),
  },
}))

function renderAt(path) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/student/units" element={<StudentUnitsPage />} />
            <Route path="/app/student/units/:unitId" element={<StudentUnitsPage />} />
            <Route path="/app/student/subjects" element={<div>Subjects list page</div>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  // Preserves the module-factory default resolved values (only call history is cleared).
  vi.clearAllMocks()
})

describe('StudentUnitsPage bare route', () => {
  it('redirects to /app/student/subjects instead of crashing when no :unitId is present', async () => {
    renderAt('/app/student/units')
    expect(await screen.findByText('Subjects list page')).toBeInTheDocument()
    expect(studentApi.subjects).not.toHaveBeenCalled()
  })
})

// Ported from the retired StudentPortalPage facade ("Phase 8 contracts"): a unit
// with a real :unitId renders breadcrumbs (subject → unit) and the redesigned
// lesson-cards list with real per-lesson completion state.
describe('StudentUnitsPage unit details', () => {
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

    renderAt('/app/student/units/u3')

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
