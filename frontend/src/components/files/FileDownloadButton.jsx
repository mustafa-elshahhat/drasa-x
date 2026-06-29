import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '../ui/Button'
import { useToast } from '../feedback/ToastProvider'
import { userMessageFor } from '../../lib/api/problemDetails'

// =============================================================================
// Phase 16 — backend-mediated download button.
//
// The caller supplies an async `download()` that fetches the bytes through
// DerasaX-backend (e.g. filesApi.download). This component owns the loading /
// error / unauthorized UX and never exposes a raw storage URL. A signed,
// time-limited token is the only credential a download ever carries.
// =============================================================================
export function FileDownloadButton({ download, label, variant = 'secondary', disabled = false }) {
  const { t } = useTranslation()
  const toast = useToast()
  const [busy, setBusy] = useState(false)

  async function run() {
    setBusy(true)
    try {
      await download()
    } catch (err) {
      const msg = err?.status === 403 ? t('files.unauthorized')
        : err?.status === 404 ? t('files.notFound')
        : userMessageFor(err, t)
      toast.error(msg)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Button variant={variant} onClick={run} loading={busy} disabled={disabled}>
      {label || t('files.download')}
    </Button>
  )
}
