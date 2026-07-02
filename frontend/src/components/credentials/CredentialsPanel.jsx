import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Copy, Check, Download } from 'lucide-react'
import { Modal, Button, Alert } from '../../shared/ui'
import { copyToClipboard } from '../../lib/clipboard'

function buildCredentialsText(t, { fullName, role, loginCode, temporaryPassword }) {
  return [
    `${t('credentials.fullName', 'Full name')}: ${fullName}`,
    `${t('credentials.role', 'Role')}: ${role}`,
    `${t('credentials.loginCode', 'Login ID')}: ${loginCode}`,
    `${t('credentials.password', 'Temporary password')}: ${temporaryPassword}`,
  ].join('\n')
}

function CredentialRow({ field, label, value, mono, copiedField, onCopy }) {
  const { t } = useTranslation()
  return (
    <div className="credentials-panel__row">
      <span className="credentials-panel__label">{label}</span>
      <span className={mono ? 'credentials-panel__value credentials-panel__value--mono' : 'credentials-panel__value'}>
        {value}
      </span>
      <Button
        type="button"
        variant="ghost"
        onClick={() => onCopy(field, value)}
        aria-label={t('credentials.copy', 'Copy')}
      >
        {copiedField === field ? <Check size={16} aria-hidden="true" /> : <Copy size={16} aria-hidden="true" />}
      </Button>
    </div>
  )
}

/**
 * One-time display of a generated login identifier + temporary password. Used after
 * onboarding a SchoolAdmin, creating a Student/Teacher/Parent, and resetting any of these
 * accounts' credentials — the backend never returns this payload again after this response.
 */
export function CredentialsPanel({ open, onClose, fullName, role, loginCode, temporaryPassword }) {
  const { t } = useTranslation()
  const [copiedField, setCopiedField] = useState(null)

  async function handleCopy(field, value) {
    const ok = await copyToClipboard(value)
    if (ok) {
      setCopiedField(field)
      setTimeout(() => setCopiedField((current) => (current === field ? null : current)), 2000)
    }
  }

  function handleCopyAll() {
    handleCopy('all', buildCredentialsText(t, { fullName, role, loginCode, temporaryPassword }))
  }

  function handleDownload() {
    const blob = new Blob([buildCredentialsText(t, { fullName, role, loginCode, temporaryPassword })], {
      type: 'text/plain',
    })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${loginCode || 'credentials'}.txt`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('credentials.title', 'Account credentials')}
      footer={
        <>
          <Button type="button" variant="secondary" onClick={handleDownload}>
            <Download size={16} aria-hidden="true" /> {t('credentials.download', 'Download as text')}
          </Button>
          <Button type="button" variant="primary" onClick={onClose}>
            {t('actions.close', 'Close')}
          </Button>
        </>
      }
    >
      <Alert
        variant="warning"
        title={t(
          'credentials.notice',
          'These credentials are shown only once. The account holder must change this password on first login.'
        )}
      />
      <div className="credentials-panel">
        <CredentialRow
          field="fullName"
          label={t('credentials.fullName', 'Full name')}
          value={fullName}
          copiedField={copiedField}
          onCopy={handleCopy}
        />
        <CredentialRow
          field="role"
          label={t('credentials.role', 'Role')}
          value={role}
          copiedField={copiedField}
          onCopy={handleCopy}
        />
        <CredentialRow
          field="loginCode"
          label={t('credentials.loginCode', 'Login ID')}
          value={loginCode}
          mono
          copiedField={copiedField}
          onCopy={handleCopy}
        />
        <CredentialRow
          field="password"
          label={t('credentials.password', 'Temporary password')}
          value={temporaryPassword}
          mono
          copiedField={copiedField}
          onCopy={handleCopy}
        />
      </div>
      <Button type="button" variant="secondary" onClick={handleCopyAll} className="credentials-panel__copy-all">
        {copiedField === 'all' ? t('credentials.copied', 'Copied') : t('credentials.copyAll', 'Copy all')}
      </Button>
    </Modal>
  )
}
