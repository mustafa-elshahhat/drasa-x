import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, within, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolAcademicYearsPage from './SchoolAcademicYearsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: academic years were create-only (no edit/archive row action at all).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    academicYears: vi.fn(),
    createAcademicYear: vi.fn(),
    updateAcademicYear: vi.fn(),
    archiveAcademicYear: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/academic-years']}>
          <SchoolAcademicYearsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.academicYears.mockResolvedValue([
    { id: 'ay-1', name: '2025-2026', code: 'AY25', startDate: '2025-09-01', endDate: '2026-06-30', isCurrent: true },
  ])
})

describe('SchoolAcademicYearsPage row actions', () => {
  it('edits an academic year via the real update endpoint', async () => {
    schoolApi.updateAcademicYear.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const dialog = await screen.findByRole('dialog')
    const nameInput = within(dialog).getByLabelText('Name', { exact: false })
    await user.clear(nameInput)
    await user.type(nameInput, '2025-2026 Renamed')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(schoolApi.updateAcademicYear).toHaveBeenCalledWith(
        'ay-1',
        expect.objectContaining({ name: '2025-2026 Renamed', code: 'AY25' }),
      ),
    )
  })

  it('archives an academic year via the real archive endpoint', async () => {
    schoolApi.archiveAcademicYear.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Archive' }))
    await waitFor(() => expect(schoolApi.archiveAcademicYear).toHaveBeenCalledWith('ay-1'))
  })
})

// Ported from the retired SchoolAdminPortalPage facade ("Phase 11 contracts"): a
// new academic year is created through the real backend.
describe('SchoolAcademicYearsPage create', () => {
  it('creates an academic year through the backend', async () => {
    schoolApi.academicYears.mockResolvedValue([])
    schoolApi.createAcademicYear.mockResolvedValue({ id: 'AY-1', name: '2031/2032' })
    const user = userEvent.setup()
    renderPage()
    await user.type(screen.getByLabelText('Name'), '2031/2032')
    await user.type(screen.getByLabelText('Code'), 'AY3132')
    await user.click(screen.getByRole('button', { name: 'Create' }))
    await waitFor(() => expect(schoolApi.createAcademicYear).toHaveBeenCalled())
    expect(schoolApi.createAcademicYear.mock.calls[0][0].name).toBe('2031/2032')
  })
})
