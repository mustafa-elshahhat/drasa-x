import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherCommunitiesPage from './TeacherCommunitiesPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// New page (greenfield): Teacher/SchoolAdmin communities creation + moderation. communities()
// returns EVERY tenant community (the backend does not filter it), so management actions must be
// gated client-side by the caller's actual membership role on that specific community, fetched
// lazily once "Manage" is expanded — a Teacher who isn't Owner/Moderator on a given community sees
// no management controls there, while SchoolAdmin always does (server-side blanket bypass).

let mockRole = 'Teacher'
vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' }, role: mockRole }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    communities: vi.fn(),
    classes: vi.fn(),
    grades: vi.fn(),
    myStudents: vi.fn(),
    createCommunity: vi.fn(),
    updateCommunity: vi.fn(),
    archiveCommunity: vi.fn(),
    communityMembers: vi.fn(),
    addCommunityMember: vi.fn(),
    communityPosts: vi.fn(),
    createCommunityPost: vi.fn(),
    moderatePost: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher/communities']}>
          <TeacherCommunitiesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  mockRole = 'Teacher'
  teacherApi.communities.mockResolvedValue([
    { id: 'com-1', name: 'Math Club', description: 'For math lovers', visibility: 1, memberCount: 2 },
  ])
  teacherApi.classes.mockResolvedValue([{ id: 'class-1', name: '7A' }])
  teacherApi.grades.mockResolvedValue([{ id: 'grade-1', name: 'Grade 7' }])
  teacherApi.myStudents.mockResolvedValue([
    { studentId: 'stud-1', fullName: 'Student One' },
    { studentId: 'stud-2', fullName: 'Student Two' },
  ])
})

describe('TeacherCommunitiesPage', () => {
  it('renders the full tenant community list (not filtered to communities the teacher owns)', async () => {
    renderPage()
    const nameEl = await screen.findByText('Math Club')
    const row = within(nameEl.closest('.student-list__item'))
    expect(row.getByText('Tenant only')).toBeInTheDocument()
    expect(row.getByText('2 members')).toBeInTheDocument()
  })

  it('creates a community via the create form (becomes Owner automatically server-side)', async () => {
    teacherApi.createCommunity.mockResolvedValue({ id: 'com-2' })
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Math Club')
    await user.type(screen.getByLabelText('Name'), 'Robotics Club')
    await user.click(screen.getByRole('button', { name: 'Create community' }))

    await waitFor(() => expect(teacherApi.createCommunity).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Robotics Club', visibility: 1, schoolClassId: null, eligibleGradeId: null,
    })))
  })

  it('shows management actions and lets an Owner add a member and moderate a post', async () => {
    teacherApi.communityMembers.mockResolvedValue([
      { userId: 'teacher-1', role: 2, joinedAt: '2026-01-01T00:00:00Z' },
      { userId: 'stud-1', role: 0, joinedAt: '2026-01-01T00:00:00Z' },
    ])
    teacherApi.communityPosts.mockResolvedValue([
      { id: 'post-1', communityId: 'com-1', userId: 'stud-1', content: 'Hi there', commentsCount: 0, createdAt: '2026-01-02T00:00:00Z' },
    ])
    teacherApi.addCommunityMember.mockResolvedValue({ userId: 'stud-2', role: 0 })
    teacherApi.moderatePost.mockResolvedValue(true)
    teacherApi.archiveCommunity.mockResolvedValue(true)
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Manage' }))
    expect(await screen.findByRole('button', { name: 'Archive' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Edit' })).toBeInTheDocument()

    // Add member: only the not-yet-member student (Student Two) is offered.
    const memberSelect = screen.getByLabelText('Add a member')
    const optionLabels = Array.from(memberSelect.querySelectorAll('option')).map((o) => o.textContent)
    expect(optionLabels).toContain('Student Two')
    expect(optionLabels).not.toContain('Student One')
    await user.selectOptions(memberSelect, 'stud-2')
    await user.click(screen.getByRole('button', { name: 'Add' }))
    await waitFor(() => expect(teacherApi.addCommunityMember).toHaveBeenCalledWith('com-1', { userId: 'stud-2', role: 0 }))

    // Moderate the existing post.
    await user.click(screen.getByRole('button', { name: 'Moderate' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByLabelText('Remove this post'))
    await user.click(within(dialog).getByRole('button', { name: 'Moderate' }))
    await waitFor(() => expect(teacherApi.moderatePost).toHaveBeenCalledWith('post-1', { status: 1, removePost: true }))

    // Archive (Owner-only).
    await user.click(screen.getByRole('button', { name: 'Archive' }))
    await waitFor(() => expect(teacherApi.archiveCommunity).toHaveBeenCalledWith('com-1'))
  })

  it('hides management actions and shows a note when the teacher is not a member of the community', async () => {
    teacherApi.communityMembers.mockResolvedValue([{ userId: 'other-teacher', role: 2, joinedAt: '2026-01-01T00:00:00Z' }])
    teacherApi.communityPosts.mockResolvedValue([])
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Manage' }))
    expect(await screen.findByText(/not a member of this community/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Archive' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Add a member')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Post content')).not.toBeInTheDocument()
  })

  it('a SchoolAdmin sees management actions on this teacher-portal page even without membership (blanket bypass)', async () => {
    mockRole = 'SchoolAdmin'
    teacherApi.communityMembers.mockResolvedValue([{ userId: 'other-teacher', role: 2, joinedAt: '2026-01-01T00:00:00Z' }])
    teacherApi.communityPosts.mockResolvedValue([])
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Manage' }))
    expect(await screen.findByRole('button', { name: 'Archive' })).toBeInTheDocument()
    expect(screen.queryByText(/not a member of this community/i)).not.toBeInTheDocument()
  })
})
