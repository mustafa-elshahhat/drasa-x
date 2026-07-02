import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, within, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolGradesPage from './SchoolGradesPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: grades were create-only (no edit/delete row action at all).
// Grades use a DIFFERENT backend shape/policy than academic years/terms: the
// delete is a HARD delete (not an archive), so it must be labeled honestly.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    grades: vi.fn(),
    createGrade: vi.fn(),
    updateGrade: vi.fn(),
    deleteGrade: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/grades']}>
          <SchoolGradesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.grades.mockResolvedValue([{ id: 'grade-1', name: 'Grade 7' }])
})

describe('SchoolGradesPage row actions', () => {
  it('edits a grade via the real update endpoint', async () => {
    schoolApi.updateGrade.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const dialog = await screen.findByRole('dialog')
    const nameInput = within(dialog).getByLabelText('Name', { exact: false })
    await user.clear(nameInput)
    await user.type(nameInput, 'Grade 7 (renamed)')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(schoolApi.updateGrade).toHaveBeenCalledWith({ id: 'grade-1', name: 'Grade 7 (renamed)' }),
    )
  })

  it('deletes a grade via the real (hard) delete endpoint, labeled as Delete not Archive', async () => {
    schoolApi.deleteGrade.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    expect(screen.queryByRole('button', { name: 'Archive' })).not.toBeInTheDocument()
    await user.click(await screen.findByRole('button', { name: 'Delete' }))
    await waitFor(() => expect(schoolApi.deleteGrade).toHaveBeenCalledWith('grade-1'))
  })
})
