import { useTranslation } from 'react-i18next'
import { Modal } from './Modal'
import { Button } from './Button'

// Confirmation dialog (Phase 7 §10). Built on the accessible Modal.
export function ConfirmDialog({
  open,
  onConfirm,
  onCancel,
  title,
  message,
  confirmLabel,
  cancelLabel,
  destructive = false,
  loading = false,
}) {
  const { t } = useTranslation()
  return (
    <Modal
      open={open}
      onClose={onCancel}
      title={title || t('confirm.title', 'Are you sure?')}
      footer={
        <>
          <Button variant="secondary" onClick={onCancel} disabled={loading}>
            {cancelLabel || t('actions.cancel', 'Cancel')}
          </Button>
          <Button
            variant={destructive ? 'danger' : 'primary'}
            onClick={onConfirm}
            loading={loading}
          >
            {confirmLabel || t('actions.confirm', 'Confirm')}
          </Button>
        </>
      }
    >
      <p>{message}</p>
    </Modal>
  )
}
