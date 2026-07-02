import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherClassesPage from './TeacherClassesPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Ported from the retired TeacherPortalPage facade ("Phase 9 contracts"): the
// classes list is scoped to the teacher's own assignments, with an honest
// empty state when there are none.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' } }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: { classes: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher/classes']}>
          <TeacherClassesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('TeacherClassesPage', () => {
  it("lists only the teacher's assigned classes", async () => {
    teacherApi.classes.mockResolvedValue([{ classId: 'PH8-CLASS-T1', name: 'Phase 8 Class', studentCount: 2 }])
    renderPage()
    expect(await screen.findByText(/Phase 8 Class/)).toBeInTheDocument()
  })

  it('shows an empty state when the teacher has no classes', async () => {
    teacherApi.classes.mockResolvedValue([])
    renderPage()
    expect(await screen.findByText('No assigned classes')).toBeInTheDocument()
  })
})
