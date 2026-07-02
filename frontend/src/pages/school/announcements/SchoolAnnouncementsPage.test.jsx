import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolAnnouncementsPage from './SchoolAnnouncementsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test for F-01 / F-10 / P0-1d: the Publish/Unpublish row action was
// silently dropped by ResourceTable, so a draft announcement could never be
// published from the UI.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    announcements: vi.fn(),
    createAnnouncement: vi.fn(),
    publishAnnouncement: vi.fn(),
  },
}))

function renderAnnouncements() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/announcements']}>
          <SchoolAnnouncementsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SchoolAnnouncementsPage row actions', () => {
  it('shows a Draft chip + Publish button for an unpublished announcement and publishes it on click', async () => {
    schoolApi.announcements.mockResolvedValue([{ id: 'ann-1', title: 'Welcome back', isActive: false }])
    schoolApi.publishAnnouncement.mockResolvedValue({})
    const user = userEvent.setup()
    renderAnnouncements()

    expect(await screen.findByText('Draft')).toBeInTheDocument()
    const publishBtn = screen.getByRole('button', { name: 'Publish' })

    await user.click(publishBtn)
    await waitFor(() => expect(schoolApi.publishAnnouncement).toHaveBeenCalledWith('ann-1', true))
  })

  it('shows a Published chip + Unpublish button for a published announcement', async () => {
    schoolApi.announcements.mockResolvedValue([{ id: 'ann-2', title: 'Exam week', isActive: true }])
    schoolApi.publishAnnouncement.mockResolvedValue({})
    const user = userEvent.setup()
    renderAnnouncements()

    expect(await screen.findByText('Published')).toBeInTheDocument()
    const unpublishBtn = screen.getByRole('button', { name: 'Unpublish' })

    await user.click(unpublishBtn)
    await waitFor(() => expect(schoolApi.publishAnnouncement).toHaveBeenCalledWith('ann-2', false))
  })
})
