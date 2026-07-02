import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentCommunitiesPage from './StudentCommunitiesPage'
import { studentApi } from '../../../features/student/studentApi'

// Regression test for §2.3 / P2-11: leave/comment API methods existed
// (studentApi.js) but were never wired to any UI control.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: {
    community: vi.fn(),
    communityPosts: vi.fn(),
    joinCommunity: vi.fn(),
    leaveCommunity: vi.fn(),
    createCommunityPost: vi.fn(),
    commentOnPost: vi.fn(),
  },
}))

function renderDetail() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/student/communities/comm-1']}>
          <Routes>
            <Route path="/app/student/communities/:communityId" element={<StudentCommunitiesPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  studentApi.community.mockResolvedValue({ id: 'comm-1', name: 'Math Club' })
  studentApi.communityPosts.mockResolvedValue([{ id: 'post-1', authorName: 'Omar', body: 'Hello', commentsCount: 2, createdAt: new Date().toISOString() }])
})

describe('StudentCommunitiesPage leave/comment', () => {
  it('calls leaveCommunity when the Leave button is clicked', async () => {
    studentApi.leaveCommunity.mockResolvedValue({})
    const user = userEvent.setup()
    renderDetail()

    await user.click(await screen.findByRole('button', { name: 'Leave community' }))
    await waitFor(() => expect(studentApi.leaveCommunity).toHaveBeenCalledWith('comm-1'))
  })

  it('submits a comment on a post via commentOnPost', async () => {
    studentApi.commentOnPost.mockResolvedValue({})
    const user = userEvent.setup()
    renderDetail()

    await screen.findByText('Hello')
    await user.type(screen.getByLabelText('Add a comment'), 'Great point!')
    await user.click(screen.getByRole('button', { name: 'Comment' }))

    await waitFor(() => expect(studentApi.commentOnPost).toHaveBeenCalledWith('post-1', 'Great point!'))
  })

  it('shows the real comments count from the post', async () => {
    renderDetail()
    expect(await screen.findByText(/2 comments/)).toBeInTheDocument()
  })
})
