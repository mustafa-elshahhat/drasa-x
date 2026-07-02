import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherUnitsPage from './TeacherUnitsPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// P1-4 — curriculum authoring UI: units → lessons were read-only (audit §6 "Curriculum
// authoring"). This proves the new "add lesson" form submits and calls teacherApi.addLesson.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: 'Teacher' }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    unitLessons: vi.fn(),
    addLesson: vi.fn(),
  },
}))

function renderAt(path) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/teacher/units/:unitId" element={<TeacherUnitsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  teacherApi.unitLessons.mockResolvedValue([])
  teacherApi.addLesson.mockResolvedValue({ id: 'lesson-new', title: 'Intro' })
})

describe('TeacherUnitsPage — curriculum authoring', () => {
  it('submits the add-lesson form and calls teacherApi.addLesson', async () => {
    const user = userEvent.setup()
    renderAt('/app/teacher/units/unit-1')

    await screen.findByRole('heading', { name: 'Add lesson' })
    await user.type(screen.getByLabelText('Lesson title', { exact: false }), 'Intro to fractions')
    await user.type(screen.getByLabelText('Lesson content', { exact: false }), 'Fractions represent parts of a whole.')
    await user.click(screen.getByRole('button', { name: 'Add lesson' }))

    expect(teacherApi.addLesson).toHaveBeenCalledWith({
      title: 'Intro to fractions',
      content: 'Fractions represent parts of a whole.',
      unitId: 'unit-1',
    })
  })

  it('blocks submission while the lesson title/content is empty', async () => {
    renderAt('/app/teacher/units/unit-1')
    await screen.findByRole('heading', { name: 'Add lesson' })
    expect(screen.getByRole('button', { name: 'Add lesson' })).toBeDisabled()
    expect(teacherApi.addLesson).not.toHaveBeenCalled()
  })
})
