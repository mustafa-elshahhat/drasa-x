import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolReportsPage from './SchoolReportsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: the reports page only ever consumed 2 of the 4 real report
// endpoints (tenant-users, ai-usage-activity) — assessment-summary and
// audit-activity existed on the backend but were never rendered.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { reports: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/reports']}>
          <SchoolReportsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SchoolReportsPage', () => {
  it('renders all 4 report cards from the 4 real endpoints (Promise.allSettled shape)', async () => {
    schoolApi.reports.mockResolvedValue({
      users: { status: 'fulfilled', value: { data: { students: 12, teachers: 3, parents: 9 } } },
      ai: { status: 'fulfilled', value: { data: { totalRequests: 20 } } },
      assessmentSummary: { status: 'fulfilled', value: { data: { kind: 'QuizAttempt', count: 34 } } },
      auditActivity: { status: 'fulfilled', value: { data: { kind: 'AuditLog', count: 11 } } },
    })
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Users by role' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'AI usage activity' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Assessment summary' })).toBeInTheDocument()
    expect(await screen.findByRole('heading', { name: 'Audit activity' })).toBeInTheDocument()

    // Each card shows real data from its own endpoint (distinct counts, no fabrication).
    expect(await screen.findByText('12')).toBeInTheDocument()
    expect(await screen.findByText('20')).toBeInTheDocument()
    expect(await screen.findByText('34')).toBeInTheDocument()
    expect(await screen.findByText('11')).toBeInTheDocument()
  })

  it('shows an honest empty state for a report whose call was rejected', async () => {
    schoolApi.reports.mockResolvedValue({
      users: { status: 'fulfilled', value: { data: { students: 12 } } },
      ai: { status: 'rejected', reason: new Error('unavailable') },
      assessmentSummary: { status: 'fulfilled', value: { data: { kind: 'QuizAttempt', count: 34 } } },
      auditActivity: { status: 'fulfilled', value: { data: { kind: 'AuditLog', count: 11 } } },
    })
    renderPage()

    expect(await screen.findByRole('heading', { name: 'AI usage activity' })).toBeInTheDocument()
    expect(await screen.findAllByText('No report data available.')).toHaveLength(1)
  })
})
