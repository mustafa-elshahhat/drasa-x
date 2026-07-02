import { useParams } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { DetailList } from '../../../shared/data-display'
import { Alert, Button, Card, Chip } from '../../../shared/ui'
import { ErrorState } from '../../../shared/feedback'
import { Head, Loading } from '../../../features/school/components'
import { useSchoolQuery } from '../../../features/school/helpers'
import { schoolApi } from '../../../features/school/schoolApi'
import { queryKeys } from '../../../lib/query/keys'
import { usePortalContext } from '../../../features/portal/context'

function UserDetailPage({ userId, locale }) {
  const { t } = useTranslation()
  const qc = useQueryClient()
  const { userId: targetUserId } = useParams()
  const user = useSchoolQuery(queryKeys.school.user(userId, targetUserId), (s) => schoolApi.getUser(targetUserId, s), { enabled: Boolean(targetUserId) })

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: queryKeys.school.user(userId, targetUserId) })
    qc.invalidateQueries({ queryKey: queryKeys.school.users(userId, user.data?.role) })
  }
  const setEnabled = useMutation({
    mutationFn: (enabled) => schoolApi.setUserEnabled(targetUserId, enabled),
    onSuccess: invalidate,
  })
  const resetCredential = useMutation({
    mutationFn: () => schoolApi.resetUserCredential(targetUserId),
  })

  const data = user.data
  const isDisabled = Boolean(data?.isDisabled ?? data?.IsDisabled)

  return (
    <>
      <Head view="userDetails" />
      {user.isLoading && <Loading />}
      {user.isError && <ErrorState error={user.error} onRetry={user.refetch} />}
      {data && (
        <>
          <Card title={data.fullName || targetUserId}>
            <div className="cluster mb-3">
              <Chip tone={isDisabled ? 'danger' : 'success'}>{t(isDisabled ? 'school.user.disabled' : 'school.user.enabled')}</Chip>
              {data.role && <Chip tone="muted">{t(`roles.${data.role}`, data.role)}</Chip>}
            </div>
            <DetailList item={data} locale={locale} />
          </Card>

          <Card title={t('school.user.status')}>
            {setEnabled.isError && <ErrorState error={setEnabled.error} onRetry={() => setEnabled.reset()} />}
            {setEnabled.isSuccess && (
              <Alert variant="success" title={t('school.common.saved')}>
                {t(isDisabled ? 'school.user.disabled_success' : 'school.user.enabled_success')}
              </Alert>
            )}
            <div className="cluster">
              <Button onClick={() => setEnabled.mutate(true)} loading={setEnabled.isPending} disabled={!isDisabled}>
                {t('school.user.enable')}
              </Button>
              <Button variant="secondary" onClick={() => setEnabled.mutate(false)} loading={setEnabled.isPending} disabled={isDisabled}>
                {t('school.user.disable')}
              </Button>
            </div>
          </Card>

          <Card title={t('school.user.resetCredential')}>
            <p className="ui-muted">{t('school.user.resetCredentialHint')}</p>
            {resetCredential.isError && <ErrorState error={resetCredential.error} onRetry={() => resetCredential.reset()} />}
            {resetCredential.data && (
              <Alert variant="success" title={t('school.credential.title')}>
                {t('school.credential.body')} — {t('school.common.loginCode')}: <code>{resetCredential.data.loginCode}</code> · {t('school.credential.password')}: <code>{resetCredential.data.temporaryPassword}</code>
              </Alert>
            )}
            <Button variant="secondary" onClick={() => resetCredential.mutate()} loading={resetCredential.isPending}>
              {t('school.user.resetCredential')}
            </Button>
          </Card>
        </>
      )}
    </>
  )
}

export default function SchoolUserDetailPage(props) {
  const { userId, locale } = usePortalContext()
  if (!userId) {
    return <ErrorState error={{ title: 'Missing session', detail: 'The authenticated user id is unavailable.' }} />
  }
  return <UserDetailPage userId={userId} locale={locale} {...props} />
}
