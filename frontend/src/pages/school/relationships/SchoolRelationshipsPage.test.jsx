import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolRelationshipsPage from './SchoolRelationshipsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for F-01 / P0-1c: the Deactivate row action was silently
// dropped by ResourceTable and never rendered for active relationships.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    relationships: vi.fn(),
    users: vi.fn(),
    createRelationship: vi.fn(),
    deactivateRelationship: vi.fn(),
  },
}))

function renderRelationships() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/relationships']}>
          <SchoolRelationshipsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.users.mockResolvedValue([])
})

describe('SchoolRelationshipsPage row actions', () => {
  it('renders a Deactivate button only for active relationships and calls the API on click', async () => {
    schoolApi.relationships.mockResolvedValue([
      { id: 'rel-1', parentId: 'p1', studentId: 's1', isActive: true },
      { id: 'rel-2', parentId: 'p2', studentId: 's2', isActive: false },
    ])
    schoolApi.deactivateRelationship.mockResolvedValue({})
    const user = userEvent.setup()
    renderRelationships()

    const buttons = await screen.findAllByRole('button', { name: 'Deactivate' })
    expect(buttons).toHaveLength(1)

    await user.click(buttons[0])
    await waitFor(() => expect(schoolApi.deactivateRelationship).toHaveBeenCalledWith('rel-1'))
  })
})

// Ported from the retired SchoolAdminPortalPage facade ("Phase 11 contracts"): a
// new parent-student link is created through the real backend with the selected ids.
describe('SchoolRelationshipsPage create link', () => {
  it('creates a parent–student link through the backend with the selected ids', async () => {
    schoolApi.relationships.mockResolvedValue([])
    schoolApi.users.mockImplementation((role) =>
      Promise.resolve(role === 'Parent'
        ? [{ id: 'p1', fullName: 'Parent One' }]
        : [{ id: 's1', fullName: 'Student One' }]))
    schoolApi.createRelationship.mockResolvedValue({ id: 'PSR-1', parentId: 'p1', studentId: 's1', isActive: true })
    const user = userEvent.setup()
    renderRelationships()
    await screen.findByText('Parent One') // options loaded
    await user.selectOptions(screen.getByLabelText('Parent'), 'p1')
    await user.selectOptions(screen.getByLabelText('Student'), 's1')
    await user.click(screen.getByRole('button', { name: 'Link parent to student' }))
    await waitFor(() => expect(schoolApi.createRelationship).toHaveBeenCalled())
    const arg = schoolApi.createRelationship.mock.calls[0][0]
    expect(arg.parentId).toBe('p1')
    expect(arg.studentId).toBe('s1')
  })
})
