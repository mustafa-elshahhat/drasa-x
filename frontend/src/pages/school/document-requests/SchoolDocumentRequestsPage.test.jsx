import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../../i18n'
import { createQueryClient } from '../../../lib/query/queryClient'
import { ToastProvider } from '../../../components/feedback/ToastProvider'
import SchoolDocumentRequestsPage from './SchoolDocumentRequestsPage'
import { schoolApi } from '../../../features/school/schoolApi'

// Regression test: the school-admin side of a parent document request only ever
// supported a text-only reply — there was no way to attach a response document
// (the backend's /response-document multipart endpoint was unused by the UI).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'admin-1', fullName: 'School Admin' }, role: 'SchoolAdmin' }),
}))

vi.mock('../../../features/school/schoolApi', () => ({
  schoolApi: {
    documentRequests: vi.fn(),
    respondDocumentRequest: vi.fn(),
    transitionDocumentRequest: vi.fn(),
    attachResponseDocument: vi.fn(),
  },
}))

function renderPage() {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ToastProvider>
          <MemoryRouter initialEntries={['/app/school/document-requests']}>
            <SchoolDocumentRequestsPage />
          </MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
  schoolApi.documentRequests.mockResolvedValue([{ id: 'req-1', title: 'Transcript request', type: 0 }])
})

describe('SchoolDocumentRequestsPage response document upload', () => {
  it('uploads a response document with no reply text typed', async () => {
    schoolApi.attachResponseDocument.mockResolvedValue({ id: 'resp-1' })
    const user = userEvent.setup()
    renderPage()

    const fileInput = await screen.findByLabelText('Attach a response document', { exact: false })
    const file = new File(['hello'], 'response.pdf', { type: 'application/pdf' })
    await user.upload(fileInput, file)
    await user.click(screen.getByRole('button', { name: 'Upload' }))

    await waitFor(() => expect(schoolApi.attachResponseDocument).toHaveBeenCalledWith('req-1', file, undefined))
  })

  it('uploads a response document using the typed reply as the document body', async () => {
    schoolApi.attachResponseDocument.mockResolvedValue({ id: 'resp-2' })
    const user = userEvent.setup()
    renderPage()

    await user.type(await screen.findByLabelText('Reply'), 'Please find the transcript attached.')
    const fileInput = screen.getByLabelText('Attach a response document', { exact: false })
    const file = new File(['hello'], 'response.pdf', { type: 'application/pdf' })
    await user.upload(fileInput, file)
    await user.click(screen.getByRole('button', { name: 'Upload' }))

    await waitFor(() =>
      expect(schoolApi.attachResponseDocument).toHaveBeenCalledWith('req-1', file, 'Please find the transcript attached.'),
    )
  })
})
