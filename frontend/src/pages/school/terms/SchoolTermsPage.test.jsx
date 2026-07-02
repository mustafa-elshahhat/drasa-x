import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, within, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolTermsPage from './SchoolTermsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: terms were create-only (no edit/archive row action at all).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    terms: vi.fn(),
    createTerm: vi.fn(),
    updateTerm: vi.fn(),
    archiveTerm: vi.fn(),
    academicYears: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/terms']}>
          <SchoolTermsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.academicYears.mockResolvedValue([])
  schoolApi.terms.mockResolvedValue([
    { id: 'term-1', name: 'Fall term', code: 'T1', order: 1, startDate: '2025-09-01', endDate: '2026-01-15', academicYearId: 'ay-1' },
  ])
})

describe('SchoolTermsPage row actions', () => {
  it('edits a term via the real update endpoint (no academicYearId in the payload)', async () => {
    schoolApi.updateTerm.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const dialog = await screen.findByRole('dialog')
    const nameInput = within(dialog).getByLabelText('Name', { exact: false })
    await user.clear(nameInput)
    await user.type(nameInput, 'Fall term (renamed)')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(schoolApi.updateTerm).toHaveBeenCalledWith(
        'term-1',
        expect.objectContaining({ name: 'Fall term (renamed)', code: 'T1', order: 1 }),
      ),
    )
    const [, payload] = schoolApi.updateTerm.mock.calls[0]
    expect(payload).not.toHaveProperty('academicYearId')
  })

  it('archives a term via the real archive endpoint', async () => {
    schoolApi.archiveTerm.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Archive' }))
    await waitFor(() => expect(schoolApi.archiveTerm).toHaveBeenCalledWith('term-1'))
  })
})
