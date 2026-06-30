import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Alert } from '../../../components/ui/Alert'
import { Card, PageHeader } from '../../../components/ui/PageHeader'
import { Spinner } from '../../../components/ui/Spinner'
import { ErrorState } from '../../../components/ui/states'
import { useAuth } from '../../../features/auth/AuthContext'
import { categoryName } from '../../../features/communication/helpers'
import { notificationsApi } from '../../../features/notifications/notificationsApi'
import { STALE, queryKeys } from '../../../lib/query/keys'

function PreferencesView() {
  const { t } = useTranslation()
  const { user } = useAuth()
  const qc = useQueryClient()
  const userId = user?.id
  const [error, setError] = useState(null)

  const prefs = useQuery({
    queryKey: queryKeys.notifications.preferences(userId),
    queryFn: ({ signal }) => notificationsApi.preferences(signal),
    enabled: !!userId,
    staleTime: STALE.medium,
  })

  const update = useMutation({
    mutationFn: (body) => notificationsApi.updatePreference(body),
    onMutate: () => setError(null),
    onError: (e) => setError(e),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.notifications.preferences(userId) }),
  })

  const toggle = (p, field, value) =>
    update.mutate({ category: p.category, inAppEnabled: field === 'inApp' ? value : p.inAppEnabled, emailEnabled: field === 'email' ? value : p.emailEnabled })

  return (
    <div>
      <PageHeader title={t('notifications.preferencesTitle')} description={t('notifications.preferencesDescription')} />
      {error && <Alert variant="error" correlationId={error.correlationId}>{error.detail || error.title || t('common.error')}</Alert>}
      {prefs.isLoading ? (
        <Spinner label={t('common.loading')} />
      ) : prefs.isError ? (
        <ErrorState error={prefs.error} onRetry={() => prefs.refetch()} />
      ) : (
        <Card>
          <table className="ui-table" data-testid="preferences-table">
            <thead>
              <tr>
                <th>{t('notifications.category.heading')}</th>
                <th>{t('notifications.inApp')}</th>
                <th>{t('notifications.email')}</th>
              </tr>
            </thead>
            <tbody>
              {(prefs.data ?? []).map((p) => (
                <tr key={p.category} data-testid={`pref-${categoryName(p.category)}`}>
                  <td>
                    {t(`notifications.category.${p.categoryName}`, p.categoryName)}
                    {p.mandatory && <span className="ui-chip ui-chip--muted"> {t('notifications.mandatory')}</span>}
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      aria-label={`${p.categoryName} ${t('notifications.inApp')}`}
                      checked={!!p.inAppEnabled}
                      disabled={p.mandatory || update.isPending}
                      onChange={(e) => toggle(p, 'inApp', e.target.checked)}
                    />
                  </td>
                  <td>
                    <input
                      type="checkbox"
                      aria-label={`${p.categoryName} ${t('notifications.email')}`}
                      checked={!!p.emailEnabled}
                      disabled={update.isPending}
                      onChange={(e) => toggle(p, 'email', e.target.checked)}
                    />
                    {!p.emailConfigured && <span className="ui-muted"> {t('notifications.emailNotConfigured')}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  )
}

// --------------------------------------------------------------------------
// Messaging — conversation list
// --------------------------------------------------------------------------

export default PreferencesView
