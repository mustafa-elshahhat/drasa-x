import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import StudentCompetitionsPage from './StudentCompetitionsPage'
import { studentApi } from '../../../features/student/studentApi'

// Ported from the retired StudentPortalPage facade ("Phase 14 closure" contract):
// durable competition submissions go through the real backend submissions endpoint.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'student-1', fullName: 'Sara' }, role: 'Student' }),
}))

vi.mock('../../../features/student/studentApi', () => ({
  studentApi: {
    competition: vi.fn(),
    leaderboard: vi.fn(),
    competitionSubmission: vi.fn(),
    submitCompetition: vi.fn(),
    enterCompetition: vi.fn(),
  },
}))

function renderAt(path = '/app/student/competitions/comp-1') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[path]}>
          <Routes>
            <Route path="/app/student/competitions/:competitionId" element={<StudentCompetitionsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('StudentCompetitionsPage', () => {
  it('Phase 14 closure — submits durable competition work through the backend submissions endpoint', async () => {
    // Backend returns explicit student flags; an entered student in an open competition can submit.
    studentApi.competition.mockResolvedValue({ id: 'comp-1', title: 'Science Fair', hasEntered: true, canSubmit: true, canEnter: false, canViewLeaderboard: false })
    studentApi.leaderboard.mockResolvedValue([])
    studentApi.competitionSubmission.mockResolvedValue(null) // not submitted yet
    studentApi.submitCompetition.mockResolvedValue({ id: 's1', content: 'My project' })
    const user = userEvent.setup()

    renderAt()

    expect(await screen.findByText('Your submission')).toBeInTheDocument()
    expect(await screen.findByText('You have not submitted yet.')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Submission'), 'My project')
    await user.click(screen.getByRole('button', { name: /submit work/i }))
    await waitFor(() => expect(studentApi.submitCompetition).toHaveBeenCalledWith('comp-1', 'My project'))
    expect(await screen.findByText('Submission saved')).toBeInTheDocument()
  })
})
