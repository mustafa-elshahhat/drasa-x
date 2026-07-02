import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import ParentConversationsPage from './ParentConversationsPage'
import { parentApi } from '../../../features/parent/parentApi'

// Regression test for F-06 / P2-1: conversation rows were display-only with no
// way to open the thread at /app/messages/:conversationId.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' }, role: 'Parent' }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: { conversations: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/parent/conversations']}>
          <ParentConversationsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => vi.clearAllMocks())

describe('ParentConversationsPage row links', () => {
  it('links each conversation row to its message thread', async () => {
    parentApi.conversations.mockResolvedValue([{ id: 'conv-1', subject: 'About homework' }])
    renderPage()

    const link = await screen.findByRole('link', { name: /About homework/ })
    expect(link).toHaveAttribute('href', '/app/messages/conv-1')
  })
})
