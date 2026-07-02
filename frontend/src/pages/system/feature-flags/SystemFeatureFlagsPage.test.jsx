import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemFeatureFlagsPage from './SystemFeatureFlagsPage'
import { systemApi } from '../../../features/system/systemApi'

// Regression test for F-01 / P0-1b: the per-row Toggle was silently dropped by
// ResourceTable and never rendered, so a flag could never be flipped from the list.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { featureFlags: vi.fn(), upsertFeatureFlag: vi.fn() },
}))

function renderFlags() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/feature-flags']}>
          <SystemFeatureFlagsPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('SystemFeatureFlagsPage row actions', () => {
  it('renders a per-row Toggle bound to the flag key and flips it on click', async () => {
    systemApi.featureFlags.mockResolvedValue([
      { key: 'ai-tutor-beta', isEnabled: false, targetTenantId: null },
    ])
    systemApi.upsertFeatureFlag.mockResolvedValue({})
    const user = userEvent.setup()
    renderFlags()

    const toggle = await screen.findByRole('switch', { name: 'ai-tutor-beta' })
    expect(toggle).toHaveAttribute('aria-checked', 'false')

    await user.click(toggle)

    await waitFor(() => expect(systemApi.upsertFeatureFlag).toHaveBeenCalledWith(
      expect.objectContaining({ key: 'ai-tutor-beta', isEnabled: true }),
    ))
  })
})
