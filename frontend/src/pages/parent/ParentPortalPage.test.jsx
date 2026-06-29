import { describe, expect, it, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { createQueryClient } from '../../lib/query/queryClient'
import { ToastProvider } from '../../components/feedback/ToastProvider'
import ParentPortalPage from './ParentPortalPage'
import { parentApi } from '../../features/parent/parentApi'
import { ApiError } from '../../lib/api/problemDetails'

vi.mock('../../features/auth/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'parent-1', fullName: 'Parent One' } }),
}))

vi.mock('../../features/parent/parentApi', () => ({
  parentApi: {
    dashboard: vi.fn(),
    children: vi.fn(),
    child: vi.fn(),
    childProgress: vi.fn(),
    childAttendance: vi.fn(),
    childPoints: vi.fn(),
    childPointsLedger: vi.fn(),
    documentRequests: vi.fn(),
    documentRequest: vi.fn(),
    createDocumentRequest: vi.fn(),
    attachRequestDocument: vi.fn(),
    downloadRequestDocument: vi.fn(),
    conversations: vi.fn(),
    notifications: vi.fn(),
    markNotificationRead: vi.fn(),
    announcements: vi.fn(),
  },
}))

function renderParent(view, path = '/app/parent') {
  const client = createQueryClient()
  return render(
    <I18nextProvider i18n={i18n}>
      <QueryClientProvider client={client}>
        <ToastProvider>
          <MemoryRouter initialEntries={[path]}>
            <Routes>
              <Route path="/app/parent" element={<ParentPortalPage view={view} />} />
              <Route path="/app/parent/children/:childId" element={<ParentPortalPage view={view} />} />
              <Route path="/app/parent/children/:childId/attendance" element={<ParentPortalPage view={view} />} />
              <Route path="/app/parent/children/:childId/points" element={<ParentPortalPage view={view} />} />
            </Routes>
          </MemoryRouter>
        </ToastProvider>
      </QueryClientProvider>
    </I18nextProvider>,
  )
}

beforeEach(() => {
  vi.clearAllMocks()
})

describe('ParentPortalPage Phase 10 contracts', () => {
  it('renders the linked-children dashboard metric from the backend', async () => {
    parentApi.dashboard.mockResolvedValue({ parentId: 'parent-1', linkedChildrenCount: 1 })
    renderParent('dashboard')
    expect(await screen.findByText('Parent dashboard')).toBeInTheDocument()
    expect(await screen.findByText('Linked children')).toBeInTheDocument()
    await waitFor(() => expect(parentApi.dashboard).toHaveBeenCalled())
  })

  it('lists only the linked children returned by the backend', async () => {
    parentApi.children.mockResolvedValue([{ studentId: 'stu-guid-1', fullName: 'Tenant1 Student', className: 'Phase 8 Class' }])
    renderParent('children')
    expect(await screen.findByText(/Tenant1 Student/)).toBeInTheDocument()
  })

  it('shows an empty state when the parent has no linked children', async () => {
    parentApi.children.mockResolvedValue([])
    renderParent('children')
    expect(await screen.findByText('No children are linked to your account.')).toBeInTheDocument()
  })

  it('surfaces a forbidden/error state when a child read is denied', async () => {
    parentApi.child.mockRejectedValue(new ApiError({ status: 403, title: 'Forbidden', detail: 'not your child' }))
    renderParent('child', '/app/parent/children/other-child')
    expect(await screen.findByText(/try again/i)).toBeInTheDocument()
  })

  it('renders the child attendance summary from authoritative records', async () => {
    parentApi.childAttendance.mockResolvedValue({
      summary: { total: 3, present: 1, absent: 1, late: 1, excused: 0, attendancePercentage: 66.67 },
      records: [{ id: 'A1', attendanceDate: '2031-01-05', status: 'Present', source: 'Manual' }],
    })
    renderParent('child-attendance', '/app/parent/children/stu-guid-1/attendance')
    expect(await screen.findByText('Attendance summary')).toBeInTheDocument()
    expect(await screen.findByText('66.67')).toBeInTheDocument()
  })

  it('Phase 14 closure — renders a linked child\'s read-only gamification points and ledger', async () => {
    parentApi.childPoints.mockResolvedValue({ studentId: 'stu-guid-1', totalPoints: 30 })
    parentApi.childPointsLedger.mockResolvedValue([{ id: 't1', points: 10, reason: 'Office hour' }])
    renderParent('child-points', '/app/parent/children/stu-guid-1/points')
    expect(await screen.findByText('Gamification points')).toBeInTheDocument()
    expect(await screen.findByText('30')).toBeInTheDocument()
    await waitFor(() => expect(parentApi.childPoints).toHaveBeenCalledWith('stu-guid-1', expect.anything()))
    await waitFor(() => expect(parentApi.childPointsLedger).toHaveBeenCalledWith('stu-guid-1', expect.anything()))
  })

  it('submits a document request for a selected child through the backend', async () => {
    parentApi.children.mockResolvedValue([{ studentId: 'stu-guid-1', fullName: 'Tenant1 Student' }])
    parentApi.documentRequests.mockResolvedValue([])
    parentApi.createDocumentRequest.mockResolvedValue({ id: 'REQ-1', status: 'Open' })
    const user = userEvent.setup()
    renderParent('documents')
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
    renderParent('documents')

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
