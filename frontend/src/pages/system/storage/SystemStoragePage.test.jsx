import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemStoragePage from './SystemStoragePage'
import { systemApi } from '../../../features/system/systemApi'

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"): the
// storage page reports an honest posture — byte accounting is not implemented yet.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { storage: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/storage']}>
          <SystemStoragePage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SystemStoragePage', () => {
  it('reports honest storage posture (byte accounting not implemented)', async () => {
    systemApi.storage.mockResolvedValue({ byteAccountingImplemented: false, note: 'Byte usage is not measured yet.', tenants: [] })
    renderPage()
    expect(await screen.findByText(/Byte usage is not measured yet\./)).toBeInTheDocument()
  })
})
