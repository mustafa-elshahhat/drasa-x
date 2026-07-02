import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolCommunitiesPage from './SchoolCommunitiesPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for the contract-gap appendix "Communities" row: SchoolCommunitiesPage was a
// 22-line read-only dump (<List query={...} /> with no columns/actions). This rebuild adds
// create/edit/archive plus a members+posts moderation drill-down.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    communities: vi.fn(),
    classes: vi.fn(),
    grades: vi.fn(),
    users: vi.fn(),
    createCommunity: vi.fn(),
    updateCommunity: vi.fn(),
    archiveCommunity: vi.fn(),
    communityMembers: vi.fn(),
    addCommunityMember: vi.fn(),
    communityPosts: vi.fn(),
    moderatePost: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/communities']}>
          <SchoolCommunitiesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.communities.mockResolvedValue([
    { id: 'com-1', name: 'Grade 7 Science Club', description: 'For science enthusiasts', visibility: 1, memberCount: 2 },
  ])
  schoolApi.classes.mockResolvedValue([{ id: 'class-1', name: '7A' }])
  schoolApi.grades.mockResolvedValue([{ id: 'grade-1', name: 'Grade 7' }])
  schoolApi.users.mockResolvedValue([
    { id: 'teach-1', fullName: 'Teacher One' },
    { id: 'stud-1', fullName: 'Student One' },
  ])
  schoolApi.communityMembers.mockResolvedValue([{ userId: 'teach-1', role: 2, joinedAt: '2026-01-01T00:00:00Z' }])
  schoolApi.communityPosts.mockResolvedValue([
    { id: 'post-1', communityId: 'com-1', userId: 'stud-1', content: 'Welcome!', commentsCount: 2, createdAt: '2026-01-02T00:00:00Z' },
  ])
})

describe('SchoolCommunitiesPage', () => {
  it('renders the community list with a visibility chip and member count', async () => {
    renderPage()

    const nameEl = await screen.findByText('Grade 7 Science Club')
    const row = within(nameEl.closest('.student-list__item'))
    expect(row.getByText('For science enthusiasts')).toBeInTheDocument()
    expect(row.getByText('Tenant only')).toBeInTheDocument()
    expect(row.getByText('2 members')).toBeInTheDocument()
  })

  it('creates a community via the create form', async () => {
    schoolApi.createCommunity.mockResolvedValue({ id: 'com-2' })
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Grade 7 Science Club')
    await user.type(screen.getByLabelText('Name'), 'Robotics Club')
    await user.click(screen.getByRole('button', { name: 'Create community' }))

    await waitFor(() => expect(schoolApi.createCommunity).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Robotics Club', visibility: 1, schoolClassId: null, eligibleGradeId: null,
    })))
  })

  it('edits a community', async () => {
    schoolApi.updateCommunity.mockResolvedValue({ id: 'com-1' })
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const dialog = await screen.findByRole('dialog')
    const nameInput = within(dialog).getByLabelText('Name', { exact: false })
    await user.clear(nameInput)
    await user.type(nameInput, 'Grade 7 Science Club (Renamed)')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(schoolApi.updateCommunity).toHaveBeenCalledWith('com-1', expect.objectContaining({
      name: 'Grade 7 Science Club (Renamed)', visibility: 1,
    })))
  })

  it('archives a community (SchoolAdmin blanket bypass)', async () => {
    schoolApi.archiveCommunity.mockResolvedValue(true)
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Archive' }))
    await waitFor(() => expect(schoolApi.archiveCommunity).toHaveBeenCalledWith('com-1'))
  })

  it('expands Manage to show resolved member names, adds a member, and moderates a post', async () => {
    schoolApi.addCommunityMember.mockResolvedValue({ userId: 'stud-1', role: 0 })
    schoolApi.moderatePost.mockResolvedValue(true)
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Manage' }))

    // Member userId is resolved to a real display name via the tenant-users lookup, not a raw id.
    const memberName = await screen.findByText('Teacher One')
    expect(within(memberName.closest('.ui-list__item')).getByText('Owner')).toBeInTheDocument()

    // Add member: only the not-yet-member user is offered.
    const memberSelect = screen.getByLabelText('Add a member')
    const optionLabels = Array.from(memberSelect.querySelectorAll('option')).map((o) => o.textContent)
    expect(optionLabels).toContain('Student One')
    expect(optionLabels).not.toContain('Teacher One')
    await user.selectOptions(memberSelect, 'stud-1')
    await user.click(screen.getByRole('button', { name: 'Add' }))
    await waitFor(() => expect(schoolApi.addCommunityMember).toHaveBeenCalledWith('com-1', { userId: 'stud-1', role: 0 }))

    // Moderate the post (authored by a different user than the member above, so its resolved
    // author name — "Student One" — is unambiguous from the Members list).
    expect(await screen.findByText('Welcome!')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Moderate' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByLabelText('Remove this post'))
    await user.click(within(dialog).getByRole('button', { name: 'Moderate' }))

    await waitFor(() => expect(schoolApi.moderatePost).toHaveBeenCalledWith('post-1', { status: 1, removePost: true }))
  })
})
