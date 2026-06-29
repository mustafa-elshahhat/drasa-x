import { useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '../ui/Button'
import { useToast } from '../feedback/ToastProvider'
import { userMessageFor } from '../../lib/api/problemDetails'

// =============================================================================
// Phase 16 — reusable, backend-only file upload control.
//
// The caller supplies an async `upload(file)` that talks to DerasaX-backend
// (e.g. filesApi.upload or a purpose-specific endpoint). This component owns the
// UX: file pick + client-side validation + loading / success / validation-error /
// unauthorized / unavailable states, with EN/AR i18n and toast feedback. It never
// constructs a storage URL itself.
// =============================================================================
export function FileUpload({
  upload,
  onUploaded,
  accept,
  maxSizeMb,
  label,
  hint,
  buttonLabel,
  disabled = false,
  unavailable = false,
}) {
  const { t } = useTranslation()
  const toast = useToast()
  const inputId = useId()
  const inputRef = useRef(null)
  const [file, setFile] = useState(null)
  const [status, setStatus] = useState('idle') // idle | uploading | success | error
  const [error, setError] = useState('')

  function pick(e) {
    setError('')
    setStatus('idle')
    const f = e.target.files?.[0] || null
    if (f && maxSizeMb && f.size > maxSizeMb * 1024 * 1024) {
      setFile(null)
      setError(t('files.tooLarge', { size: maxSizeMb }))
      return
    }
    setFile(f)
  }

  async function submit() {
    if (!file) {
      setError(t('files.required'))
      return
    }
    setStatus('uploading')
    setError('')
    try {
      const result = await upload(file)
      setStatus('success')
      setFile(null)
      if (inputRef.current) inputRef.current.value = ''
      toast.success(t('files.uploadSuccess'))
      onUploaded?.(result)
    } catch (err) {
      setStatus('error')
      // 403 → unauthorized message; everything else → normalized message.
      const msg = err?.status === 403 ? t('files.unauthorized') : userMessageFor(err, t)
      setError(msg)
      toast.error(msg)
    }
  }

  if (unavailable) {
    return (
      <div className="file-upload file-upload--unavailable" role="note">
        <p>{t('files.unavailable')}</p>
      </div>
    )
  }

  return (
    <div className="file-upload">
      {label && (
        <label htmlFor={inputId} className="file-upload__label">
          {label}
        </label>
      )}
      {hint && <p className="file-upload__hint">{hint}</p>}
      <input
        id={inputId}
        ref={inputRef}
        type="file"
        accept={accept}
        onChange={pick}
        disabled={disabled || status === 'uploading'}
        aria-describedby={error ? `${inputId}-err` : undefined}
        aria-invalid={error ? true : undefined}
      />
      <Button onClick={submit} loading={status === 'uploading'} disabled={disabled || !file}>
        {buttonLabel || t('files.upload')}
      </Button>
      {status === 'uploading' && <p className="file-upload__status" role="status">{t('files.uploading')}</p>}
      {status === 'success' && <p className="file-upload__status file-upload__status--ok" role="status">{t('files.uploadSuccess')}</p>}
      {error && (
        <p id={`${inputId}-err`} className="file-upload__error" role="alert">
          {error}
        </p>
      )}
    </div>
  )
}
