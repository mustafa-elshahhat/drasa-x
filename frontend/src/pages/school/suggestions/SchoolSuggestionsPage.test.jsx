import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, within, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SchoolSuggestionsPage from './SchoolSuggestionsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// New page: SchoolAdmin lists + moderates anonymous suggestions. The DTO never
// carries author identity, so the list must never show/fabricate one.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: { suggestions: vi.fn(), moderateSuggestion: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/school/suggestions']}>
          <SchoolSuggestionsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.suggestions.mockResolvedValue([
    { id: 'sug-1', title: 'More sports equipment', body: 'We need more footballs', status: 0, submittedAt: '2026-01-05T10:00:00Z' },
  ])
})

describe('SchoolSuggestionsPage', () => {
  it('renders the suggestion list with a status chip and never an author field', async () => {
    renderPage()

    expect(await screen.findByText('More sports equipment')).toBeInTheDocument()
    expect(screen.getByText('We need more footballs')).toBeInTheDocument()
    expect(screen.getByText('Submitted')).toBeInTheDocument()
    // No author identity is ever rendered — the DTO intentionally omits it (anonymity).
    expect(screen.queryByText(/^author$/i)).not.toBeInTheDocument()
  })

  it('moderates a suggestion via the real moderate endpoint', async () => {
    schoolApi.moderateSuggestion.mockResolvedValue({})
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Review' }))
    const dialog = await screen.findByRole('dialog')
    await user.selectOptions(within(dialog).getByLabelText('Status'), '2')
    await user.type(within(dialog).getByLabelText('Review notes'), 'Approved for next term budget')
    await user.click(within(dialog).getByRole('button', { name: 'Save' }))

    await waitFor(() =>
      expect(schoolApi.moderateSuggestion).toHaveBeenCalledWith('sug-1', {
        status: 2,
        reviewNotes: 'Approved for next term budget',
      }),
    )
  })
})
