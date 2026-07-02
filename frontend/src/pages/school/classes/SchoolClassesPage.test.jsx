import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolClassesPage from './SchoolClassesPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for §4 "Class (school)" / P1-2: the classes list had no row
// action at all ("create only; no roster/detail/edit").

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { classes: vi.fn(), grades: vi.fn(), academicYears: vi.fn(), createClass: vi.fn() },
}))

function renderClasses() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/classes']}>
          <SchoolClassesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.grades.mockResolvedValue([])
  schoolApi.academicYears.mockResolvedValue([])
})

describe('SchoolClassesPage row actions', () => {
  it('links each class row to its detail route', async () => {
    schoolApi.classes.mockResolvedValue([{ id: 'class-1', name: '7A', code: '7A-2026' }])
    renderClasses()

    const link = await screen.findByRole('link', { name: 'Open' })
    expect(link).toHaveAttribute('href', '/app/school/classes/class-1')
  })
})
