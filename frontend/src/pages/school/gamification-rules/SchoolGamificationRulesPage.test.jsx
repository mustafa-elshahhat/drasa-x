import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolGamificationRulesPage from './SchoolGamificationRulesPage'
import { schoolApi } from '../../../features/school/schoolApi'

// New page (contract-gap appendix "Badges/Gamification"): GamificationController.RulesAsync is
// readable by Teacher or SchoolAdmin, but UpsertRuleAsync (PUT gamification/rules) is
// SchoolAdmin-only server-side, so the editor lives on schoolApi (not the pre-existing dead
// teacherApi.gamificationRules/upsertGamificationRule pair).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    gamificationRules: vi.fn(),
    upsertGamificationRule: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/gamification-rules']}>
          <SchoolGamificationRulesPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.gamificationRules.mockResolvedValue([
    { id: 'rule-1', code: 'OH_ATTEND', name: 'Attend office hours', description: null, trigger: 0, points: 10, badgeId: null, enabled: true },
  ])
})

describe('SchoolGamificationRulesPage', () => {
  it('renders existing rules with a translated trigger chip', async () => {
    renderPage()

    expect(await screen.findByText('OH_ATTEND')).toBeInTheDocument()
    expect(screen.getByText('Attend office hours')).toBeInTheDocument()
    expect(screen.getByText('Office-hour attended')).toBeInTheDocument()
    expect(screen.getByText('10')).toBeInTheDocument()
  })

  it('the create form pre-selects a trigger with no existing rule and auto-fills Code from it', async () => {
    renderPage()
    await screen.findByText('OH_ATTEND')

    // Trigger 0 (OfficeHourAttended) already has a rule; the next free trigger is 1 (CompetitionTopRank).
    const triggerSelect = screen.getByLabelText('Trigger')
    expect(triggerSelect.value).toBe('1')
    expect(screen.getByLabelText('Code', { exact: false }).value).toBe('CompetitionTopRank')

    const user = userEvent.setup()
    await user.selectOptions(triggerSelect, '3')
    expect(screen.getByLabelText('Code', { exact: false }).value).toBe('CommunityPost')
  })

  it('creates a new rule via the create form', async () => {
    schoolApi.upsertGamificationRule.mockResolvedValue({ id: 'rule-2' })
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('OH_ATTEND')

    await user.type(screen.getByLabelText('Name', { exact: false }), 'Top rank bonus')
    await user.type(screen.getByLabelText('Points', { exact: false }), '50')
    await user.click(screen.getByRole('button', { name: 'Create rule' }))

    await waitFor(() => expect(schoolApi.upsertGamificationRule).toHaveBeenCalledWith(expect.objectContaining({
      code: 'CompetitionTopRank', trigger: 1, name: 'Top rank bonus', points: 50, enabled: true,
    })))
  })

  it('edits an existing rule without exposing Code/Trigger as editable (locked after creation)', async () => {
    schoolApi.upsertGamificationRule.mockResolvedValue({ id: 'rule-1' })
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Edit' }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).queryByLabelText('Code', { exact: false })).not.toBeInTheDocument()
    expect(within(dialog).queryByLabelText('Trigger')).not.toBeInTheDocument()

    const pointsInput = within(dialog).getByLabelText('Points', { exact: false })
    await user.clear(pointsInput)
    await user.type(pointsInput, '25')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() => expect(schoolApi.upsertGamificationRule).toHaveBeenCalledWith(expect.objectContaining({
      code: 'OH_ATTEND', trigger: 0, points: 25, enabled: true,
    })))
  })
})
