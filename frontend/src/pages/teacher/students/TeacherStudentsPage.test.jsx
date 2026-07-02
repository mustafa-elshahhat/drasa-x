import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import TeacherStudentsPage from './TeacherStudentsPage'
import { teacherApi } from '../../../features/teacher/teacherApi'

// Ported from the retired TeacherPortalPage facade ("Phase 14 closure" contracts):
// teacher-side gamification point awards go through the tested backend API, and
// the client-side form validation blocks out-of-range/empty-reason submissions.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'teacher-1', fullName: 'Teacher One' } }),
}))

vi.mock('../../../features/teacher/teacherApi', () => ({
  teacherApi: {
    myStudents: vi.fn(),
    studentProgress: vi.fn(),
    studentPoints: vi.fn(),
    studentPointsLedger: vi.fn(),
    awardPoints: vi.fn(),
    badgeCatalog: vi.fn(),
    studentBadges: vi.fn(),
    awardBadge: vi.fn(),
  },
}))

function renderAt(studentId) {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={[`/app/teacher/students/${studentId}`]}>
          <Routes>
            <Route path="/app/teacher/students/:studentId" element={<TeacherStudentsPage />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('TeacherStudentsPage — gamification points', () => {
  it('awards points to an assigned student via the tested gamification API', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One', averageQuizPercentage: 80 }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 15 })
    teacherApi.studentPointsLedger.mockResolvedValue([{ id: 't1', points: 15, reason: 'Seed' }])
    teacherApi.awardPoints.mockResolvedValue({ id: 'tx1' })
    teacherApi.badgeCatalog.mockResolvedValue([{ id: 'b1', code: 'STAR', name: 'Star performer', type: 0 }])
    teacherApi.studentBadges.mockResolvedValue([])
    const user = userEvent.setup()
    renderAt('stu-1')

    expect(await screen.findByText('Gamification points')).toBeInTheDocument()
    await user.type(screen.getByLabelText('Points'), '25')
    await user.type(screen.getByLabelText('Reason'), 'Excellent participation')
    await user.click(screen.getByRole('button', { name: /award points/i }))
    await waitFor(() => expect(teacherApi.awardPoints).toHaveBeenCalled())
    const [studentId, body] = teacherApi.awardPoints.mock.calls[0]
    expect(studentId).toBe('stu-1')
    expect(body.points).toBe(25)
    expect(body.reason).toBe('Excellent participation')
    expect(typeof body.idempotencyKey).toBe('string')
  })

  it('the award form rejects an out-of-range / empty-reason submission client-side', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One' }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 0 })
    teacherApi.studentPointsLedger.mockResolvedValue([])
    teacherApi.badgeCatalog.mockResolvedValue([])
    teacherApi.studentBadges.mockResolvedValue([])
    const user = userEvent.setup()
    renderAt('stu-1')
    await screen.findByText('Gamification points')
    // Out of range (>1000) with no reason → submit stays disabled, award never called.
    await user.type(screen.getByLabelText('Points'), '5000')
    expect(screen.getByRole('button', { name: /award points/i })).toBeDisabled()
    expect(teacherApi.awardPoints).not.toHaveBeenCalled()
  })
})

// Fix pass — Communities/Badges/Gamification-rules workstream: teacher badge-award UI
// (teacherApi.badgeCatalog/studentBadges/awardBadge), mirroring the gamification-points tests above.
describe('TeacherStudentsPage — badges', () => {
  it('awards a catalog badge (not yet earned) to an assigned student via the tested badge API', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One' }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 0 })
    teacherApi.studentPointsLedger.mockResolvedValue([])
    teacherApi.badgeCatalog.mockResolvedValue([
      { id: 'b1', code: 'STAR', name: 'Star performer', type: 0 },
      { id: 'b2', code: 'STREAK5', name: 'Five-day streak', type: 1 },
    ])
    teacherApi.studentBadges.mockResolvedValue([{ id: 'sb1', badgeId: 'b2', studentId: 'stu-1', awardedAt: '2026-01-01T00:00:00Z' }])
    teacherApi.awardBadge.mockResolvedValue({ id: 'sb2' })
    const user = userEvent.setup()
    renderAt('stu-1')

    // The already-earned badge is shown by resolved name (not raw id), and only the un-earned
    // badge is offered in the picker.
    expect(await screen.findByText('Five-day streak')).toBeInTheDocument()
    await user.selectOptions(screen.getByLabelText('Badge'), 'b1')
    await user.type(screen.getByLabelText('Reason (optional)'), 'Great quiz streak')
    await user.click(screen.getByRole('button', { name: /award badge/i }))

    await waitFor(() => expect(teacherApi.awardBadge).toHaveBeenCalled())
    const [studentId, body] = teacherApi.awardBadge.mock.calls[0]
    expect(studentId).toBe('stu-1')
    expect(body.badgeId).toBe('b1')
    expect(body.reason).toBe('Great quiz streak')
  })

  it('hides the award picker once every catalog badge has already been earned', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One' }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 0 })
    teacherApi.studentPointsLedger.mockResolvedValue([])
    teacherApi.badgeCatalog.mockResolvedValue([{ id: 'b1', code: 'STAR', name: 'Star performer', type: 0 }])
    teacherApi.studentBadges.mockResolvedValue([{ id: 'sb1', badgeId: 'b1', studentId: 'stu-1', awardedAt: '2026-01-01T00:00:00Z' }])
    renderAt('stu-1')

    expect(await screen.findByText('Star performer')).toBeInTheDocument()
    expect(screen.queryByLabelText('Badge')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /award badge/i })).not.toBeInTheDocument()
  })

  // Deliberate scope decision (not an oversight): POST students/{id}/streak is activity-derived
  // with no reason/context and no product spec for a manual override, so no streak-update control
  // is built. This test keeps that decision visible and tested rather than a silent gap.
  it('renders no manual streak-update control', async () => {
    teacherApi.myStudents.mockResolvedValue([{ studentId: 'stu-1', fullName: 'Pupil One' }])
    teacherApi.studentProgress.mockResolvedValue({})
    teacherApi.studentPoints.mockResolvedValue({ studentId: 'stu-1', totalPoints: 0 })
    teacherApi.studentPointsLedger.mockResolvedValue([])
    teacherApi.badgeCatalog.mockResolvedValue([])
    teacherApi.studentBadges.mockResolvedValue([])
    renderAt('stu-1')

    await screen.findByText('Gamification points')
    expect(screen.queryByRole('button', { name: /streak/i })).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/streak/i)).not.toBeInTheDocument()
  })
})
