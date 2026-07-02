import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentSubjectDetailsPage from './StudentSubjectDetailsPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for §9 row 2 / P3-1: 'Grade 11' and '86%' were shown as real
// data whenever the backend field was absent — fabricated production fallbacks.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: { subject: vi.fn(), units: vi.fn() },
}))

function renderAt() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/subjects/subj-1']}>
          <Routes>
            <Route path="/app/student/subjects/:subjectId" element={<StudentSubjectDetailsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  studentApi.units.mockResolvedValue([])
})

describe('StudentSubjectDetailsPage honest fallbacks', () => {
  it('never renders the fabricated "Grade 11" or "86%" placeholders when the backend omits those fields', async () => {
    studentApi.subject.mockResolvedValue({ id: 'subj-1', name: 'Algebra' })
    renderAt()

    expect(await screen.findByRole('heading', { name: 'Algebra' })).toBeInTheDocument()
    expect(screen.queryByText('Grade 11')).not.toBeInTheDocument()
    expect(screen.queryByText('86%')).not.toBeInTheDocument()
    expect(screen.getByText('—')).toBeInTheDocument()
  })

  it('renders the real grade/average score when the backend provides them', async () => {
    studentApi.subject.mockResolvedValue({ id: 'subj-1', name: 'Algebra', gradeName: 'Grade 9', averageScore: '73%' })
    renderAt()

    expect(await screen.findByText('Grade 9')).toBeInTheDocument()
    expect(screen.getByText('73%')).toBeInTheDocument()
  })
})
