import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useTranslation } from 'react-i18next'
import { useAuth } from '../../features/auth/AuthContext'
import { useToast } from '../../shared/feedback/toast'
import { useDocumentTitle } from '../../app/useDocumentTitle'
import { PageHeader, Card, Button, Alert } from '../../shared/ui'
import { PasswordField } from '../../shared/form'
import { applyServerErrors } from '../../lib/forms/errors'
import { ApiError } from '../../lib/api/problemDetails'

// Change-password page — a REAL backend-connected form (Phase 7 §9). Proves the
// form foundation end-to-end against POST /api/v1/account/change-password:
// typed schema, client validation, backend validation mapping, submit state,
// duplicate-submit prevention, accessible errors, success notification.
function buildSchema(t) {
  return z
    .object({
      currentPassword: z.string().min(1),
      newPassword: z.string().min(8, t('security.tooShort', 'New password must be at least 8 characters.')),
      confirmPassword: z.string().min(1),
    })
    .refine((v) => v.newPassword === v.confirmPassword, {
      path: ['confirmPassword'],
      message: t('security.mismatch', 'The new passwords do not match.'),
    })
}

export default function SecurityPage() {
  const { changePassword } = useAuth()
  const toast = useToast()
  const { t } = useTranslation()
  const [formError, setFormError] = useState(null)
  useDocumentTitle({ titleKey: 'security.title' })

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm({ resolver: zodResolver(buildSchema(t)) })

  async function onSubmit(values) {
    setFormError(null)
    try {
      await changePassword({ currentPassword: values.currentPassword, newPassword: values.newPassword })
      toast.success(t('security.changed', 'Your password has been changed.'))
      reset()
    } catch (err) {
      const formLevel = applyServerErrors(err instanceof ApiError ? err : new ApiError({ status: 0 }), setError, {
        fieldMap: { CurrentPassword: 'currentPassword', NewPassword: 'newPassword' },
      })
      if (formLevel) setFormError({ message: formLevel, correlationId: err?.correlationId })
    }
  }

  return (
    <>
      <PageHeader title={t('security.title', 'Security')} />
      <Card title={t('security.changePassword', 'Change password')}>
        <form onSubmit={handleSubmit(onSubmit)} noValidate className="form-narrow">
          {formError && (
            <Alert variant="error" title={formError.message} correlationId={formError.correlationId} />
          )}
          <PasswordField
            label={t('security.currentPassword', 'Current password')}
            autoComplete="current-password"
            required
            error={errors.currentPassword?.message}
            {...register('currentPassword')}
          />
          <PasswordField
            label={t('security.newPassword', 'New password')}
            autoComplete="new-password"
            required
            error={errors.newPassword?.message}
            {...register('newPassword')}
          />
          <PasswordField
            label={t('security.confirmPassword', 'Confirm new password')}
            autoComplete="new-password"
            required
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />
          <Button type="submit" variant="primary" loading={isSubmitting}>
            {t('security.changePassword', 'Change password')}
          </Button>
        </form>
      </Card>
    </>
  )
}
