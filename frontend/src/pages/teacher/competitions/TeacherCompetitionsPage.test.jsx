import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherCompetitionsPage from './TeacherCompetitionsPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Regression test for F-09 / P2-4: the score form required a hand-typed Entry
// id; the submissions list didn't expose one. Score must now come from
// clicking a submission row.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teach-1', fullName: 'Teacher One' }, role: 'Teacher' }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    competition: vi.fn(),
    competitionSubmissions: vi.fn(),
    competitionLeaderboard: vi.fn(),
    scoreCompetitionEntry: vi.fn(),
  },
}))

function renderDetail() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/teacher/competitions/comp-1']}>
          <Routes>
            <Route path="/app/teacher/competitions/:competitionId" element={<TeacherCompetitionsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  teacherApi.competition.mockResolvedValue({ id: 'comp-1', title: 'Math Olympiad', status: 1 })
  teacherApi.competitionLeaderboard.mockResolvedValue([])
})

describe('TeacherCompetitionsPage scoring', () => {
  it('has no free-text Entry id field', async () => {
    teacherApi.competitionSubmissions.mockResolvedValue([{ id: 'sub-1', studentId: 'stud-1', studentName: 'Sara', entryId: 'entry-1', content: 'work' }])
    renderDetail()
    await screen.findByRole('button', { name: 'Score this submission' })
    expect(screen.queryByLabelText('Entry id')).not.toBeInTheDocument()
  })

  it('scores a submission by clicking its row action, without typing an id', async () => {
    teacherApi.competitionSubmissions.mockResolvedValue([{ id: 'sub-1', studentId: 'stud-1', studentName: 'Sara', entryId: 'entry-1', content: 'work' }])
    teacherApi.scoreCompetitionEntry.mockResolvedValue({})
    const user = userEvent.setup()
    renderDetail()

    await user.click(await screen.findByRole('button', { name: 'Score this submission' }))
    expect(await screen.findByText('Scoring: Sara')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Score', { exact: false }), '95')
    await user.click(screen.getByRole('button', { name: 'Record score' }))

    await waitFor(() => expect(teacherApi.scoreCompetitionEntry).toHaveBeenCalledWith('comp-1', 'entry-1', 95))
  })

  it('shows an honest message instead of a scoring action when a submission has no matching entry', async () => {
    teacherApi.competitionSubmissions.mockResolvedValue([{ id: 'sub-2', studentId: 'stud-2', studentName: 'Omar', entryId: null, content: 'work' }])
    renderDetail()
    expect(await screen.findByText('No matching entry — cannot score')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Score this submission' })).not.toBeInTheDocument()
  })
})
