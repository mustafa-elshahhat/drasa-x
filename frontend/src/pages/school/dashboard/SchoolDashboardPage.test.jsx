import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolDashboardPage from './SchoolDashboardPage'
import { schoolApi } from '../../../features/school/schoolApi'
import { ApiError } from '../../../lib/api/problemDetails'

// Ported from the retired SchoolAdminPortalPage facade ("Phase 11 contracts"): the
// dashboard renders the real tenant summary, and surfaces an honest error/retry
// state when the read is denied.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { dashboard: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school']}>
          <SchoolDashboardPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SchoolDashboardPage', () => {
  it('renders the real tenant summary from the backend dashboard contract', async () => {
    schoolApi.dashboard.mockResolvedValue({
      tenantId: 'tenant-1', tenantName: 'Main School', tenantStatus: 'Active',
      students: 3, teachers: 1, parents: 2, classes: 2, subjects: 1, parentStudentLinks: 1, academicYears: 1,
    })
    renderPage()
    expect(await screen.findByRole('heading', { name: 'School administration' })).toBeInTheDocument()
    expect(await screen.findByText('Main School')).toBeInTheDocument()
    await waitFor(() => expect(schoolApi.dashboard).toHaveBeenCalled())
  })

  it('surfaces an error/retry state when the dashboard read is denied', async () => {
    schoolApi.dashboard.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'nope' }))
    renderPage()
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })
})
