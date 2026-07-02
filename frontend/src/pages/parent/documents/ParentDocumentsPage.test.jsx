import { describe, expect, it, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { renderWithProviders } from '../../../test/utils'
import ParentDocumentsPage from './ParentDocumentsPage'
import { parentApi } from '../../../features/parent/parentApi'

// Regression test for Task 2 (audit-driven fix pass): a parent's document-request
// staff replies (ParentRequestDto.Responses) were never rendered at all, so parents
// could not read a staff reply's text, let alone download an attached response
// document. ParentRequestResponseDto {Id, ResponderId, Body, RespondedAt} carries no
// "has a document" flag, so the download is attempted unconditionally per response
// (the backend honestly 404s when a given response has no attachment).

vi.mock('../../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' }, role: 'Parent' }),
}))

vi.mock('../../../features/parent/parentApi', () => ({
  parentApi: {
    children: vi.fn(),
    documentRequests: vi.fn(),
    createDocumentRequest: vi.fn(),
    attachRequestDocument: vi.fn(),
    downloadRequestDocument: vi.fn(),
    downloadResponseDocument: vi.fn(),
  },
}))

function renderPage() {
  return renderWithProviders(<ParentDocumentsPage />, { route: '/app/parent/documents' })
}

beforeEach(() => {
  vi.clearAllMocks()
  parentApi.children.mockResolvedValue([])
})

describe('ParentDocumentsPage staff replies', () => {
  it('renders a staff reply body and downloads its document with the request + response ids', async () => {
    parentApi.documentRequests.mockResolvedValue([
      {
        id: 'req-1',
        title: 'Transcript request',
        responses: [
          { id: 'resp-1', responderId: 'staff-1', body: 'Here is your transcript.', respondedAt: '2026-01-05T10:00:00Z' },
        ],
      },
    ])
    parentApi.downloadResponseDocument.mockResolvedValue('response-document')
    const user = userEvent.setup()
    renderPage()

    expect(await screen.findByText('Here is your transcript.')).toBeInTheDocument()

    const downloadBtn = screen.getByRole('button', { name: 'Download response document' })
    await user.click(downloadBtn)

    await waitFor(() => expect(parentApi.downloadResponseDocument).toHaveBeenCalledWith('req-1', 'resp-1'))
  })

  it('renders no staff-replies section for a request with no responses', async () => {
    parentApi.documentRequests.mockResolvedValue([{ id: 'req-2', title: 'Meeting request', responses: [] }])
    renderPage()

    await screen.findByText('Meeting request')
    expect(screen.queryByText('Staff replies')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Download response document' })).not.toBeInTheDocument()
  })
})

// Ported from the retired ParentPortalPage facade ("Phase 10 contracts"): new
// document requests and their attachments both go through the real backend.
describe('ParentDocumentsPage — create + attach', () => {
  it('submits a document request for a selected child through the backend', async () => {
    parentApi.children.mockResolvedValue([{ studentId: 'stu-guid-1', fullName: 'Tenant1 Student' }])
    parentApi.documentRequests.mockResolvedValue([])
    parentApi.createDocumentRequest.mockResolvedValue({ id: 'REQ-1', status: 'Open' })
    const user = userEvent.setup()
    renderPage()
    await screen.findByText('Tenant1 Student')
    await user.selectOptions(screen.getByLabelText('Child'), 'stu-guid-1')
    await user.type(screen.getByLabelText('Subject'), 'Transcript request')
    await user.type(screen.getByLabelText('Details'), 'Please issue an official transcript.')
    await user.click(screen.getByRole('button', { name: /submit request/i }))
    await waitFor(() => expect(parentApi.createDocumentRequest).toHaveBeenCalled())
    const arg = parentApi.createDocumentRequest.mock.calls[0][0]
    expect(arg.studentId).toBe('stu-guid-1')
    expect(arg.type).toBe(0) // ParentRequestType.Document (backend binds the enum numerically)
    expect(arg.title).toBe('Transcript request')
  })

  it('attaches a sensitive document to an existing request through the durable backend endpoint', async () => {
    parentApi.children.mockResolvedValue([])
    parentApi.documentRequests.mockResolvedValue([{ id: 'REQ-1', title: 'Transcript', status: 'Open' }])
    parentApi.attachRequestDocument.mockResolvedValue({ id: 'FILE-1' })
    const user = userEvent.setup()
    renderPage()

    // The request row exposes the embedded FileUpload (label = files.parentDocTitle).
    const input = await screen.findByLabelText('Attach a document')
    const file = new File([new Uint8Array([1, 2, 3])], 'transcript.pdf', { type: 'application/pdf' })
    await user.upload(input, file)
    await user.click(screen.getByRole('button', { name: /^upload$/i }))

    await waitFor(() => expect(parentApi.attachRequestDocument).toHaveBeenCalled())
    const [reqId, sent] = parentApi.attachRequestDocument.mock.calls[0]
    expect(reqId).toBe('REQ-1')
    expect(sent).toBeInstanceOf(File)
  })
})
