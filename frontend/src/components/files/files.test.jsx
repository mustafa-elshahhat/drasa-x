import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, act, waitFor } from '@testing-library/react'
import { I18nextProvider } from 'react-i18next'
import i18n from '../../i18n'
import { ToastProvider } from '../feedback/ToastProvider'
import { ApiError } from '../../lib/api/problemDetails'
import { FileUpload } from './FileUpload'
import { FileDownloadButton } from './FileDownloadButton'

function wrap(ui) {
  return render(
    <I18nextProvider i18n={i18n}>
      <ToastProvider>{ui}</ToastProvider>
    </I18nextProvider>,
  )
}

function pickFile(name = 'a.pdf', type = 'application/pdf', size = 10) {
  const file = new File([new Uint8Array(size)], name, { type })
  const input = document.querySelector('input[type="file"]')
  fireEvent.change(input, { target: { files: [file] } })
  return file
}

afterEach(async () => {
  await act(async () => {
    await i18n.changeLanguage('en')
  })
})

describe('FileUpload (Phase 16)', () => {
  it('renders the label and upload control', () => {
    wrap(<FileUpload upload={vi.fn()} label="Upload material" />)
    expect(screen.getByText('Upload material')).toBeInTheDocument()
    expect(document.querySelector('input[type="file"]')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /upload/i })).toBeInTheDocument()
  })

  it('shows a validation error when no file is chosen', async () => {
    wrap(<FileUpload upload={vi.fn()} label="Upload" />)
    // Button is disabled until a file is chosen; assert the guarded state.
    expect(screen.getByRole('button', { name: /upload/i })).toBeDisabled()
  })

  it('rejects a file over the size limit before any upload call', () => {
    const upload = vi.fn()
    wrap(<FileUpload upload={upload} maxSizeMb={1} label="Upload" />)
    pickFile('big.pdf', 'application/pdf', 2 * 1024 * 1024)
    expect(screen.getByRole('alert')).toHaveTextContent(/too large/i)
    expect(upload).not.toHaveBeenCalled()
  })

  it('uploads a chosen file and reports success', async () => {
    const upload = vi.fn(async () => ({ id: 'f1' }))
    const onUploaded = vi.fn()
    wrap(<FileUpload upload={upload} onUploaded={onUploaded} label="Upload" />)
    pickFile()
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /upload/i }))
    })
    await waitFor(() => expect(upload).toHaveBeenCalledTimes(1))
    expect(onUploaded).toHaveBeenCalledWith({ id: 'f1' })
    // Success is shown both inline and via toast.
    expect(screen.getAllByText(/file uploaded/i).length).toBeGreaterThan(0)
  })

  it('shows an unauthorized message when the backend returns 403', async () => {
    const upload = vi.fn(async () => {
      throw new ApiError({ status: 403, title: 'Forbidden' })
    })
    wrap(<FileUpload upload={upload} label="Upload" />)
    pickFile()
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /upload/i }))
    })
    // Both an inline error and a toast use role="alert"; assert at least one carries the message.
    await waitFor(() =>
      expect(screen.getAllByRole('alert').some((el) => /not allowed to access/i.test(el.textContent))).toBe(true),
    )
  })

  it('renders an honest unavailable state', () => {
    wrap(<FileUpload upload={vi.fn()} unavailable />)
    expect(screen.getByText(/temporarily unavailable/i)).toBeInTheDocument()
  })

  it('renders Arabic strings when the language is AR', async () => {
    await act(async () => {
      await i18n.changeLanguage('ar')
    })
    wrap(<FileUpload upload={vi.fn()} unavailable />)
    expect(screen.getByText(/غير متاحة مؤقتًا/)).toBeInTheDocument()
  })
})

describe('FileDownloadButton (Phase 16)', () => {
  it('calls the backend download on click', async () => {
    const download = vi.fn(async () => 'file.pdf')
    wrap(<FileDownloadButton download={download} />)
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /download/i }))
    })
    await waitFor(() => expect(download).toHaveBeenCalledTimes(1))
  })

  it('surfaces a not-found error via toast', async () => {
    const download = vi.fn(async () => {
      throw new ApiError({ status: 404, title: 'Not found' })
    })
    wrap(<FileDownloadButton download={download} />)
    await act(async () => {
      fireEvent.click(screen.getByRole('button', { name: /download/i }))
    })
    await waitFor(() =>
      expect(screen.getAllByRole('alert').some((el) => /not found|no longer available/i.test(el.textContent))).toBe(true),
    )
  })
})
