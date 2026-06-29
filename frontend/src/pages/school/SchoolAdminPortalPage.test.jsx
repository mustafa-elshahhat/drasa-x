import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import SchoolAdminPortalPage from './SchoolAdminPortalPage'
import { schoolApi } from '../../features/school/schoolApi'
import { ApiError } from '../../lib/api/problemDetails'

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../features/school/schoolApi', () => ({
  schoolApi: {
    dashboard: vi.fn(), profile: vi.fn(), subscription: vi.fn(), usage: vi.fn(),
    academicYears: vi.fn(), createAcademicYear: vi.fn(), terms: vi.fn(), createTerm: vi.fn(),
    grades: vi.fn(), createGrade: vi.fn(), classes: vi.fn(), createClass: vi.fn(), subjects: vi.fn(),
    users: vi.fn(), createUser: vi.fn(), setUserEnabled: vi.fn(),
    relationships: vi.fn(), createRelationship: vi.fn(), deactivateRelationship: vi.fn(),
    subjectAssignments: vi.fn(), createSubjectAssignment: vi.fn(), classAssignments: vi.fn(), createClassAssignment: vi.fn(),
    announcements: vi.fn(), createAnnouncement: vi.fn(), publishAnnouncement: vi.fn(),
    documentRequests: vi.fn(), respondDocumentRequest: vi.fn(), transitionDocumentRequest: vi.fn(),
    communities: vi.fn(), competitions: vi.fn(), reports: vi.fn(), aiUsage: vi.fn(),
    support: vi.fn(), respondSupport: vi.fn(), audit: vi.fn(), settings: vi.fn(), upsertSetting: vi.fn(),
  },
}))

function renderSchool(view) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school']}>
          <SchoolAdminPortalPage view={view} />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SchoolAdminPortalPage Phase 11 contracts', () => {
  it('renders the real tenant summary from the backend dashboard contract', async () => {
    schoolApi.dashboard.mockResolvedValue({
      tenantId: 'tenant-1', tenantName: 'Main School', tenantStatus: 'Active',
      students: 3, teachers: 1, parents: 2, classes: 2, subjects: 1, parentStudentLinks: 1, academicYears: 1,
    })
    renderSchool('dashboard')
    expect(await screen.findByRole('heading', { name: 'School administration' })).toBeInTheDocument()
    expect(await screen.findByText('Main School')).toBeInTheDocument()
    await waitFor(() => expect(schoolApi.dashboard).toHaveBeenCalled())
  })

  it('lists the tenant students returned by the backend', async () => {
    schoolApi.users.mockResolvedValue([{ id: 's1', fullName: 'Tenant1 Student', loginCode: 'STU-T1', role: 'Student' }])
    renderSchool('students')
    expect(await screen.findByText(/Tenant1 Student/)).toBeInTheDocument()
    await waitFor(() => expect(schoolApi.users).toHaveBeenCalledWith('Student', expect.anything()))
  })

  it('shows an empty state when there are no accounts', async () => {
    schoolApi.users.mockResolvedValue([])
    renderSchool('teachers')
    expect(await screen.findByText('No accounts yet.')).toBeInTheDocument()
  })

  it('surfaces an error/retry state when the dashboard read is denied', async () => {
    schoolApi.dashboard.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'nope' }))
    renderSchool('dashboard')
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })

  it('creates a parent–student link through the backend with the selected ids', async () => {
    schoolApi.relationships.mockResolvedValue([])
    schoolApi.users.mockImplementation((role) =>
      Promise.resolve(role === 'Parent'
        ? [{ id: 'p1', fullName: 'Parent One' }]
        : [{ id: 's1', fullName: 'Student One' }]))
    schoolApi.createRelationship.mockResolvedValue({ id: 'PSR-1', parentId: 'p1', studentId: 's1', isActive: true })
    const user = userEvent.setup()
    renderSchool('relationships')
    await screen.findByText('Parent One') // options loaded
    await user.selectOptions(screen.getByLabelText('Parent'), 'p1')
    await user.selectOptions(screen.getByLabelText('Student'), 's1')
    await user.click(screen.getByRole('button', { name: 'Link parent to student' }))
    await waitFor(() => expect(schoolApi.createRelationship).toHaveBeenCalled())
    const arg = schoolApi.createRelationship.mock.calls[0][0]
    expect(arg.parentId).toBe('p1')
    expect(arg.studentId).toBe('s1')
  })

  it('creates an academic year through the backend', async () => {
    schoolApi.academicYears.mockResolvedValue([])
    schoolApi.createAcademicYear.mockResolvedValue({ id: 'AY-1', name: '2031/2032' })
    const user = userEvent.setup()
    renderSchool('academic-years')
    await user.type(screen.getByLabelText('Name'), '2031/2032')
    await user.type(screen.getByLabelText('Code'), 'AY3132')
    await user.click(screen.getByRole('button', { name: 'Create' }))
    await waitFor(() => expect(schoolApi.createAcademicYear).toHaveBeenCalled())
    expect(schoolApi.createAcademicYear.mock.calls[0][0].name).toBe('2031/2032')
  })
})
