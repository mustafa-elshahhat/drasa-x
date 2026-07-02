import { describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import SystemHealthPage from './SystemHealthPage'
import { systemApi } from '../../../features/system/systemApi'

// Ported from the retired SystemAdminPortalPage facade ("Phase 12 contracts"):
// Phase 19 observability fields (storage/AI/background-job dependency health,
// request metrics, deployment) render on the operational status page.

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'sys-1', fullName: 'Platform Admin' }, role: 'SystemAdmin' }),
}))

vi.mock('../../../features/system/systemApi', () => ({
  systemApi: { operationalStatus: vi.fn() },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <MemoryRouter initialEntries={['/app/system/health']}>
          <SystemHealthPage />
        </MemoryRouter>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

describe('SystemHealthPage', () => {
  it('renders Phase 19 observability fields on the operational status page', async () => {
    systemApi.operationalStatus.mockResolvedValue({
      health: { api: 'up', databaseReachable: true, databaseNote: 'ok' },
      storage: { configured: true, status: 'healthy', note: 'Local storage root is accessible.' },
      aiService: { configured: false, status: 'degraded', note: 'AI service unreachable.' },
      backgroundJobs: { configured: true, status: 'healthy', note: 'disabled' },
      metrics: { totalRequests: 42, status2xx: 40, status3xx: 0, status4xx: 2, status5xx: 0, avgLatencyMs: 12.3 },
      version: '1.2.3', environment: 'Production', uptimeSeconds: 123.4,
    })
    renderPage()
    expect(await screen.findByText('File storage')).toBeInTheDocument()
    expect(screen.getByText('AI service')).toBeInTheDocument()
    expect(screen.getByText('Background jobs')).toBeInTheDocument()
    expect(screen.getByText('Request metrics')).toBeInTheDocument()
    expect(screen.getByText('Deployment')).toBeInTheDocument()
    await waitFor(() => expect(systemApi.operationalStatus).toHaveBeenCalled())
  })
})
