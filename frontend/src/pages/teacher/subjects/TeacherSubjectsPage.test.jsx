import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherSubjectsPage from './TeacherSubjectsPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// P1-4 — curriculum authoring UI: subjects were previously 100% read-only (audit §6 "Curriculum
// authoring"). These tests prove the new "add subject" / "add unit" forms submit and call the
// right teacherApi methods.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: 'Teacher' }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    subjects: vi.fn(),
    grades: vi.fn(),
    subjectUnits: vi.fn(),
    addSubject: vi.fn(),
    addUnit: vi.fn(),
  },
}))

function renderAt(path) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/teacher/subjects" element={<TeacherSubjectsPage />} />
            <Route path="/app/teacher/subjects/:subjectId" element={<TeacherSubjectsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  teacherApi.subjects.mockResolvedValue([])
  teacherApi.grades.mockResolvedValue([{ id: 'grade-7', name: 'Grade 7' }])
  teacherApi.subjectUnits.mockResolvedValue([])
  teacherApi.addSubject.mockResolvedValue({ id: 'subj-new', name: 'Algebra' })
  teacherApi.addUnit.mockResolvedValue({ id: 'unit-new', title: 'Unit 1' })
})

describe('TeacherSubjectsPage — curriculum authoring', () => {
  it('submits the add-subject form and calls teacherApi.addSubject', async () => {
    const user = userEvent.setup()
    renderAt('/app/teacher/subjects')

    await screen.findByRole('heading', { name: 'Add subject' })
    await user.type(screen.getByLabelText('Subject name', { exact: false }), 'Algebra')
    await user.selectOptions(screen.getByLabelText('Grade', { exact: false }), 'grade-7')
    await user.click(screen.getByRole('button', { name: 'Add subject' }))

    expect(teacherApi.addSubject).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'Algebra', gradeId: 'grade-7' }),
    )
  })

  it('does not submit the add-subject form while required fields are empty', async () => {
    renderAt('/app/teacher/subjects')
    await screen.findByRole('heading', { name: 'Add subject' })
    expect(screen.getByRole('button', { name: 'Add subject' })).toBeDisabled()
    expect(teacherApi.addSubject).not.toHaveBeenCalled()
  })

  it('submits the add-unit form on the subject detail view and calls teacherApi.addUnit', async () => {
    const user = userEvent.setup()
    renderAt('/app/teacher/subjects/subj-1')

    await screen.findByRole('heading', { name: 'Add unit' })
    await user.type(screen.getByLabelText('Unit title', { exact: false }), 'Fractions')
    await user.click(screen.getByRole('button', { name: 'Add unit' }))

    expect(teacherApi.addUnit).toHaveBeenCalledWith({ title: 'Fractions', subjectId: 'subj-1' })
  })
})
